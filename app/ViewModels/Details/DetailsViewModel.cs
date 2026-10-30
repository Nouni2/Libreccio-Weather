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
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.ViewModels.Details;

/// <summary>
/// The detail screen of a location's days: a tab for each day, and a page of the chosen kind of data for the selected
/// day. The page and the selected pollutant stay the same when another day is selected.
/// </summary>
public sealed partial class DetailsViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private Location _location;
    private Weather _weather;
    private bool _isBuilt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(MenuIconKey))]
    private DetailScreen _selectedScreen = DetailScreen.Conditions;

    [ObservableProperty]
    private int _selectedDayIndex;

    [ObservableProperty]
    private PollutantIndex? _selectedPollutant;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MenuIconKey), nameof(MenuState))]
    private bool _isMenuOpen;

    [ObservableProperty]
    private IReadOnlyList<object> _content = [];

    /// <summary>
    /// Opens on a day and a page. A missing or unknown day opens on today, and a missing page on the conditions.
    /// </summary>
    public DetailsViewModel(MainViewModel main, Location location, int? dailyIndex, DetailScreen? screen)
    {
        _main = main;
        _location = location;
        _weather = location.Weather ?? new Weather();
        SelectedScreen = screen ?? DetailScreen.Conditions;
        SelectedDayIndex = dailyIndex is not { } index || index == -1 || index >= _weather.DailyForecast.Count
            ? _weather.TodayIndex ?? 0
            : index;

        BuildNavigation();
        _isBuilt = true;
        BuildContent();
    }

    public string Title => SelectedScreen.Name;

    public IReadOnlyList<DetailsDayTab> Days { get; private set; } = [];

    public IReadOnlyList<DetailsMenuItem> Screens { get; private set; } = [];

    public string MenuIconKey => IsMenuOpen ? "IconClose" : GetIconKey(SelectedScreen);

    public string MenuState => Strings.Get(IsMenuOpen ? "label_expanded" : "label_collapsed");

    public Location Location => _location;

    /// <summary>
    /// The same screen built again, such as in another language, on the same day, page and pollutant.
    /// </summary>
    public DetailsViewModel Rebuild()
    {
        return new DetailsViewModel(_main, _location, SelectedDayIndex, SelectedScreen) { SelectedPollutant = SelectedPollutant };
    }

    internal void UpdateLocation(Location location)
    {
        if (location.FormattedId != _location.FormattedId || ReferenceEquals(location, _location))
        {
            return;
        }

        DateTimeOffset? selectedDate = SelectedDayIndex >= 0 && SelectedDayIndex < _weather.DailyForecast.Count
            ? _weather.DailyForecast[SelectedDayIndex].Date : null;
        _isBuilt = false;
        _location = location;
        _weather = location.Weather ?? new Weather();
        BuildNavigation();
        OnPropertyChanged(nameof(Location));
        OnPropertyChanged(nameof(Days));
        OnPropertyChanged(nameof(Screens));

        int index = _weather.DailyForecast.ToList().FindIndex(daily => daily.Date == selectedDate);
        SelectedDayIndex = index >= 0 ? index : _weather.TodayIndex ?? 0;
        if (SelectedScreen != DetailScreen.FeelsLike && !Screens.Any(item => item.Screen == SelectedScreen))
        {
            SelectedScreen = Screens.FirstOrDefault()?.Screen ?? DetailScreen.Conditions;
        }

        _isBuilt = true;
        BuildContent();
    }

    private void BuildNavigation()
    {
        int? todayIndex = _weather.TodayIndex;
        Days = [.. _weather.DailyForecast.Select((daily, position) => CreateDayTab(daily, position, todayIndex))];
        Screens = [.. DetailScreen.ToDetailScreenList(_location).Select(item =>
            new DetailsMenuItem(item, GetIconKey(item), new RelayCommand(() =>
            {
                SelectedScreen = item;
                IsMenuOpen = false;
            })))];
    }

    partial void OnSelectedScreenChanged(DetailScreen value) => BuildContent();

    partial void OnSelectedDayIndexChanged(int value) => BuildContent();

    partial void OnSelectedPollutantChanged(PollutantIndex? value) => BuildContent();

    [RelayCommand]
    private void Back()
    {
        _main.GoBack();
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }

    [RelayCommand]
    private void CloseMenu()
    {
        IsMenuOpen = false;
    }

    private void BuildContent()
    {
        if (!_isBuilt)
        {
            return;
        }

        if (SelectedDayIndex < 0 || SelectedDayIndex >= _weather.DailyForecast.Count)
        {
            Content = [];
            return;
        }

        Content = DetailsPage.Build(new DetailsContext(
            _location, _weather, _weather.DailyForecast[SelectedDayIndex], _main.Settings, _main.RegionCode,
            SelectedScreen, SelectedPollutant,
            screen => SelectedScreen = screen,
            pollutant => SelectedPollutant = pollutant));
    }

    private DetailsDayTab CreateDayTab(Daily daily, int position, int? todayIndex)
    {
        TimeZoneInfo timeZone = _location.TimeZone;
        string weekday;
        string fullWeekday;
        if (todayIndex is not { } today)
        {
            weekday = DateFormatting.GetWeek(daily.Date, timeZone);
            fullWeekday = DateFormatting.GetWeek(daily.Date, timeZone, full: true);
        }
        else if (position == today - 1)
        {
            weekday = Strings.Get("daily_yesterday_short");
            fullWeekday = Strings.Get("daily_yesterday");
        }
        else if (position == today)
        {
            weekday = Strings.Get("daily_today_short");
            fullWeekday = Strings.Get("daily_today");
        }
        else if (position == today + 1)
        {
            weekday = Strings.Get("daily_tomorrow_short");
            fullWeekday = Strings.Get("daily_tomorrow");
        }
        else
        {
            weekday = DateFormatting.GetWeek(daily.Date, timeZone);
            fullWeekday = DateFormatting.GetWeek(daily.Date, timeZone, full: true);
        }

        return new DetailsDayTab(
            weekday,
            DateFormatting.GetFormattedDayOfTheMonth(daily.Date, timeZone),
            DateFormatting.GetFormattedAbbreviatedMonth(daily.Date, timeZone),
            fullWeekday + " " + DateFormatting.GetFormattedFullDayAndMonth(daily.Date, timeZone));
    }

    internal static string GetIconKey(DetailScreen screen)
    {
        return screen switch
        {
            _ when screen == DetailScreen.Wind => "IconWind",
            _ when screen == DetailScreen.AirQuality => "IconHaze",
            _ when screen == DetailScreen.Pollen => "IconAllergy",
            _ when screen == DetailScreen.UVIndex => "IconUV",
            _ when screen == DetailScreen.Precipitation => "IconPrecipitation",
            _ when screen == DetailScreen.Humidity => "IconHumidityPercentage",
            _ when screen == DetailScreen.Pressure => "IconGauge",
            _ when screen == DetailScreen.CloudCover => "IconCloud",
            _ when screen == DetailScreen.Visibility => "IconEye",
            _ when screen == DetailScreen.SunMoon => "IconClearNight",
            _ => "IconThermostat",
        };
    }
}

public sealed record DetailsDayTab(string Weekday, string DayOfMonth, string Month, string AutomationName);

public sealed record DetailsMenuItem(DetailScreen Screen, string IconKey, IRelayCommand Command)
{
    public string Name => Screen.Name;
}
