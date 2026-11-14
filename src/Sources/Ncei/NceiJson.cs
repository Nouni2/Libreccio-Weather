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

namespace LibreccioWeather.Core.Sources.Ncei;

internal sealed record NceiDataResult
{
    [JsonPropertyName("DATE")]
    public required string Date { get; init; }
    [JsonPropertyName("STATION")]
    public required string Station { get; init; }
    [JsonPropertyName("TMAX")]
    public string? TMax { get; init; }
    [JsonPropertyName("TMIN")]
    public string? TMin { get; init; }
}

internal sealed record NceiStationsResult
{
    public NceiStationsResultResults[]? Results { get; init; }
}

internal sealed record NceiStationsResultResults
{
    public required NceiStationsResultResultsStation[] Stations { get; init; }
    public required double[] Centroid { get; init; }
}

internal sealed record NceiStationsResultResultsStation
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
