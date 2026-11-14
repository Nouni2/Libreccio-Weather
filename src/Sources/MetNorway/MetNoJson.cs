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

namespace LibreccioWeather.Core.Sources.MetNorway;

internal sealed record MetNoForecastResult
{
    public MetNoForecastProperties? Properties { get; init; }
}

internal sealed record MetNoNowcastResult
{
    public MetNoNowcastProperties? Properties { get; init; }
}

internal sealed record MetNoForecastProperties
{
    public MetNoForecastPropertiesMeta? Meta { get; init; }
    public MetNoForecastTimeseries[]? Timeseries { get; init; }
}

internal sealed record MetNoForecastPropertiesMeta
{
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

internal sealed record MetNoNowcastProperties
{
    public MetNoForecastTimeseries[]? Timeseries { get; init; }
}

internal sealed record MetNoForecastTimeseries
{
    public required DateTimeOffset Time { get; init; }
    public MetNoForecastData? Data { get; init; }
}

internal sealed record MetNoForecastData
{
    public MetNoForecastDataInstant? Instant { get; init; }
    [JsonPropertyName("next_1_hours")]
    public MetNoForecastDataNextHours? Next1Hours { get; init; }
    [JsonPropertyName("next_6_hours")]
    public MetNoForecastDataNextHours? Next6Hours { get; init; }
    [JsonPropertyName("next_12_hours")]
    public MetNoForecastDataNextHours? Next12Hours { get; init; }
    [JsonIgnore]
    public string? SymbolCode => Next1Hours?.Summary?.SymbolCode ?? Next6Hours?.Summary?.SymbolCode ?? Next12Hours?.Summary?.SymbolCode;
}

internal sealed record MetNoForecastDataInstant
{
    public MetNoForecastDataDetails? Details { get; init; }
}

internal sealed record MetNoForecastDataNextHours
{
    public MetNoForecastDataSummary? Summary { get; init; }
    public MetNoForecastDataDetails? Details { get; init; }
}

internal sealed record MetNoForecastDataSummary
{
    [JsonPropertyName("symbol_code")]
    public string? SymbolCode { get; init; }
}

internal sealed record MetNoForecastDataDetails
{
    [JsonPropertyName("air_pressure_at_sea_level")]
    public double? AirPressureAtSeaLevel { get; init; }
    [JsonPropertyName("air_temperature")]
    public double? AirTemperature { get; init; }
    [JsonPropertyName("dew_point_temperature")]
    public double? DewPointTemperature { get; init; }
    [JsonPropertyName("precipitation_rate")]
    public double? PrecipitationRate { get; init; }
    [JsonPropertyName("precipitation_amount")]
    public double? PrecipitationAmount { get; init; }
    [JsonPropertyName("probability_of_precipitation")]
    public double? ProbabilityOfPrecipitation { get; init; }
    [JsonPropertyName("probability_of_thunder")]
    public double? ProbabilityOfThunder { get; init; }
    [JsonPropertyName("relative_humidity")]
    public double? RelativeHumidity { get; init; }
    [JsonPropertyName("ultraviolet_index_clear_sky")]
    public double? UltravioletIndexClearSky { get; init; }
    [JsonPropertyName("wind_from_direction")]
    public double? WindFromDirection { get; init; }
    [JsonPropertyName("wind_speed")]
    public double? WindSpeed { get; init; }
    [JsonPropertyName("wind_speed_of_gust")]
    public double? WindSpeedOfGust { get; init; }
    [JsonPropertyName("cloud_area_fraction")]
    public double? CloudAreaFraction { get; init; }
}

internal sealed record MetNoAirQualityResult
{
    public MetNoAirQualityData? Data { get; init; }
}

internal sealed record MetNoAirQualityData
{
    public MetNoAirQualityTime[]? Time { get; init; }
}

internal sealed record MetNoAirQualityTime
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public MetNoAirQualityVariables? Variables { get; init; }
}

internal sealed record MetNoAirQualityVariables
{
    [JsonPropertyName("no2_concentration")]
    public MetNoAirQualityConcentration? NO2Concentration { get; init; }
    [JsonPropertyName("pm10_concentration")]
    public MetNoAirQualityConcentration? PM10Concentration { get; init; }
    [JsonPropertyName("pm25_concentration")]
    public MetNoAirQualityConcentration? PM25Concentration { get; init; }
    [JsonPropertyName("o3_concentration")]
    public MetNoAirQualityConcentration? O3Concentration { get; init; }
    [JsonPropertyName("so2_concentration")]
    public MetNoAirQualityConcentration? SO2Concentration { get; init; }
}

internal sealed record MetNoAirQualityConcentration
{
    public double? Value { get; init; }
}

internal sealed record MetNoAlertResult
{
    public MetNoAlert[]? Features { get; init; }
}

internal sealed record MetNoAlert
{
    public MetNoAlertProperties? Properties { get; init; }
    [JsonPropertyName("when")]
    public MetNoAlertWhen? WhenAlert { get; init; }
}

internal sealed record MetNoAlertProperties
{
    public required string Id { get; init; }
    public string? EventAwarenessName { get; init; }
    public string? Description { get; init; }
    public string? Instruction { get; init; }
    public string? Severity { get; init; }
    public DateTimeOffset? EventEndingTime { get; init; }
}

internal sealed record MetNoAlertWhen
{
    public DateTimeOffset[]? Interval { get; init; }
}
