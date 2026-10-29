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
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.OpenMeteo;

/// <summary>
/// Forecast, current conditions, minutely precipitation, air quality, pollen and location search from Open-Meteo.
/// </summary>
public sealed class OpenMeteoService
{
    public const string Id = "openmeteo";
    public const string Name = "Open-Meteo";
    public const string PrivacyPolicyUrl = "https://open-meteo.com/en/terms#privacy";
    public const string LocationSearchAttribution = "Open-Meteo (CC BY 4.0) • GeoNames";

    private const string ForecastBaseUrl = "https://api.open-meteo.com/";
    private const string AirQualityBaseUrl = "https://air-quality-api.open-meteo.com/";
    private const string GeocodingBaseUrl = "https://geocoding-api.open-meteo.com/";

    private const string WeatherAttribution = "Open-Meteo (CC BY 4.0)";
    private const string AirQualityAttribution = "Open-Meteo (CC BY 4.0) • CAMS ENSEMBLE data provider";

    // Open-Meteo answers with this message when the chosen models do not cover the location.
    private const string NoDataMessage = "No data is available for this location";

    private static readonly string[] DailyFields =
    [
        "temperature_2m_max", "temperature_2m_min", "apparent_temperature_max", "apparent_temperature_min",
        "sunshine_duration", "uv_index_max",
        "relative_humidity_2m_mean", "relative_humidity_2m_max", "relative_humidity_2m_min",
        "dew_point_2m_mean", "dew_point_2m_max", "dew_point_2m_min",
        "pressure_msl_mean", "pressure_msl_max", "pressure_msl_min",
        "cloud_cover_mean", "cloud_cover_max", "cloud_cover_min",
        "visibility_mean", "visibility_max", "visibility_min",
    ];

    private static readonly string[] HourlyFields =
    [
        "temperature_2m", "apparent_temperature", "precipitation_probability", "precipitation", "rain", "showers",
        "snowfall", "weather_code", "wind_speed_10m", "wind_direction_10m", "wind_gusts_10m", "uv_index", "is_day",
        "relative_humidity_2m", "dew_point_2m", "pressure_msl", "cloud_cover", "visibility",
    ];

    private static readonly string[] CurrentFields =
    [
        "temperature_2m", "apparent_temperature", "weather_code", "wind_speed_10m", "wind_direction_10m",
        "wind_gusts_10m", "uv_index", "relative_humidity_2m", "dew_point_2m", "pressure_msl", "cloud_cover",
        "visibility",
    ];

    private static readonly string[] MinutelyFields = ["precipitation"];

    private static readonly string[] AirQualityFields =
        ["pm10", "pm2_5", "carbon_monoxide", "nitrogen_dioxide", "sulphur_dioxide", "ozone"];

    private static readonly string[] PollenFields =
        ["alder_pollen", "birch_pollen", "grass_pollen", "mugwort_pollen", "olive_pollen", "ragweed_pollen"];

    private readonly HttpClient _httpClient;

    public OpenMeteoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Features this source provides, with the credit shown for each one.
    /// </summary>
    public static IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } =
        new Dictionary<SourceFeature, string>
        {
            [SourceFeature.Forecast] = WeatherAttribution,
            [SourceFeature.Current] = WeatherAttribution,
            [SourceFeature.AirQuality] = AirQualityAttribution,
            [SourceFeature.Pollen] = AirQualityAttribution,
            [SourceFeature.Minutely] = WeatherAttribution,
        };

    /// <summary>
    /// Links added to each name that appears in a credit.
    /// </summary>
    public static IReadOnlyDictionary<string, string> AttributionLinks { get; } = new Dictionary<string, string>
    {
        [Name] = "https://open-meteo.com/",
        ["CAMS ENSEMBLE data provider"] = "https://confluence.ecmwf.int/display/CKB/"
            + "CAMS+Regional%3A+European+air+quality+analysis+and+forecast+data+documentation/"
            + "#CAMSRegional:Europeanairqualityanalysisandforecastdatadocumentation-"
            + "Howtoacknowledge,citeandrefertothedata",
    };

    public static SourceContinent Continent => SourceContinent.Worldwide;

    // Pollen comes from the CAMS European forecast, which covers 25° W to 45° E and 30° N to 72° N.
    // See https://ads.atmosphere.copernicus.eu/datasets/cams-europe-air-quality-forecasts?tab=overview
    public static bool IsFeatureSupportedForLocation(Location location, SourceFeature feature)
    {
        return feature != SourceFeature.Pollen
            || location.Latitude is >= 30.0 and <= 72.0 && location.Longitude is >= -25.0 and <= 45.0;
    }

    /// <summary>
    /// Requests the given features. A feature that fails is listed in the result's failed features
    /// instead of failing the whole request.
    /// </summary>
    /// <exception cref="InvalidLocationException">The chosen weather models do not cover the location.</exception>
    public async Task<WeatherWrapper> RequestWeatherAsync(
        Location location,
        IReadOnlyCollection<SourceFeature> requestedFeatures,
        CancellationToken cancellationToken = default)
    {
        Dictionary<SourceFeature, Exception> failedFeatures = [];
        bool forecast = requestedFeatures.Contains(SourceFeature.Forecast);
        bool current = requestedFeatures.Contains(SourceFeature.Current);
        bool minutely = requestedFeatures.Contains(SourceFeature.Minutely);
        bool airQuality = requestedFeatures.Contains(SourceFeature.AirQuality);
        bool pollen = requestedFeatures.Contains(SourceFeature.Pollen);

        Task<OpenMeteoWeatherResult> weatherTask = forecast || minutely || current
            ? RequestForecastAsync(location, forecast, current, minutely, failedFeatures, cancellationToken)
            : Task.FromResult(new OpenMeteoWeatherResult());
        Task<OpenMeteoAirQualityResult> airQualityTask = airQuality || pollen
            ? RequestAirQualityAsync(location, airQuality, pollen, failedFeatures, cancellationToken)
            : Task.FromResult(new OpenMeteoAirQualityResult());

        await Task.WhenAll(weatherTask, airQualityTask).ConfigureAwait(false);
        OpenMeteoWeatherResult weather = await weatherTask.ConfigureAwait(false);
        OpenMeteoAirQualityResult airQualityResult = await airQualityTask.ConfigureAwait(false);

        return new WeatherWrapper
        {
            DailyForecast = forecast ? GetDailyList(weather.Daily, location) : null,
            HourlyForecast = forecast ? GetHourlyList(weather.Hourly) : null,
            Current = current ? GetCurrent(weather.Current) : null,
            AirQuality = airQuality ? GetAirQuality(airQualityResult.Hourly) : null,
            Pollen = pollen ? GetPollen(airQualityResult.Hourly) : null,
            MinutelyForecast = minutely ? GetMinutelyList(weather.MinutelyFifteen) : null,
            FailedFeatures = failedFeatures,
        };
    }

    /// <summary>
    /// Up to 20 places matching the query, named in the selected language.
    /// </summary>
    /// <exception cref="LocationSearchException">The search failed, as opposed to finding nothing.</exception>
    public async Task<IReadOnlyList<LocationAddressInfo>> RequestLocationSearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        string url = GeocodingBaseUrl + "v1/search?format=json"
            + "&name=" + Uri.EscapeDataString(query)
            + "&count=20"
            + "&language=" + Uri.EscapeDataString(LanguageCode(Strings.Culture));

        OpenMeteoLocationResults? results = await _httpClient
            .GetFromJsonAsync<OpenMeteoLocationResults>(url, cancellationToken)
            .ConfigureAwait(false);

        // A successful search with no match has a generation time but no results.
        if (results?.Results is null)
        {
            return results?.GenerationTimeMs is > 0.0 ? [] : throw new LocationSearchException();
        }

        return results.Results
            .Where(result => !string.IsNullOrEmpty(result.CountryCode))
            .Select(ConvertLocation)
            .ToList();
    }

    private async Task<OpenMeteoWeatherResult> RequestForecastAsync(
        Location location,
        bool forecast,
        bool current,
        bool minutely,
        Dictionary<SourceFeature, Exception> failedFeatures,
        CancellationToken cancellationToken)
    {
        string url = ForecastBaseUrl + "v1/forecast?timezone=auto&timeformat=unixtime"
            + "&latitude=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&longitude=" + location.Longitude.ToString(CultureInfo.InvariantCulture)
            + "&models=" + string.Join(",", GetWeatherModels(location).Select(model => model.Id))
            + "&daily=" + (forecast ? string.Join(",", DailyFields) : "")
            + "&hourly=" + (forecast ? string.Join(",", HourlyFields) : "")
            + "&minutely_15=" + (minutely ? string.Join(",", MinutelyFields) : "")
            + "&current=" + (current ? string.Join(",", CurrentFields) : "")
            + "&forecast_days=16&past_days=1&windspeed_unit=ms";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (body.Contains(NoDataMessage, StringComparison.Ordinal))
                {
                    throw new InvalidLocationException();
                }

                response.EnsureSuccessStatusCode();
            }

            return await response.Content
                .ReadFromJsonAsync<OpenMeteoWeatherResult>(cancellationToken)
                .ConfigureAwait(false) ?? new OpenMeteoWeatherResult();
        }
        catch (Exception exception) when (IsRecoverable(exception, cancellationToken))
        {
            lock (failedFeatures)
            {
                if (forecast)
                {
                    failedFeatures[SourceFeature.Forecast] = exception;
                }

                if (minutely)
                {
                    failedFeatures[SourceFeature.Minutely] = exception;
                }

                if (current)
                {
                    failedFeatures[SourceFeature.Current] = exception;
                }
            }

            return new OpenMeteoWeatherResult();
        }
    }

    private async Task<OpenMeteoAirQualityResult> RequestAirQualityAsync(
        Location location,
        bool airQuality,
        bool pollen,
        Dictionary<SourceFeature, Exception> failedFeatures,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> fields = (airQuality ? AirQualityFields : []).Concat(pollen ? PollenFields : []);
        string url = AirQualityBaseUrl + "v1/air-quality?timezone=auto&timeformat=unixtime"
            + "&latitude=" + location.Latitude.ToString(CultureInfo.InvariantCulture)
            + "&longitude=" + location.Longitude.ToString(CultureInfo.InvariantCulture)
            + "&hourly=" + string.Join(",", fields)
            + "&forecast_days=7&past_days=1";

        try
        {
            return await _httpClient
                .GetFromJsonAsync<OpenMeteoAirQualityResult>(url, cancellationToken)
                .ConfigureAwait(false) ?? new OpenMeteoAirQualityResult();
        }
        catch (Exception exception) when (IsRecoverable(exception, cancellationToken))
        {
            lock (failedFeatures)
            {
                if (airQuality)
                {
                    failedFeatures[SourceFeature.AirQuality] = exception;
                }

                if (pollen)
                {
                    failedFeatures[SourceFeature.Pollen] = exception;
                }
            }

            return new OpenMeteoAirQualityResult();
        }
    }

    // A cancellation asked by the caller and an uncovered location stop the whole request; any other error only fails its features.
    private static bool IsRecoverable(Exception exception, CancellationToken cancellationToken)
    {
        return exception is not InvalidLocationException
            && !(exception is OperationCanceledException && cancellationToken.IsCancellationRequested);
    }

    private static CurrentWrapper? GetCurrent(OpenMeteoWeatherCurrent? current)
    {
        if (current is null)
        {
            return null;
        }

        return new CurrentWrapper
        {
            WeatherText = GetWeatherText(current.WeatherCode),
            WeatherCode = GetWeatherCode(current.WeatherCode),
            Temperature = new TemperatureWrapper
            {
                Temperature = Celsius(current.Temperature),
                FeelsLike = Celsius(current.ApparentTemperature),
            },
            Wind = new Wind
            {
                Degree = current.WindDirection,
                Speed = MetersPerSecond(current.WindSpeed),
                Gusts = MetersPerSecond(current.WindGusts),
            },
            UV = new UV { Index = current.UVIndex },
            RelativeHumidity = Percent(current.RelativeHumidity),
            DewPoint = Celsius(current.DewPoint),
            Pressure = Hectopascals(current.PressureMsl),
            CloudCover = Percent(current.CloudCover),
            Visibility = Meters(current.Visibility),
        };
    }

    // The last day of the response is left out.
    private static IReadOnlyList<DailyWrapper>? GetDailyList(OpenMeteoWeatherDaily? daily, Location location)
    {
        if (daily is null)
        {
            return null;
        }

        List<DailyWrapper> dailyList = new(Math.Max(daily.Time.Length - 1, 0));
        for (int i = 0; i < daily.Time.Length - 1; i++)
        {
            dailyList.Add(new DailyWrapper
            {
                Date = StartOfDay(daily.Time[i], location.TimeZone),
                UV = new UV { Index = At(daily.UVIndexMax, i) },
                SunshineDuration = At(daily.SunshineDuration, i) is { } seconds ? TimeSpan.FromSeconds(seconds) : null,
                RelativeHumidity = new DailyRelativeHumidity
                {
                    Average = Percent(At(daily.RelativeHumidityMean, i)),
                    Max = Percent(At(daily.RelativeHumidityMax, i)),
                    Min = Percent(At(daily.RelativeHumidityMin, i)),
                },
                DewPoint = new DailyDewPoint
                {
                    Average = Celsius(At(daily.DewPointMean, i)),
                    Max = Celsius(At(daily.DewPointMax, i)),
                    Min = Celsius(At(daily.DewPointMin, i)),
                },
                Pressure = new DailyPressure
                {
                    Average = Hectopascals(At(daily.PressureMslMean, i)),
                    Max = Hectopascals(At(daily.PressureMslMax, i)),
                    Min = Hectopascals(At(daily.PressureMslMin, i)),
                },
                CloudCover = new DailyCloudCover
                {
                    Average = Percent(At(daily.CloudCoverMean, i)),
                    Max = Percent(At(daily.CloudCoverMax, i)),
                    Min = Percent(At(daily.CloudCoverMin, i)),
                },
                Visibility = new DailyVisibility
                {
                    Average = Meters(At(daily.VisibilityMean, i)),
                    Max = Meters(At(daily.VisibilityMax, i)),
                    Min = Meters(At(daily.VisibilityMin, i)),
                },
            });
        }

        return dailyList;
    }

    private static IReadOnlyList<HourlyWrapper>? GetHourlyList(OpenMeteoWeatherHourly? hourly)
    {
        if (hourly is null)
        {
            return null;
        }

        List<HourlyWrapper> hourlyList = new(hourly.Time.Length);
        for (int i = 0; i < hourly.Time.Length; i++)
        {
            int? weatherCode = At(hourly.WeatherCode, i);
            hourlyList.Add(new HourlyWrapper
            {
                Date = DateTimeOffset.FromUnixTimeSeconds(hourly.Time[i]),
                IsDaylight = hourly.IsDay is { } isDay && i < isDay.Length ? isDay[i] > 0 : null,
                WeatherText = GetWeatherText(weatherCode),
                WeatherCode = GetWeatherCode(weatherCode),
                Temperature = new TemperatureWrapper
                {
                    Temperature = Celsius(At(hourly.Temperature, i)),
                    FeelsLike = Celsius(At(hourly.ApparentTemperature, i)),
                },
                Precipitation = new Models.Precipitation
                {
                    Total = Millimeters(At(hourly.Precipitation, i)),
                    Rain = Millimeters(Sum(At(hourly.Rain, i), At(hourly.Showers, i))),
                    Snow = At(hourly.Snowfall, i) is { } snowfall ? Units.Precipitation.FromCentimeters(snowfall) : null,
                },
                PrecipitationProbability = new PrecipitationProbability
                {
                    Total = Percent(At(hourly.PrecipitationProbability, i)),
                },
                Wind = new Wind
                {
                    Degree = At(hourly.WindDirection, i),
                    Speed = MetersPerSecond(At(hourly.WindSpeed, i)),
                    Gusts = MetersPerSecond(At(hourly.WindGusts, i)),
                },
                UV = new UV { Index = At(hourly.UVIndex, i) },
                RelativeHumidity = Percent(At(hourly.RelativeHumidity, i)),
                DewPoint = Celsius(At(hourly.DewPoint, i)),
                Pressure = Hectopascals(At(hourly.PressureMsl, i)),
                CloudCover = Percent(At(hourly.CloudCover, i)),
                Visibility = Meters(At(hourly.Visibility, i)),
            });
        }

        return hourlyList;
    }

    private static AirQualityWrapper? GetAirQuality(OpenMeteoAirQualityHourly? hourly)
    {
        if (hourly is null)
        {
            return null;
        }

        Dictionary<DateTimeOffset, AirQuality> byHour = [];
        for (int i = 0; i < hourly.Time.Length; i++)
        {
            byHour[DateTimeOffset.FromUnixTimeSeconds(hourly.Time[i])] = new AirQuality
            {
                PM25 = Micrograms(At(hourly.PM25, i)),
                PM10 = Micrograms(At(hourly.PM10, i)),
                SO2 = Micrograms(At(hourly.SulphurDioxide, i)),
                NO2 = Micrograms(At(hourly.NitrogenDioxide, i)),
                O3 = Micrograms(At(hourly.Ozone, i)),
                CO = Micrograms(At(hourly.CarbonMonoxide, i)),
            };
        }

        return new AirQualityWrapper { HourlyForecast = byHour };
    }

    private static PollenWrapper? GetPollen(OpenMeteoAirQualityHourly? hourly)
    {
        if (hourly is null)
        {
            return null;
        }

        Dictionary<DateTimeOffset, Pollen> byHour = [];
        for (int i = 0; i < hourly.Time.Length; i++)
        {
            byHour[DateTimeOffset.FromUnixTimeSeconds(hourly.Time[i])] = new Pollen
            {
                Alder = PerCubicMeter(At(hourly.AlderPollen, i)),
                Birch = PerCubicMeter(At(hourly.BirchPollen, i)),
                Grass = PerCubicMeter(At(hourly.GrassPollen, i)),
                Mugwort = PerCubicMeter(At(hourly.MugwortPollen, i)),
                Olive = PerCubicMeter(At(hourly.OlivePollen, i)),
                Ragweed = PerCubicMeter(At(hourly.RagweedPollen, i)),
            };
        }

        return new PollenWrapper { HourlyForecast = byHour };
    }

    // Two hours of 15-minute intervals from the one in progress. Amounts per 15 minutes are multiplied by 4 to give mm/h.
    private static IReadOnlyList<Minutely>? GetMinutelyList(OpenMeteoWeatherMinutely? minutely)
    {
        if (minutely is null || minutely.Time.Length == 0)
        {
            return null;
        }

        long fifteenMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-15).ToUnixTimeSeconds();
        int start = Array.FindIndex(minutely.Time, time => time >= fifteenMinutesAgo);
        if (start < 0)
        {
            return null;
        }

        int end = Math.Min(start + 8, minutely.Time.Length - 1);
        List<Minutely> result = [];
        for (int i = start; i < end; i++)
        {
            result.Add(new Minutely
            {
                Date = DateTimeOffset.FromUnixTimeSeconds(minutely.Time[i]),
                MinuteInterval = 15,
                PrecipitationIntensity = At(minutely.Precipitation, i) is { } amount
                    ? Units.Precipitation.FromMillimeters(amount * 4)
                    : null,
            });
        }

        return result;
    }

    private static string? GetWeatherText(int? code)
    {
        string? key = code switch
        {
            0 => "openmeteo_weather_text_clear_sky",
            1 => "openmeteo_weather_text_mainly_clear",
            2 => "openmeteo_weather_text_partly_cloudy",
            3 => "openmeteo_weather_text_overcast",
            45 => "openmeteo_weather_text_fog",
            48 => "openmeteo_weather_text_depositing_rime_fog",
            51 => "openmeteo_weather_text_drizzle_light_intensity",
            53 => "openmeteo_weather_text_drizzle_moderate_intensity",
            55 => "openmeteo_weather_text_drizzle_dense_intensity",
            56 => "openmeteo_weather_text_freezing_drizzle_light_intensity",
            57 => "openmeteo_weather_text_freezing_drizzle_dense_intensity",
            61 => "openmeteo_weather_text_rain_slight_intensity",
            63 => "openmeteo_weather_text_rain_moderate_intensity",
            65 => "openmeteo_weather_text_rain_heavy_intensity",
            66 => "openmeteo_weather_text_freezing_rain_light_intensity",
            67 => "openmeteo_weather_text_freezing_rain_heavy_intensity",
            71 => "openmeteo_weather_text_snow_slight_intensity",
            73 => "openmeteo_weather_text_snow_moderate_intensity",
            75 => "openmeteo_weather_text_snow_heavy_intensity",
            77 => "openmeteo_weather_text_snow_grains",
            80 => "openmeteo_weather_text_rain_showers_slight",
            81 => "openmeteo_weather_text_rain_showers_moderate",
            82 => "openmeteo_weather_text_rain_showers_violent",
            85 => "openmeteo_weather_text_snow_showers_slight",
            86 => "openmeteo_weather_text_snow_showers_heavy",
            95 => "openmeteo_weather_text_thunderstorm_slight_or_moderate",
            96 => "openmeteo_weather_text_thunderstorm_with_slight_hail",
            99 => "openmeteo_weather_text_thunderstorm_with_heavy_hail",
            _ => null,
        };
        return key is null ? null : Strings.Get(key);
    }

    // WMO weather interpretation codes, as used by Open-Meteo.
    private static WeatherCode? GetWeatherCode(int? code)
    {
        return code switch
        {
            0 or 1 => WeatherCode.Clear,
            2 => WeatherCode.PartlyCloudy,
            3 => WeatherCode.Cloudy,
            45 or 48 => WeatherCode.Fog,
            51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => WeatherCode.Rain,
            71 or 73 or 75 or 85 or 86 => WeatherCode.Snow,
            77 => WeatherCode.Sleet,
            95 or 96 or 99 => WeatherCode.Thunderstorm,
            _ => null,
        };
    }

    private static LocationAddressInfo ConvertLocation(OpenMeteoLocationResult result)
    {
        return new LocationAddressInfo
        {
            Latitude = result.Latitude,
            Longitude = result.Longitude,
            TimeZoneId = result.Timezone,
            Country = result.Country,
            CountryCode = result.CountryCode!,
            Admin1 = result.Admin1,
            Admin2 = result.Admin2,
            Admin3 = result.Admin3,
            Admin4 = result.Admin4,
            City = result.Name,
            CityCode = result.Id.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static IReadOnlyList<OpenMeteoWeatherModel> GetWeatherModels(Location location)
    {
        if (location.Parameters.TryGetValue(Id, out IReadOnlyDictionary<string, string>? parameters)
            && parameters.TryGetValue("weatherModels", out string? models))
        {
            return models.Split(',')
                .Select(OpenMeteoWeatherModel.GetInstance)
                .OfType<OpenMeteoWeatherModel>()
                .ToList();
        }

        return [OpenMeteoWeatherModel.BestMatch];
    }

    // Open-Meteo names places in the language given; Traditional Chinese needs its region to be told apart.
    private static string LanguageCode(CultureInfo culture)
    {
        string language = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        bool traditionalChinese = language == "zh"
            && (culture.Name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                || culture.Name.EndsWith("-TW", StringComparison.OrdinalIgnoreCase)
                || culture.Name.EndsWith("-HK", StringComparison.OrdinalIgnoreCase)
                || culture.Name.EndsWith("-MO", StringComparison.OrdinalIgnoreCase));
        if (!traditionalChinese)
        {
            return language;
        }

        string region = culture.Name.Split('-')[^1].ToLowerInvariant();
        return language + "-" + (region == "hant" ? "tw" : region);
    }

    // Daily times are local midnight. Adding an hour before going back to midnight keeps the right day
    // when a daylight saving change makes the given time fall at 23:00 the day before.
    private static DateTimeOffset StartOfDay(long unixSeconds, TimeZoneInfo timeZone)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(unixSeconds), timeZone);
        DateTime midnight = local.AddHours(1).Date;
        return new DateTimeOffset(midnight, timeZone.GetUtcOffset(midnight));
    }

    private static T? At<T>(T?[]? values, int index)
        where T : struct
    {
        return values is not null && index < values.Length ? values[index] : null;
    }

    // Null only when both are missing, so a missing shower amount does not hide the rain amount.
    private static double? Sum(double? first, double? second)
    {
        return first is null && second is null ? null : (first ?? 0.0) + (second ?? 0.0);
    }

    private static Units.Temperature? Celsius(double? value) => value is { } v ? Units.Temperature.FromCelsius(v) : null;

    private static Speed? MetersPerSecond(double? value) => value is { } v ? Speed.FromMetersPerSecond(v) : null;

    private static Ratio? Percent(int? value) => value is { } v ? Ratio.FromPercent((long)v) : null;

    private static Pressure? Hectopascals(double? value) => value is { } v ? Pressure.FromHectopascals(v) : null;

    private static Distance? Meters(double? value) => value is { } v ? Distance.FromMeters(v) : null;

    private static Units.Precipitation? Millimeters(double? value) =>
        value is { } v ? Units.Precipitation.FromMillimeters(v) : null;

    private static PollutantConcentration? Micrograms(double? value) =>
        value is { } v ? PollutantConcentration.FromMicrogramsPerCubicMeter(v) : null;

    private static PollenConcentration? PerCubicMeter(double? value) =>
        value is { } v ? PollenConcentration.FromPerCubicMeter(v) : null;
}
