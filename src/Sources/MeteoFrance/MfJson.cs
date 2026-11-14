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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibreccioWeather.Core.Sources.MeteoFrance;

internal sealed record MfCurrentResult
{
    public MfCurrentProperties? Properties { get; init; }
}

internal sealed record MfCurrentProperties
{
    public MfCurrentGridded? Gridded { get; init; }
}

internal sealed record MfCurrentGridded
{
    [JsonPropertyName("T")]
    public double? Temperature { get; init; }
    [JsonPropertyName("wind_speed")]
    public double? WindSpeed { get; init; }
    [JsonPropertyName("wind_direction")]
    public int? WindDirection { get; init; }
    [JsonPropertyName("wind_icon")]
    public string? WindIcon { get; init; }
    [JsonPropertyName("weather_icon")]
    public string? WeatherIcon { get; init; }
    [JsonPropertyName("weather_description")]
    public string? WeatherDescription { get; init; }
}

internal sealed record MfForecastResult
{
    public MfForecastProperties? Properties { get; init; }
    public MfGeometry? Geometry { get; init; }
    [JsonPropertyName("update_time")]
    public DateTimeOffset? UpdateTime { get; init; }
}

internal sealed record MfGeometry
{
    public double[]? Coordinates { get; init; }
}

internal sealed record MfForecastProperties
{
    public required string Country { get; init; }
    public required string Name { get; init; }
    public required string Timezone { get; init; }
    [JsonPropertyName("french_department")]
    public string? FrenchDepartment { get; init; }
    [JsonPropertyName("zone_vigi1")]
    public string? ZoneVigi1 { get; init; }
    [JsonPropertyName("daily_forecast")]
    public MfForecastDaily[]? DailyForecast { get; init; }
    public MfForecastHourly[]? Forecast { get; init; }
    [JsonPropertyName("probability_forecast")]
    public MfForecastProbability[]? ProbabilityForecast { get; init; }
}

internal sealed record MfForecastDaily
{
    public required DateTimeOffset Time { get; init; }
    [JsonPropertyName("T_min")]
    public double? TMin { get; init; }
    [JsonPropertyName("T_max")]
    public double? TMax { get; init; }
    [JsonPropertyName("relative_humidity_min")]
    public int? RelativeHumidityMin { get; init; }
    [JsonPropertyName("relative_humidity_max")]
    public int? RelativeHumidityMax { get; init; }
    [JsonPropertyName("daily_weather_icon")]
    public string? DailyWeatherIcon { get; init; }
    [JsonPropertyName("daily_weather_description")]
    public string? DailyWeatherDescription { get; init; }
    [JsonPropertyName("uv_index")]
    public int? UVIndex { get; init; }
    [JsonPropertyName("sunrise_time")]
    public DateTimeOffset? SunriseTime { get; init; }
    [JsonPropertyName("sunset_time")]
    public DateTimeOffset? SunsetTime { get; init; }
}

internal sealed record MfForecastHourly
{
    public required DateTimeOffset Time { get; init; }
    [JsonPropertyName("T")]
    public double? T { get; init; }
    [JsonPropertyName("T_windchill")]
    public double? TWindchill { get; init; }
    [JsonPropertyName("weather_icon")]
    public string? WeatherIcon { get; init; }
    [JsonPropertyName("weather_description")]
    public string? WeatherDescription { get; init; }
    [JsonPropertyName("wind_direction")]
    public int? WindDirection { get; init; }
    [JsonPropertyName("wind_speed")]
    public int? WindSpeed { get; init; }
    [JsonPropertyName("wind_icon")]
    public string? WindIcon { get; init; }
    [JsonPropertyName("wind_speed_gust")]
    public int? WindSpeedGust { get; init; }
    [JsonPropertyName("rain_1h")]
    public double? Rain1h { get; init; }
    [JsonPropertyName("rain_3h")]
    public double? Rain3h { get; init; }
    [JsonPropertyName("rain_6h")]
    public double? Rain6h { get; init; }
    [JsonPropertyName("rain_12h")]
    public double? Rain12h { get; init; }
    [JsonPropertyName("rain_24h")]
    public double? Rain24h { get; init; }
    [JsonPropertyName("snow_1h")]
    public double? Snow1h { get; init; }
    [JsonPropertyName("snow_3h")]
    public double? Snow3h { get; init; }
    [JsonPropertyName("snow_6h")]
    public double? Snow6h { get; init; }
    [JsonPropertyName("snow_12h")]
    public double? Snow12h { get; init; }
    [JsonPropertyName("snow_24h")]
    public double? Snow24h { get; init; }
    [JsonPropertyName("relative_humidity")]
    public int? RelativeHumidity { get; init; }
    [JsonPropertyName("P_sea")]
    public double? PSea { get; init; }
    [JsonPropertyName("total_cloud_cover")]
    public int? TotalCloudCover { get; init; }
}

internal sealed record MfForecastProbability
{
    public required DateTimeOffset Time { get; init; }
    [JsonPropertyName("rain_hazard_3h")]
    public int? RainHazard3h { get; init; }
    [JsonPropertyName("rain_hazard_6h")]
    public int? RainHazard6h { get; init; }
    [JsonPropertyName("snow_hazard_3h")]
    public int? SnowHazard3h { get; init; }
    [JsonPropertyName("snow_hazard_6h")]
    public int? SnowHazard6h { get; init; }
    [JsonPropertyName("freezing_hazard")]
    public int? FreezingHazard { get; init; }
}

internal sealed record MfRainResult
{
    [JsonPropertyName("update_time")]
    public DateTimeOffset? UpdateTime { get; init; }
    public MfRainProperties? Properties { get; init; }
}

internal sealed record MfRainProperties
{
    [JsonPropertyName("forecast")]
    public MfRainForecast[]? RainForecasts { get; init; }
}

internal sealed record MfRainForecast
{
    public required DateTimeOffset Time { get; init; }
    [JsonPropertyName("rain_intensity")]
    public int? RainIntensity { get; init; }
}

internal sealed record MfNormalsResult
{
    public MfGeometry? Geometry { get; init; }
    public MfNormalsProperties? Properties { get; init; }
}

internal sealed record MfNormalsProperties
{
    public MfNormalsStats[]? Stats { get; init; }
}

internal sealed record MfNormalsStats
{
    public string? Period { get; init; }
    [JsonPropertyName("T_min")]
    public double? TMin { get; init; }
    [JsonPropertyName("T_max")]
    public double? TMax { get; init; }
}

internal sealed record MfWarningsResult
{
    [JsonPropertyName("update_time")]
    public DateTimeOffset? UpdateTime { get; init; }
    [JsonPropertyName("end_validity_time")]
    public DateTimeOffset? EndValidityTime { get; init; }
    public MfWarningTimelaps[]? Timelaps { get; init; }
    public MfWarningAdvice[]? Advices { get; init; }
    public MfWarningConsequence[]? Consequences { get; init; }
    public MfWarningComments? Comments { get; init; }
    public MfWarningComments? Text { get; init; }
    [JsonPropertyName("text_avalanche")]
    public MfWarningComments? TextAvalanche { get; init; }
}

internal sealed record MfWarningTimelaps
{
    [JsonPropertyName("phenomenon_id")]
    public required string PhenomenonId { get; init; }
    [JsonPropertyName("timelaps_items")]
    public MfWarningTimelapsItem[]? TimelapsItems { get; init; }
}

internal sealed record MfWarningTimelapsItem
{
    [JsonPropertyName("begin_time")]
    public required DateTimeOffset BeginTime { get; init; }
    [JsonPropertyName("end_time")]
    public DateTimeOffset? EndTime { get; init; }
    [JsonPropertyName("color_id")]
    public required int ColorId { get; init; }
}

internal sealed record MfWarningAdvice
{
    [JsonPropertyName("phenomenon_id")]
    public required string PhenomenonId { get; init; }
    [JsonPropertyName("text_advice")]
    public string? TextAdvice { get; init; }
}

internal sealed record MfWarningConsequence
{
    [JsonPropertyName("phenomenon_id")]
    public required string PhenomenonId { get; init; }
    [JsonPropertyName("text_consequence")]
    public string? TextConsequence { get; init; }
}

internal sealed record MfWarningComments
{
    [JsonPropertyName("bloc_title")]
    public string? BlocTitle { get; init; }
    [JsonPropertyName("text_bloc_item")]
    public MfWarningTextBlocItem[]? TextBlocItems { get; init; }
}

internal sealed record MfWarningTextBlocItem
{
    [JsonPropertyName("type_name")]
    public string? TypeName { get; init; }
    [JsonPropertyName("text_items")]
    public MfWarningTextItem[]? TextItems { get; init; }
}

internal sealed record MfWarningTextItem
{
    [JsonPropertyName("hazard_code")]
    public string? HazardCode { get; init; }
    [JsonPropertyName("term_items")]
    public MfWarningTermItem[]? TermItems { get; init; }
}

internal sealed record MfWarningTermItem
{
    [JsonPropertyName("risk_name")]
    public string? RiskName { get; init; }
    [JsonPropertyName("subdivision_text")]
    public MfWarningSubdivisionText[]? SubdivisionTexts { get; init; }
}

internal sealed record MfWarningSubdivisionText
{
    [JsonPropertyName("underline_text")]
    public string? UnderlineText { get; init; }
    [JsonPropertyName("bold_text")]
    public string? BoldText { get; init; }
    public string[]? Text { get; init; }
}

internal sealed record MfWarningDictionaryResult
{
    public MfWarningDictionaryPhenomenon[]? Phenomenons { get; init; }
    public MfWarningDictionaryColor[]? Colors { get; init; }
}

internal sealed record MfWarningDictionaryPhenomenon
{
    public int? Id { get; init; }
    public string? Name { get; init; }
}

internal sealed record MfWarningDictionaryColor
{
    public int? Id { get; init; }
    public string? Name { get; init; }
    public string? HexaCode { get; init; }
}

internal sealed record MfWarningsOverseasResult
{
    [JsonPropertyName("update_time")]
    public DateTimeOffset? UpdateTime { get; init; }
    [JsonPropertyName("end_validity_time")]
    public DateTimeOffset? EndValidityTime { get; init; }
    [JsonPropertyName("color_max")]
    public int? ColorMax { get; init; }
    public MfWarningOverseasTimelaps[]? Timelaps { get; init; }
    public MfWarningOverseasAdvice[]? Advices { get; init; }
    public MfWarningOverseasConsequence[]? Consequences { get; init; }
    public MfWarningOverseasComments? Comments { get; init; }
    public MfWarningOverseasComments? Text { get; init; }
    [JsonPropertyName("text_avalanche")]
    public MfWarningOverseasComments? TextAvalanche { get; init; }
}

internal sealed record MfWarningOverseasTimelaps
{
    [JsonPropertyName("phenomenon_id")]
    public required int PhenomenonId { get; init; }
    [JsonPropertyName("timelaps_items")]
    public MfWarningTimelapsItem[]? TimelapsItems { get; init; }
}

internal sealed record MfWarningOverseasAdvice
{
    [JsonPropertyName("phenomenon_id")]
    public required int PhenomenonId { get; init; }
    [JsonPropertyName("text_advice")]
    public string? TextAdvice { get; init; }
}

internal sealed record MfWarningOverseasConsequence
{
    [JsonPropertyName("phenomenon_id")]
    public required int PhenomenonId { get; init; }
    [JsonPropertyName("text_consequence")]
    public string? TextConsequence { get; init; }
}

internal sealed record MfWarningOverseasComments
{
    [JsonPropertyName("begin_time")]
    public DateTimeOffset? BeginTime { get; init; }
    [JsonPropertyName("end_time")]
    public DateTimeOffset? EndTime { get; init; }
    [JsonPropertyName("text_bloc_item")]
    public MfWarningOverseasTextBlocItem[]? TextBlocItems { get; init; }
}

internal sealed record MfWarningOverseasTextBlocItem
{
    [JsonConverter(typeof(MfStringListConverter))]
    public string[]? Title { get; init; }
    [JsonConverter(typeof(MfStringListConverter))]
    public string[]? Text { get; init; }
}

internal sealed class MfStringListConverter : JsonConverter<string[]>
{
    public override string[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement element = document.RootElement;
        if (element.ValueKind == JsonValueKind.String)
        {
            return [element.GetString()!];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a string or an array.");
        }

        return element.EnumerateArray().Select(item => item.ValueKind switch
        {
            JsonValueKind.String => item.GetString()!,
            JsonValueKind.Array or JsonValueKind.Object => "",
            _ => item.GetRawText(),
        }).ToArray();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
