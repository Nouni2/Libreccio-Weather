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

namespace LibreccioWeather.Core.Models.Wrappers;

/// <summary>
/// The temperature as a source reports it, before feels-like values are computed.
/// </summary>
public sealed record TemperatureWrapper
{
    public Units.Temperature? Temperature { get; init; }

    public Units.Temperature? FeelsLike { get; init; }

    public Models.Temperature ToTemperature(
        Units.Temperature? computedApparent = null,
        Units.Temperature? computedWindChill = null,
        Units.Temperature? computedHumidex = null)
    {
        return new Models.Temperature
        {
            Value = Temperature,
            SourceFeelsLike = FeelsLike,
            ComputedApparent = computedApparent,
            ComputedWindChill = computedWindChill,
            ComputedHumidex = computedHumidex,
        };
    }
}
