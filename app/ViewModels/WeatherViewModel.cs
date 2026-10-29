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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// The header of the main screen: place, condition, temperature, feels-like temperature, and the day and night range.
/// </summary>
public sealed partial class WeatherViewModel : ObservableObject
{
    private readonly WeatherRefresher _refresher;
    private readonly TemperatureUnit _temperatureUnit;
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

    public WeatherViewModel(WeatherRefresher refresher, TemperatureUnit temperatureUnit, Location location)
    {
        _refresher = refresher;
        _temperatureUnit = temperatureUnit;
        _location = location;
        TemperatureUnitText = UnitFormatting.GetNominativeUnit(temperatureUnit);
        if (location.Weather is { } weather)
        {
            Show(weather);
        }
    }

    public string Place => _location.GetPlace();

    public string TemperatureUnitText { get; }

    [RelayCommand]
    private async Task RefreshAsync()
    {
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
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Show(Weather weather)
    {
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
