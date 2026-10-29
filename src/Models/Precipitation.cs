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
/// Precipitation amounts, in total and by type.
/// </summary>
public sealed record Precipitation
{
    // Half-day thresholds in mm, from the India Meteorological Department daily values divided by two.
    // See https://mausam.imd.gov.in/imd_latest/contents/pdf/forecasting_sop.pdf
    public const double PrecipitationHalfDayVeryLight = 1.25;
    public const double PrecipitationHalfDayLight = 7.75;
    public const double PrecipitationHalfDayMedium = 32.25;
    public const double PrecipitationHalfDayHeavy = 57.75;
    public const double PrecipitationHalfDayRainstorm = 102.25;

    // Hourly thresholds in mm, from chapter 9.3.1 (nowcasting) of the same document.
    public const double PrecipitationHourlyLight = 5.0;
    public const double PrecipitationHourlyMedium = 10.0;
    public const double PrecipitationHourlyHeavy = 15.0;
    public const double PrecipitationHourlyRainstorm = 20.0;

    public Units.Precipitation? Total { get; init; }

    public Units.Precipitation? Thunderstorm { get; init; }

    public Units.Precipitation? Rain { get; init; }

    public Units.Precipitation? Snow { get; init; }

    public Units.Precipitation? Ice { get; init; }
}
