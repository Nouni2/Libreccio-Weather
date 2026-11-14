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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources.Serialization;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.MetNorway;

public sealed class MetNoService(HttpClient httpClient) : IWeatherSource, IHttpSource
{
    public const string Id = "metno";
    private const string BaseUrl = "https://api.met.no/weatherapi/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new SourceDateConverter() },
    };
    private static readonly string[] NordicCountries = ["NO", "SE", "FI", "DK"];
    private static readonly HashSet<string> WeatherSymbols = new(StringComparer.Ordinal)
    {
        "clearsky", "cloudy", "fair", "fog", "heavyrain", "heavyrainshowers", "heavysleet", "heavysleetshowers",
        "heavysnow", "heavysnowshowers", "lightrain", "lightrainshowers", "lightsleet", "lightsleetshowers",
        "lightsnow", "lightsnowshowers", "partlycloudy", "rain", "rainshowers", "sleet", "sleetshowers", "snow", "snowshowers",
    };
    private static readonly string UserAgent = "Libreccio-Weather/" + typeof(MetNoService).Assembly.GetName().Version
        + " (+https://github.com/Nouni2/Libreccio-Weather/issues)";

    string ISource.Id => Id;
    public string Name => IsNorwegian ? "Meteorologisk institutt" : "MET Norway";
    public string PrivacyPolicyUrl => IsNorwegian ? "https://www.met.no/om-oss/personvern" : "https://www.met.no/en/About-us/privacy";
    public SourceContinent Continent => SourceContinent.Worldwide;
    private static bool IsNorwegian => Strings.Culture.TwoLetterISOLanguageName.StartsWith("no", StringComparison.Ordinal);

    public IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        ["MET Norway"] = "https://www.met.no/",
    };

    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
    {
        [SourceFeature.Forecast] = "MET Norway (NLOD / CC BY 4.0)",
        [SourceFeature.Current] = "MET Norway (NLOD / CC BY 4.0)",
        [SourceFeature.Minutely] = "MET Norway (NLOD / CC BY 4.0)",
        [SourceFeature.AirQuality] = "MET Norway (NLOD / CC BY 4.0)",
        [SourceFeature.Alert] = "MET Norway (NLOD / CC BY 4.0)",
    };

    public bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return feature switch
        {
            SourceFeature.Current or SourceFeature.Minutely => NordicCountries.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase),
            SourceFeature.AirQuality => string.Equals(location.CountryCode, "NO", StringComparison.OrdinalIgnoreCase),
            SourceFeature.Alert => string.Equals(location.CountryCode, "NO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(location.CountryCode, "SJ", StringComparison.OrdinalIgnoreCase),
            SourceFeature.Forecast => true,
            _ => false,
        };
    }

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return string.Equals(location.CountryCode, "NO", StringComparison.OrdinalIgnoreCase) ? IFeatureSource.PriorityHighest
            : new[] { "SJ", "SE", "FI", "DK" }.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase)
                && IsFeatureSupportedForLocation(location, feature) ? IFeatureSource.PriorityHigh : IFeatureSource.PriorityNone;
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(Location location,
        IReadOnlyCollection<SourceFeature> requestedFeatures, CancellationToken cancellationToken = default)
    {
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        string coordinates = "lat=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon=" + location.Longitude.ToString(CultureInfo.InvariantCulture);
        bool forecast = requestedFeatures.Contains(SourceFeature.Forecast);
        bool current = requestedFeatures.Contains(SourceFeature.Current);
        bool minutely = requestedFeatures.Contains(SourceFeature.Minutely);
        bool airQuality = requestedFeatures.Contains(SourceFeature.AirQuality);
        bool alert = requestedFeatures.Contains(SourceFeature.Alert);
        Task<MetNoForecastResult> forecastTask = forecast
            ? RequestAsync("locationforecast/2.0/complete.json?" + coordinates, [SourceFeature.Forecast],
                new MetNoForecastResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MetNoForecastResult());
        List<SourceFeature> nowcastFeatures = [];
        if (minutely)
        {
            nowcastFeatures.Add(SourceFeature.Minutely);
        }

        if (current)
        {
            nowcastFeatures.Add(SourceFeature.Current);
        }

        Task<MetNoNowcastResult> nowcastTask = current || minutely
            ? RequestAsync("nowcast/2.0/complete.json?" + coordinates, nowcastFeatures,
                new MetNoNowcastResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MetNoNowcastResult());
        Task<MetNoAirQualityResult> airQualityTask = airQuality
            ? RequestAsync("airqualityforecast/0.1/?" + coordinates, [SourceFeature.AirQuality],
                new MetNoAirQualityResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MetNoAirQualityResult());
        Task<MetNoAlertResult> alertsTask = alert
            ? RequestAsync("metalerts/2.0/current.json?lang=" + (IsNorwegian ? "no" : "en") + "&" + coordinates,
                [SourceFeature.Alert], new MetNoAlertResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new MetNoAlertResult());

        await Task.WhenAll(forecastTask, nowcastTask, airQualityTask, alertsTask).ConfigureAwait(false);
        MetNoForecastTimeseries[]? forecastSeries = (await forecastTask.ConfigureAwait(false)).Properties?.Timeseries;
        MetNoForecastTimeseries[]? nowcastSeries = (await nowcastTask.ConfigureAwait(false)).Properties?.Timeseries;
        Dictionary<DateTimeOffset, AirQuality> hourlyAirQuality = [];
        foreach (MetNoAirQualityTime item in (await airQualityTask.ConfigureAwait(false)).Data?.Time ?? [])
        {
            hourlyAirQuality[item.From] = new AirQuality
            {
                PM25 = Micrograms(item.Variables?.PM25Concentration?.Value),
                PM10 = Micrograms(item.Variables?.PM10Concentration?.Value),
                SO2 = Micrograms(item.Variables?.SO2Concentration?.Value),
                NO2 = Micrograms(item.Variables?.NO2Concentration?.Value),
                O3 = Micrograms(item.Variables?.O3Concentration?.Value),
            };
        }

        return new WeatherWrapper
        {
            DailyForecast = forecast ? GetDailyList(location, forecastSeries) : null,
            HourlyForecast = forecast ? GetHourlyList(forecastSeries) : null,
            Current = current ? GetCurrent(nowcastSeries?.FirstOrDefault()?.Data) : null,
            AirQuality = airQuality ? new AirQualityWrapper { HourlyForecast = hourlyAirQuality } : null,
            MinutelyForecast = minutely ? GetMinutelyList(nowcastSeries) : null,
            AlertList = alert ? GetAlerts(await alertsTask.ConfigureAwait(false)) : null,
            FailedFeatures = failedFeatures,
        };
    }

    private static CurrentWrapper? GetCurrent(MetNoForecastData? data)
    {
        return data is null ? null : new CurrentWrapper
        {
            WeatherText = GetWeatherText(data.SymbolCode),
            WeatherCode = GetWeatherCode(data.SymbolCode),
            Temperature = new TemperatureWrapper { Temperature = Celsius(data.Instant?.Details?.AirTemperature) },
            Wind = GetWind(data.Instant?.Details),
            RelativeHumidity = Percent(data.Instant?.Details?.RelativeHumidity),
            DewPoint = Celsius(data.Instant?.Details?.DewPointTemperature),
            Pressure = Hectopascals(data.Instant?.Details?.AirPressureAtSeaLevel),
            CloudCover = Percent(data.Instant?.Details?.CloudAreaFraction),
        };
    }

    private static IReadOnlyList<HourlyWrapper> GetHourlyList(MetNoForecastTimeseries[]? timeseries)
    {
        return timeseries?.Select(item => new HourlyWrapper
        {
            Date = item.Time,
            WeatherText = GetWeatherText(item.Data?.SymbolCode),
            WeatherCode = GetWeatherCode(item.Data?.SymbolCode),
            Temperature = new TemperatureWrapper { Temperature = Celsius(item.Data?.Instant?.Details?.AirTemperature) },
            Precipitation = new Models.Precipitation
            {
                Total = Millimeters(item.Data?.Next1Hours?.Details?.PrecipitationAmount
                    ?? item.Data?.Next6Hours?.Details?.PrecipitationAmount ?? item.Data?.Next12Hours?.Details?.PrecipitationAmount),
            },
            PrecipitationProbability = new PrecipitationProbability
            {
                Total = Percent(item.Data?.Next1Hours?.Details?.ProbabilityOfPrecipitation
                    ?? item.Data?.Next6Hours?.Details?.ProbabilityOfPrecipitation ?? item.Data?.Next12Hours?.Details?.ProbabilityOfPrecipitation),
                Thunderstorm = Percent(item.Data?.Next1Hours?.Details?.ProbabilityOfThunder
                    ?? item.Data?.Next6Hours?.Details?.ProbabilityOfThunder ?? item.Data?.Next12Hours?.Details?.ProbabilityOfThunder),
            },
            Wind = GetWind(item.Data?.Instant?.Details),
            UV = new UV { Index = item.Data?.Instant?.Details?.UltravioletIndexClearSky },
            RelativeHumidity = Percent(item.Data?.Instant?.Details?.RelativeHumidity),
            DewPoint = Celsius(item.Data?.Instant?.Details?.DewPointTemperature),
            Pressure = Hectopascals(item.Data?.Instant?.Details?.AirPressureAtSeaLevel),
            CloudCover = Percent(item.Data?.Instant?.Details?.CloudAreaFraction),
        }).ToList() ?? [];
    }

    private static IReadOnlyList<DailyWrapper> GetDailyList(Location location, MetNoForecastTimeseries[]? timeseries)
    {
        DateTime[] dates = (timeseries ?? []).GroupBy(item => TimeZoneInfo.ConvertTime(item.Time, location.TimeZone).Date)
            .Select(group => group.Key).ToArray();
        List<DailyWrapper> daily = [];
        for (int i = 0; i < dates.Length - 1; i++)
        {
            DateTime local = DateTime.SpecifyKind(dates[i], DateTimeKind.Unspecified);
            while (location.TimeZone.IsInvalidTime(local))
            {
                local = local.AddMinutes(1);
            }

            daily.Add(new DailyWrapper { Date = new DateTimeOffset(local, location.TimeZone.GetUtcOffset(local)) });
        }

        return daily;
    }

    private static IReadOnlyList<Minutely> GetMinutelyList(MetNoForecastTimeseries[]? timeseries)
    {
        List<Minutely> minutely = [];
        if (timeseries is not { Length: >= 2 })
        {
            return minutely;
        }

        for (int i = 0; i < timeseries.Length; i++)
        {
            long interval = i < timeseries.Length - 1
                ? timeseries[i + 1].Time.ToUnixTimeMilliseconds() - timeseries[i].Time.ToUnixTimeMilliseconds()
                : timeseries[i].Time.ToUnixTimeMilliseconds() - timeseries[i - 1].Time.ToUnixTimeMilliseconds();
            minutely.Add(new Minutely
            {
                Date = timeseries[i].Time,
                MinuteInterval = UnitFormatting.RoundToInt((double)(interval / 60_000)),
                PrecipitationIntensity = Millimeters(timeseries[i].Data?.Instant?.Details?.PrecipitationRate),
            });
        }

        return minutely;
    }

    private static IReadOnlyList<Alert>? GetAlerts(MetNoAlertResult result)
    {
        return result.Features?.Where(item => item.Properties is not null).Select(item =>
        {
            MetNoAlertProperties properties = item.Properties!;
            AlertSeverity severity = properties.Severity?.ToLowerInvariant() switch
            {
                "extreme" => AlertSeverity.Extreme,
                "severe" => AlertSeverity.Severe,
                "moderate" => AlertSeverity.Moderate,
                "minor" => AlertSeverity.Minor,
                _ => AlertSeverity.Unknown,
            };
            return new Alert
            {
                AlertId = properties.Id,
                StartDate = item.WhenAlert?.Interval is { Length: > 0 } startInterval ? startInterval[0] : null,
                EndDate = item.WhenAlert?.Interval is { Length: > 1 } interval ? interval[1] : properties.EventEndingTime,
                Headline = properties.EventAwarenessName,
                Description = properties.Description,
                Instruction = properties.Instruction,
                Source = "MET Norway",
                Severity = severity,
                Color = Alert.ColorFromSeverity(severity),
            };
        }).ToList();
    }

    private static WeatherCode? GetWeatherCode(string? icon)
    {
        string? code = icon?.Replace("_night", "", StringComparison.Ordinal).Replace("_day", "", StringComparison.Ordinal);
        return code switch
        {
            "clearsky" or "fair" => WeatherCode.Clear,
            "partlycloudy" => WeatherCode.PartlyCloudy,
            "cloudy" => WeatherCode.Cloudy,
            "fog" => WeatherCode.Fog,
            "heavyrain" or "heavyrainshowers" or "lightrain" or "lightrainshowers" or "rain" or "rainshowers" => WeatherCode.Rain,
            "heavyrainandthunder" or "heavyrainshowersandthunder" or "heavysleetandthunder" or "heavysleetshowersandthunder"
                or "heavysnowandthunder" or "heavysnowshowersandthunder" or "lightrainandthunder" or "lightrainshowersandthunder"
                or "lightsleetandthunder" or "lightsleetshowersandthunder" or "lightsnowandthunder" or "lightsnowshowersandthunder"
                or "rainandthunder" or "rainshowersandthunder" or "sleetandthunder" or "sleetshowersandthunder"
                or "snowandthunder" or "snowshowersandthunder" => WeatherCode.Thunderstorm,
            "heavysnow" or "heavysnowshowers" or "lightsnow" or "lightsnowshowers" or "snow" or "snowshowers" => WeatherCode.Snow,
            "heavysleet" or "heavysleetshowers" or "lightsleet" or "lightsleetshowers" or "sleet" or "sleetshowers" => WeatherCode.Sleet,
            _ => null,
        };
    }

    private static string? GetWeatherText(string? icon)
    {
        if (icon is null)
        {
            return null;
        }

        string code = icon.Replace("_night", "", StringComparison.Ordinal).Replace("_day", "", StringComparison.Ordinal)
            .Replace("andthunder", "", StringComparison.Ordinal);
        string? text = WeatherSymbols.Contains(code) ? Strings.Get("metno_weather_text_" + code) : null;
        return icon.Contains("andthunder", StringComparison.Ordinal)
            ? Strings.Format("metno_weather_text_andthunder", text ?? Strings.Get("null_data_text")) : text;
    }

    private async Task<T> RequestAsync<T>(string query, IReadOnlyCollection<SourceFeature> features, T empty,
        Dictionary<SourceFeature, Exception> failedFeatures, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, BaseUrl + query);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false) ?? empty;
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            lock (failedFeatures)
            {
                foreach (SourceFeature feature in features)
                {
                    failedFeatures[feature] = exception;
                }
            }

            return empty;
        }
    }

    private static Wind? GetWind(MetNoForecastDataDetails? details) => details is null ? null : new Wind
    {
        Degree = details.WindFromDirection,
        Speed = MetersPerSecond(details.WindSpeed),
        Gusts = MetersPerSecond(details.WindSpeedOfGust),
    };

    private static Units.Temperature? Celsius(double? value) => value is { } number ? Units.Temperature.FromCelsius(number) : null;
    private static Speed? MetersPerSecond(double? value) => value is { } number ? Speed.FromMetersPerSecond(number) : null;
    private static Units.Precipitation? Millimeters(double? value) => value is { } number ? Units.Precipitation.FromMillimeters(number) : null;
    private static Pressure? Hectopascals(double? value) => value is { } number ? Pressure.FromHectopascals(number) : null;
    private static Ratio? Percent(double? value) => value is { } number ? Ratio.FromPercent(number) : null;
    private static PollutantConcentration? Micrograms(double? value) => value is { } number ? PollutantConcentration.FromMicrogramsPerCubicMeter(number) : null;
}
