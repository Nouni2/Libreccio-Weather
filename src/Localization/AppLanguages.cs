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

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// The languages the app ships in, and the choice of which one to use.
/// </summary>
public static class AppLanguages
{
    public const string Default = "en";

    /// <summary>
    /// Language codes of the shipped languages: every language with translated app strings. Adding a language here
    /// makes the string importer generate its files.
    /// </summary>
    public static IReadOnlyList<string> Codes { get; } =
    [
        Default, "ar", "az", "be", "bg", "bn", "bs", "ca", "ckb", "cpe", "cs", "da", "de", "el", "en-AU", "en-CA", "en-GB",
        "en-US", "eo", "es", "et", "eu", "fa", "fi", "fr", "ga", "gl", "haw", "he", "hi", "hr", "hu", "ia", "id", "is", "it",
        "ja", "kab", "kn", "ko", "lt", "lv", "mk", "mr", "nb-NO", "nl", "oc", "pl", "pt", "pt-BR", "ro", "ru", "sk", "sl-SI",
        "sr", "sv", "ta", "th", "tr", "uk", "ur", "vi", "zh-CN", "zh-HK", "zh-TW",
    ];

    /// <summary>
    /// The culture to use for a language chosen in the settings, or for the Windows display language when none is chosen.
    /// A language that is not shipped falls back to English.
    /// </summary>
    public static CultureInfo Resolve(string? chosenLanguageCode, CultureInfo windowsLanguage)
    {
        if (chosenLanguageCode is not null && Find(chosenLanguageCode) is { } chosen && GetCulture(chosen) is { } chosenCulture)
        {
            return chosenCulture;
        }

        List<CultureInfo> windowsChain = Ancestors(windowsLanguage);

        // A shipped language among the Windows culture's own ancestors keeps that culture, whose text then falls back
        // through those ancestors, and French on a Canadian system formats numbers the Canadian way.
        // A language counts only in the script it is written in, as Android only takes a folder whose likely script is
        // the requested one, so Serbian in Latin script does not take the Cyrillic Serbian text.
        if (windowsChain.Any(culture => Find(culture.Name) is not null && SameScript(culture, windowsLanguage)))
        {
            return windowsLanguage;
        }

        // Otherwise the shipped variant closest to it, such as Simplified Chinese from China on a Singapore system, or
        // Slovenian from Slovenia on a Slovenian system without a region. The earlier variant in the list wins a tie.
        foreach (CultureInfo ancestor in windowsChain)
        {
            foreach (string code in Codes)
            {
                if (GetCulture(code) is { } shipped && SameScript(shipped, windowsLanguage)
                    && Ancestors(shipped).Any(culture => culture.Name == ancestor.Name))
                {
                    return shipped;
                }
            }
        }

        return CultureInfo.GetCultureInfo(Default);
    }

    /// <summary>
    /// The culture of a language code, or null when Windows does not know it: without ICU, Windows' own culture data
    /// lacks some languages, such as Creole English (cpe).
    /// </summary>
    public static CultureInfo? GetCulture(string languageCode)
    {
        try
        {
            return CultureInfo.GetCultureInfo(languageCode);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// A language's name in that language, starting with a capital, such as "Français" or "English (Australia)", or
    /// the code itself when no name is known.
    /// </summary>
    public static string GetOwnName(string languageCode)
    {
        CultureInfo? culture = GetCulture(languageCode);
        string name = Icu.GetOwnDisplayName(languageCode) ?? culture?.NativeName ?? "";
        if (name.Length == 0)
        {
            name = languageCode;
        }

        return char.IsLower(name[0])
            ? (culture ?? CultureInfo.InvariantCulture).TextInfo.ToUpper(name[0]) + name[1..]
            : name;
    }

    /// <summary>
    /// Whether a language writes numbers in its own digits by default, as Arabic, Persian, Central Kurdish, Bengali and
    /// Marathi do in CLDR, which Android follows when it formats numbers.
    /// </summary>
    public static bool UsesNativeDigits(CultureInfo culture)
    {
        return Icu.GetNumberingSystem(culture) is { } system
            ? system != "latn"
            : culture.Name.Split('-')[0] is "ar" or "fa" or "ckb" or "bn" or "mr";
    }

    // Without ICU the scripts are unknown, and the languages are taken as written in the same one.
    private static bool SameScript(CultureInfo first, CultureInfo second)
    {
        return Icu.GetLikelyScript(first) is not { } firstScript || Icu.GetLikelyScript(second) is not { } secondScript
            || firstScript == secondScript;
    }

    private static string? Find(string languageCode)
    {
        return Codes.FirstOrDefault(code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    // The culture and its parents, such as zh-SG, zh-Hans and zh, without the invariant culture.
    private static List<CultureInfo> Ancestors(CultureInfo culture)
    {
        List<CultureInfo> chain = [];
        for (CultureInfo current = culture; current.Name.Length > 0; current = current.Parent)
        {
            chain.Add(current);
        }

        return chain;
    }
}
