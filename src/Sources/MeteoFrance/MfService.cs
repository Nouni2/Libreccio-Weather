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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources.Serialization;

namespace LibreccioWeather.Core.Sources.MeteoFrance;

public sealed partial class MfService : IWeatherSource, IHttpSource, IConfigurableSource, ILocationParametersSource, IReverseGeocodingSource
{
    public const string Id = "mf";
    private const string BaseUrl = "https://webservice.meteofrance.com/";
    private const string UserAgent = "okhttp/5.2.1";
    private static readonly string[] AlertCountries = ["FR", "AD", "BL", "GF", "GP", "MF", "MQ", "NC", "PF", "PM", "RE", "WF", "YT"];
    private static readonly string[] NormalsCountries = ["FR", "AD", "MC", "BL", "GF", "GP", "MF", "MQ", "NC", "PF", "PM", "RE", "WF", "YT"];
    private static readonly string[] OverseasDepartments = ["971", "972", "973", "974", "975", "976", "977", "978", "986", "987", "988"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new SourceDateConverter() },
    };
    private static readonly string DefaultWsftKey = ReadBuildKey("MfWsftKey");
    private static readonly string DefaultWsftJwtKey = ReadBuildKey("MfWsftJwtKey");

    private readonly HttpClient _httpClient;
    private readonly ConfigStore _config;

    public MfService(HttpClient httpClient, ConfigStore config)
    {
        _httpClient = httpClient;
        _config = config;
        string countryName = CountryNames.GetCountryName("FR");
        Name = "Météo-France".Contains(countryName, StringComparison.Ordinal) ? "Météo-France" : "Météo-France (" + countryName + ")";
    }

    string ISource.Id => Id;
    public string Name { get; }
    public string PrivacyPolicyUrl => "https://meteofrance.com/application-meteo-france-politique-de-confidentialite";
    public SourceContinent Continent => SourceContinent.Europe;
    public bool IsConfigured => GetToken().Length > 0;
    public bool IsRestricted => false;
    public IReadOnlyList<string>? KnownAmbiguousCountryCodes => ["CN"];

    public string WsftKey
    {
        get => _config.GetString("wsft_key", "") ?? "";
        set => _config.PutString("wsft_key", value);
    }

    public IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        ["Météo-France"] = "https://meteofrance.com/",
    };

    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
    {
        [SourceFeature.Forecast] = "Météo-France (Etalab)",
        [SourceFeature.Current] = "Météo-France (Etalab)",
        [SourceFeature.Minutely] = "Météo-France (Etalab)",
        [SourceFeature.Alert] = "Météo-France (Etalab)",
        [SourceFeature.Normals] = "Météo-France (Etalab)",
        [SourceFeature.ReverseGeocoding] = "Météo-France (Etalab)",
    };

    public bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return feature switch
        {
            SourceFeature.Current or SourceFeature.Minutely => string.Equals(location.CountryCode, "FR", StringComparison.OrdinalIgnoreCase),
            SourceFeature.Alert => AlertCountries.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase),
            SourceFeature.Normals => NormalsCountries.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase),
            SourceFeature.Forecast or SourceFeature.ReverseGeocoding => true,
            _ => false,
        };
    }

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return IsFeatureSupportedForLocation(location,
            feature is SourceFeature.Forecast or SourceFeature.ReverseGeocoding ? SourceFeature.Normals : feature)
            ? IFeatureSource.PriorityHighest : IFeatureSource.PriorityNone;
    }

    public IReadOnlyList<SourcePreference> GetPreferences()
    {
        return [new SourcePreference("settings_weather_source_mf_api_key", WsftKey, value => WsftKey = value)];
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(
        Location location, IReadOnlyCollection<SourceFeature> requestedFeatures, CancellationToken cancellationToken = default)
    {
        string token = GetToken();
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        string coordinates = "lat=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon=" + location.Longitude.ToString(CultureInfo.InvariantCulture);
        string languageCode = Strings.Culture.TwoLetterISOLanguageName.ToLowerInvariant();
        string region = Strings.Culture.Name.Split('-')[^1].ToLowerInvariant();
        if (languageCode == "zh" && region is "tw" or "hk" or "mo")
        {
            languageCode += "-" + region;
        }

        string language = "&lang=" + Uri.EscapeDataString(languageCode);
        string dated = "&formatDate=iso";
        bool current = requestedFeatures.Contains(SourceFeature.Current);
        bool forecast = requestedFeatures.Contains(SourceFeature.Forecast);
        bool minutely = requestedFeatures.Contains(SourceFeature.Minutely);
        bool alert = requestedFeatures.Contains(SourceFeature.Alert);
        bool normals = requestedFeatures.Contains(SourceFeature.Normals);

        Task<MfCurrentResult> currentTask = current
            ? RequestAsync("v2/observation?" + coordinates + language + dated, token, SourceFeature.Current,
                new MfCurrentResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfCurrentResult());
        Task<MfForecastResult> forecastTask = forecast
            ? RequestAsync("v2/forecast?" + coordinates + dated, token, SourceFeature.Forecast,
                new MfForecastResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfForecastResult());
        Task<MfRainResult> rainTask = minutely
            ? RequestAsync("v3/nowcast/rain?" + coordinates + language + dated, token, SourceFeature.Minutely,
                new MfRainResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfRainResult());

        string? domain = location.Parameters.GetValueOrDefault(Id)?.GetValueOrDefault("domain");
        bool hasDomain = !string.IsNullOrEmpty(domain);
        bool overseas = domain?.StartsWith("VIGI", StringComparison.Ordinal) == true;
        if (alert && !hasDomain)
        {
            lock (failedFeatures)
            {
                failedFeatures[SourceFeature.Alert] = new InvalidLocationException();
            }
        }

        string domainQuery = "domain=" + Uri.EscapeDataString(domain ?? "");
        Task<MfWarningsResult> warningsJ0Task = alert && hasDomain && !overseas
            ? RequestAsync("v3/warning/full?" + domainQuery + "&echeance=J0" + dated, token, SourceFeature.Alert,
                new MfWarningsResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfWarningsResult());
        Task<MfWarningsResult> warningsJ1Task = alert && hasDomain && !overseas
            ? RequestAsync("v3/warning/full?" + domainQuery + "&echeance=J1" + dated, token, SourceFeature.Alert,
                new MfWarningsResult(), failedFeatures, cancellationToken, recordFailure: false)
            : Task.FromResult(new MfWarningsResult());
        Task<MfWarningDictionaryResult> dictionaryTask = alert && hasDomain && overseas
            ? RequestAsync("v2/warning/dictionary?" + domainQuery, token, SourceFeature.Alert,
                new MfWarningDictionaryResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfWarningDictionaryResult());
        Task<MfWarningsOverseasResult> overseasTask = alert && hasDomain && overseas
            ? RequestAsync("v2/warning/full?" + domainQuery + (domain == "VIGI974" ? "&warning_type=vigilance4colors" : "") + dated,
                token, SourceFeature.Alert, new MfWarningsOverseasResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfWarningsOverseasResult());
        Task<MfNormalsResult> normalsTask = normals
            ? RequestAsync("v2/normals?" + coordinates, token, SourceFeature.Normals,
                new MfNormalsResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MfNormalsResult());

        await Task.WhenAll(currentTask, forecastTask, rainTask, warningsJ0Task, warningsJ1Task, dictionaryTask, overseasTask, normalsTask)
            .ConfigureAwait(false);
        MfForecastResult forecastResult = await forecastTask.ConfigureAwait(false);
        return new WeatherWrapper
        {
            Current = current ? GetCurrent(await currentTask.ConfigureAwait(false)) : null,
            DailyForecast = forecast ? GetDailyList(location, forecastResult.Properties?.DailyForecast) : null,
            HourlyForecast = forecast ? GetHourlyList(forecastResult.Properties?.Forecast, forecastResult.Properties?.ProbabilityForecast) : null,
            MinutelyForecast = minutely ? GetMinutelyList(await rainTask.ConfigureAwait(false)) : null,
            AlertList = alert && hasDomain
                ? overseas
                    ? GetOverseasWarningsList(await dictionaryTask.ConfigureAwait(false), await overseasTask.ConfigureAwait(false))
                    : GetWarningsList(await warningsJ0Task.ConfigureAwait(false), await warningsJ1Task.ConfigureAwait(false))
                : null,
            Normals = normals ? GetNormals(await normalsTask.ConfigureAwait(false)) : null,
            FailedFeatures = failedFeatures,
        };
    }

    public bool NeedsLocationParametersRefresh(Location location, bool coordinatesChanged, IReadOnlyCollection<SourceFeature> features)
    {
        return features.Contains(SourceFeature.Alert)
            && (coordinatesChanged || string.IsNullOrEmpty(location.Parameters.GetValueOrDefault(Id)?.GetValueOrDefault("domain")));
    }

    public async Task<IReadOnlyDictionary<string, string>> RequestLocationParametersAsync(
        Location location, CancellationToken cancellationToken = default)
    {
        string query = "v2/forecast?lat=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon=" + location.Longitude.ToString(CultureInfo.InvariantCulture) + "&formatDate=iso";
        MfForecastResult result = await GetAsync<MfForecastResult>(query, GetToken(), cancellationToken).ConfigureAwait(false);
        string department = result.Properties?.FrenchDepartment ?? throw new InvalidLocationException();
        string domain;
        if (FrenchDepartments.Contains(department))
        {
            domain = department;
        }
        else if (OverseasDepartments.Contains(department, StringComparer.Ordinal))
        {
            domain = department is "977" or "978" ? "VIGI978-977" : "VIGI" + department;
        }
        else
        {
            throw new InvalidLocationException();
        }

        return new Dictionary<string, string> { ["domain"] = domain };
    }

    private async Task<T> RequestAsync<T>(string query, string token, SourceFeature feature, T empty,
        Dictionary<SourceFeature, Exception> failedFeatures, CancellationToken cancellationToken, bool recordFailure = true)
    {
        try
        {
            return await GetAsync<T>(query, token, cancellationToken).ConfigureAwait(false);
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

    private async Task<T> GetAsync<T>(string query, string token, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BaseUrl + query + "&token=" + Uri.EscapeDataString(token));
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("The weather source returned an empty response.");
    }

    private static string ReadBuildKey(string name)
    {
        return typeof(MfService).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == name)?.Value ?? "";
    }

    private string GetWsftKeyOrDefault()
    {
        string key = WsftKey;
        return key.Length > 0 ? key : DefaultWsftKey;
    }

    private string GetToken()
    {
        // A key the user entered wins; otherwise the bundled signing secret makes a token, and the bundled key is used
        // when that secret cannot sign.
        string key = GetWsftKeyOrDefault();
        if (key != DefaultWsftKey)
        {
            return key;
        }

        // HS256 refuses a secret shorter than 256 bits, which is when the bundled key takes over.
        byte[] secret = Encoding.UTF8.GetBytes(DefaultWsftJwtKey);
        if (secret.Length < 32)
        {
            return DefaultWsftKey;
        }

        string header = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        string payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["class"] = "mobile",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString(),
        }));
        string input = header + "." + payload;
        return input + "." + Base64Url(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(input)));
    }

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
