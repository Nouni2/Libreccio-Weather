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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.BrightSky;

/// <summary>
/// Forecast, current conditions, minutely precipitation and alerts for Germany from Bright Sky, which serves the
/// Deutscher Wetterdienst's open data.
/// </summary>
public sealed class BrightSkyService : IWeatherSource, IHttpSource
{
    public const string Id = "brightsky";
    public const string BaseUrl = "https://api.brightsky.dev/";

    private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private readonly HttpClient _httpClient;
    private readonly ConfigStore? _config;

    /// <param name="config">This source's own settings, which hold the address of another Bright Sky server.</param>
    public BrightSkyService(HttpClient httpClient, ConfigStore? config = null)
    {
        _httpClient = httpClient;
        _config = config;
        Name = "Bright Sky (DWD) (" + CountryNames.GetCountryName("DE") + ")";
        string attribution = "Bright Sky, Data basis: Deutscher Wetterdienst. "
            + Strings.Format("data_modified", Strings.Get("desktop_app_name"));
        SupportedFeatures = new Dictionary<SourceFeature, string>
        {
            [SourceFeature.Forecast] = attribution,
            [SourceFeature.Current] = attribution,
            [SourceFeature.Minutely] = attribution,
            [SourceFeature.Alert] = attribution,
        };
    }

    string ISource.Id => Id;

    public string Name { get; }

    public string PrivacyPolicyUrl => "https://brightsky.dev/";

    public IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        ["Bright Sky"] = "https://brightsky.dev/",
        ["Deutscher Wetterdienst"] = "https://www.dwd.de/",
    };

    public SourceContinent Continent => SourceContinent.Europe;

    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; }

    /// <summary>
    /// The server, Bright Sky's own unless another is set. Setting null goes back to Bright Sky's own.
    /// </summary>
    public string? Instance
    {
        get => _config?.GetString("instance", null) ?? BaseUrl;
        set
        {
            if (value is not null)
            {
                _config?.PutString("instance", value);
            }
            else
            {
                _config?.Remove("instance");
            }
        }
    }

    public bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return string.Equals(location.CountryCode, "DE", StringComparison.OrdinalIgnoreCase);
    }

    public int GetFeaturePriorityForLocation(Location location, SourceFeature feature)
    {
        return IsFeatureSupportedForLocation(location, feature) ? IFeatureSource.PriorityHighest : IFeatureSource.PriorityNone;
    }

    public async Task<WeatherWrapper> RequestWeatherAsync(
        Location location,
        IReadOnlyCollection<SourceFeature> requestedFeatures,
        CancellationToken cancellationToken = default)
    {
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        bool forecast = requestedFeatures.Contains(SourceFeature.Forecast);
        bool current = requestedFeatures.Contains(SourceFeature.Current);
        bool alerts = requestedFeatures.Contains(SourceFeature.Alert);
        bool minutely = requestedFeatures.Contains(SourceFeature.Minutely);
        string instance = Instance ?? BaseUrl;
        string coordinates = "lat=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon=" + location.Longitude.ToString(CultureInfo.InvariantCulture);

        // From yesterday's midnight to the midnight 12 days after today's, in the location's time zone.
        DateTime today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Date;
        string firstDate = today.AddDays(-1).ToString(DateFormat, CultureInfo.InvariantCulture);
        string lastDate = today.AddDays(12).ToString(DateFormat, CultureInfo.InvariantCulture);

        Task<BrightSkyWeatherResult> weatherTask = forecast
            ? RequestAsync(instance + "weather?" + coordinates + "&date=" + Uri.EscapeDataString(firstDate)
                + "&last_date=" + Uri.EscapeDataString(lastDate),
                SourceFeature.Forecast, new BrightSkyWeatherResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new BrightSkyWeatherResult());
        Task<BrightSkyCurrentWeatherResult> currentTask = current
            ? RequestAsync(instance + "current_weather?" + coordinates,
                SourceFeature.Current, new BrightSkyCurrentWeatherResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new BrightSkyCurrentWeatherResult());
        Task<BrightSkyAlertsResult> alertsTask = alerts
            ? RequestAsync(instance + "alerts?" + coordinates,
                SourceFeature.Alert, new BrightSkyAlertsResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new BrightSkyAlertsResult());
        Task<BrightSkyRadarResult> radarTask = minutely
            ? RequestAsync(instance + "radar?" + coordinates + "&distance=1&format=plain&last_date="
                + Uri.EscapeDataString("2099-01-01T00:00:00"),
                SourceFeature.Minutely, new BrightSkyRadarResult(), failedFeatures, cancellationToken)
            : Task.FromResult(new BrightSkyRadarResult());

        await Task.WhenAll(weatherTask, currentTask, alertsTask, radarTask).ConfigureAwait(false);
        BrightSkyWeatherResult weather = await weatherTask.ConfigureAwait(false);
        BrightSkyCurrentWeatherResult currentWeather = await currentTask.ConfigureAwait(false);
        BrightSkyAlertsResult alertsResult = await alertsTask.ConfigureAwait(false);
        BrightSkyRadarResult radar = await radarTask.ConfigureAwait(false);

        return new WeatherWrapper
        {
            DailyForecast = forecast ? GetDailyForecast(location, weather.Weather) : null,
            HourlyForecast = forecast ? GetHourlyForecast(weather.Weather) : null,
            Current = current ? GetCurrent(currentWeather.Weather) : null,
            MinutelyForecast = minutely ? GetMinutely(radar) : null,
            AlertList = alerts ? GetAlertList(alertsResult.Alerts, Strings.Culture.TwoLetterISOLanguageName) : null,
            FailedFeatures = failedFeatures,
        };
    }

    // A failed request leaves its feature empty and records it as failed, except when the caller cancels.
    private async Task<T> RequestAsync<T>(
        string url,
        SourceFeature feature,
        T empty,
        Dictionary<SourceFeature, Exception> failedFeatures,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(url, cancellationToken).ConfigureAwait(false) ?? empty;
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

    // Each 5-minute amount, in hundredths of a millimeter, becomes an hourly rate.
    private static List<Minutely>? GetMinutely(BrightSkyRadarResult? radarResult)
    {
        return radarResult?.Radar?.Select(radar => new Minutely
        {
            Date = radar.Timestamp,
            MinuteInterval = 5,
            PrecipitationIntensity = radar.Precipitation5 is { Length: > 0 } grid && grid[0] is { Length: > 0 } row
                ? Units.Precipitation.FromMicrometers((long)row[0] * 12 * 10)
                : null,
        }).ToList();
    }

    private static CurrentWrapper? GetCurrent(BrightSkyCurrentWeather? result)
    {
        if (result is null)
        {
            return null;
        }

        return new CurrentWrapper
        {
            WeatherCode = GetWeatherCode(result.Icon),
            Temperature = new TemperatureWrapper { Temperature = Celsius(result.Temperature) },
            Wind = new Wind
            {
                Degree = result.WindDirection,
                Speed = KilometersPerHour(result.WindSpeed),
                Gusts = KilometersPerHour(result.WindGustSpeed),
            },
            RelativeHumidity = Percent(result.RelativeHumidity),
            DewPoint = Celsius(result.DewPoint),
            Pressure = result.Pressure is { } pressure ? Pressure.FromHectopascals(pressure) : null,
            CloudCover = Percent(result.CloudCover),
            Visibility = result.Visibility is { } visibility ? Distance.FromMeters((long)visibility) : null,
        };
    }

    // The API has no days, so the days are the dates of the hours, without values, leaving out the last date.
    private static List<DailyWrapper>? GetDailyForecast(Location location, BrightSkyWeather[]? weatherResult)
    {
        if (weatherResult is null)
        {
            return null;
        }

        List<string> days = [.. weatherResult.Select(hour => WeatherCompletionDate(hour.Timestamp, location.TimeZone)).Distinct()];
        List<DailyWrapper> dailyList = [];
        for (int i = 0; i < days.Count - 1; i++)
        {
            dailyList.Add(new DailyWrapper { Date = StartOfDay(days[i], location.TimeZone) });
        }

        return dailyList;
    }

    private static List<HourlyWrapper>? GetHourlyForecast(BrightSkyWeather[]? weatherResult)
    {
        return weatherResult?.Select(result => new HourlyWrapper
        {
            Date = result.Timestamp,
            WeatherCode = GetWeatherCode(result.Icon),
            Temperature = new TemperatureWrapper { Temperature = Celsius(result.Temperature) },
            Precipitation = new Models.Precipitation
            {
                Total = result.Precipitation is { } total ? Units.Precipitation.FromMillimeters(total) : null,
            },
            PrecipitationProbability = new PrecipitationProbability { Total = Percent(result.PrecipitationProbability) },
            Wind = new Wind
            {
                Degree = result.WindDirection,
                Speed = KilometersPerHour(result.WindSpeed),
                Gusts = KilometersPerHour(result.WindGustSpeed),
            },
            RelativeHumidity = Percent(result.RelativeHumidity),
            DewPoint = Celsius(result.DewPoint),
            Pressure = result.Pressure is { } pressure ? Pressure.FromHectopascals(pressure) : null,
            CloudCover = Percent(result.CloudCover),
            Visibility = result.Visibility is { } visibility ? Distance.FromMeters((double)visibility) : null,
            SunshineDuration = result.Sunshine is { } sunshine ? TimeSpan.FromMinutes(sunshine) : null,
        }).ToList();
    }

    // Texts are in German for German and in English for every other language.
    private static List<Alert>? GetAlertList(BrightSkyAlert[]? alertList, string languageCode)
    {
        if (alertList is null || alertList.Length == 0)
        {
            return null;
        }

        bool german = languageCode == "de";
        return [.. alertList.Select(alert =>
        {
            string? severity = alert.Severity?.ToLowerInvariant();
            return new Alert
            {
                // A missing identifier reads "null", as the text of a missing number does.
                AlertId = alert.Id?.ToString(CultureInfo.InvariantCulture) ?? "null",
                StartDate = alert.Onset,
                EndDate = alert.Expires,
                Headline = german ? alert.HeadlineDe : alert.HeadlineEn,
                Description = german ? alert.DescriptionDe : alert.DescriptionEn,
                Instruction = german ? alert.InstructionDe : alert.InstructionEn,
                Source = "Deutscher Wetterdienst",
                Severity = severity switch
                {
                    "extreme" => AlertSeverity.Extreme,
                    "severe" => AlertSeverity.Severe,
                    "moderate" => AlertSeverity.Moderate,
                    "minor" => AlertSeverity.Minor,
                    _ => AlertSeverity.Unknown,
                },
                Color = severity switch
                {
                    "extreme" => Rgb(241, 48, 255),
                    "severe" => Rgb(255, 48, 48),
                    "moderate" => Rgb(255, 179, 48),
                    "minor" => Rgb(255, 238, 48),
                    _ => Alert.ColorFromSeverity(AlertSeverity.Unknown),
                },
            };
        })];
    }

    private static WeatherCode? GetWeatherCode(string? icon)
    {
        return icon switch
        {
            "clear-day" or "clear-night" => WeatherCode.Clear,
            "partly-cloudy-day" or "partly-cloudy-night" => WeatherCode.PartlyCloudy,
            "cloudy" => WeatherCode.Cloudy,
            "fog" => WeatherCode.Fog,
            "wind" => WeatherCode.Wind,
            "rain" => WeatherCode.Rain,
            "sleet" => WeatherCode.Sleet,
            "snow" => WeatherCode.Snow,
            "hail" => WeatherCode.Hail,
            "thunderstorm" => WeatherCode.Thunderstorm,
            _ => null,
        };
    }

    private static string WeatherCompletionDate(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        return Refresh.WeatherCompletion.IsoDate(date, timeZone);
    }

    private static DateTimeOffset StartOfDay(string isoDate, TimeZoneInfo timeZone)
    {
        DateTime midnight = DateTime.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DateTimeOffset(midnight, timeZone.GetUtcOffset(midnight));
    }

    private static int Rgb(int red, int green, int blue)
    {
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }

    private static Units.Temperature? Celsius(double? value) => value is { } v ? Units.Temperature.FromCelsius(v) : null;

    private static Speed? KilometersPerHour(double? value) => value is { } v ? Speed.FromKilometersPerHour(v) : null;

    private static Ratio? Percent(int? value) => value is { } v ? Ratio.FromPercent((long)v) : null;
}
