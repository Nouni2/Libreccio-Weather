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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Sources.MeteoFrance;

public sealed partial class MfService
{
    private static IReadOnlyList<Alert> GetWarningsList(MfWarningsResult today, MfWarningsResult tomorrow)
    {
        List<Alert> result = [];
        Alert? first = GetBulletinWarning(today);
        Alert? second = GetBulletinWarning(tomorrow);
        if (first is not null && second is not null)
        {
            if (first.Headline == second.Headline && first.StartDate == second.StartDate
                && first.Color == second.Color && first.Description == second.Description)
            {
                result.Add(second);
            }
            else
            {
                result.Add(first);
                result.Add(second);
            }
        }
        else if (first is not null)
        {
            result.Add(first);
        }
        else if (second is not null)
        {
            result.Add(second);
        }

        result.AddRange(GetWarningsList(today));
        result.AddRange(GetWarningsList(tomorrow));
        return result;
    }

    private static Alert? GetBulletinWarning(MfWarningsResult result)
    {
        MfWarningTextBlocItem[] blocks = result.Text?.TextBlocItems?
            .Where(block => block.TextItems?.Any(item => item.HazardCode is null) == true).ToArray() ?? [];
        if (result.UpdateTime is not { } updated || blocks.Length == 0)
        {
            return null;
        }

        string[] colors = blocks.SelectMany(block => block.TextItems ?? [])
            .Where(item => item.HazardCode is null).SelectMany(item => item.TermItems ?? [])
            .Select(item => item.RiskName).OfType<string>().Where(name => name.Length > 0).ToArray();
        int color = colors.Contains("Rouge", StringComparer.Ordinal) ? GetWarningColor(4)
            : colors.Contains("Orange", StringComparer.Ordinal) ? GetWarningColor(3)
            : colors.Contains("Jaune", StringComparer.Ordinal) ? GetWarningColor(2)
            : colors.Contains("Vert", StringComparer.Ordinal) ? GetWarningColor(1)
            : Alert.ColorFromSeverity(AlertSeverity.Unknown);
        string title = result.Text!.BlocTitle ?? Strings.Get("mf_warning_bulletin");
        return new Alert
        {
            AlertId = ObjectsHash(title, color, updated).ToString(CultureInfo.InvariantCulture),
            StartDate = updated,
            EndDate = result.EndValidityTime,
            Headline = title,
            Description = GetWarningContent(null, result),
            Source = "Météo-France",
            Severity = AlertSeverity.Extreme,
            Color = color,
        };
    }

    private static IEnumerable<Alert> GetWarningsList(MfWarningsResult result)
    {
        foreach (MfWarningTimelaps timelaps in result.Timelaps ?? [])
        {
            foreach (MfWarningTimelapsItem item in timelaps.TimelapsItems ?? [])
            {
                if (item.ColorId <= 1)
                {
                    continue;
                }

                yield return new Alert
                {
                    AlertId = ObjectsHash(timelaps.PhenomenonId, item.ColorId, item.BeginTime.ToUnixTimeMilliseconds()).ToString(CultureInfo.InvariantCulture),
                    StartDate = item.BeginTime,
                    EndDate = item.EndTime,
                    Headline = GetWarningType(timelaps.PhenomenonId) + " — " + GetWarningText(item.ColorId),
                    Description = item.ColorId >= 3 ? GetWarningContent(timelaps.PhenomenonId, result) : null,
                    Source = "Météo-France",
                    Severity = AlertSeverity.GetInstance(item.ColorId),
                    Color = GetWarningColor(item.ColorId),
                };
            }
        }
    }

    private static IReadOnlyList<Alert> GetOverseasWarningsList(MfWarningDictionaryResult dictionary, MfWarningsOverseasResult result)
    {
        List<Alert> alerts = [];
        if (result.Text?.TextBlocItems is { Length: > 0 } blocks)
        {
            StringBuilder content = new();
            foreach (MfWarningOverseasTextBlocItem block in blocks)
            {
                if (content.Length > 0)
                {
                    content.Append("\n\n");
                }

                foreach (string titlePart in block.Title ?? [])
                {
                    content.Append("<h2>").Append(titlePart).Append("</h2>\n");
                }

                foreach (string text in block.Text ?? [])
                {
                    content.Append(text).Append('\n');
                }
            }

            string title = Strings.Get("mf_warning_bulletin");
            alerts.Add(new Alert
            {
                AlertId = ObjectsHash(title, result.ColorMax, result.Text.BeginTime).ToString(CultureInfo.InvariantCulture),
                StartDate = result.Text.BeginTime,
                EndDate = result.Text.EndTime,
                Headline = title,
                Description = content.ToString(),
                Source = "Météo-France",
                Severity = AlertSeverity.Extreme,
                Color = ParseWarningColor(dictionary.Colors?.FirstOrDefault(color => color.Id == result.ColorMax)?.HexaCode),
            });
        }

        foreach (MfWarningOverseasTimelaps timelaps in result.Timelaps ?? [])
        {
            foreach (MfWarningTimelapsItem item in timelaps.TimelapsItems ?? [])
            {
                if (item.ColorId <= 1)
                {
                    continue;
                }

                string? consequences = result.Consequences?.FirstOrDefault(value => value.PhenomenonId == timelaps.PhenomenonId)?.TextConsequence;
                string? advice = result.Advices?.FirstOrDefault(value => value.PhenomenonId == timelaps.PhenomenonId)?.TextAdvice;
                StringBuilder content = new();
                AppendAdvice(content, consequences, advice);
                MfWarningDictionaryColor? color = dictionary.Colors?.FirstOrDefault(value => value.Id == item.ColorId);
                string? colorName = color?.Name;
                AlertSeverity severity = colorName is null ? AlertSeverity.Unknown
                    : colorName.Contains("rouge", StringComparison.Ordinal) || colorName.Contains("violet", StringComparison.Ordinal) ? AlertSeverity.Extreme
                    : colorName.Contains("orange", StringComparison.Ordinal) ? AlertSeverity.Severe
                    : colorName.Contains("jaune", StringComparison.Ordinal) || colorName.Contains("blanc", StringComparison.Ordinal) ? AlertSeverity.Moderate
                    : colorName.Contains("vert", StringComparison.Ordinal) || colorName.Contains("bleu", StringComparison.Ordinal) ? AlertSeverity.Minor
                    : AlertSeverity.Unknown;
                alerts.Add(new Alert
                {
                    AlertId = ObjectsHash(timelaps.PhenomenonId, item.ColorId, item.BeginTime.ToUnixTimeMilliseconds()).ToString(CultureInfo.InvariantCulture),
                    StartDate = item.BeginTime,
                    EndDate = item.EndTime,
                    Headline = dictionary.Phenomenons?.FirstOrDefault(value => value.Id == timelaps.PhenomenonId)?.Name
                        ?? GetWarningType(timelaps.PhenomenonId.ToString(CultureInfo.InvariantCulture)),
                    Description = content.ToString(),
                    Source = "Météo-France",
                    Severity = severity,
                    Color = ParseWarningColor(color?.HexaCode),
                });
            }
        }

        return alerts;
    }

    private static string? GetWarningContent(string? phenomenonId, MfWarningsResult result)
    {
        StringBuilder content = new();
        foreach (MfWarningTextBlocItem block in result.Text?.TextBlocItems ?? [])
        {
            if (block.TextItems?.Any(item => item.HazardCode == phenomenonId) != true)
            {
                continue;
            }

            if (content.Length > 0)
            {
                content.Append("\n\n");
            }

            if (!string.IsNullOrEmpty(block.TypeName))
            {
                content.Append("<h2>").Append(block.TypeName).Append("</h2>\n");
            }

            foreach (MfWarningTextItem textItem in block.TextItems.Where(item => item.HazardCode == phenomenonId))
            {
                MfWarningTermItem[] terms = textItem.TermItems ?? [];
                for (int termIndex = 0; termIndex < terms.Length; termIndex++)
                {
                    MfWarningSubdivisionText[] subdivisions = terms[termIndex].SubdivisionTexts ?? [];
                    for (int index = 0; index < subdivisions.Length; index++)
                    {
                        MfWarningSubdivisionText subdivision = subdivisions[index];
                        if (!string.IsNullOrEmpty(subdivision.UnderlineText))
                        {
                            content.Append("<u>").Append(subdivision.UnderlineText).Append("</u> ");
                        }

                        if (!string.IsNullOrEmpty(subdivision.BoldText))
                        {
                            content.Append("<b>").Append(subdivision.BoldText).Append("</b> ");
                        }

                        if (subdivision.Text is not null)
                        {
                            content.AppendJoin('\n', subdivision.Text);
                        }

                        if (index != subdivisions.Length - 1)
                        {
                            content.Append("\n\n");
                        }
                    }

                    if (termIndex != terms.Length - 1)
                    {
                        content.Append("\n\n");
                    }
                }
            }
        }

        AppendAdvice(content, result.Consequences?.FirstOrDefault(item => item.PhenomenonId == phenomenonId)?.TextConsequence,
            result.Advices?.FirstOrDefault(item => item.PhenomenonId == phenomenonId)?.TextAdvice);
        return content.Length == 0 ? null : content.ToString();
    }

    private static void AppendAdvice(StringBuilder content, string? consequences, string? advice)
    {
        if (!string.IsNullOrEmpty(consequences))
        {
            if (content.Length > 0)
            {
                content.Append("\n\n");
            }

            content.Append("<h2>").Append(Strings.Get("mf_warning_consequences")).Append("</h2>\n").Append(consequences);
        }

        if (!string.IsNullOrEmpty(advice))
        {
            if (content.Length > 0)
            {
                content.Append("\n\n");
            }

            content.Append("<h2>").Append(Strings.Get("mf_warning_advice")).Append("</h2>\n").Append(advice);
        }
    }

    private static string GetWarningType(string phenomenonId)
    {
        string suffix = phenomenonId is "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8" or "9" ? phenomenonId : "other";
        return Strings.Get("mf_warning_type_" + suffix);
    }

    private static string GetWarningText(int colorId) => Strings.Get("mf_warning_level_"
        + (colorId is >= 2 and <= 4 ? colorId : 0).ToString(CultureInfo.InvariantCulture));

    private static int GetWarningColor(int colorId)
    {
        return colorId switch
        {
            4 => unchecked((int)0xFFCC0000),
            3 => unchecked((int)0xFFFFB82B),
            2 => unchecked((int)0xFFFFF600),
            1 => unchecked((int)0xFF31AA35),
            _ => Alert.ColorFromSeverity(AlertSeverity.Unknown),
        };
    }

    private static int ParseWarningColor(string? value)
    {
        if (value is null)
        {
            return Alert.ColorFromSeverity(AlertSeverity.Unknown);
        }

        if (value.StartsWith('#') && value.Length is 7 or 9)
        {
            uint color = uint.Parse(value.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return unchecked((int)(value.Length == 7 ? color | 0xFF000000 : color));
        }

        return value.ToLowerInvariant() switch
        {
            "black" => unchecked((int)0xFF000000),
            "darkgray" or "darkgrey" => unchecked((int)0xFF444444),
            "gray" or "grey" => unchecked((int)0xFF888888),
            "lightgray" or "lightgrey" => unchecked((int)0xFFCCCCCC),
            "white" => unchecked((int)0xFFFFFFFF),
            "red" => unchecked((int)0xFFFF0000),
            "green" or "lime" => unchecked((int)0xFF00FF00),
            "blue" => unchecked((int)0xFF0000FF),
            "yellow" => unchecked((int)0xFFFFFF00),
            "cyan" or "aqua" => unchecked((int)0xFF00FFFF),
            "magenta" or "fuchsia" => unchecked((int)0xFFFF00FF),
            "maroon" => unchecked((int)0xFF800000),
            "navy" => unchecked((int)0xFF000080),
            "olive" => unchecked((int)0xFF808000),
            "purple" => unchecked((int)0xFF800080),
            "silver" => unchecked((int)0xFFC0C0C0),
            "teal" => unchecked((int)0xFF008080),
            _ => throw new FormatException("Invalid warning color."),
        };
    }

    private static int ObjectsHash(params object?[] values)
    {
        int result = 1;
        foreach (object? value in values)
        {
            int hash = value switch
            {
                null => 0,
                string text => StringHash(text),
                int number => number,
                long number => unchecked((int)(number ^ (long)((ulong)number >> 32))),
                DateTimeOffset date => unchecked((int)(date.ToUnixTimeMilliseconds() ^ (long)((ulong)date.ToUnixTimeMilliseconds() >> 32))),
                _ => throw new ArgumentException("Unsupported alert identity component.", nameof(values)),
            };
            result = unchecked(31 * result + hash);
        }

        return result;
    }

    private static int StringHash(string value)
    {
        int hash = 0;
        foreach (char character in value)
        {
            hash = unchecked(31 * hash + character);
        }

        return hash;
    }
}
