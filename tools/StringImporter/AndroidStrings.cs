/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LibreccioWeather.StringImporter;

/// <summary>
/// Reads Android string resources and converts their text to .NET composite format strings.
/// </summary>
internal static partial class AndroidStrings
{
    private const string ReferencePrefix = "@string/";

    /// <summary>
    /// Strings and plural items of the given files, keyed by name. Plural items get the quantity as a suffix, such as "_one".
    /// Files that do not exist are skipped.
    /// </summary>
    public static Dictionary<string, string> Read(IEnumerable<string> files)
    {
        Dictionary<string, string> strings = [];
        foreach (string file in files.Where(File.Exists))
        {
            XElement root = XDocument.Load(file).Root ?? throw new InvalidDataException($"Empty resource file: {file}");
            foreach (XElement element in root.Elements())
            {
                string? name = element.Attribute("name")?.Value;
                if (name is null)
                {
                    continue;
                }

                if (element.Name.LocalName == "string")
                {
                    strings[name] = Convert(element);
                }
                else if (element.Name.LocalName == "plurals")
                {
                    foreach (XElement item in element.Elements("item"))
                    {
                        strings[name + "_" + item.Attribute("quantity")?.Value] = Convert(item);
                    }
                }
            }
        }

        return strings;
    }

    /// <summary>
    /// Replaces "@string/name" values with the text they point to, looked up first in the same language,
    /// then in the default language.
    /// </summary>
    public static void ResolveReferences(Dictionary<string, string> strings, IReadOnlyDictionary<string, string> defaults)
    {
        foreach (string key in strings.Keys.ToList())
        {
            string value = strings[key];
            for (int depth = 0; value.StartsWith(ReferencePrefix, StringComparison.Ordinal) && depth < 10; depth++)
            {
                string target = value[ReferencePrefix.Length..];
                value = strings.TryGetValue(target, out string? local) ? local
                    : defaults.TryGetValue(target, out string? fallback) ? fallback
                    : throw new InvalidDataException($"'{key}' points to the missing string '{target}'.");
            }

            strings[key] = value;
        }
    }

    // Markup such as <b> or <xliff:g> is dropped and only its text is kept.
    private static string Convert(XElement element)
    {
        string raw = string.Concat(element.DescendantNodes().OfType<XText>().Select(text => text.Value));
        return ToCompositeFormat(Unescape(ApplyQuotingRules(raw)));
    }

    // Android collapses runs of whitespace into one space and trims the ends, unless the whole text is in double quotes.
    // Unescaped double quotes are not part of the text.
    private static string ApplyQuotingRules(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"' && trimmed[^2] != '\\')
        {
            return trimmed[1..^1];
        }

        string collapsed = WhitespaceRun().Replace(trimmed, " ");
        return UnescapedQuote().Replace(collapsed, "");
    }

    private static string Unescape(string text)
    {
        StringBuilder builder = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];
            if (current != '\\' || i == text.Length - 1)
            {
                builder.Append(current);
                continue;
            }

            char next = text[++i];
            switch (next)
            {
                case 'n':
                    builder.Append('\n');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'u' when i + 4 < text.Length
                    && int.TryParse(text.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code):
                    builder.Append((char)code);
                    i += 4;
                    break;
                default:
                    builder.Append(next);
                    break;
            }
        }

        return builder.ToString();
    }

    // Placeholders such as %1$s or %d become {0}, braces are doubled so they stay literal, and %% becomes %.
    // A placeholder without a position takes the next free index, as in Java.
    // Text without placeholders is displayed as is, like an Android string read without arguments,
    // so a lone % such as in "50 %" stays untouched.
    private static string ToCompositeFormat(string text)
    {
        if (text.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            || !Placeholder().Matches(text).Any(match => match.Groups["type"].Value != "%"))
        {
            return text;
        }

        string escaped = text.Replace("{", "{{").Replace("}", "}}");
        int nextIndex = 0;
        return Placeholder().Replace(escaped, match =>
        {
            if (match.Groups["type"].Value == "%")
            {
                return "%";
            }

            int index = match.Groups["position"].Success
                ? int.Parse(match.Groups["position"].Value, CultureInfo.InvariantCulture) - 1
                : nextIndex++;
            return "{" + index.ToString(CultureInfo.InvariantCulture) + "}";
        });
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex(@"(?<!\\)""")]
    private static partial Regex UnescapedQuote();

    // Flags are not recognised: French writes a space before %, and "% d" must not read as a placeholder.
    [GeneratedRegex(@"%(?:(?<position>\d+)\$)?(?:\.\d+)?(?<type>[sdf%])")]
    private static partial Regex Placeholder();
}
