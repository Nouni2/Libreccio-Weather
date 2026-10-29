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
    /// Language codes of the shipped languages. Adding a language here makes the string importer generate its files.
    /// </summary>
    public static IReadOnlyList<string> Codes { get; } = [Default, "fr"];

    /// <summary>
    /// The culture to use for a language chosen in the settings, or for the Windows display language when none is chosen.
    /// A language that is not shipped falls back to English.
    /// </summary>
    public static CultureInfo Resolve(string? chosenLanguageCode, CultureInfo windowsLanguage)
    {
        if (chosenLanguageCode is not null && IsShipped(chosenLanguageCode))
        {
            return CultureInfo.GetCultureInfo(chosenLanguageCode);
        }

        // The Windows culture keeps its region, so French on a Canadian system formats numbers the Canadian way.
        return IsShipped(windowsLanguage.Name) ? windowsLanguage : CultureInfo.GetCultureInfo(Default);
    }

    // A regional variant such as fr-CA counts as shipped when its language is shipped.
    private static bool IsShipped(string languageCode)
    {
        string language = languageCode.Split('-')[0];
        return Codes.Any(code => string.Equals(code, language, StringComparison.OrdinalIgnoreCase));
    }
}
