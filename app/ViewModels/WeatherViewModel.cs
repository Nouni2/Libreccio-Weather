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

using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.ViewModels.Forecast;
using LibreccioWeather.ViewModels.WeatherCards;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace LibreccioWeather.ViewModels;

public sealed partial class WeatherViewModel : ObservableObject
{
    private readonly WeatherRefresher _refresher;
    private readonly MainViewModel _main;
    private readonly TemperatureUnit _temperatureUnit;
    private readonly PrecipitationUnit _precipitationUnit;
    private readonly PressureUnit _pressureUnit;
    private readonly DistanceUnit _distanceUnit;
    private readonly SpeedUnit _speedUnit;
    private Location _location;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _weatherText;

    [ObservableProperty]
    private string? _temperatureText;

    [ObservableProperty]
    private string? _feelsLikeText;

    [ObservableProperty]
    private string? _temperatureRangeText;

    [ObservableProperty]
    private string? _errorText;

    [ObservableProperty]
    private string? _refreshTimeText;

    [ObservableProperty]
    private string? _refreshTimeDescription;

    [ObservableProperty]
    private Brush _backgroundBrush = Brushes.Transparent;

    [ObservableProperty]
    private IReadOnlyList<object> _cards = [];

    public WeatherViewModel(WeatherRefresher refresher, MainViewModel main, Location location)
    {
        _refresher = refresher;
        _main = main;
        _temperatureUnit = main.Settings.GetTemperatureUnit(main.RegionCode);
        _precipitationUnit = main.Settings.GetPrecipitationUnit(main.RegionCode);
        _pressureUnit = main.Settings.GetPressureUnit(main.RegionCode);
        _distanceUnit = main.Settings.GetDistanceUnit(main.RegionCode);
        _speedUnit = main.Settings.GetSpeedUnit(main.RegionCode);
        _location = location;
        TemperatureUnitText = UnitFormatting.GetNominativeUnit(_temperatureUnit);
        if (location.Weather is { } weather)
        {
            Show(weather);
        }
    }

    public string Place => _location.GetPlace();

    public string LocationFormattedId => _location.FormattedId;

    public Location ShownLocation => _location;

    /// <summary>
    /// Shows refreshed weather for this location.
    /// </summary>
    public void UpdateLocation(Location location)
    {
        if (ReferenceEquals(_location, location))
        {
            return;
        }

        _location = location;
        if (location.Weather is { } weather)
        {
            Show(weather);
        }
    }

    public string TemperatureUnitText { get; }

    [RelayCommand]
    private void OpenLocationList()
    {
        _main.OpenLocationList();
    }

    /// <summary>
    /// A card of the current conditions opens today's page of its kind on the detail screen.
    /// </summary>
    [RelayCommand]
    private void OpenCardDetails(ConditionsCardViewModel card)
    {
        DetailScreen screen = card.Kind switch
        {
            ConditionsCardKind.Precipitation => DetailScreen.Precipitation,
            ConditionsCardKind.Wind => DetailScreen.Wind,
            ConditionsCardKind.AirQuality => DetailScreen.AirQuality,
            ConditionsCardKind.Humidity => DetailScreen.Humidity,
            ConditionsCardKind.UV => DetailScreen.UVIndex,
            ConditionsCardKind.Visibility => DetailScreen.Visibility,
            ConditionsCardKind.Pressure => DetailScreen.Pressure,
            _ => DetailScreen.Pollen,
        };
        _main.OpenDetails(_location, _location.Weather?.TodayIndex, screen);
    }

    /// <summary>
    /// Shows the result of a refresh that the screen this one replaces is still running, so the location is not
    /// requested a second time.
    /// </summary>
    public void FollowRefresh(WeatherViewModel previous)
    {
        IsLoading = true;
        previous.PropertyChanged += OnPreviousChanged;

        void OnPreviousChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(IsLoading) || previous.IsLoading)
            {
                return;
            }

            previous.PropertyChanged -= OnPreviousChanged;
            UpdateLocation(previous.ShownLocation);
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // A refresh already running for this location is not started again.
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            WeatherResult result = await _refresher.GetWeatherAsync(_location);
            _location = _location with { Weather = result.Weather };
            ErrorText = result.Errors.Any(error => error.Error == RefreshErrorType.InvalidLocation)
                ? Strings.Get("weather_message_invalid_location")
                : null;
            if (result.Weather is { } weather)
            {
                Show(weather);
            }

            _main.UpdateLocation(_location);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Writes the time since the last refresh again. The view calls it every minute while it is shown.
    /// </summary>
    public void UpdateRefreshTime()
    {
        DateTimeOffset? refreshTime = _location.Weather?.Base.RefreshTime;
        RefreshTimeText = refreshTime is { } time ? RelativeTimeFormatting.GetRelativeTime(time) : null;
        RefreshTimeDescription = refreshTime is { } updated
            && RelativeTimeFormatting.GetRelativeTimeInSentence(updated) is { } inSentence
            ? Strings.Format("location_last_updated_x", inSentence)
            : null;
    }

    private void Show(Weather weather)
    {
        UpdateRefreshTime();
        Current? current = weather.Current;
        WeatherText = current?.WeatherText is { Length: > 0 } text ? text : null;

        Core.Units.Temperature? temperature = current?.Temperature?.Value;
        TemperatureText = temperature is { } value ? UnitFormatting.FormatRounded(value, _temperatureUnit) : null;

        // The feels-like temperature is only shown when it rounds to a different value.
        Core.Units.Temperature? feelsLike = current?.Temperature?.FeelsLikeTemperature;
        FeelsLikeText = feelsLike is { } feels
            && (temperature is not { } actual
                || UnitFormatting.RoundToInt(actual.ToDouble(_temperatureUnit))
                    != UnitFormatting.RoundToInt(feels.ToDouble(_temperatureUnit)))
            ? Strings.Format("temperature_feels_like_with_unit", FormatNarrow(feels))
            : null;

        TemperatureRangeText = GetTemperatureRangeSummary(weather);
        // Hourly flags supply daylight while solar events are unavailable.
        bool daylight = weather.NextHourlyForecast.FirstOrDefault()?.IsDaylight ?? true;
        BackgroundBrush = _main.Settings.MainScreenBackgroundEnabled
            ? WeatherBackground.GetBrush(current?.WeatherCode, daylight) : Brushes.Transparent;
        List<object> cards = [];
        foreach (CardDisplay card in _main.Settings.CardDisplayList)
        {
            if (card == CardDisplay.DailyForecast && weather.DailyForecast.Count > 0)
            {
                if (ForecastCardViewModel.CreateDaily(weather, _location, _temperatureUnit, _precipitationUnit, _speedUnit, _main.Settings,
                    _main.SelectedDailyTab, tab => _main.SelectedDailyTab = tab) is { } daily)
                {
                    daily.DetailsRequested += (_, request) => _main.OpenDetails(_location, request.DailyIndex, request.Screen);
                    cards.Add(daily);
                }
            }
            else if (card == CardDisplay.HourlyForecast && weather.NextHourlyForecast.Count > 0)
            {
                if (ForecastCardViewModel.CreateHourly(weather, _location, _temperatureUnit, _precipitationUnit,
                    _speedUnit, _pressureUnit, _distanceUnit, _main.Settings,
                    _main.SelectedHourlyTab, tab => _main.SelectedHourlyTab = tab) is { } hourly)
                {
                    hourly.DetailsRequested += (_, request) => _main.OpenDetails(_location, request.DailyIndex, request.Screen);
                    cards.Add(hourly);
                }
            }
            else if (ConditionsCardViewModel.Create(card, weather, _location, _main.Settings, _main.RegionCode) is { } conditions)
            {
                cards.Add(conditions);
            }
        }

        Cards = cards;
    }

    // The next two half days: last night and today before 06:00, today and tonight until 18:00,
    // then tonight and tomorrow.
    private string? GetTemperatureRangeSummary(Weather weather)
    {
        if (weather.TodayIndex is not { } todayIndex)
        {
            return null;
        }

        Daily today = weather.DailyForecast[todayIndex];
        int currentHour = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _location.TimeZone).Hour;
        bool isDayFirst;
        Core.Units.Temperature? first;
        Core.Units.Temperature? second;
        if (currentHour < 6)
        {
            isDayFirst = false;
            first = todayIndex > 0 ? weather.DailyForecast[todayIndex - 1].Night?.Temperature?.Value : null;
            second = today.Day?.Temperature?.Value;
        }
        else if (currentHour < 18)
        {
            isDayFirst = true;
            first = today.Day?.Temperature?.Value;
            second = today.Night?.Temperature?.Value;
        }
        else
        {
            isDayFirst = false;
            first = today.Night?.Temperature?.Value;
            second = weather.Tomorrow?.Day?.Temperature?.Value;
        }

        string separator = Strings.Get("colon_separator");
        List<string> parts = [];
        if (first is { } firstValue)
        {
            parts.Add(Strings.Get(isDayFirst ? "daytime_short" : "nighttime_short") + separator + FormatNarrow(firstValue));
        }

        if (second is { } secondValue)
        {
            parts.Add(Strings.Get(isDayFirst ? "nighttime_short" : "daytime_short") + separator + FormatNarrow(secondValue));
        }

        return parts.Count > 0 ? string.Join(Strings.Get("dot_separator"), parts) : null;
    }

    private string FormatNarrow(Core.Units.Temperature temperature)
    {
        return UnitFormatting.Format(temperature, _temperatureUnit, UnitWidth.Narrow, UnitWidth.Narrow);
    }
}
