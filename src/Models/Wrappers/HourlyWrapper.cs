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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Models.Wrappers;

/// <summary>
/// An hour as a source reports it. Daylight may be unknown here and is completed when merging.
/// </summary>
public sealed record HourlyWrapper
{
    public required DateTimeOffset Date { get; init; }

    public bool? IsDaylight { get; init; }

    public string? WeatherText { get; init; }

    public WeatherCode? WeatherCode { get; init; }

    public TemperatureWrapper? Temperature { get; init; }

    public Precipitation? Precipitation { get; init; }

    public PrecipitationProbability? PrecipitationProbability { get; init; }

    public Wind? Wind { get; init; }

    public UV? UV { get; init; }

    public Ratio? RelativeHumidity { get; init; }

    public Units.Temperature? DewPoint { get; init; }

    /// <summary>
    /// Pressure at sea level.
    /// </summary>
    public Pressure? Pressure { get; init; }

    public Ratio? CloudCover { get; init; }

    public Distance? Visibility { get; init; }

    /// <summary>
    /// Time with direct sunshine, not time with daylight.
    /// </summary>
    public TimeSpan? SunshineDuration { get; init; }

    /// <param name="airQuality">Air quality for this hour, which sources report separately.</param>
    /// <param name="isDaylight">Replaces the daylight of the source when given. Defaults to daylight when neither is known.</param>
    /// <param name="uv">Replaces the UV of the source when given.</param>
    public Hourly ToHourly(AirQuality? airQuality = null, bool? isDaylight = null, UV? uv = null)
    {
        return new Hourly
        {
            Date = Date,
            IsDaylight = isDaylight ?? IsDaylight ?? true,
            WeatherText = WeatherText,
            WeatherCode = WeatherCode,
            Temperature = Temperature?.ToTemperature(),
            Precipitation = Precipitation,
            PrecipitationProbability = PrecipitationProbability,
            Wind = Wind,
            AirQuality = airQuality,
            UV = uv ?? UV,
            RelativeHumidity = RelativeHumidity,
            DewPoint = DewPoint,
            Pressure = Pressure,
            CloudCover = CloudCover,
            Visibility = Visibility,
        };
    }
}
