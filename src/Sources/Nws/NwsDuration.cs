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
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.Nws;

internal static class NwsDuration
{
    private const long NanosInMillisecond = 1_000_000;
    private const long MaxMillis = long.MaxValue / 2;
    private const long MaxNanos = MaxMillis / NanosInMillisecond * NanosInMillisecond - 1;
    private static readonly BigInteger InfiniteNanos = (BigInteger)MaxMillis * NanosInMillisecond;
    private static readonly Regex IntegerPattern = new("^[+-]?[0-9]+\\z", RegexOptions.CultureInvariant);

    internal static int ParseWholeHours(string text)
    {
        int index = 0;
        bool negative = text.StartsWith('-');
        if (text.StartsWith('+') || negative)
        {
            index++;
        }

        if (index >= text.Length || text[index++] != 'P' || index == text.Length)
        {
            throw new ArgumentException("Invalid ISO duration.", nameof(text));
        }

        bool time = false;
        long previousUnit = long.MaxValue;
        DurationValue result = new(BigInteger.Zero, false);
        while (index < text.Length)
        {
            if (text[index] == 'T')
            {
                if (time || ++index == text.Length)
                {
                    throw new ArgumentException("Invalid ISO duration.", nameof(text));
                }

                time = true;
                continue;
            }

            int start = index;
            while (index < text.Length && (text[index] is >= '0' and <= '9' or '+' or '-' or '.'))
            {
                index++;
            }

            if (index == start || index == text.Length)
            {
                throw new ArgumentException("Invalid ISO duration component.", nameof(text));
            }

            string component = text[start..index];
            long unit = (text[index++], time) switch
            {
                ('D', false) => 86_400_000_000_000,
                ('H', true) => 3_600_000_000_000,
                ('M', true) => 60_000_000_000,
                ('S', true) => 1_000_000_000,
                _ => throw new ArgumentException("Invalid ISO duration unit.", nameof(text)),
            };
            if (previousUnit <= unit)
            {
                throw new ArgumentException("Invalid ISO duration component order.", nameof(text));
            }

            previousUnit = unit;
            int dot = component.IndexOf('.');
            if (unit == 1_000_000_000 && dot > 0)
            {
                result = Add(result, FromInteger(ParseInteger(component[..dot]), unit));
                double fraction = double.Parse(component[dot..], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                long nanos = UnitRounding.RoundToLong(fraction * unit);
                result = Add(result, new DurationValue(nanos, false));
            }
            else
            {
                result = Add(result, FromInteger(ParseInteger(component), unit));
            }
        }

        BigInteger nanosResult = negative ? -result.Nanoseconds : result.Nanoseconds;
        long hours = BigInteger.Abs(nanosResult) == InfiniteNanos ? nanosResult.Sign < 0 ? long.MinValue : long.MaxValue
            : (long)(nanosResult / 3_600_000_000_000);
        // The interval limit is applied after narrowing the whole-hour count.
        return unchecked((int)hours);
    }

    private static long ParseInteger(string text)
    {
        if (!IntegerPattern.IsMatch(text))
        {
            throw new ArgumentException("Invalid ISO duration number.", nameof(text));
        }

        BigInteger value = BigInteger.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        if (BigInteger.Abs(value) > 9_999_999_999_999_999L)
        {
            return value.Sign < 0 ? long.MinValue : long.MaxValue;
        }

        return (long)value;
    }

    private static DurationValue FromInteger(long value, long unit)
    {
        return value >= -(MaxNanos / unit) && value <= MaxNanos / unit
            ? new DurationValue((BigInteger)value * unit, false)
            : FromMilliseconds((BigInteger)value * (unit / NanosInMillisecond));
    }

    private static DurationValue FromMilliseconds(BigInteger value)
    {
        if (BigInteger.Abs(value) > MaxMillis)
        {
            value = value.Sign * (BigInteger)MaxMillis;
        }

        return new DurationValue(value * NanosInMillisecond, BigInteger.Abs(value) > MaxNanos / NanosInMillisecond);
    }

    private static DurationValue Add(DurationValue left, DurationValue right)
    {
        bool leftInfinite = BigInteger.Abs(left.Nanoseconds) == InfiniteNanos;
        bool rightInfinite = BigInteger.Abs(right.Nanoseconds) == InfiniteNanos;
        if (leftInfinite || rightInfinite)
        {
            if (leftInfinite && rightInfinite && left.Nanoseconds.Sign != right.Nanoseconds.Sign)
            {
                throw new ArgumentException("Opposite infinite durations cannot be added.");
            }

            return leftInfinite ? left : right;
        }

        if (!left.Milliseconds && !right.Milliseconds)
        {
            BigInteger sum = left.Nanoseconds + right.Nanoseconds;
            return BigInteger.Abs(sum) <= MaxNanos ? new DurationValue(sum, false)
                : FromMilliseconds(sum / NanosInMillisecond);
        }

        if (left.Milliseconds && right.Milliseconds)
        {
            return FromMilliseconds((left.Nanoseconds + right.Nanoseconds) / NanosInMillisecond);
        }

        BigInteger millis = left.Nanoseconds / NanosInMillisecond + right.Nanoseconds / NanosInMillisecond;
        if (BigInteger.Abs(millis) <= MaxNanos / NanosInMillisecond)
        {
            return new DurationValue(millis * NanosInMillisecond
                + (left.Milliseconds ? right.Nanoseconds : left.Nanoseconds) % NanosInMillisecond, false);
        }

        return FromMilliseconds(millis);
    }

    private readonly record struct DurationValue(BigInteger Nanoseconds, bool Milliseconds);
}
