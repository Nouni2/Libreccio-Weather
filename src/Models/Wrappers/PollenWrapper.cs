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

public sealed record PollenWrapper
{
    /// <summary>
    /// Used as today's pollen when there is neither a daily nor an hourly forecast.
    /// </summary>
    public Pollen? Current { get; init; }

    public IReadOnlyDictionary<DateTimeOffset, Pollen>? DailyForecast { get; init; }

    public IReadOnlyDictionary<DateTimeOffset, Pollen>? HourlyForecast { get; init; }
}
