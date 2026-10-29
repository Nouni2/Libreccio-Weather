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

namespace LibreccioWeather.Core.Models;

/// <summary>
/// Concentrations of the pollutants the air quality index is computed from.
/// The index follows the 2023 Plume AQI, with WHO recommendations where it gives no value.
/// See https://plumelabs.files.wordpress.com/2023/06/plume_aqi_2023.pdf
/// and https://www.who.int/news-room/fact-sheets/detail/ambient-(outdoor)-air-quality-and-health
/// </summary>
public sealed record AirQuality
{
    public PollutantConcentration? PM25 { get; init; }

    public PollutantConcentration? PM10 { get; init; }

    public PollutantConcentration? SO2 { get; init; }

    public PollutantConcentration? NO2 { get; init; }

    public PollutantConcentration? O3 { get; init; }

    public PollutantConcentration? CO { get; init; }

    public bool IsValid => PM25 is not null || PM10 is not null || SO2 is not null || NO2 is not null || O3 is not null || CO is not null;

    // SO2 and CO are not part of the index.
    public bool IsIndexValid => PM25 is not null || PM10 is not null || NO2 is not null || O3 is not null;

    public AirQuality ToValid()
    {
        return this with
        {
            PM25 = PM25?.ToValidOrNull(),
            PM10 = PM10?.ToValidOrNull(),
            SO2 = SO2?.ToValidOrNull(),
            NO2 = NO2?.ToValidOrNull(),
            O3 = O3?.ToValidOrNull(),
            CO = CO?.ToValidOrNull(),
        };
    }
}
