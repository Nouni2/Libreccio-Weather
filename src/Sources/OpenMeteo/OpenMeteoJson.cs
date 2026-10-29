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

namespace LibreccioWeather.Core.Sources.OpenMeteo;

// Shapes of the Open-Meteo JSON responses. Times are Unix seconds, as requested with timeformat=unixtime.

internal sealed record OpenMeteoWeatherResult
{
    [JsonPropertyName("current")]
    public OpenMeteoWeatherCurrent? Current { get; init; }

    [JsonPropertyName("daily")]
    public OpenMeteoWeatherDaily? Daily { get; init; }

    [JsonPropertyName("hourly")]
    public OpenMeteoWeatherHourly? Hourly { get; init; }

    [JsonPropertyName("minutely_15")]
    public OpenMeteoWeatherMinutely? MinutelyFifteen { get; init; }

    [JsonPropertyName("error")]
    public bool? Error { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

internal sealed record OpenMeteoWeatherCurrent
{
    [JsonPropertyName("temperature_2m")]
    public double? Temperature { get; init; }

    [JsonPropertyName("apparent_temperature")]
    public double? ApparentTemperature { get; init; }

    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeed { get; init; }

    [JsonPropertyName("wind_direction_10m")]
    public double? WindDirection { get; init; }

    [JsonPropertyName("wind_gusts_10m")]
    public double? WindGusts { get; init; }

    [JsonPropertyName("uv_index")]
    public double? UVIndex { get; init; }

    [JsonPropertyName("relative_humidity_2m")]
    public int? RelativeHumidity { get; init; }

    [JsonPropertyName("dew_point_2m")]
    public double? DewPoint { get; init; }

    [JsonPropertyName("pressure_msl")]
    public double? PressureMsl { get; init; }

    [JsonPropertyName("cloud_cover")]
    public int? CloudCover { get; init; }

    [JsonPropertyName("visibility")]
    public double? Visibility { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }
}

internal sealed record OpenMeteoWeatherDaily
{
    [JsonPropertyName("time")]
    public long[] Time { get; init; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public double?[]? TemperatureMax { get; init; }

    [JsonPropertyName("temperature_2m_min")]
    public double?[]? TemperatureMin { get; init; }

    [JsonPropertyName("apparent_temperature_max")]
    public double?[]? ApparentTemperatureMax { get; init; }

    [JsonPropertyName("apparent_temperature_min")]
    public double?[]? ApparentTemperatureMin { get; init; }

    [JsonPropertyName("sunshine_duration")]
    public double?[]? SunshineDuration { get; init; }

    [JsonPropertyName("uv_index_max")]
    public double?[]? UVIndexMax { get; init; }

    [JsonPropertyName("relative_humidity_2m_mean")]
    public int?[]? RelativeHumidityMean { get; init; }

    [JsonPropertyName("relative_humidity_2m_max")]
    public int?[]? RelativeHumidityMax { get; init; }

    [JsonPropertyName("relative_humidity_2m_min")]
    public int?[]? RelativeHumidityMin { get; init; }

    [JsonPropertyName("dew_point_2m_mean")]
    public double?[]? DewPointMean { get; init; }

    [JsonPropertyName("dew_point_2m_max")]
    public double?[]? DewPointMax { get; init; }

    [JsonPropertyName("dew_point_2m_min")]
    public double?[]? DewPointMin { get; init; }

    [JsonPropertyName("pressure_msl_mean")]
    public double?[]? PressureMslMean { get; init; }

    [JsonPropertyName("pressure_msl_max")]
    public double?[]? PressureMslMax { get; init; }

    [JsonPropertyName("pressure_msl_min")]
    public double?[]? PressureMslMin { get; init; }

    [JsonPropertyName("cloud_cover_mean")]
    public int?[]? CloudCoverMean { get; init; }

    [JsonPropertyName("cloud_cover_max")]
    public int?[]? CloudCoverMax { get; init; }

    [JsonPropertyName("cloud_cover_min")]
    public int?[]? CloudCoverMin { get; init; }

    [JsonPropertyName("visibility_mean")]
    public double?[]? VisibilityMean { get; init; }

    [JsonPropertyName("visibility_max")]
    public double?[]? VisibilityMax { get; init; }

    [JsonPropertyName("visibility_min")]
    public double?[]? VisibilityMin { get; init; }
}

internal sealed record OpenMeteoWeatherHourly
{
    [JsonPropertyName("time")]
    public long[] Time { get; init; } = [];

    [JsonPropertyName("temperature_2m")]
    public double?[]? Temperature { get; init; }

    [JsonPropertyName("apparent_temperature")]
    public double?[]? ApparentTemperature { get; init; }

    [JsonPropertyName("precipitation_probability")]
    public int?[]? PrecipitationProbability { get; init; }

    [JsonPropertyName("precipitation")]
    public double?[]? Precipitation { get; init; }

    [JsonPropertyName("rain")]
    public double?[]? Rain { get; init; }

    [JsonPropertyName("showers")]
    public double?[]? Showers { get; init; }

    [JsonPropertyName("snowfall")]
    public double?[]? Snowfall { get; init; }

    [JsonPropertyName("weather_code")]
    public int?[]? WeatherCode { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double?[]? WindSpeed { get; init; }

    [JsonPropertyName("wind_direction_10m")]
    public int?[]? WindDirection { get; init; }

    [JsonPropertyName("wind_gusts_10m")]
    public double?[]? WindGusts { get; init; }

    [JsonPropertyName("uv_index")]
    public double?[]? UVIndex { get; init; }

    // The API returns 0 or 1 rather than a boolean.
    [JsonPropertyName("is_day")]
    public int[]? IsDay { get; init; }

    [JsonPropertyName("relative_humidity_2m")]
    public int?[]? RelativeHumidity { get; init; }

    [JsonPropertyName("dew_point_2m")]
    public double?[]? DewPoint { get; init; }

    [JsonPropertyName("pressure_msl")]
    public double?[]? PressureMsl { get; init; }

    [JsonPropertyName("cloud_cover")]
    public int?[]? CloudCover { get; init; }

    [JsonPropertyName("visibility")]
    public double?[]? Visibility { get; init; }
}

internal sealed record OpenMeteoWeatherMinutely
{
    [JsonPropertyName("time")]
    public long[] Time { get; init; } = [];

    [JsonPropertyName("precipitation")]
    public double?[]? Precipitation { get; init; }
}

internal sealed record OpenMeteoAirQualityResult
{
    [JsonPropertyName("hourly")]
    public OpenMeteoAirQualityHourly? Hourly { get; init; }
}

internal sealed record OpenMeteoAirQualityHourly
{
    [JsonPropertyName("time")]
    public long[] Time { get; init; } = [];

    [JsonPropertyName("pm10")]
    public double?[]? PM10 { get; init; }

    [JsonPropertyName("pm2_5")]
    public double?[]? PM25 { get; init; }

    [JsonPropertyName("carbon_monoxide")]
    public double?[]? CarbonMonoxide { get; init; }

    [JsonPropertyName("nitrogen_dioxide")]
    public double?[]? NitrogenDioxide { get; init; }

    [JsonPropertyName("sulphur_dioxide")]
    public double?[]? SulphurDioxide { get; init; }

    [JsonPropertyName("ozone")]
    public double?[]? Ozone { get; init; }

    [JsonPropertyName("alder_pollen")]
    public double?[]? AlderPollen { get; init; }

    [JsonPropertyName("birch_pollen")]
    public double?[]? BirchPollen { get; init; }

    [JsonPropertyName("grass_pollen")]
    public double?[]? GrassPollen { get; init; }

    [JsonPropertyName("mugwort_pollen")]
    public double?[]? MugwortPollen { get; init; }

    [JsonPropertyName("olive_pollen")]
    public double?[]? OlivePollen { get; init; }

    [JsonPropertyName("ragweed_pollen")]
    public double?[]? RagweedPollen { get; init; }
}

internal sealed record OpenMeteoLocationResults
{
    [JsonPropertyName("results")]
    public List<OpenMeteoLocationResult>? Results { get; init; }

    [JsonPropertyName("generationtime_ms")]
    public double? GenerationTimeMs { get; init; }
}

internal sealed record OpenMeteoLocationResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }

    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("admin1")]
    public string? Admin1 { get; init; }

    [JsonPropertyName("admin2")]
    public string? Admin2 { get; init; }

    [JsonPropertyName("admin3")]
    public string? Admin3 { get; init; }

    [JsonPropertyName("admin4")]
    public string? Admin4 { get; init; }
}
