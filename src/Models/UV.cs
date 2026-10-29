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

namespace LibreccioWeather.Core.Models;

public sealed record UV
{
    public const double UVIndexLow = 3.0;
    public const double UVIndexMiddle = 6.0;
    public const double UVIndexHigh = 8.0;
    public const double UVIndexExcessive = 11.0;

    /// <summary>
    /// Lower bound of each UV level, from low to extreme.
    /// </summary>
    public static IReadOnlyList<int> UVThresholds { get; } =
        [0, (int)UVIndexLow, (int)UVIndexMiddle, (int)UVIndexHigh, (int)UVIndexExcessive];

    public double? Index { get; init; }

    public bool IsValid => Index is not null;
}
