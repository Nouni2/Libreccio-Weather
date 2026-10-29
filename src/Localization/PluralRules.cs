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
/// CLDR plural categories for whole numbers. Each shipped language needs its rule here.
/// See https://www.unicode.org/cldr/charts/latest/supplemental/language_plural_rules.html
/// </summary>
public static class PluralRules
{
    public static string Select(CultureInfo culture, long count)
    {
        return culture.TwoLetterISOLanguageName switch
        {
            "fr" => count is 0 or 1 ? "one" : "other",
            _ => count == 1 ? "one" : "other",
        };
    }
}
