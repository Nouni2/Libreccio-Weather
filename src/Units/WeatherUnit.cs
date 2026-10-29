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

public abstract class WeatherUnit
{
    protected WeatherUnit(string id, UnitDecimals decimals)
    {
        Id = id;
        Decimals = decimals;
    }

    /// <summary>
    /// Identifier used when the unit is stored, for example in settings.
    /// </summary>
    public string Id { get; }

    public UnitDecimals Decimals { get; }

    public int GetPrecision(UnitWidth width)
    {
        return width switch
        {
            UnitWidth.Narrow => Decimals.Narrow,
            UnitWidth.Long => Decimals.Long,
            _ => Decimals.Short,
        };
    }

    public override string ToString()
    {
        return Id;
    }
}
