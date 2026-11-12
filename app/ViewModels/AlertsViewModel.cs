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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.ViewModels.WeatherCards;
using Brush = System.Windows.Media.Brush;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// An alert on the alerts screen: its triangle, headline and dates, then its description, its instruction and its
/// source, each after a divider when something comes before it.
/// </summary>
public sealed class AlertItem
{
    public AlertItem(Alert alert, Location location, bool isLast)
    {
        Headline = string.IsNullOrEmpty(alert.Headline) ? Strings.Get("alert") : alert.Headline;
        string dates = AlertFormatting.GetFormattedDates(alert, location.TimeZone);
        Dates = dates.Length == 0 ? null : dates;
        DatesDescription = AlertFormatting.GetFormattedDates(alert, location.TimeZone, full: true);
        ColorBrush = AlertCardItem.GetBrush(alert.Color);
        Description = string.IsNullOrWhiteSpace(alert.Description) ? null : AlertFormatting.GetPlainDescription(alert.Description);
        Instruction = string.IsNullOrWhiteSpace(alert.Instruction) ? null : alert.Instruction;
        Source = string.IsNullOrWhiteSpace(alert.Source) ? null : Strings.Format("alert_source", alert.Source);
        HasDividerBeforeInstruction = Description is not null && Instruction is not null;
        HasDividerBeforeSource = Source is not null && (Description is not null || Instruction is not null);
        IsLast = isLast;
    }

    public string Headline { get; }

    public string? Dates { get; }

    public string DatesDescription { get; }

    public Brush ColorBrush { get; }

    public string? Description { get; }

    public string? Instruction { get; }

    public string? Source { get; }

    public bool HasDates => Dates is not null;

    public bool HasDescription => Description is not null;

    public bool HasInstruction => Instruction is not null;

    public bool HasSource => Source is not null;

    public bool HasDividerBeforeInstruction { get; }

    public bool HasDividerBeforeSource { get; }

    public bool IsLast { get; }
}

/// <summary>
/// Every alert of a location, scrolled to the one that was opened, or to the first. The screen is light or dark by the
/// location's daylight, as the detail screen is.
/// </summary>
public sealed partial class AlertsViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    public AlertsViewModel(MainViewModel main, Location location, string? alertId)
    {
        _main = main;
        Title = Strings.Get("alerts");
        IReadOnlyList<Alert> alerts = location.Weather?.AlertList ?? [];
        Items = [.. alerts.Select((alert, index) => new AlertItem(alert, location, index == alerts.Count - 1))];
        int index = alertId is null ? -1 : alerts.ToList().FindIndex(alert => alert.AlertId == alertId);
        ScrollIndex = Math.Max(index, 0);
        IsLightTheme = ThemeManager.IsLightTheme(main.Settings, RiseProgress.IsDaylight(location, DateTimeOffset.UtcNow));
    }

    public string Title { get; }

    public IReadOnlyList<AlertItem> Items { get; }

    /// <summary>
    /// The alert the list starts at.
    /// </summary>
    public int ScrollIndex { get; }

    public bool IsLightTheme { get; }

    [RelayCommand]
    private void Back()
    {
        _main.GoBack();
    }
}
