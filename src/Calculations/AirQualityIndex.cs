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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Calculations;

public static class AirQualityIndex
{
    public static IReadOnlyList<int> AqiThresholds { get; } = [0, 20, 50, 100, 150, 250];

    public static int IndexExcessivePollution => AqiThresholds[^1];

    /// <summary>
    /// The air quality index, which is the highest index of O3, NO2, PM10 and PM2.5. SO2 and CO are not part of it.
    /// </summary>
    public static int? GetIndex(AirQuality airQuality)
    {
        int?[] pollutants =
        [
            PollutantIndex.O3.GetIndex(PollutantIndex.O3.GetConcentration(airQuality)),
            PollutantIndex.NO2.GetIndex(PollutantIndex.NO2.GetConcentration(airQuality)),
            PollutantIndex.PM10.GetIndex(PollutantIndex.PM10.GetConcentration(airQuality)),
            PollutantIndex.PM25.GetIndex(PollutantIndex.PM25.GetConcentration(airQuality)),
        ];
        return pollutants.Max();
    }

    /// <summary>
    /// The air quality index, or the index of one pollutant.
    /// </summary>
    public static int? GetIndex(AirQuality airQuality, PollutantIndex? pollutant)
    {
        return pollutant is null ? GetIndex(airQuality) : pollutant.GetIndex(pollutant.GetConcentration(airQuality));
    }

    public static int? GetAqiToLevel(int? aqi)
    {
        return aqi is { } value ? PollutantIndex.GetLevel(value, AqiThresholds) : null;
    }

    public static string? GetAqiToName(int? aqi)
    {
        return GetAqiToLevel(aqi) is { } level ? Strings.GetArray("air_quality_levels").ElementAtOrDefault(level) : null;
    }

    public static string? GetAqiToDescription(int? aqi)
    {
        return GetAqiToLevel(aqi) is { } level
            ? Strings.GetArray("air_quality_level_descriptions").ElementAtOrDefault(level) : null;
    }

    public static string? GetAqiToHarmlessExposure(int? aqi)
    {
        return GetAqiToLevel(aqi) is { } level
            ? Strings.GetArray("air_quality_level_harmless_exposures").ElementAtOrDefault(level) : null;
    }

    public static string? GetName(AirQuality airQuality, PollutantIndex? pollutant = null)
    {
        return GetAqiToName(GetIndex(airQuality, pollutant));
    }

    public static string? GetDescription(AirQuality airQuality, PollutantIndex? pollutant = null)
    {
        return GetAqiToDescription(GetIndex(airQuality, pollutant));
    }
}
