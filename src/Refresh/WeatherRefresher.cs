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
using System.Net.NetworkInformation;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.OpenMeteo;

namespace LibreccioWeather.Core.Refresh;

/// <summary>
/// Refreshes the weather of a location: requests each feature from its chosen source, keeps cached data that is
/// still fresh or that a source failed to replace, and completes the result into one weather model.
/// </summary>
public sealed class WeatherRefresher
{
    // Minutes a feature stays fresh before it is requested again.
    public const int WaitMain = 5;
    public const int WaitCurrent = 1;
    public const int WaitAirQuality = 5;
    public const int WaitPollen = 5;
    public const int WaitMinutely = 5;
    public const int WaitMinutelyOngoing = 1;
    public const int WaitAlerts = 5;
    public const int WaitAlertsOngoing = 1;

    /// <summary>
    /// Distance in meters within which a current position keeps its normals.
    /// </summary>
    public const int CachingDistanceLimit = 5000;

    // Current conditions refreshed less than this many minutes ago are reused when a refresh brings none.
    private const int CurrentReuseMinutes = 30;

    private const int MaxConcurrentSources = 5;

    private readonly OpenMeteoService _openMeteo;

    public WeatherRefresher(OpenMeteoService openMeteo)
    {
        _openMeteo = openMeteo;
    }

    /// <param name="ignoreCaching">Requests every feature even when its cached data is still fresh.</param>
    public async Task<WeatherResult> GetWeatherAsync(
        Location location,
        bool ignoreCaching = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!location.IsUsable || location.NeedsGeocodeRefresh)
            {
                return new WeatherResult(location.Weather, [new RefreshError(RefreshErrorType.InvalidLocation)]);
            }

            Dictionary<string, List<SourceFeature>> featuresBySource = GroupFeaturesBySource(location);

            // The refresh time shown to the user always changes, even when only cached data is reused.
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Base previousBase = location.Weather?.Base ?? new Base();
            Base newBase = previousBase with { RefreshTime = now };

            DateTimeOffset yesterdayMidnight = StartOfDay(WeatherCompletion.IsoDate(now.AddDays(-1), location.TimeZone), location.TimeZone);

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return new WeatherResult(location.Weather, [new RefreshError(RefreshErrorType.NetworkUnavailable)]);
            }

            if (featuresBySource.Count == 0)
            {
                return new WeatherResult(location.Weather, [new RefreshError(RefreshErrorType.InvalidLocation)]);
            }

            List<RefreshError> errors = [];
            Dictionary<string, WeatherWrapper?> sourceCalls = [];
            using (SemaphoreSlim semaphore = new(MaxConcurrentSources))
            {
                await Task.WhenAll(featuresBySource.Select(async entry =>
                {
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        WeatherWrapper? result = await RequestSourceAsync(
                            entry.Key, entry.Value, location, ignoreCaching, errors, cancellationToken).ConfigureAwait(false);
                        lock (sourceCalls)
                        {
                            sourceCalls[entry.Key] = result;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })).ConfigureAwait(false);
            }

            foreach ((string source, WeatherWrapper? wrapper) in sourceCalls)
            {
                foreach ((SourceFeature feature, Exception exception) in wrapper?.FailedFeatures ?? new Dictionary<SourceFeature, Exception>())
                {
                    errors.Add(new RefreshError(
                        RefreshError.GetTypeFromException(exception, RefreshErrorType.DataRefreshFailed), source, feature));
                }
            }

            UpdateTimes times = new(newBase);
            WeatherWrapper weatherWrapper = SelectFromSources(location, sourceCalls, errors, yesterdayMidnight, now, times);

            // 1) Brings back hours and days since yesterday's midnight that the new data no longer has.
            WeatherWrapper completed = WeatherCompletion.CompleteNewWeatherWithPreviousData(
                weatherWrapper, location.Weather, yesterdayMidnight, location.AirQualitySource, location.PollenSource);

            // 2) Computes everything possible for each hour.
            IReadOnlyList<Hourly> hourlyComputed = WeatherCompletion.ComputeMissingHourlyData(completed.HourlyForecast) ?? [];

            // 3) Builds the days with their air quality and pollen, then completes them from the hours.
            IReadOnlyList<Daily> dailyForecast = WeatherCompletion.CompleteDailyListFromHourlyList(
                WeatherCompletion.ConvertDailyWrapperToDailyList(completed),
                hourlyComputed,
                completed.AirQuality?.HourlyForecast ?? new Dictionary<DateTimeOffset, AirQuality>(),
                completed.Pollen?.HourlyForecast ?? new Dictionary<DateTimeOffset, Pollen>(),
                ByDate(completed.HourlyForecast ?? [], hourly => hourly.Date, hourly => hourly.SunshineDuration),
                completed.Pollen?.Current,
                location);

            // 4) Completes the hours from their day.
            IReadOnlyList<Hourly> hourlyForecast = WeatherCompletion.CompleteHourlyListFromDailyList(
                hourlyComputed,
                dailyForecast,
                completed.AirQuality?.HourlyForecast ?? new Dictionary<DateTimeOffset, AirQuality>(),
                location);

            // Hourly air quality that matches no forecast hour cannot be shown with it.
            if (completed.AirQuality?.HourlyForecast is { Count: > 0 } hourlyAirQuality
                && hourlyForecast.Count > 0
                && !hourlyForecast.Any(hourly => hourlyAirQuality.ContainsKey(hourly.Date)))
            {
                errors.Add(new RefreshError(
                    RefreshErrorType.IncompatibleForecastTimes, location.AirQualitySource, SourceFeature.AirQuality));
            }

            // At 15:59, the current hour is the one starting at 15:00. Today is found with 23 hours of margin
            // for daylight saving changes.
            Hourly? currentHour = hourlyForecast.FirstOrDefault(hourly => hourly.Date >= now.AddHours(-1));
            Daily? currentDay = dailyForecast.FirstOrDefault(daily => daily.Date >= yesterdayMidnight.AddHours(23));

            bool currentStillValid = IsUpdateStillValid(newBase.CurrentUpdateTime, CurrentReuseMinutes);
            CurrentWrapper? currentWrapper = completed.Current
                ?? (currentStillValid ? location.Weather?.Current?.ToCurrentWrapper() : null);
            AirQuality? currentAirQuality = completed.AirQuality?.Current?.ToValid()
                ?? (currentStillValid
                    ? location.Weather?.Current?.AirQuality
                    : completed.AirQuality?.HourlyForecast?.FirstOrDefault(pair => pair.Key >= now.AddHours(-1)).Value);

            Weather weather = new()
            {
                Base = times.ToBase(newBase),
                Current = WeatherCompletion.CompleteCurrentFromHourlyData(
                    currentWrapper, currentHour, currentDay, currentAirQuality, location),
                DailyForecast = dailyForecast,
                HourlyForecast = hourlyForecast,
                MinutelyForecast = completed.MinutelyForecast?.Select(minutely => minutely.ToValidOrNull()).ToList() ?? [],
                AlertList = completed.AlertList ?? [],
                Normals = completed.Normals ?? new Dictionary<Month, Normals>(),
            };
            return new WeatherResult(weather, errors);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            return new WeatherResult(location.Weather, [new RefreshError(RefreshErrorType.DataRefreshFailed)]);
        }
    }

    private static Dictionary<string, List<SourceFeature>> GroupFeaturesBySource(Location location)
    {
        (string? Source, SourceFeature Feature)[] choices =
        [
            (location.ForecastSource, SourceFeature.Forecast),
            (location.CurrentSource, SourceFeature.Current),
            (location.AirQualitySource, SourceFeature.AirQuality),
            (location.PollenSource, SourceFeature.Pollen),
            (location.MinutelySource, SourceFeature.Minutely),
            (location.AlertSource, SourceFeature.Alert),
            (location.NormalsSource, SourceFeature.Normals),
        ];

        Dictionary<string, List<SourceFeature>> bySource = [];
        foreach ((string? source, SourceFeature feature) in choices)
        {
            if (string.IsNullOrEmpty(source))
            {
                continue;
            }

            if (!bySource.TryGetValue(source, out List<SourceFeature>? features))
            {
                features = [];
                bySource[source] = features;
            }

            features.Add(feature);
        }

        return bySource;
    }

    // Returns null when nothing was requested from the source, so that previous data is used.
    private async Task<WeatherWrapper?> RequestSourceAsync(
        string source,
        List<SourceFeature> features,
        Location location,
        bool ignoreCaching,
        List<RefreshError> errors,
        CancellationToken cancellationToken)
    {
        void AddError(RefreshError error)
        {
            lock (errors)
            {
                errors.Add(error);
            }
        }

        if (source != OpenMeteoService.Id)
        {
            AddError(new RefreshError(RefreshErrorType.SourceNotInstalled, source));
            return null;
        }

        List<SourceFeature> featuresToUpdate = features
            .Where(feature =>
            {
                if (!OpenMeteoService.SupportedFeatures.ContainsKey(feature)
                    || !OpenMeteoService.IsFeatureSupportedForLocation(location, feature))
                {
                    AddError(new RefreshError(RefreshErrorType.UnsupportedFeature, source));
                    return false;
                }

                return true;
            })
            .Where(feature => ignoreCaching || !IsWeatherDataStillValid(location, feature))
            .ToList();

        if (featuresToUpdate.Count == 0)
        {
            return null;
        }

        try
        {
            return await _openMeteo.RequestWeatherAsync(location, featuresToUpdate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            RefreshErrorType type = RefreshError.GetTypeFromException(exception, RefreshErrorType.DataRefreshFailed);
            foreach (SourceFeature feature in featuresToUpdate)
            {
                AddError(new RefreshError(type, source, feature));
            }

            return null;
        }
    }

    // Each feature comes only from the source chosen for it. When that source failed or sent nothing,
    // the previous data is kept.
    private static WeatherWrapper SelectFromSources(
        Location location,
        Dictionary<string, WeatherWrapper?> sourceCalls,
        List<RefreshError> errors,
        DateTimeOffset yesterdayMidnight,
        DateTimeOffset now,
        UpdateTimes times)
    {
        bool Failed(string source, SourceFeature feature) =>
            errors.Any(error => error.Feature == feature && error.Source == source);

        WeatherWrapper? From(string? source) =>
            source is not null ? sourceCalls.GetValueOrDefault(source) : null;

        IReadOnlyList<DailyWrapper>? dailyForecast = null;
        IReadOnlyList<HourlyWrapper>? hourlyForecast = null;
        if (location.ForecastSource.Length > 0 && !Failed(location.ForecastSource, SourceFeature.Forecast))
        {
            if (From(location.ForecastSource)?.DailyForecast is { } daily)
            {
                if (daily.Count == 0)
                {
                    errors.Add(new RefreshError(RefreshErrorType.InvalidIncompleteData, location.ForecastSource, SourceFeature.Forecast));
                }
                else
                {
                    times.Forecast = now;
                    dailyForecast = daily;
                }
            }

            hourlyForecast = From(location.ForecastSource)?.HourlyForecast;
        }

        CurrentWrapper? current = null;
        if (!string.IsNullOrEmpty(location.CurrentSource)
            && !Failed(location.CurrentSource, SourceFeature.Current)
            && From(location.CurrentSource)?.Current is { } newCurrent)
        {
            times.Current = now;
            current = newCurrent;
        }

        AirQualityWrapper? airQuality = null;
        if (!string.IsNullOrEmpty(location.AirQualitySource))
        {
            airQuality = !Failed(location.AirQualitySource, SourceFeature.AirQuality)
                && From(location.AirQualitySource)?.AirQuality is { } newAirQuality
                ? Updated(newAirQuality, () => times.AirQuality = now)
                : location.Weather?.ToAirQualityWrapperList(yesterdayMidnight);
        }

        PollenWrapper? pollen = null;
        if (!string.IsNullOrEmpty(location.PollenSource))
        {
            pollen = !Failed(location.PollenSource, SourceFeature.Pollen)
                && From(location.PollenSource)?.Pollen is { } newPollen
                ? Updated(newPollen, () => times.Pollen = now)
                : location.Weather?.ToPollenWrapperList(yesterdayMidnight);
        }

        IReadOnlyList<Minutely>? minutely = null;
        if (!string.IsNullOrEmpty(location.MinutelySource))
        {
            minutely = !Failed(location.MinutelySource, SourceFeature.Minutely)
                && From(location.MinutelySource)?.MinutelyForecast is { } newMinutely
                ? Updated(newMinutely, () => times.Minutely = now)
                : location.Weather?.ToMinutelyWrapper();
        }

        // Alerts received despite an error are still accepted.
        IReadOnlyList<Alert>? alerts = null;
        if (!string.IsNullOrEmpty(location.AlertSource))
        {
            IReadOnlyList<Alert>? newAlerts = From(location.AlertSource)?.AlertList;
            bool rejected = Failed(location.AlertSource, SourceFeature.Alert) && (newAlerts is null || newAlerts.Count == 0);
            alerts = !rejected && newAlerts is not null
                ? Updated(newAlerts, () => times.Alerts = now)
                : location.Weather?.ToAlertsWrapper();
        }

        // New normals are combined with the months already stored, except for the current position.
        IReadOnlyDictionary<Month, Normals>? normals = null;
        if (!string.IsNullOrEmpty(location.NormalsSource))
        {
            if (!Failed(location.NormalsSource, SourceFeature.Normals)
                && From(location.NormalsSource)?.Normals is { } newNormals)
            {
                times.Normals = now;
                times.NormalsLatitude = location.Latitude;
                times.NormalsLongitude = location.Longitude;
                Dictionary<Month, Normals> combined = new(
                    (!location.IsCurrentPosition ? location.Weather?.Normals : null) ?? new Dictionary<Month, Normals>());
                foreach ((Month month, Normals value) in newNormals)
                {
                    combined[month] = value;
                }

                normals = combined;
            }
            else
            {
                normals = location.Weather?.Normals;
            }
        }

        return new WeatherWrapper
        {
            DailyForecast = dailyForecast ?? location.Weather?.ToDailyWrapperList(yesterdayMidnight),
            HourlyForecast = hourlyForecast ?? location.Weather?.ToHourlyWrapperList(yesterdayMidnight),
            Current = current,
            AirQuality = airQuality,
            Pollen = pollen,
            MinutelyForecast = minutely,
            AlertList = alerts,
            Normals = normals,
        };
    }

    private static T Updated<T>(T value, Action markUpdated)
    {
        markUpdated();
        return value;
    }

    private static bool IsWeatherDataStillValid(Location location, SourceFeature feature)
    {
        if (location.Weather is not { } weather)
        {
            return false;
        }

        Base weatherBase = weather.Base;
        switch (feature)
        {
            case SourceFeature.Current:
                return IsUpdateStillValid(weatherBase.CurrentUpdateTime, WaitCurrent);
            case SourceFeature.AirQuality:
                return IsUpdateStillValid(weatherBase.AirQualityUpdateTime, WaitAirQuality);
            case SourceFeature.Pollen:
                return IsUpdateStillValid(weatherBase.PollenUpdateTime, WaitPollen);
            case SourceFeature.Minutely:
                bool precipitationComing = weather.MinutelyForecast.Any(minutely => (minutely.PrecipitationIntensity?.InMicrometers ?? 0.0) > 0);
                return IsUpdateStillValid(weatherBase.MinutelyUpdateTime, precipitationComing ? WaitMinutelyOngoing : WaitMinutely);
            case SourceFeature.Alert:
                return IsUpdateStillValid(weatherBase.AlertsUpdateTime, weather.CurrentAlertList.Count == 0 ? WaitAlerts : WaitAlertsOngoing);
            case SourceFeature.Normals:
                if (weatherBase.NormalsUpdateTime is null
                    || weatherBase.NormalsUpdateLongitude == 0.0
                    || weatherBase.NormalsUpdateLatitude == 0.0)
                {
                    return false;
                }

                if (location.IsCurrentPosition)
                {
                    return DistanceBetween(
                        weatherBase.NormalsUpdateLatitude,
                        weatherBase.NormalsUpdateLongitude,
                        location.Latitude,
                        location.Longitude) <= CachingDistanceLimit;
                }

                Month month = Month.Of(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Month);
                return weather.Normals.TryGetValue(month, out Normals? normals)
                    && (normals.DaytimeTemperature is not null || normals.NighttimeTemperature is not null);
            default:
                return IsUpdateStillValid(weatherBase.ForecastUpdateTime, WaitMain);
        }
    }

    private static bool IsUpdateStillValid(DateTimeOffset? updateTime, int waitMinutes)
    {
        if (updateTime is not { } time)
        {
            return false;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return now >= time && now - time < TimeSpan.FromMinutes(waitMinutes);
    }

    // Great-circle distance in meters, with the mean Earth radius used by Google's maps-utils.
    private static double DistanceBetween(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double EarthRadius = 6371009.0;
        double lat1 = latitude1 * Math.PI / 180.0;
        double lat2 = latitude2 * Math.PI / 180.0;
        double deltaLatitude = lat2 - lat1;
        double deltaLongitude = (longitude2 - longitude1) * Math.PI / 180.0;
        double haversine = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);
        return 2 * EarthRadius * Math.Asin(Math.Sqrt(haversine));
    }

    private static DateTimeOffset StartOfDay(string isoDate, TimeZoneInfo timeZone)
    {
        DateTime midnight = DateTime.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DateTimeOffset(midnight, timeZone.GetUtcOffset(midnight));
    }

    private static Dictionary<DateTimeOffset, TValue> ByDate<TItem, TValue>(
        IEnumerable<TItem> items,
        Func<TItem, DateTimeOffset> date,
        Func<TItem, TValue> value)
    {
        Dictionary<DateTimeOffset, TValue> result = [];
        foreach (TItem item in items)
        {
            result[date(item)] = value(item);
        }

        return result;
    }

    // The update time of each feature, changed when the feature receives new data during the refresh.
    private sealed class UpdateTimes(Base previous)
    {
        public DateTimeOffset? Forecast { get; set; } = previous.ForecastUpdateTime;

        public DateTimeOffset? Current { get; set; } = previous.CurrentUpdateTime;

        public DateTimeOffset? AirQuality { get; set; } = previous.AirQualityUpdateTime;

        public DateTimeOffset? Pollen { get; set; } = previous.PollenUpdateTime;

        public DateTimeOffset? Minutely { get; set; } = previous.MinutelyUpdateTime;

        public DateTimeOffset? Alerts { get; set; } = previous.AlertsUpdateTime;

        public DateTimeOffset? Normals { get; set; } = previous.NormalsUpdateTime;

        public double NormalsLatitude { get; set; } = previous.NormalsUpdateLatitude;

        public double NormalsLongitude { get; set; } = previous.NormalsUpdateLongitude;

        public Base ToBase(Base current)
        {
            return current with
            {
                ForecastUpdateTime = Forecast,
                CurrentUpdateTime = Current,
                AirQualityUpdateTime = AirQuality,
                PollenUpdateTime = Pollen,
                MinutelyUpdateTime = Minutely,
                AlertsUpdateTime = Alerts,
                NormalsUpdateTime = Normals,
                NormalsUpdateLatitude = NormalsLatitude,
                NormalsUpdateLongitude = NormalsLongitude,
            };
        }
    }
}
