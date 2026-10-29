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

/// <summary>
/// When each part of the weather was last refreshed.
/// </summary>
public sealed record Base
{
    public DateTimeOffset? RefreshTime { get; init; }

    public DateTimeOffset? ForecastUpdateTime { get; init; }

    public DateTimeOffset? CurrentUpdateTime { get; init; }

    public DateTimeOffset? AirQualityUpdateTime { get; init; }

    public DateTimeOffset? PollenUpdateTime { get; init; }

    public DateTimeOffset? MinutelyUpdateTime { get; init; }

    public DateTimeOffset? AlertsUpdateTime { get; init; }

    public DateTimeOffset? NormalsUpdateTime { get; init; }

    public double NormalsUpdateLatitude { get; init; }

    public double NormalsUpdateLongitude { get; init; }
}
