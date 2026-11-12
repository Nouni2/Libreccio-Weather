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
/// CLDR plural categories for whole numbers, as Android chooses plural strings with ICU's rules.
/// See https://www.unicode.org/cldr/charts/latest/supplemental/language_plural_rules.html
/// </summary>
public static class PluralRules
{
    public static string Select(CultureInfo culture, long count)
    {
        return Icu.SelectPlural(count, culture) ?? SelectWithoutIcu(culture, count);
    }

    /// <summary>
    /// The CLDR rules of each shipped language for whole numbers, used without Windows' ICU. A language CLDR has no
    /// rules for only has "other", as in ICU's root rules.
    /// </summary>
    public static string SelectWithoutIcu(CultureInfo culture, long count)
    {
        // CLDR's rules work on the absolute value; long.MinValue has none as a long.
        ulong n = count < 0 ? (ulong)(-(count + 1)) + 1 : (ulong)count;
        ulong mod10 = n % 10;
        ulong mod100 = n % 100;
        bool millions = n != 0 && n % 1_000_000 == 0;
        string[] parts = culture.Name.Split('-');
        string language = parts[0];

        return language switch
        {
            "en" or "de" or "nl" or "sv" or "nb" or "no" or "da" or "et" or "fi" or "el" or "bg" or "eu" or "gl" or "tr"
                or "az" or "hu" or "ta" or "ur" or "mr" or "eo" or "ia" or "haw" or "ckb" => n == 1 ? "one" : "other",
            "it" or "es" or "ca" => n == 1 ? "one" : millions ? "many" : "other",
            "pt" when parts.Contains("PT") => n == 1 ? "one" : millions ? "many" : "other",
            "pt" or "fr" => n <= 1 ? "one" : millions ? "many" : "other",
            "hi" or "bn" or "kn" or "fa" or "kab" => n <= 1 ? "one" : "other",
            "is" or "mk" => mod10 == 1 && mod100 != 11 ? "one" : "other",
            "ru" or "uk" or "be" => mod10 == 1 && mod100 != 11 ? "one"
                : mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) ? "few"
                : "many",
            "pl" => n == 1 ? "one"
                : mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) ? "few"
                : "many",
            "cs" or "sk" => n == 1 ? "one" : n is >= 2 and <= 4 ? "few" : "other",
            "hr" or "bs" or "sr" => mod10 == 1 && mod100 != 11 ? "one"
                : mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) ? "few"
                : "other",
            "sl" => mod100 == 1 ? "one" : mod100 == 2 ? "two" : mod100 is 3 or 4 ? "few" : "other",
            "lt" => mod10 == 1 && mod100 is not (>= 11 and <= 19) ? "one"
                : mod10 >= 2 && mod100 is not (>= 11 and <= 19) ? "few"
                : "other",
            "lv" => mod10 == 0 || mod100 is >= 11 and <= 19 ? "zero" : mod10 == 1 ? "one" : "other",
            "ro" => n == 1 ? "one" : n == 0 || mod100 is >= 1 and <= 19 ? "few" : "other",
            "ga" => n switch
            {
                1 => "one",
                2 => "two",
                >= 3 and <= 6 => "few",
                >= 7 and <= 10 => "many",
                _ => "other",
            },
            "ar" => n switch
            {
                0 => "zero",
                1 => "one",
                2 => "two",
                _ => mod100 is >= 3 and <= 10 ? "few" : mod100 >= 11 ? "many" : "other",
            },
            "he" => n == 1 ? "one" : n == 2 ? "two" : "other",
            _ => "other",
        };
    }
}
