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

namespace LibreccioWeather.Core.Sources.Geography;

internal static class GeoJsonFeatures
{
    internal static JsonElement[] Load(string resourceName)
    {
        using Stream stream = typeof(GeoJsonFeatures).Assembly.GetManifestResourceStream(
            "LibreccioWeather.Core.resources.geography." + resourceName)
            ?? throw new InvalidOperationException("The geographic resource is missing: " + resourceName);
        using JsonDocument document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty("features").EnumerateArray().Select(feature => feature.Clone()).ToArray();
    }

    internal static string? Property(JsonElement feature, string name)
    {
        if (!feature.TryGetProperty("properties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Object
            || !properties.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
    }

    internal static bool Matches(JsonElement feature, double latitude, double longitude, bool matchPoints = false)
    {
        if (!feature.TryGetProperty("geometry", out JsonElement geometry) || geometry.ValueKind != JsonValueKind.Object
            || !geometry.TryGetProperty("type", out JsonElement type) || !geometry.TryGetProperty("coordinates", out JsonElement coordinates))
        {
            return false;
        }

        bool ContainsPolygon(JsonElement polygon)
        {
            foreach (JsonElement ring in polygon.EnumerateArray())
            {
                SourceCoordinate[] points = ring.EnumerateArray()
                    .Select(point => new SourceCoordinate(point[1].GetDouble(), point[0].GetDouble())).ToArray();
                if (SourceGeometry.ContainsLocation(latitude, longitude, points, geodesic: true))
                {
                    return true;
                }
            }

            return false;
        }

        return type.GetString() switch
        {
            "Polygon" => ContainsPolygon(coordinates),
            "MultiPolygon" => coordinates.EnumerateArray().Any(ContainsPolygon),
            "Point" when matchPoints => SourceGeometry.ComputeDistanceBetween(new SourceCoordinate(latitude, longitude),
                new SourceCoordinate(coordinates[1].GetDouble(), coordinates[0].GetDouble())) < 50000,
            _ => false,
        };
    }
}
