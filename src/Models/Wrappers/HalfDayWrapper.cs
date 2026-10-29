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

public sealed record HalfDayWrapper
{
    /// <summary>
    /// A short description of the weather condition.
    /// </summary>
    public string? WeatherText { get; init; }

    /// <summary>
    /// A long description of the weather condition, used as a summary of the half day.
    /// </summary>
    public string? WeatherSummary { get; init; }

    public WeatherCode? WeatherCode { get; init; }

    public TemperatureWrapper? Temperature { get; init; }

    public Precipitation? Precipitation { get; init; }

    public PrecipitationProbability? PrecipitationProbability { get; init; }

    public PrecipitationDuration? PrecipitationDuration { get; init; }

    public Wind? Wind { get; init; }

    public HalfDay ToHalfDay()
    {
        return new HalfDay
        {
            WeatherText = WeatherText,
            WeatherSummary = WeatherSummary,
            WeatherCode = WeatherCode,
            Temperature = Temperature?.ToTemperature(),
            Precipitation = Precipitation,
            PrecipitationProbability = PrecipitationProbability,
            PrecipitationDuration = PrecipitationDuration,
            Wind = Wind,
        };
    }
}
