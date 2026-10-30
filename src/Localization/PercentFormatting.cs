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

public static class PercentFormatting
{
    public static string Format(double percent, int precision, CultureInfo culture)
    {
        double fraction = percent > 0 ? percent / 100.0 : 0;
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        numberFormat.NumberDecimalSeparator = numberFormat.PercentDecimalSeparator;
        numberFormat.NumberGroupSeparator = numberFormat.PercentGroupSeparator;
        numberFormat.NumberGroupSizes = numberFormat.PercentGroupSizes;
        string pattern = precision == 0 ? "#,##0" : "#,##0." + new string('#', precision);
        string digits;
        if (double.IsPositiveInfinity(fraction))
        {
            digits = numberFormat.PositiveInfinitySymbol;
        }
        else
        {
            decimal value = decimal.Parse(fraction.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture) * 100;
            digits = Math.Round(value, precision, MidpointRounding.ToEven).ToString(pattern, numberFormat);
        }
        string symbol = numberFormat.PercentSymbol;
        // French CLDR percent patterns use a non-breaking space, which PercentPositivePattern does not retain.
        string space = culture.TwoLetterISOLanguageName == "fr" ? "\u00A0" : " ";
        return numberFormat.PercentPositivePattern switch
        {
            0 => digits + space + symbol,
            1 => digits + symbol,
            2 => symbol + digits,
            3 => symbol + space + digits,
            _ => throw new ArgumentOutOfRangeException(nameof(culture)),
        };
    }
}
