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

using System.Text.Json.Serialization;

namespace LibreccioWeather.Core.Sources.BrightSky;

// Shapes of the Bright Sky JSON responses. Times are ISO 8601 with their offset.

internal sealed record BrightSkyWeatherResult
{
    [JsonPropertyName("weather")]
    public BrightSkyWeather[]? Weather { get; init; }
}

// Precipitation, wind and sunshine cover the hour before the timestamp; the other values are at the timestamp.
internal sealed record BrightSkyWeather
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("precipitation")]
    public double? Precipitation { get; init; }

    [JsonPropertyName("precipitation_probability")]
    public int? PrecipitationProbability { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("wind_direction")]
    public int? WindDirection { get; init; }

    [JsonPropertyName("wind_speed")]
    public double? WindSpeed { get; init; }

    [JsonPropertyName("wind_gust_direction")]
    public int? WindGustDirection { get; init; }

    [JsonPropertyName("wind_gust_speed")]
    public double? WindGustSpeed { get; init; }

    [JsonPropertyName("cloud_cover")]
    public int? CloudCover { get; init; }

    [JsonPropertyName("dew_point")]
    public double? DewPoint { get; init; }

    [JsonPropertyName("relative_humidity")]
    public int? RelativeHumidity { get; init; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; init; }

    [JsonPropertyName("visibility")]
    public int? Visibility { get; init; }

    /// <summary>
    /// Minutes of sunshine.
    /// </summary>
    [JsonPropertyName("sunshine")]
    public double? Sunshine { get; init; }
}

internal sealed record BrightSkyCurrentWeatherResult
{
    [JsonPropertyName("weather")]
    public BrightSkyCurrentWeather? Weather { get; init; }
}

internal sealed record BrightSkyCurrentWeather
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("wind_direction_10")]
    public int? WindDirection { get; init; }

    [JsonPropertyName("wind_speed_10")]
    public double? WindSpeed { get; init; }

    [JsonPropertyName("wind_gust_direction_10")]
    public int? WindGustDirection { get; init; }

    [JsonPropertyName("wind_gust_speed_10")]
    public double? WindGustSpeed { get; init; }

    [JsonPropertyName("cloud_cover")]
    public int? CloudCover { get; init; }

    [JsonPropertyName("dew_point")]
    public double? DewPoint { get; init; }

    [JsonPropertyName("relative_humidity")]
    public int? RelativeHumidity { get; init; }

    [JsonPropertyName("pressure_msl")]
    public double? Pressure { get; init; }

    [JsonPropertyName("visibility")]
    public int? Visibility { get; init; }
}

internal sealed record BrightSkyAlertsResult
{
    [JsonPropertyName("alerts")]
    public BrightSkyAlert[]? Alerts { get; init; }
}

internal sealed record BrightSkyAlert
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("onset")]
    public DateTimeOffset? Onset { get; init; }

    [JsonPropertyName("expires")]
    public DateTimeOffset? Expires { get; init; }

    [JsonPropertyName("severity")]
    public string? Severity { get; init; }

    [JsonPropertyName("headline_en")]
    public string? HeadlineEn { get; init; }

    [JsonPropertyName("headline_de")]
    public string? HeadlineDe { get; init; }

    [JsonPropertyName("description_en")]
    public string? DescriptionEn { get; init; }

    [JsonPropertyName("description_de")]
    public string? DescriptionDe { get; init; }

    [JsonPropertyName("instruction_en")]
    public string? InstructionEn { get; init; }

    [JsonPropertyName("instruction_de")]
    public string? InstructionDe { get; init; }
}

internal sealed record BrightSkyRadarResult
{
    [JsonPropertyName("radar")]
    public BrightSkyRadar[]? Radar { get; init; }
}

internal sealed record BrightSkyRadar
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Hundredths of a millimeter in 5 minutes, as a grid around the location; with a distance of 1 and the plain
    /// format, the first cell is the location's.
    /// </summary>
    [JsonPropertyName("precipitation_5")]
    public int[][]? Precipitation5 { get; init; }
}
