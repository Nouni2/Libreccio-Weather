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
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.Core.Background;

public enum WeatherUpdateJobResult
{
    Success,

    /// <summary>
    /// The job could not run now and should be tried again later, such as without a network.
    /// </summary>
    Retry,

    Failure,

    /// <summary>
    /// Another update was already running, so this one did not start.
    /// </summary>
    AlreadyRunning,
}

/// <summary>
/// Refreshes saved locations in the background: the first one, plus a few more once a day when Mistray shows several.
/// One update runs at a time.
/// </summary>
public sealed class WeatherUpdateJob
{
    // Mistray's multiple locations style shows this many locations.
    private const int CitiesStyleLocationCount = 4;

    private readonly WeatherRefresher _refresher;
    private readonly LocationRepository _locations;
    private readonly WeatherRepository _weather;
    private readonly SettingsManager _settings;
    private readonly SemaphoreSlim _running = new(1, 1);
    private readonly HashSet<string> _locationsToRetry = new(StringComparer.Ordinal);

    public WeatherUpdateJob(
        WeatherRefresher refresher,
        LocationRepository locations,
        WeatherRepository weather,
        SettingsManager settings)
    {
        _refresher = refresher;
        _locations = locations;
        _weather = weather;
        _settings = settings;
    }

    /// <summary>
    /// Raised for each location whose weather was refreshed, with its new weather. It can be raised on any thread.
    /// </summary>
    public event EventHandler<Location>? LocationUpdated;

    /// <param name="locationFormattedId">The location to refresh, or null to choose them automatically.</param>
    public async Task<WeatherUpdateJobResult> RunAsync(string? locationFormattedId = null, CancellationToken cancellationToken = default)
    {
        if (!await _running.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return WeatherUpdateJobResult.AlreadyRunning;
        }

        try
        {
            if (!NetworkStatus.IsOnline())
            {
                return WeatherUpdateJobResult.Retry;
            }

            _settings.WeatherUpdateLastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            try
            {
                List<Location> locationsToUpdate = await GetLocationsToUpdateAsync(locationFormattedId).ConfigureAwait(false);
                bool retryRequired = await UpdateWeatherDataAsync(locationsToUpdate, cancellationToken).ConfigureAwait(false);
                return retryRequired ? WeatherUpdateJobResult.Failure : WeatherUpdateJobResult.Success;
            }
            catch (OperationCanceledException)
            {
                // A cancelled update counts as done, so it is not tried again straight away.
                return WeatherUpdateJobResult.Success;
            }
            catch (Exception)
            {
                return WeatherUpdateJobResult.Failure;
            }
        }
        finally
        {
            _running.Release();
        }
    }

    private async Task<List<Location>> GetLocationsToUpdateAsync(string? locationFormattedId)
    {
        if (locationFormattedId is not null)
        {
            Location? location = await _locations.GetLocationAsync(locationFormattedId).ConfigureAwait(false);
            return location is null ? [] : [await WithWeatherAsync(location).ConfigureAwait(false)];
        }

        int count = _settings.IsWidgetNotificationEnabled && _settings.WidgetNotificationStyle == NotificationStyle.Cities
            ? CitiesStyleLocationCount
            : 1;

        // The device's own date, not the location's, as the refresh dates are compared with it.
        string today = DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        List<Location> locations = [];
        IReadOnlyList<Location> saved = await _locations.GetXLocationsAsync(count).ConfigureAwait(false);
        _locationsToRetry.IntersectWith(saved.Select(location => location.FormattedId));
        for (int index = 0; index < saved.Count; index++)
        {
            Location location = await WithWeatherAsync(saved[index]).ConfigureAwait(false);

            // Secondary locations use daily information, but a failed attempt must not count as their daily refresh.
            if (index == 0
                || _locationsToRetry.Contains(location.FormattedId)
                || location.Weather?.Base.RefreshTime is not { } refreshTime
                || string.CompareOrdinal(WeatherCompletion.IsoDate(refreshTime, location.TimeZone), today) < 0)
            {
                locations.Add(location);
            }
        }

        return locations;
    }

    private async Task<bool> UpdateWeatherDataAsync(List<Location> locationsToUpdate, CancellationToken cancellationToken)
    {
        List<Location> newUpdates = [];
        bool retryRequired = false;
        foreach (Location location in locationsToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The location itself is not refreshed: there is no current position or reverse geocoding.
            if (location.IsUsable && !location.NeedsGeocodeRefresh)
            {
                try
                {
                    WeatherResult result = await _refresher.GetWeatherAsync(location, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    // Configuration errors need a settings change; retry errors that another request can resolve.
                    bool locationRetryRequired = result.Errors.Any(error => error.Error is
                        RefreshErrorType.NetworkUnavailable or RefreshErrorType.ServerTimeout
                        or RefreshErrorType.ApiLimitReached or RefreshErrorType.ServerUnavailable
                        or RefreshErrorType.ParsingError or RefreshErrorType.InvalidIncompleteData
                        or RefreshErrorType.OutdatedServerData or RefreshErrorType.IncompatibleForecastTimes
                        or RefreshErrorType.DataRefreshFailed);
                    retryRequired |= locationRetryRequired;
                    if (locationRetryRequired)
                    {
                        // A failed feature may still advance the refresh timestamp used to skip secondary cities.
                        _locationsToRetry.Add(location.FormattedId);
                    }
                    else
                    {
                        _locationsToRetry.Remove(location.FormattedId);
                    }

                    newUpdates.Add(location with { Weather = result.Weather });
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _locationsToRetry.Add(location.FormattedId);
                    retryRequired = true;
                }
            }
        }

        foreach (Location location in newUpdates)
        {
            LocationUpdated?.Invoke(this, location);
        }

        return retryRequired;
    }

    private async Task<Location> WithWeatherAsync(Location location)
    {
        return location with { Weather = await _weather.GetWeatherByLocationIdAsync(location.FormattedId).ConfigureAwait(false) };
    }
}
