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

namespace LibreccioWeather.Core.Models;

/// <summary>
/// The daytime or nighttime part of a day.
/// </summary>
public sealed record HalfDay
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

    public Temperature? Temperature { get; init; }

    public Precipitation? Precipitation { get; init; }

    public PrecipitationProbability? PrecipitationProbability { get; init; }

    public PrecipitationDuration? PrecipitationDuration { get; init; }

    public Wind? Wind { get; init; }

    public HalfDayWrapper ToHalfDayWrapper()
    {
        return new HalfDayWrapper
        {
            WeatherText = WeatherText,
            WeatherSummary = WeatherSummary,
            WeatherCode = WeatherCode,
            Temperature = Temperature?.ToTemperatureWrapper(),
            Precipitation = Precipitation,
            PrecipitationProbability = PrecipitationProbability,
            PrecipitationDuration = PrecipitationDuration,
            Wind = Wind,
        };
    }
}
