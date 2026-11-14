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
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources.Serialization;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.Nws;

public sealed partial class NwsService(HttpClient httpClient) : IWeatherSource, IHttpSource, ILocationParametersSource, IReverseGeocodingSource
{
    public const string Id = "nws";
    private const string BaseUrl = "https://api.weather.gov/";
    private const int OutdatedHours = 2;
    private const string Attribution = "National Weather Service (NWS)";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new SourceDateConverter() },
    };
    private static readonly string[] Countries = ["US", "GU", "MP", "PR", "VI"];
    private static readonly string UserAgent = "Libreccio-Weather/" + typeof(NwsService).Assembly.GetName().Version
        + " (+https://github.com/Nouni2/Libreccio-Weather/issues)";

    string ISource.Id => Id;
    public string Name => "NWS (" + CountryNames.GetCountryName("US") + ")";
    public string PrivacyPolicyUrl => "https://www.weather.gov/privacy";
    public SourceContinent Continent => SourceContinent.NorthAmerica;
    public IReadOnlyList<string>? KnownAmbiguousCountryCodes => [];
    public IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        [Attribution] = "https://www.weather.gov/",
    };
    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
    {
        [SourceFeature.Forecast] = Attribution,
        [SourceFeature.Current] = Attribution,
        [SourceFeature.Alert] = Attribution,
        [SourceFeature.ReverseGeocoding] = Attribution,
    };

    public bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return Countries.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase);
    }

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return IsFeatureSupportedForLocation(location, feature) ? IFeatureSource.PriorityHighest : IFeatureSource.PriorityNone;
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(Location location,
        IReadOnlyCollection<SourceFeature> requestedFeatures, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string>? parameters = location.Parameters.GetValueOrDefault(Id);
        string? gridId = parameters?.GetValueOrDefault("gridId");
        string? gridX = parameters?.GetValueOrDefault("gridX");
        string? gridY = parameters?.GetValueOrDefault("gridY");
        string? station = parameters?.GetValueOrDefault("station");
        bool forecast = requestedFeatures.Contains(SourceFeature.Forecast);
        bool current = requestedFeatures.Contains(SourceFeature.Current);
        bool alert = requestedFeatures.Contains(SourceFeature.Alert);
        if (string.IsNullOrEmpty(gridId) || string.IsNullOrEmpty(gridX) || string.IsNullOrEmpty(gridY)
            || (current && string.IsNullOrEmpty(station)))
        {
            throw new InvalidLocationException();
        }

        Dictionary<SourceFeature, Exception> failedFeatures = [];
        string gridPath = forecast ? GridPath(gridId, int.Parse(gridX, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
            int.Parse(gridY, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)) : "";
        Task<NwsGridPointResult> forecastTask = forecast
            ? RequestFeatureAsync(gridPath, SourceFeature.Forecast, new NwsGridPointResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new NwsGridPointResult());
        Task<NwsDailyResult> dailyTask = forecast
            ? RequestFeatureAsync(gridPath + "/forecast", SourceFeature.Forecast, new NwsDailyResult(), failedFeatures,
                cancellationToken, daily: true)
            : Task.FromResult(new NwsDailyResult());
        Task<NwsCurrentResult> currentTask = current
            ? RequestFeatureAsync("stations/" + Uri.EscapeDataString(station!) + "/observations/latest", SourceFeature.Current,
                new NwsCurrentResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new NwsCurrentResult());
        Task<NwsAlertsResult> alertsTask = alert
            ? RequestFeatureAsync("alerts/active?point=" + Uri.EscapeDataString(Coordinates(location.Latitude, location.Longitude)),
                SourceFeature.Alert, new NwsAlertsResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new NwsAlertsResult());
        await Task.WhenAll(forecastTask, dailyTask, currentTask, alertsTask).ConfigureAwait(false);

        NwsCurrentResult currentResult = await currentTask.ConfigureAwait(false);
        CurrentWrapper? currentWrapper = null;
        if (current)
        {
            // Some stations label local observation times as UTC; the standard offset gives them the same grace period.
            if (currentResult.Properties?.Timestamp is { } timestamp
                && timestamp.ToUnixTimeMilliseconds() < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    - OutdatedHours * 3_600_000L + (long)location.TimeZone.BaseUtcOffset.TotalMilliseconds)
            {
                failedFeatures[SourceFeature.Current] = new OutdatedServerDataException();
            }
            else
            {
                currentWrapper = GetCurrent(currentResult);
            }
        }

        return new WeatherWrapper
        {
            DailyForecast = forecast ? GetDailyForecast(location, await dailyTask.ConfigureAwait(false)) : null,
            HourlyForecast = forecast ? GetHourlyForecast((await forecastTask.ConfigureAwait(false)).Properties) : null,
            Current = currentWrapper,
            AlertList = alert ? GetAlerts((await alertsTask.ConfigureAwait(false)).Features) : null,
            FailedFeatures = failedFeatures,
        };
    }

    private static CurrentWrapper? GetCurrent(NwsCurrentResult currentResult)
    {
        if (currentResult.Properties is not { } properties)
        {
            return null;
        }

        double? computedPressure = properties.SeaLevelPressure is null
            ? PressureCalculations.ComputeMeanSeaLevelPressure(properties.BarometricPressure?.Value / 100.0,
                properties.Elevation?.Value, properties.Temperature?.Value, properties.RelativeHumidity?.Value,
                currentResult.Geometry?.Coordinates is { Length: > 1 } coordinates ? coordinates[1] : null)
            : null;
        return new CurrentWrapper
        {
            WeatherText = properties.TextDescription,
            WeatherCode = GetWeatherCode(properties.Icon),
            Temperature = new TemperatureWrapper
            {
                Temperature = Celsius(properties.Temperature?.Value),
                FeelsLike = Celsius(properties.WindChill?.Value),
            },
            // A failed station anemometer reports zero, so these observations omit its wind.
            Wind = properties.WindSpeed?.Value is { } speed && speed != 0.0 ? new Wind
            {
                Degree = properties.WindDirection?.Value,
                Speed = Speed.FromKilometersPerHour(speed),
                Gusts = KilometersPerHour(properties.WindGust?.Value),
            } : null,
            RelativeHumidity = Percent(properties.RelativeHumidity?.Value),
            DewPoint = Celsius(properties.Dewpoint?.Value),
            Pressure = properties.SeaLevelPressure is not null
                ? Pascals(properties.SeaLevelPressure.Value)
                : computedPressure is { } pressure ? Pressure.FromHectopascals(pressure) : null,
            Visibility = Meters(properties.Visibility?.Value),
        };
    }

    private static IReadOnlyList<DailyWrapper> GetDailyForecast(Location location, NwsDailyResult dailyResult)
    {
        Dictionary<string, HalfDayWrapper> dayParts = [];
        Dictionary<string, HalfDayWrapper> nightParts = [];
        foreach (NwsDailyPeriods period in dailyResult.Properties?.Periods ?? [])
        {
            string date = TimeZoneInfo.ConvertTime(period.StartTime, location.TimeZone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            HalfDayWrapper halfDay = new()
            {
                WeatherText = period.ShortForecast,
                WeatherCode = GetWeatherCode(period.Icon),
                Temperature = new TemperatureWrapper { Temperature = Celsius(period.Temperature?.Value) },
                PrecipitationProbability = new PrecipitationProbability { Total = Percent(period.ProbabilityOfPrecipitation?.Value) },
                Wind = new Wind
                {
                    Degree = GetWindDegree(period.WindDirection),
                    Speed = KilometersPerHour(period.WindSpeed?.MaxValue ?? period.WindSpeed?.Value),
                    Gusts = KilometersPerHour(period.WindGust?.Value),
                },
            };
            (period.IsDaytime ? dayParts : nightParts)[date] = halfDay;
        }

        List<DailyWrapper> dailyList = [];
        foreach (string key in nightParts.Keys.Order(StringComparer.Ordinal))
        {
            dailyList.Add(new DailyWrapper
            {
                Date = StartOfDay(key, location.TimeZone),
                Day = dayParts.GetValueOrDefault(key),
                Night = nightParts[key],
            });
        }

        string lastDay = dayParts.Keys.OrderDescending(StringComparer.Ordinal).First();
        string lastNight = nightParts.Keys.OrderDescending(StringComparer.Ordinal).First();
        if (lastDay != lastNight)
        {
            dailyList.Add(new DailyWrapper { Date = StartOfDay(lastDay, location.TimeZone), Day = dayParts[lastDay] });
        }

        return dailyList;
    }

    public bool NeedsLocationParametersRefresh(Location location, bool coordinatesChanged, IReadOnlyCollection<SourceFeature> features)
    {
        IReadOnlyDictionary<string, string>? parameters = location.Parameters.GetValueOrDefault(Id);
        return coordinatesChanged || string.IsNullOrEmpty(parameters?.GetValueOrDefault("gridId"))
            || string.IsNullOrEmpty(parameters?.GetValueOrDefault("gridX"))
            || string.IsNullOrEmpty(parameters?.GetValueOrDefault("gridY"))
            || (features.Contains(SourceFeature.Current) && string.IsNullOrEmpty(parameters?.GetValueOrDefault("station")));
    }

    public async Task<IReadOnlyDictionary<string, string>> RequestLocationParametersAsync(Location location,
        CancellationToken cancellationToken = default)
    {
        NwsPointProperties properties = (await RequestAsync<NwsPointResult>(
            "points/" + Coordinates(location.Latitude, location.Longitude), cancellationToken).ConfigureAwait(false)).Properties
            ?? throw new InvalidLocationException();
        NwsStationsResult stations = await RequestAsync<NwsStationsResult>(GridPath(properties.GridId, properties.GridX, properties.GridY)
            + "/stations", cancellationToken).ConfigureAwait(false);
        Dictionary<string, string> parameters = new()
        {
            ["gridId"] = properties.GridId,
            ["gridX"] = properties.GridX.ToString(CultureInfo.InvariantCulture),
            ["gridY"] = properties.GridY.ToString(CultureInfo.InvariantCulture),
        };
        if (stations.Features?.FirstOrDefault()?.Properties?.StationIdentifier is { } station)
        {
            parameters["station"] = station;
        }

        return parameters;
    }

    public async Task<IReadOnlyList<LocationAddressInfo>> RequestNearestLocationAsync(double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        NwsPointProperties properties = (await RequestAsync<NwsPointResult>("points/" + Coordinates(latitude, longitude),
            cancellationToken).ConfigureAwait(false)).Properties ?? throw new InvalidLocationException();
        string? state = properties.RelativeLocation?.Properties?.State;
        string countryCode = state switch
        {
            "AS" or "PR" or "VI" => state,
            "GU" => string.Equals(properties.TimeZone, "Pacific/Saipan", StringComparison.OrdinalIgnoreCase) ? "MP" : "GU",
            _ => "US",
        };
        return [new LocationAddressInfo
        {
            TimeZoneId = properties.TimeZone,
            Country = CountryNames.GetCountryName("US"),
            CountryCode = countryCode,
            Admin1 = countryCode != "US" ? CountryNames.GetCountryName(countryCode) : state,
            Admin1Code = state,
            City = properties.RelativeLocation?.Properties?.City,
        }];
    }

    private async Task<T> RequestFeatureAsync<T>(string path, SourceFeature feature, T empty,
        Dictionary<SourceFeature, Exception> failedFeatures, CancellationToken cancellationToken, bool daily = false)
    {
        try
        {
            return await RequestAsync<T>(path, cancellationToken, daily).ConfigureAwait(false);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            lock (failedFeatures)
            {
                failedFeatures[feature] = exception;
            }

            return empty;
        }
    }

    private async Task<T> RequestAsync<T>(string path, CancellationToken cancellationToken, bool daily = false)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BaseUrl + path);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        if (daily)
        {
            request.Headers.Add("Feature-Flags", "forecast_temperature_qv,forecast_wind_speed_qv");
        }

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("The weather source returned an empty response.");
    }

    private static string Coordinates(double latitude, double longitude)
        => latitude.ToString(CultureInfo.InvariantCulture) + "," + longitude.ToString(CultureInfo.InvariantCulture);

    private static string GridPath(string gridId, int gridX, int gridY)
        => "gridpoints/" + Uri.EscapeDataString(gridId) + "/" + gridX.ToString(CultureInfo.InvariantCulture)
            + "," + gridY.ToString(CultureInfo.InvariantCulture);

    private static DateTimeOffset StartOfDay(string date, TimeZoneInfo timeZone)
    {
        DateTime local = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone));
    }

    private static double? GetWindDegree(string? direction) => direction switch
    {
        "N" => 0.0, "NNE" => 22.5, "NE" => 45.0, "ENE" => 67.5,
        "E" => 90.0, "ESE" => 112.5, "SE" => 135.0, "SSE" => 157.5,
        "S" => 180.0, "SSW" or "SSO" => 202.5, "SW" or "SO" => 225.0, "WSW" or "OSO" => 247.5,
        "W" or "O" => 270.0, "WNW" or "ONO" => 292.5, "NW" or "NO" => 315.0, "NNW" or "NNO" => 337.5,
        "VR" or "VAR" => -1.0,
        _ => null,
    };

    private static Units.Temperature? Celsius(double? value) => value is { } number ? Units.Temperature.FromCelsius(number) : null;
    private static Speed? KilometersPerHour(double? value) => value is { } number ? Speed.FromKilometersPerHour(number) : null;
    private static Ratio? Percent(double? value) => value is { } number ? Ratio.FromPercent(number) : null;
    private static Pressure? Pascals(double? value) => value is { } number ? Pressure.FromPascals(number) : null;
    private static Distance? Meters(double? value) => value is { } number ? Distance.FromMeters(number) : null;
}
