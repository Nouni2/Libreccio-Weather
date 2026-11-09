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
/// Writes a number as a string template writes it on the JVM, for text that other apps read.
/// </summary>
public static class JavaNumberText
{
    /// <summary>
    /// The shortest digits that read back as the same number, always with a fraction: "48.0" rather than "48". From
    /// 0.001 up to but excluding 10,000,000 the number is written out; outside that range it takes an exponent, as
    /// "1.0E-4".
    /// </summary>
    public static string Format(double value)
    {
        if (double.IsNaN(value))
        {
            return "NaN";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "Infinity" : "-Infinity";
        }

        if (value == 0)
        {
            return double.IsNegative(value) ? "-0.0" : "0.0";
        }

        string sign = value < 0 ? "-" : "";
        double magnitude = Math.Abs(value);
        string shortest = magnitude.ToString("R", CultureInfo.InvariantCulture);
        if (magnitude >= 1e-3 && magnitude < 1e7)
        {
            return sign + (shortest.Contains('.') ? shortest : shortest + ".0");
        }

        int exponent = 0;
        int exponentIndex = shortest.IndexOf('E');
        if (exponentIndex >= 0)
        {
            exponent = int.Parse(shortest[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            shortest = shortest[..exponentIndex];
        }

        int point = shortest.IndexOf('.');
        string digits = point >= 0 ? shortest.Remove(point, 1) : shortest;
        int integerLength = point >= 0 ? point : shortest.Length;
        int leadingZeros = digits.Length - digits.TrimStart('0').Length;
        digits = digits.TrimStart('0').TrimEnd('0');
        int scientificExponent = integerLength + exponent - leadingZeros - 1;
        string fraction = digits.Length > 1 ? digits[1..] : "0";
        return sign + digits[0] + "." + fraction + "E" + scientificExponent.ToString(CultureInfo.InvariantCulture);
    }
}
