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

using LibreccioWeather.Core.Sources;

namespace LibreccioWeather.Core.Models.Wrappers;

/// <summary>
/// The weather as sources report it, before it is merged and missing values are computed.
/// </summary>
public sealed record WeatherWrapper
{
    public IReadOnlyList<DailyWrapper>? DailyForecast { get; init; }

    public IReadOnlyList<HourlyWrapper>? HourlyForecast { get; init; }

    public CurrentWrapper? Current { get; init; }

    public AirQualityWrapper? AirQuality { get; init; }

    public PollenWrapper? Pollen { get; init; }

    public IReadOnlyList<Minutely>? MinutelyForecast { get; init; }

    public IReadOnlyList<Alert>? AlertList { get; init; }

    public IReadOnlyDictionary<Month, Normals>? Normals { get; init; }

    /// <summary>
    /// Features a source could not provide, with the error each one failed with.
    /// </summary>
    public IReadOnlyDictionary<SourceFeature, Exception>? FailedFeatures { get; init; }
}
