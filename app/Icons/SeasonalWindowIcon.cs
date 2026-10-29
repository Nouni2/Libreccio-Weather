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

using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Seasons;
using Microsoft.Win32;

namespace LibreccioWeather.Icons;

/// <summary>
/// Gives a window the icon of the current season at the location on screen: its latitude and today's date in its
/// time zone. Without a location, the computer's date and the northern hemisphere are used.
/// The season is checked again at the next midnight, when the system clock changes and when Windows resumes.
/// </summary>
public sealed class SeasonalWindowIcon : IDisposable
{
    private readonly Window _window;
    private readonly DispatcherTimer _midnightTimer;
    private readonly Dictionary<Season, BitmapFrame> _icons = [];
    private Location? _location;

    public SeasonalWindowIcon(Window window)
    {
        _window = window;
        _midnightTimer = new DispatcherTimer(DispatcherPriority.Normal, window.Dispatcher);
        _midnightTimer.Tick += (_, _) => Update();

        // These events are static, so they keep this object alive until Dispose detaches them.
        SystemEvents.TimeChanged += OnTimeChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Update();
    }

    /// <summary>
    /// Follows the location now on screen, or none.
    /// </summary>
    public void ShowLocation(Location? location)
    {
        _location = location;
        Update();
    }

    public void Dispose()
    {
        SystemEvents.TimeChanged -= OnTimeChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _midnightTimer.Stop();
    }

    private void Update()
    {
        TimeZoneInfo timeZone = _location?.TimeZone ?? TimeZoneInfo.Local;
        DateTimeOffset now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        Season season = MeteorologicalSeasons.Get(DateOnly.FromDateTime(now.DateTime), _location?.Latitude ?? 0.0);
        _window.Icon = GetIcon(season);

        // The midnight of the time zone whose date decides the season. If that midnight is skipped by a daylight saving
        // change, the timer fires near it and the next check finds the new date or waits again.
        DateTime nextMidnight = now.Date.AddDays(1);
        DateTimeOffset nextMidnightUtc = new(nextMidnight, timeZone.GetUtcOffset(nextMidnight));
        TimeSpan delay = nextMidnightUtc - DateTimeOffset.UtcNow;
        _midnightTimer.Stop();
        _midnightTimer.Interval = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        _midnightTimer.Start();
    }

    // Loaded with the ICO decoder and kept as is, so WPF picks the frame that fits each size and display scaling.
    // A single extracted or resized image would be rescaled and look blurry.
    private BitmapFrame GetIcon(Season season)
    {
        if (!_icons.TryGetValue(season, out BitmapFrame? icon))
        {
            string name = season switch
            {
                Season.Winter => "winter",
                Season.Spring => "spring",
                Season.Summer => "summer",
                _ => "autumn",
            };
            icon = BitmapFrame.Create(new Uri($"pack://application:,,,/assets/icons/{name}.ico", UriKind.Absolute));
            _icons[season] = icon;
        }

        return icon;
    }

    // System events can arrive on another thread, so the update runs on the window's thread.
    private void OnTimeChanged(object? sender, EventArgs e)
    {
        _ = _window.Dispatcher.InvokeAsync(() =>
        {
            // The computer's time zone may be what changed, and .NET keeps it cached.
            TimeZoneInfo.ClearCachedData();
            Update();
        });
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            _ = _window.Dispatcher.InvokeAsync(Update);
        }
    }
}
