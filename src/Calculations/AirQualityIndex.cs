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

using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Calculations;

public static class AirQualityIndex
{
    public static IReadOnlyList<int> AqiThresholds { get; } = [0, 20, 50, 100, 150, 250];

    public static int? GetIndex(AirQuality airQuality)
    {
        int?[] pollutants =
        [
            GetIndex(airQuality.O3?.InMicrogramsPerCubicMeter, [0, 50, 100, 160, 240, 480]),
            GetIndex(airQuality.NO2?.InMicrogramsPerCubicMeter, [0, 10, 25, 200, 400, 1000]),
            GetIndex(airQuality.PM10?.InMicrogramsPerCubicMeter, [0, 15, 45, 80, 160, 400]),
            GetIndex(airQuality.PM25?.InMicrogramsPerCubicMeter, [0, 5, 15, 30, 60, 150]),
        ];
        return pollutants.Max();
    }

    public static int? GetAqiToLevel(int? aqi)
    {
        return aqi is { } value ? GetLevel(value, AqiThresholds) : null;
    }

    private static int? GetLevel(double concentration, IReadOnlyList<int> thresholds)
    {
        for (int level = thresholds.Count - 1; level >= 0; level--)
        {
            if (concentration >= thresholds[level])
            {
                return level;
            }
        }

        return null;
    }

    private static int? GetIndex(double? concentration, IReadOnlyList<int> thresholds)
    {
        if (concentration is not { } cp || GetLevel(cp, thresholds) is not { } level)
        {
            return null;
        }

        double index = level < thresholds.Count - 1
            ? ((double)AqiThresholds[level + 1] - AqiThresholds[level])
                / ((double)thresholds[level + 1] - thresholds[level]) * (cp - thresholds[level]) + AqiThresholds[level]
            : cp * AqiThresholds[^1] / thresholds[^1];
        return (int)Math.Clamp(UnitRounding.RoundToLong(index), int.MinValue, int.MaxValue);
    }
}
