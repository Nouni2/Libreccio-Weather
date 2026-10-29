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

using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Models;

public sealed record Hourly
{
    public required DateTimeOffset Date { get; init; }

    public bool IsDaylight { get; init; } = true;

    public string? WeatherText { get; init; }

    public WeatherCode? WeatherCode { get; init; }

    public Temperature? Temperature { get; init; }

    public Precipitation? Precipitation { get; init; }

    public PrecipitationProbability? PrecipitationProbability { get; init; }

    public Wind? Wind { get; init; }

    public AirQuality? AirQuality { get; init; }

    public UV? UV { get; init; }

    public Ratio? RelativeHumidity { get; init; }

    public Units.Temperature? DewPoint { get; init; }

    /// <summary>
    /// Pressure at sea level.
    /// </summary>
    public Pressure? Pressure { get; init; }

    public Ratio? CloudCover { get; init; }

    public Distance? Visibility { get; init; }

    public HourlyWrapper ToHourlyWrapper()
    {
        return new HourlyWrapper
        {
            Date = Date,
            IsDaylight = IsDaylight,
            WeatherText = WeatherText,
            WeatherCode = WeatherCode,
            Temperature = Temperature?.ToTemperatureWrapper(),
            Precipitation = Precipitation,
            PrecipitationProbability = PrecipitationProbability,
            Wind = Wind,
            UV = UV,
            RelativeHumidity = RelativeHumidity,
            DewPoint = DewPoint,
            Pressure = Pressure,
            CloudCover = CloudCover,
            Visibility = Visibility,
        };
    }
}
