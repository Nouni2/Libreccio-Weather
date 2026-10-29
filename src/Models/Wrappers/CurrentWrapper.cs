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

public sealed record CurrentWrapper
{
    public string? WeatherText { get; init; }

    public WeatherCode? WeatherCode { get; init; }

    public TemperatureWrapper? Temperature { get; init; }

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

    public Distance? Ceiling { get; init; }

    public string? DailyForecast { get; init; }

    /// <summary>
    /// A description of the nowcast.
    /// </summary>
    public string? HourlyForecast { get; init; }

    /// <param name="uv">Replaces the UV of the source when given.</param>
    public Current ToCurrent(UV? uv = null)
    {
        return new Current
        {
            WeatherText = WeatherText,
            WeatherCode = WeatherCode,
            Temperature = Temperature?.ToTemperature(),
            Wind = Wind,
            UV = uv ?? UV,
            RelativeHumidity = RelativeHumidity,
            DewPoint = DewPoint,
            Pressure = Pressure,
            CloudCover = CloudCover,
            Visibility = Visibility,
            Ceiling = Ceiling,
            DailyForecast = DailyForecast,
            HourlyForecast = HourlyForecast,
        };
    }
}
