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

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources.Geography;

namespace LibreccioWeather.Core.Sources.ClimWeb;

public abstract class ClimWebService : IWeatherSource, IHttpSource, IConfigurableSource, ILocationParametersSource
{
    private readonly HttpClient _httpClient;
    private readonly ConfigStore _config;
    private readonly string _countryCode;
    private readonly string _baseUrl;
    private readonly string _attribution;
    private readonly string? _cityClimatePageId;
    private readonly string _instancePreference;

    protected ClimWebService(HttpClient httpClient, ConfigStore config, string id, string countryCode, string name,
        string baseUrl, string attribution, string? cityClimatePageId, string instancePreference, bool omitRepeatedCountry = false)
    {
        _httpClient = httpClient;
        _config = config;
        Id = id;
        _countryCode = countryCode;
        _baseUrl = baseUrl;
        _attribution = attribution;
        _cityClimatePageId = cityClimatePageId;
        _instancePreference = instancePreference;
        string country = CountryNames.GetCountryName(countryCode);
        Name = omitRepeatedCountry && name.Contains(country, StringComparison.Ordinal) ? name : name + " (" + country + ")";
        SupportedFeatures = new Dictionary<SourceFeature, string>
        {
            [SourceFeature.Alert] = attribution,
            [SourceFeature.Normals] = attribution,
        };
        AttributionLinks = new Dictionary<string, string> { [attribution] = baseUrl };
    }

    public string Id { get; }
    public string Name { get; }
    public string PrivacyPolicyUrl => "";
    public SourceContinent Continent => SourceContinent.Africa;
    public bool IsConfigured => true;
    public bool IsRestricted => false;
    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; }
    public IReadOnlyDictionary<string, string> AttributionLinks { get; }

    private string Instance
    {
        get => _config.GetString("instance", null) ?? _baseUrl;
        set => _config.PutString("instance", value == _baseUrl || value.Length == 0 ? null : value);
    }

    public IReadOnlyList<SourcePreference> GetPreferences()
    {
        return [new SourcePreference(_instancePreference, Instance == _baseUrl ? "" : Instance, value => Instance = value, _baseUrl)];
    }

    public bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return string.Equals(location.CountryCode, _countryCode, StringComparison.OrdinalIgnoreCase)
            && (feature == SourceFeature.Alert || (feature == SourceFeature.Normals && !string.IsNullOrEmpty(_cityClimatePageId)));
    }

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return IsFeatureSupportedForLocation(location, feature) ? IFeatureSource.PriorityHighest : IFeatureSource.PriorityNone;
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(Location location, IReadOnlyCollection<SourceFeature> requestedFeatures,
        CancellationToken cancellationToken = default)
    {
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        bool alerts = requestedFeatures.Contains(SourceFeature.Alert);
        bool normals = requestedFeatures.Contains(SourceFeature.Normals) && !string.IsNullOrEmpty(_cityClimatePageId);
        Task<ClimWebAlertsResult> alertsTask = alerts
            ? RequestAsync("api/cap/alerts.geojson", SourceFeature.Alert, new ClimWebAlertsResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new ClimWebAlertsResult());
        string? cityId = location.Parameters.GetValueOrDefault(Id)?.GetValueOrDefault("cityId");
        if (normals && cityId is null)
        {
            lock (failedFeatures)
            {
                failedFeatures[SourceFeature.Normals] = new InvalidLocationException();
            }
        }

        Task<ClimWebNormals[]> normalsTask = normals && cityId is not null
            ? RequestAsync("api/cityclimate/data/" + Uri.EscapeDataString(_cityClimatePageId!) + "/?city_id=" + Uri.EscapeDataString(cityId),
                SourceFeature.Normals, Array.Empty<ClimWebNormals>(), failedFeatures, cancellationToken, recordFailure: false)
            : Task.FromResult(Array.Empty<ClimWebNormals>());
        await Task.WhenAll(alertsTask, normalsTask).ConfigureAwait(false);
        return new WeatherWrapper
        {
            AlertList = alerts ? GetAlertList(location, await alertsTask.ConfigureAwait(false)) : null,
            Normals = normals ? GetNormals(await normalsTask.ConfigureAwait(false)) : null,
            FailedFeatures = failedFeatures,
        };
    }

    private IReadOnlyList<Alert> GetAlertList(Location location, ClimWebAlertsResult result)
    {
        List<Alert> alerts = [];
        if (result.Features.ValueKind != JsonValueKind.Array)
        {
            return alerts;
        }

        foreach (JsonElement feature in result.Features.EnumerateArray())
        {
            if (!MatchesLocation(location, feature))
            {
                continue;
            }

            JsonElement properties = feature.TryGetProperty("properties", out JsonElement values) ? values : default;
            string? Property(string key) => properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(key, out JsonElement value)
                ? value.ValueKind == JsonValueKind.Null ? null : value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
                : null;
            AlertSeverity severity = Property("severity")?.ToLowerInvariant() switch
            {
                "extreme" => AlertSeverity.Extreme,
                "severe" => AlertSeverity.Severe,
                "moderate" => AlertSeverity.Moderate,
                "minor" => AlertSeverity.Minor,
                _ => AlertSeverity.Unknown,
            };
            alerts.Add(new Alert
            {
                AlertId = Property("id") ?? (Property("event") ?? "null") + " " + (Property("areaDesc") ?? "null") + " " + (Property("onset") ?? "null"),
                StartDate = ParseDate(Property("onset")),
                EndDate = ParseDate(Property("expires")),
                Headline = Trim(Property("headline")),
                Description = Trim(Property("description")),
                Instruction = Trim(Property("instruction")),
                Source = _attribution,
                Severity = severity,
                Color = Alert.ColorFromSeverity(severity),
            });
        }

        return alerts;
    }

    private static bool MatchesLocation(Location location, JsonElement feature)
    {
        try
        {
            if (feature.TryGetProperty("properties", out JsonElement properties)
                && properties.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
            {
                return false;
            }

            if (!feature.TryGetProperty("geometry", out JsonElement geometry) || geometry.ValueKind != JsonValueKind.Object
                || !geometry.TryGetProperty("type", out JsonElement type) || !geometry.TryGetProperty("coordinates", out JsonElement coordinates))
            {
                return false;
            }

            bool ContainsPolygon(JsonElement polygon)
            {
                foreach (JsonElement ring in polygon.EnumerateArray())
                {
                    List<SourceCoordinate> points = [];
                    foreach (JsonElement point in ring.EnumerateArray())
                    {
                        if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2)
                        {
                            return false;
                        }

                        double Number(JsonElement value) => value.ValueKind == JsonValueKind.String
                            ? double.Parse(value.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture) : value.GetDouble();
                        points.Add(new SourceCoordinate(Number(point[1]), Number(point[0])));
                    }

                    if (SourceGeometry.ContainsLocation(location.Latitude, location.Longitude, points, geodesic: true))
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
                _ => false,
            };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or IndexOutOfRangeException or FormatException)
        {
            return false;
        }
    }

    private static IReadOnlyDictionary<Month, Normals>? GetNormals(ClimWebNormals[] values)
    {
        if (values.Length == 0)
        {
            return null;
        }

        Dictionary<Month, Normals> normals = [];
        foreach (Month month in Enum.GetValues<Month>())
        {
            string pattern = "^[0-9]{4}-" + ((int)month).ToString("D2", CultureInfo.InvariantCulture) + "-[0-9]{2}\\z";
            ClimWebNormals? item = values.Where(value => value.Date is not null && Regex.IsMatch(value.Date, pattern, RegexOptions.CultureInvariant))
                .OrderBy(value => value.Date, StringComparer.Ordinal).LastOrDefault();
            if (item is not null)
            {
                normals[month] = new Normals
                {
                    DaytimeTemperature = Celsius(item.MaxTemp ?? item.MaximumTemperature ?? item.MeanMaximumTemperature ?? item.TemperatureMaximale),
                    NighttimeTemperature = Celsius(item.MinTemp ?? item.MinimumTemperature ?? item.MeanMinimumTemperature ?? item.TemperatureMinimale),
                };
            }
        }

        return normals;
    }

    public bool NeedsLocationParametersRefresh(Location location, bool coordinatesChanged, IReadOnlyCollection<SourceFeature> features)
    {
        IReadOnlyDictionary<string, string>? parameters = location.Parameters.GetValueOrDefault(Id);
        return features.Contains(SourceFeature.Normals)
            && (coordinatesChanged || string.IsNullOrEmpty(parameters?.GetValueOrDefault("cityId"))
                || string.IsNullOrEmpty(parameters?.GetValueOrDefault("citySlug")));
    }

    public async Task<IReadOnlyDictionary<string, string>> RequestLocationParametersAsync(Location location, CancellationToken cancellationToken = default)
    {
        ClimWebLocation[] cities = await _httpClient.GetFromJsonAsync<ClimWebLocation[]>(Instance + "api/cities?format=json", cancellationToken)
            .ConfigureAwait(false) ?? [];
        double nearestDistance = double.PositiveInfinity;
        string nearestCity = "";
        string nearestSlug = "";
        foreach (ClimWebLocation city in cities)
        {
            if (city.Coordinates is not { Length: >= 2 } coordinates || city.Id is null || city.Slug is null)
            {
                continue;
            }

            double distance = SourceGeometry.ComputeDistanceBetween(new SourceCoordinate(coordinates[1], coordinates[0]),
                new SourceCoordinate(location.Latitude, location.Longitude));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCity = city.Id;
                nearestSlug = city.Slug;
            }
        }

        return new Dictionary<string, string> { ["cityId"] = nearestCity, ["citySlug"] = nearestSlug };
    }

    private async Task<T> RequestAsync<T>(string query, SourceFeature feature, T empty, Dictionary<SourceFeature, Exception> failedFeatures,
        CancellationToken cancellationToken, bool recordFailure = true)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(Instance + query, cancellationToken).ConfigureAwait(false) ?? empty;
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            if (recordFailure)
            {
                lock (failedFeatures)
                {
                    failedFeatures[feature] = exception;
                }
            }

            return empty;
        }
    }

    private static DateTimeOffset? ParseDate(string? value) => value is null ? null
        : Icu.ParseDate(value, "yyyy-MM-dd'T'HH:mm:ssZ", TimeZoneInfo.Local, CultureInfo.GetCultureInfo("en"))
            ?? throw new JsonException("The alert date could not be parsed.");

    private static string? Trim(string? value)
    {
        if (value is null)
        {
            return null;
        }

        bool Whitespace(char character) => character is >= '\u0009' and <= '\u000D' or >= '\u001C' and <= '\u001F'
            || char.GetUnicodeCategory(character) is UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;
        int first = 0;
        int last = value.Length;
        while (first < last && Whitespace(value[first]))
        {
            first++;
        }

        while (last > first && Whitespace(value[last - 1]))
        {
            last--;
        }

        return value[first..last];
    }

    private static Units.Temperature? Celsius(double? value) => value is { } number ? Units.Temperature.FromCelsius(number) : null;
}
