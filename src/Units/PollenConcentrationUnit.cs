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

public sealed class PollenConcentrationUnit : WeatherUnit
{
    /// <summary>
    /// Pollen grains or mold spores per cubic meter of air.
    /// </summary>
    public static readonly PollenConcentrationUnit PerCubicMeter = new("pcum", new UnitDecimals(0));

    public static IReadOnlyList<PollenConcentrationUnit> All { get; } = [PerCubicMeter];

    private PollenConcentrationUnit(string id, UnitDecimals decimals)
        : base(id, decimals)
    {
    }

    public static PollenConcentrationUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }
}
