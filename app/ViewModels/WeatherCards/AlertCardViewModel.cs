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

using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// An alert of the main screen's alert card: a triangle in the alert's color, its headline and when it applies.
/// </summary>
public sealed class AlertCardItem
{
    public AlertCardItem(Alert alert, Location location, Action open)
    {
        Headline = string.IsNullOrEmpty(alert.Headline) ? Strings.Get("alert") : alert.Headline;
        Dates = alert.StartDate is null ? null : AlertFormatting.GetFormattedDates(alert, location.TimeZone);
        DatesDescription = alert.StartDate is null ? null : AlertFormatting.GetFormattedDates(alert, location.TimeZone, full: true);
        ColorBrush = GetBrush(alert.Color);
        OpenCommand = new RelayCommand(open);
    }

    public string Headline { get; }

    public string? Dates { get; }

    public string? DatesDescription { get; }

    public bool HasDates => Dates is not null;

    public Brush ColorBrush { get; }

    public IRelayCommand OpenCommand { get; }

    /// <summary>
    /// A brush of a color stored as 0xAARRGGBB.
    /// </summary>
    public static Brush GetBrush(int argb)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// The card at the top of the main screen while an alert has not ended: the alerts in force, or a line announcing
/// alerts to come when none is in force yet. Each one opens the alerts screen.
/// </summary>
public sealed class AlertCardViewModel
{
    private AlertCardViewModel(IReadOnlyList<AlertCardItem> items, string upcomingText, Action open)
    {
        Items = items;
        UpcomingText = upcomingText;
        OpenCommand = new RelayCommand(open);
    }

    public IReadOnlyList<AlertCardItem> Items { get; }

    public bool HasCurrentAlerts => Items.Count > 0;

    public bool HasOnlyUpcomingAlerts => Items.Count == 0;

    public string UpcomingText { get; }

    public IRelayCommand OpenCommand { get; }

    /// <summary>
    /// The card, or null when every alert has ended.
    /// </summary>
    public static AlertCardViewModel? Create(Weather weather, Location location, MainViewModel main)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (!weather.AlertList.Any(alert => alert.EndDate is null || alert.EndDate > now))
        {
            return null;
        }

        List<AlertCardItem> items = [.. weather.CurrentAlertList.Select(alert =>
            new AlertCardItem(alert, location, () => main.OpenAlerts(location, alert.AlertId)))];
        return new AlertCardViewModel(items, Strings.Get("alerts_to_follow"), () => main.OpenAlerts(location, null));
    }
}
