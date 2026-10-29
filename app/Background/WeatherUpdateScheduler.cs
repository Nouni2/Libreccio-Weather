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
    // Shorter intervals turn background updates off.
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(15);

    // Each retry waits this much longer than the previous one.
    private static readonly TimeSpan BackoffDelay = TimeSpan.FromMinutes(10);

    private readonly WeatherUpdateJob _job;
    private readonly SettingsManager _settings;
    private readonly Timer _timer;
    private readonly Lock _lock = new();
    private int _retryCount;

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
        _timer.Dispose();
    }

    private void Reschedule()
    {
        lock (_lock)
        {
            _retryCount = 0;
            if (_settings.UpdateInterval.Interval is not { } interval || interval <= MinimumInterval)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }

            DateTimeOffset due = DateTimeOffset.FromUnixTimeMilliseconds(_settings.WeatherUpdateLastTimestamp) + interval;
            TimeSpan delay = due - DateTimeOffset.UtcNow;
            _timer.Change(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }
    }

    private async Task RunAsync()
    {
        // Battery saver stands for Android's low battery state: Windows or the user decides when it applies.
        // Without the conditions the job waits. A change of network or power state schedules it again, and it is also
        // checked again after the retry delay, in case Windows reports no change when Battery saver turns off.
        if (!NetworkStatus.IsOnline() || (_settings.IgnoreUpdatesWhenBatteryLow && BatterySaver.IsOn()))
        {
            lock (_lock)
            {
                _timer.Change(BackoffDelay, Timeout.InfiniteTimeSpan);
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

        if (result is WeatherUpdateJobResult.Retry or WeatherUpdateJobResult.AlreadyRunning)
        {
            lock (_lock)
            {
                _retryCount++;
                _timer.Change(BackoffDelay * _retryCount, Timeout.InfiniteTimeSpan);
            }
        }
        else
        {
            Reschedule();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
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
