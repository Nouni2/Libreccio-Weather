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
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Calculations;

/// <summary>
/// A pollutant, with the concentrations at which each air quality level starts.
/// PM2.5, PM10, O3 and NO2 follow the 2023 Plume AQI; SO2 and CO follow the 2021 WHO guidelines.
/// </summary>
public sealed class PollutantIndex
{
    public static readonly PollutantIndex PM25 = new("pm25", [0, 5, 15, 30, 60, 150], 60, "air_quality_pm25",
        "air_quality_pm_sources", airQuality => airQuality.PM25?.InMicrogramsPerCubicMeter);

    public static readonly PollutantIndex PM10 = new("pm10", [0, 15, 45, 80, 160, 400], 160, "air_quality_pm10",
        "air_quality_pm_sources", airQuality => airQuality.PM10?.InMicrogramsPerCubicMeter);

    public static readonly PollutantIndex O3 = new("o3", [0, 50, 100, 160, 240, 480], 240, "air_quality_o3",
        "air_quality_o3_sources", airQuality => airQuality.O3?.InMicrogramsPerCubicMeter);

    public static readonly PollutantIndex NO2 = new("no2", [0, 10, 25, 200, 400, 1000], 200, "air_quality_no2",
        "air_quality_no2_sources", airQuality => airQuality.NO2?.InMicrogramsPerCubicMeter);

    // 20 and 40 are daily guidelines, 270 hourly and 500 over ten minutes; 960 prolongs them linearly.
    public static readonly PollutantIndex SO2 = new("so2", [0, 20, 40, 270, 500, 960], 270, "air_quality_so2",
        "air_quality_so2_sources", airQuality => airQuality.SO2?.InMicrogramsPerCubicMeter);

    // 2 and 4 are daily guidelines, 35 hourly and 100 over fifteen minutes; 230 prolongs them linearly.
    public static readonly PollutantIndex CO = new("co", [0, 2, 4, 35, 100, 230], 35, "air_quality_co",
        "air_quality_co_sources", airQuality => airQuality.CO?.InMilligramsPerCubicMeter);

    public static IReadOnlyList<PollutantIndex> Entries { get; } = [PM25, PM10, O3, NO2, SO2, CO];

    private readonly string _nameKey;
    private readonly Func<AirQuality, double?> _concentration;

    private PollutantIndex(
        string id, IReadOnlyList<int> thresholds, int maxY, string nameKey, string sourcesKey,
        Func<AirQuality, double?> concentration)
    {
        Id = id;
        Thresholds = thresholds;
        MaxY = maxY;
        _nameKey = nameKey;
        SourcesKey = sourcesKey;
        _concentration = concentration;
    }

    public string Id { get; }

    public IReadOnlyList<int> Thresholds { get; }

    /// <summary>
    /// The lowest top of a chart of this pollutant's concentration.
    /// </summary>
    public int MaxY { get; }

    public string ShortName => Strings.Get(_nameKey);

    public string VoicedName => Strings.Get(_nameKey + "_voice");

    public string SourcesKey { get; }

    public string AboutPollutantKey => _nameKey + "_about";

    public string AboutIndexKey => _nameKey + "_index";

    /// <summary>
    /// CO is measured in milligrams per cubic meter, every other pollutant in micrograms.
    /// </summary>
    public PollutantConcentrationUnit Unit => this == CO
        ? PollutantConcentrationUnit.MilligramPerCubicMeter
        : PollutantConcentrationUnit.MicrogramPerCubicMeter;

    /// <summary>
    /// The full name, such as "Particulate matter &lt; 2.5 µm", where particulate matter gets its size as a formatted
    /// micrometer length and every other pollutant its short name.
    /// </summary>
    public string GetFullName()
    {
        // The size is formatted as a plain number in micrometers, since a precipitation quantity would round 2.5 µm.
        string argument = this == PM10 || this == PM25
            ? UnitFormatting.Format(this == PM10 ? 10.0 : 2.5, PrecipitationUnit.Micrometer, UnitWidth.Long)
            : ShortName;
        return Strings.Format(_nameKey + "_full", argument);
    }

    public double? GetConcentration(AirQuality airQuality) => _concentration(airQuality);

    public int? GetIndex(double? concentration)
    {
        if (concentration is not { } cp || GetLevel(cp, Thresholds) is not { } level)
        {
            return null;
        }

        double index = level < Thresholds.Count - 1
            ? ((double)AirQualityIndex.AqiThresholds[level + 1] - AirQualityIndex.AqiThresholds[level])
                / ((double)Thresholds[level + 1] - Thresholds[level]) * (cp - Thresholds[level]) + AirQualityIndex.AqiThresholds[level]
            // A linear index continues above the last threshold.
            : cp * AirQualityIndex.AqiThresholds[^1] / Thresholds[^1];
        return (int)Math.Clamp(UnitRounding.RoundToLong(index), int.MinValue, int.MaxValue);
    }

    public int? GetLevel(double? concentration)
    {
        return concentration is { } cp ? GetLevel(cp, Thresholds) : null;
    }

    internal static int? GetLevel(double value, IReadOnlyList<int> thresholds)
    {
        for (int level = thresholds.Count - 1; level >= 0; level--)
        {
            if (value >= thresholds[level])
            {
                return level;
            }
        }

        return null;
    }

    public override string ToString() => Id;
}
