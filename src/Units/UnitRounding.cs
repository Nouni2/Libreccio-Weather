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

namespace LibreccioWeather.Core.Units;

internal static class UnitRounding
{
    // Rounds half towards positive infinity, so -2.5 gives -2 and 2.5 gives 3.
    // Math.Round would round half to even and store different values for the same input.
    public static long RoundToLong(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Cannot round NaN to an integer.", nameof(value));
        }

        // Adding 0.5 first can turn a value just below a half into a tie, or change a large exact integer.
        double floor = Math.Floor(value);
        return ToLong(value - floor >= 0.5 ? floor + 1 : floor);
    }

    public static long ToLong(double value)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        if (value >= long.MaxValue)
        {
            return long.MaxValue;
        }

        if (value <= long.MinValue)
        {
            return long.MinValue;
        }

        return (long)value;
    }
}
