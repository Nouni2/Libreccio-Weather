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

namespace LibreccioWeather.Core.Sources.Nws;

internal sealed record NwsGridPointResult
{
    public NwsGridPointProperties? Properties { get; init; }
}

internal sealed record NwsGridPointProperties
{
    public NwsValueDoubleContainer? Temperature { get; init; }
    public NwsValueDoubleContainer? Dewpoint { get; init; }
    public NwsValueIntContainer? RelativeHumidity { get; init; }
    public NwsValueDoubleContainer? ApparentTemperature { get; init; }
    public NwsValueDoubleContainer? WetBulbGlobeTemperature { get; init; }
    public NwsValueDoubleContainer? HeatIndex { get; init; }
    public NwsValueDoubleContainer? WindChill { get; init; }
    public NwsValueIntContainer? SkyCover { get; init; }
    public NwsValueIntContainer? WindDirection { get; init; }
    public NwsValueDoubleContainer? WindSpeed { get; init; }
    public NwsValueDoubleContainer? WindGust { get; init; }
    public NwsValueWeatherContainer? Weather { get; init; }
    public NwsValueIntContainer? ProbabilityOfPrecipitation { get; init; }
    public NwsValueDoubleContainer? QuantitativePrecipitation { get; init; }
    public NwsValueDoubleContainer? IceAccumulation { get; init; }
    public NwsValueDoubleContainer? SnowfallAmount { get; init; }
    public NwsValueDoubleContainer? CeilingHeight { get; init; }
    public NwsValueDoubleContainer? Visibility { get; init; }
    public NwsValueDoubleContainer? Pressure { get; init; }
    public NwsValueDoubleContainer? ProbabilityOfThunder { get; init; }
}

internal sealed record NwsValueDoubleContainer
{
    public NwsValueDouble[]? Values { get; init; }
}

internal sealed record NwsValueDouble
{
    public required string ValidTime { get; init; }
    public double? Value { get; init; }
}

internal sealed record NwsValueIntContainer
{
    public NwsValueInt[]? Values { get; init; }
}

internal sealed record NwsValueInt
{
    public required string ValidTime { get; init; }
    public int? Value { get; init; }
}

internal sealed record NwsValueWeatherContainer
{
    public NwsValueWeather[]? Values { get; init; }
}

internal sealed record NwsValueWeather
{
    public required string ValidTime { get; init; }
    public NwsValueWeatherValue[]? Value { get; init; }
}

internal sealed record NwsValueWeatherValue
{
    public string? Coverage { get; init; }
    public string? Weather { get; init; }
    public string? Intensity { get; init; }
    public string[]? Attributes { get; init; }
}

internal sealed record NwsDailyResult
{
    public NwsDailyProperties? Properties { get; init; }
}

internal sealed record NwsDailyProperties
{
    public NwsDailyPeriods[]? Periods { get; init; }
}

internal sealed record NwsDailyPeriods
{
    public required DateTimeOffset StartTime { get; init; }
    public required bool IsDaytime { get; init; }
    public NwsCurrentValue? Temperature { get; init; }
    public NwsCurrentValue? ProbabilityOfPrecipitation { get; init; }
    public NwsCurrentValue? WindSpeed { get; init; }
    public NwsCurrentValue? WindGust { get; init; }
    public string? WindDirection { get; init; }
    public string? Icon { get; init; }
    public string? ShortForecast { get; init; }
}

internal sealed record NwsCurrentResult
{
    public NwsPointLocationGeometry? Geometry { get; init; }
    public NwsCurrentProperties? Properties { get; init; }
}

internal sealed record NwsCurrentProperties
{
    public NwsCurrentValue? Elevation { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
    public string? TextDescription { get; init; }
    public string? Icon { get; init; }
    public NwsCurrentValue? Temperature { get; init; }
    public NwsCurrentValue? Dewpoint { get; init; }
    public NwsCurrentValue? WindDirection { get; init; }
    public NwsCurrentValue? WindSpeed { get; init; }
    public NwsCurrentValue? WindGust { get; init; }
    public NwsCurrentValue? BarometricPressure { get; init; }
    public NwsCurrentValue? SeaLevelPressure { get; init; }
    public NwsCurrentValue? Visibility { get; init; }
    public NwsCurrentValue? RelativeHumidity { get; init; }
    public NwsCurrentValue? WindChill { get; init; }
}

internal sealed record NwsCurrentValue
{
    public double? Value { get; init; }
    public double? MaxValue { get; init; }
}

internal sealed record NwsAlertsResult
{
    public NwsAlert[]? Features { get; init; }
}

internal sealed record NwsAlert
{
    public NwsAlertProperties? Properties { get; init; }
}

internal sealed record NwsAlertProperties
{
    public required string Id { get; init; }
    public DateTimeOffset? Effective { get; init; }
    public DateTimeOffset? Onset { get; init; }
    public DateTimeOffset? Expires { get; init; }
    public DateTimeOffset? Ends { get; init; }
    public string? Status { get; init; }
    public string? Severity { get; init; }
    public string? Event { get; init; }
    public string? Headline { get; init; }
    public string? Description { get; init; }
    public string? Instruction { get; init; }
    public string? Sender { get; init; }
    public string? SenderName { get; init; }
}

internal sealed record NwsPointResult
{
    public NwsPointProperties? Properties { get; init; }
}

internal sealed record NwsPointProperties
{
    public required string GridId { get; init; }
    public required int GridX { get; init; }
    public required int GridY { get; init; }
    public NwsPointLocation? RelativeLocation { get; init; }
    public string? ForecastZone { get; init; }
    public required string TimeZone { get; init; }
}

internal sealed record NwsPointLocation
{
    public NwsPointLocationProperties? Properties { get; init; }
    public NwsPointLocationGeometry? Geometry { get; init; }
}

internal sealed record NwsPointLocationProperties
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? StationIdentifier { get; init; }
}

internal sealed record NwsPointLocationGeometry
{
    public double[]? Coordinates { get; init; }
}

internal sealed record NwsStationsResult
{
    public NwsPointLocation[]? Features { get; init; }
}
