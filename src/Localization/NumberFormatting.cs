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
/// Numbers formatted the way Android's ICU number formatter shows them, with the conventions of a language.
/// </summary>
public static class NumberFormatting
{
    /// <summary>
    /// Formats a number with at most <paramref name="decimals"/> decimals and no trailing zeros.
    /// With no decimals, halves round towards positive infinity, so -2.5 gives "-2".
    /// With decimals, halves round to even.
    /// </summary>
    public static string Format(double value, int decimals, CultureInfo culture, bool showSign = false)
    {
        double number = decimals == 0 ? UnitFormatting.RoundToInt(value) : value;
        string precision = decimals == 0 ? "precision-integer" : "." + new string('#', decimals);
        string sign = showSign ? "sign-always" : "sign-auto";
        if (Icu.SupportsUnitNames && Icu.FormatNumber($"{precision} {sign}", number, culture)?.Text is { } formatted)
        {
            return formatted;
        }

        if (decimals != 0 && !double.IsFinite(value))
        {
            return value.ToString(culture);
        }

        string pattern = decimals == 0 ? "#,##0" : "#,##0." + new string('#', decimals);
        if (Math.Abs(value) >= (double)decimal.MaxValue && decimals != 0)
        {
            return (showSign && value >= 0 ? culture.NumberFormat.PositiveSign : "") + value.ToString(pattern, culture);
        }

        decimal rounded;
        bool negative;
        if (decimals == 0)
        {
            int whole = UnitFormatting.RoundToInt(value);
            rounded = whole;
            negative = whole < 0;
        }
        else
        {
            // ICU rounds the shortest decimal form of a double, which "R" gives, not its exact binary value.
            rounded = Math.Round(
                decimal.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture),
                decimals,
                MidpointRounding.ToEven);
            // A negative value that rounds to zero keeps its minus sign, as ICU shows "-0".
            negative = double.IsNegative(value);
        }

        // English and French group every thousand, matching their minimum grouping digits of 1 in CLDR.
        string digits = Math.Abs(rounded).ToString(pattern, culture);
        if (negative)
        {
            return culture.NumberFormat.NegativeSign + digits;
        }

        return showSign ? culture.NumberFormat.PositiveSign + digits : digits;
    }
}
