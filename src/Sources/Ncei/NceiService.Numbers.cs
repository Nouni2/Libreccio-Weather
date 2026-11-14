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
using System.Numerics;
using System.Text.RegularExpressions;

namespace LibreccioWeather.Core.Sources.Ncei;

public sealed partial class NceiService
{
    private static readonly Regex TemperaturePattern = new(
        @"\A[\x00-\x20]*(?<sign>[+-]?)(?:(?<special>NaN|Infinity)|0[xX](?<hex>(?:[0-9a-fA-F]+(?:\.[0-9a-fA-F]*)?|\.[0-9a-fA-F]+))[pP](?<exponent>[+-]?[0-9]+)[fFdD]?|(?<decimal>(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?)[fFdD]?)[\x00-\x20]*\z",
        RegexOptions.CultureInvariant);

    private static double? ParseTemperature(string? text)
    {
        if (text is null)
        {
            return null;
        }

        Match match = TemperaturePattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        bool negative = match.Groups["sign"].Value == "-";
        if (match.Groups["special"].Success)
        {
            return match.Groups["special"].Value == "NaN" ? double.NaN
                : negative ? double.NegativeInfinity : double.PositiveInfinity;
        }

        if (match.Groups["decimal"].Success)
        {
            return double.TryParse(match.Groups["sign"].Value + match.Groups["decimal"].Value,
                NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;
        }

        string hex = match.Groups["hex"].Value;
        int point = hex.IndexOf('.');
        int fractionalDigits = point < 0 ? 0 : hex.Length - point - 1;
        BigInteger significand = BigInteger.Parse("0" + hex.Replace(".", "", StringComparison.Ordinal),
            NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        if (significand.IsZero)
        {
            return negative ? -0.0 : 0.0;
        }

        BigInteger exponent = BigInteger.Parse(match.Groups["exponent"].Value, NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture) - (BigInteger)fractionalDigits * 4;
        long bits = significand.GetBitLength();
        BigInteger leadingExponent = exponent + bits - 1;
        if (leadingExponent > 1023)
        {
            return negative ? double.NegativeInfinity : double.PositiveInfinity;
        }

        if (leadingExponent < -1075)
        {
            return negative ? -0.0 : 0.0;
        }

        // Round the exact binary value once, including when it falls in the subnormal range.
        int shift = checked((int)BigInteger.Max(bits - 53, -1074 - exponent));
        BigInteger rounded;
        if (shift > 0)
        {
            rounded = significand >> shift;
            BigInteger remainder = significand - (rounded << shift);
            BigInteger halfway = BigInteger.One << (shift - 1);
            if (remainder > halfway || (remainder == halfway && !rounded.IsEven))
            {
                rounded++;
            }
        }
        else
        {
            rounded = significand << -shift;
        }

        double result = Math.ScaleB((double)rounded, checked((int)(exponent + shift)));
        return negative ? -result : result;
    }
}
