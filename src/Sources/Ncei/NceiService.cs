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
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources.Geography;

namespace LibreccioWeather.Core.Sources.Ncei;

public sealed partial class NceiService(HttpClient httpClient) : IWeatherSource, IHttpSource, ILocationParametersSource
{
    public const string Id = "ncei";
    public const int DistanceLimit = 60000;
    private const string BaseUrl = "https://www.ncei.noaa.gov/";
    private const int EarthPolarRadius = 6356752;
    private const int EarthEquatorialRadius = 6378137;
    private const string Attribution = "National Centers for Environmental Information";
    private const string Dataset = "global-summary-of-the-month";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] PriorityCountries = ["US", "PR", "VI", "MP", "GU"];

    string ISource.Id => Id;
    public string Name => "NCEI";
    public string PrivacyPolicyUrl => "https://www.ncei.noaa.gov/privacy";
    public SourceContinent Continent => SourceContinent.Worldwide;
    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
    {
        [SourceFeature.Normals] = Attribution,
    };
    public IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        [Attribution] = "https://www.ncei.noaa.gov/",
    };

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return PriorityCountries.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase)
            ? IFeatureSource.PriorityHighest : IFeatureSource.PriorityNone;
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(Location location, IReadOnlyCollection<SourceFeature> requestedFeatures,
        CancellationToken cancellationToken = default)
    {
        (int initialYear, int finalYear) = Years(location);
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        string encodedStations = location.Parameters.GetValueOrDefault(Id)?.GetValueOrDefault("stations") ?? "";
        Dictionary<string, double> stationMap = JsonSerializer.Deserialize<Dictionary<string, double>>(encodedStations)
            ?? throw new JsonException("The station map is null.");
        string stations = string.Join(',', stationMap.Keys);
        NceiDataResult[] normals = [];
        if (stations.Length > 0)
        {
            try
            {
                normals = await RequestAsync<NceiDataResult[]>("access/services/data/v1?dataset=" + Dataset
                    + "&dataTypes=TMAX%2CTMIN&stations=" + Uri.EscapeDataString(stations)
                    + "&startDate=" + initialYear.ToString(CultureInfo.InvariantCulture) + "-01-01"
                    + "&endDate=" + finalYear.ToString(CultureInfo.InvariantCulture) + "-12-31"
                    + "&includeAttributes=true&includeStationName=true&includeStationLocation=true&units=metric&format=json",
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                failedFeatures[SourceFeature.Normals] = exception;
            }
        }

        return new WeatherWrapper { Normals = GetNormals(normals, stationMap), FailedFeatures = failedFeatures };
    }

    private static IReadOnlyDictionary<Month, Normals> GetNormals(IReadOnlyList<NceiDataResult> normalsList,
        Dictionary<string, double> stationMap)
    {
        Dictionary<string, double> stationWeights = stationMap.ToDictionary(station => station.Key, station => GetWeight(station.Value));
        Dictionary<Month, Normals> result = [];
        foreach (Month month in Enum.GetValues<Month>())
        {
            string monthEnding = "-" + ((int)month).ToString("D2", CultureInfo.InvariantCulture);
            double tMaxWeightedSum = 0.0;
            double tMaxWeightTotal = 0.0;
            double tMinWeightedSum = 0.0;
            double tMinWeightTotal = 0.0;
            foreach (NceiDataResult item in normalsList)
            {
                if (!item.Date.EndsWith(monthEnding, StringComparison.Ordinal) || !stationWeights.TryGetValue(item.Station, out double weight))
                {
                    continue;
                }

                if (ParseTemperature(item.TMax) is { } tMax)
                {
                    tMaxWeightedSum += tMax * weight;
                    tMaxWeightTotal += weight;
                }

                if (ParseTemperature(item.TMin) is { } tMin)
                {
                    tMinWeightedSum += tMin * weight;
                    tMinWeightTotal += weight;
                }
            }

            result[month] = new Normals
            {
                DaytimeTemperature = tMaxWeightTotal > 0 ? Units.Temperature.FromCelsius(tMaxWeightedSum / tMaxWeightTotal) : null,
                NighttimeTemperature = tMinWeightTotal > 0 ? Units.Temperature.FromCelsius(tMinWeightedSum / tMinWeightTotal) : null,
            };
        }

        return result;
    }

    private static double GetWeight(double distance)
    {
        double sigmaDistance = DistanceLimit / 3.0;
        double x = distance / sigmaDistance;
        return 1.0 / Math.Sqrt(2.0 * Math.PI) * Math.Exp(-Math.Pow(x, 2.0) / 2.0);
    }

    public bool NeedsLocationParametersRefresh(Location location, bool coordinatesChanged, IReadOnlyCollection<SourceFeature> features)
    {
        return coordinatesChanged || string.IsNullOrEmpty(location.Parameters.GetValueOrDefault(Id)?.GetValueOrDefault("stations"));
    }

    public async Task<IReadOnlyDictionary<string, string>> RequestLocationParametersAsync(Location location,
        CancellationToken cancellationToken = default)
    {
        double north = Math.Min(location.Latitude + DistanceLimit / (2 * Math.PI * EarthPolarRadius) * 360, 90.0);
        double south = Math.Max(location.Latitude - DistanceLimit / (2 * Math.PI * EarthPolarRadius) * 360, -90.0);
        double multiple = Math.Cos(location.Latitude / 180 * Math.PI);
        double east = multiple != 0.0
            ? Math.Min(location.Longitude + DistanceLimit / (2 * Math.PI * EarthEquatorialRadius * multiple) * 360, 180.0) : 180.0;
        double west = multiple != 0.0
            ? Math.Max(location.Longitude - DistanceLimit / (2 * Math.PI * EarthEquatorialRadius * multiple) * 360, -180.0) : -180.0;
        string bbox = string.Join(',', new[] { north, west, south, east }.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        (int initialYear, int finalYear) = Years(location);
        NceiStationsResult stations = await RequestAsync<NceiStationsResult>("access/services/search/v1/data?dataTypes=TMAX%2CTMIN&bbox="
            + Uri.EscapeDataString(bbox) + "&startDate=" + initialYear.ToString(CultureInfo.InvariantCulture) + "-01-01T00%3A00%3A00"
            + "&endDate=" + finalYear.ToString(CultureInfo.InvariantCulture) + "-12-31T23%3A59%3A59&dataset=" + Dataset + "&limit=10&offset=0",
            cancellationToken).ConfigureAwait(false);
        Dictionary<string, double> stationMap = [];
        foreach (NceiStationsResultResults result in stations.Results ?? [])
        {
            if (result.Stations.FirstOrDefault() is { } station)
            {
                stationMap[station.Id] = SourceGeometry.ComputeDistanceBetween(
                    new SourceCoordinate(location.Latitude, location.Longitude),
                    new SourceCoordinate(result.Centroid[1], result.Centroid[0]));
            }
        }

        return new Dictionary<string, string> { ["stations"] = JsonSerializer.Serialize(stationMap) };
    }

    private static (int Initial, int Final) Years(Location location)
    {
        int year = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Year;
        int finalYear = year / 10 * 10;
        return (finalYear - 29, finalYear);
    }

    private async Task<T> RequestAsync<T>(string path, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(BaseUrl + path, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("The weather source returned an empty response.");
    }
}
