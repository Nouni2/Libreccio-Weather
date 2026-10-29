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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Settings;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// A card of the location list: the place, and below it the current weather, active alerts or the area it is in.
/// </summary>
public sealed class LocationItemViewModel
{
    // A location whose alerts were refreshed longer ago than this no longer reports its alerts.
    private static readonly TimeSpan AlertValidity = TimeSpan.FromHours(24);

    public LocationItemViewModel(Location location, bool selected, SettingsManager settings)
    {
        Location = location;
        IsSelected = selected;
        Title = (location.IsCurrentPosition ? "⊙ " : "") + location.GetPlace();
        AlertCount = location.Weather?.CurrentAlertList.Count ?? 0;
        Body = GetBody(settings);
    }

    public Location Location { get; }

    public bool IsSelected { get; }

    public string Title { get; }

    public int AlertCount { get; }

    public bool HasAlerts => AlertCount > 0;

    public string Body { get; }

    // Outdated weather says when it was last updated. Without ICU there is no relative time to say it with, so the
    // card then shows what it would for fresh weather.
    private string GetBody(SettingsManager settings)
    {
        if (!Location.IsUsable)
        {
            return Strings.Get("location_current_not_found_yet");
        }

        if (Location.Weather?.Base.RefreshTime is not { } refreshTime)
        {
            return Location.AdministrationLevels();
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(Location.AlertSource))
        {
            if (refreshTime < now - AlertValidity && LastUpdated(refreshTime) is { } alertsOutdated)
            {
                return alertsOutdated;
            }

            if (AlertCount > 0)
            {
                return "⚠ " + Strings.Get("location_has_active_alerts");
            }
        }

        if (refreshTime < now - settings.UpdateInterval.Validity && LastUpdated(refreshTime) is { } outdated)
        {
            return outdated;
        }

        string? weatherText = Location.Weather?.Current?.WeatherText;
        return string.IsNullOrEmpty(weatherText) ? Location.AdministrationLevels() : weatherText;
    }

    private static string? LastUpdated(DateTimeOffset refreshTime)
    {
        return RelativeTimeFormatting.GetRelativeTimeInSentence(refreshTime) is { } relative
            ? Strings.Format("location_last_updated_x", relative)
            : null;
    }
}
