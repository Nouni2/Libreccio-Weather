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

namespace LibreccioWeather.Core.Units;

/// <summary>
/// How many decimals a value in a unit is displayed with, for each display width.
/// </summary>
public readonly record struct UnitDecimals
{
    public UnitDecimals(int @short, int? narrow = null, int? @long = null)
    {
        Short = @short;
        Narrow = narrow ?? @short;
        Long = @long ?? @short;
    }

    public int Short { get; }

    public int Narrow { get; }

    public int Long { get; }

    // Rounds half away from zero and pads with zeros, so 2.5 with 0 decimals gives "3" and 1.2 with 2 decimals gives "1.20".
    internal static string FormatToExactDecimals(double value, int decimals)
    {
        double rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        return rounded.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
