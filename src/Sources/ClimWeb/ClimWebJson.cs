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

namespace LibreccioWeather.Core.Sources.ClimWeb;

internal sealed record ClimWebAlertsResult
{
    public JsonElement Features { get; init; }
}

internal sealed record ClimWebLocation
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public double[]? Coordinates { get; init; }
    public string? Slug { get; init; }
}

internal sealed record ClimWebNormals
{
    public string? Date { get; init; }
    [JsonPropertyName("max-temp")]
    public double? MaxTemp { get; init; }
    [JsonPropertyName("maximum-temperature")]
    public double? MaximumTemperature { get; init; }
    [JsonPropertyName("mean-maximum-temperature")]
    public double? MeanMaximumTemperature { get; init; }
    [JsonPropertyName("temperature-maximale")]
    public double? TemperatureMaximale { get; init; }
    [JsonPropertyName("min-temp")]
    public double? MinTemp { get; init; }
    [JsonPropertyName("minimum-temperature")]
    public double? MinimumTemperature { get; init; }
    [JsonPropertyName("mean-minimum-temperature")]
    public double? MeanMinimumTemperature { get; init; }
    [JsonPropertyName("temperature-minimale")]
    public double? TemperatureMinimale { get; init; }
}
