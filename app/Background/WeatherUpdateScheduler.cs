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

using System.Net.NetworkInformation;
using LibreccioWeather.Core.Background;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using Microsoft.Win32;

namespace LibreccioWeather.Background;

/// <summary>
/// Runs the weather update job at the interval chosen in the settings, counted from the last update, so an update
/// that fell due while the app was closed or the computer was asleep runs as soon as it can.
/// It only runs with a network and, when the setting asks for it, not while Battery saver is on.
/// </summary>
public sealed class WeatherUpdateScheduler : IDisposable
{
    private const int MaximumRetryCount = 30;

    // Shorter intervals turn background updates off.
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(15);

    // Each retry waits this much longer than the previous one.
    private static readonly TimeSpan BackoffDelay = TimeSpan.FromMinutes(10);

    private readonly WeatherUpdateJob _job;
    private readonly SettingsManager _settings;
    private readonly Timer _timer;
    private readonly Lock _lock = new();
    private int _retryCount;
    private DateTimeOffset? _retryAt;
    private bool _disposed;

    public WeatherUpdateScheduler(WeatherUpdateJob job, SettingsManager settings)
    {
        _job = job;
        _settings = settings;
        _timer = new Timer(_ => _ = RunAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        _settings.SettingsChanged += OnSettingsChanged;
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Reschedule();
    }

    public void Dispose()
    {
        _settings.SettingsChanged -= OnSettingsChanged;
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        lock (_lock)
        {
            _disposed = true;
            _timer.Dispose();
        }
    }

    private void Reschedule(bool resetRetry = false)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (_settings.UpdateInterval.Interval is not { } interval || interval <= MinimumInterval)
            {
                _retryCount = 0;
                _retryAt = null;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            if (resetRetry)
            {
                _retryCount = 0;
                _retryAt = null;
            }

            DateTimeOffset due = _retryAt
                ?? DateTimeOffset.FromUnixTimeMilliseconds(_settings.WeatherUpdateLastTimestamp) + interval;
            TimeSpan delay = due - DateTimeOffset.UtcNow;
            _timer.Change(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task RunAsync()
    {
        lock (_lock)
        {
            if (_disposed || _settings.UpdateInterval.Interval is not { } interval || interval <= MinimumInterval)
            {
                return;
            }

            if (_retryAt is { } retryAt)
            {
                TimeSpan remaining = retryAt - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    _timer.Change(remaining, Timeout.InfiniteTimeSpan);
                    return;
                }
            }
        }

        // Battery saver stands for Android's low battery state: Windows or the user decides when it applies.
        // Without the conditions the job waits. A change of network or power state schedules it again, and it is also
        // checked again after the retry delay, in case Windows reports no change when Battery saver turns off.
        if (!NetworkStatus.IsOnline() || (_settings.IgnoreUpdatesWhenBatteryLow && BatterySaver.IsOn()))
        {
            lock (_lock)
            {
                if (!_disposed && _settings.UpdateInterval.Interval is { } interval && interval > MinimumInterval)
                {
                    _timer.Change(BackoffDelay, Timeout.InfiniteTimeSpan);
                }
            }

            return;
        }

        WeatherUpdateJobResult result;
        try
        {
            result = await _job.RunAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            result = WeatherUpdateJobResult.Failure;
        }

        if (result is WeatherUpdateJobResult.Retry or WeatherUpdateJobResult.AlreadyRunning or WeatherUpdateJobResult.Failure)
        {
            lock (_lock)
            {
                if (!_disposed && _settings.UpdateInterval.Interval is { } interval && interval > MinimumInterval)
                {
                    _retryCount = Math.Min(_retryCount + 1, MaximumRetryCount);
                    TimeSpan delay = BackoffDelay * _retryCount;
                    _retryAt = DateTimeOffset.UtcNow + delay;
                    _timer.Change(delay, Timeout.InfiniteTimeSpan);
                }
            }
        }
        else
        {
            Reschedule(resetRetry: true);
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        if (e is SettingsChangedEventArgs
            {
                PropertyName: not (nameof(SettingsManager.UpdateInterval) or nameof(SettingsManager.IgnoreUpdatesWhenBatteryLow)),
            })
        {
            return;
        }

        Reschedule();
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (e.IsAvailable)
        {
            Reschedule();
        }
    }

    // A resume or a change of power state, such as Battery saver turning off, may let a waiting update run.
    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Resume or PowerModes.StatusChange)
        {
            Reschedule();
        }
    }
}
