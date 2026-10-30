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

using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;

namespace LibreccioWeather.Tray;

public sealed record TrayPopupContent(
    string Place, string? WeatherText, string? TemperatureText, string? IconPath, string? AuxiliaryText, string? RefreshText,
    bool IsCities, IReadOnlyList<TrayPopupItem> Items)
{
    public static TrayPopupContent Create(Location? location, IReadOnlyList<Location> locations, SettingsManager settings, TemperatureUnit unit)
    {
        Core.Models.Weather? weather = location?.Weather;
        bool daylight = weather?.NextHourlyForecast.FirstOrDefault()?.IsDaylight ?? true;
        bool feelsLike = settings.IsWidgetNotificationUsingFeelsLike;
        Core.Units.Temperature? temperature = SelectTemperature(weather?.Current?.Temperature, feelsLike);
        NotificationStyle style = settings.WidgetNotificationStyle;
        List<TrayPopupItem> items = [];
        if (style == NotificationStyle.Cities)
        {
            foreach (Location city in locations.Where(city => city.FormattedId != location?.FormattedId).Take(3))
            {
                if (city.Weather is not { } cityWeather)
                {
                    continue;
                }

                bool cityDaylight = cityWeather.NextHourlyForecast.FirstOrDefault()?.IsDaylight ?? true;
                Daily? today = cityWeather.Today;
                string? range = today is null ? null : FormatDailyTemperature(today, unit, feelsLike);
                string title = city.GetPlace(true) + (today is null ? "" : Strings.GetUnit("locale_separator") + range);
                items.Add(new TrayPopupItem(title, null,
                    WeatherIcons.GetPath(cityDaylight ? today?.Day?.WeatherCode : today?.Night?.WeatherCode, cityDaylight)));
            }
        }
        else if (style == NotificationStyle.Daily && weather is not null && location is not null)
        {
            DateTime localToday = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Date;
            foreach (Daily daily in weather.DailyForecastStartingToday.Take(5))
            {
                string title = TimeZoneInfo.ConvertTime(daily.Date, location.TimeZone).Date == localToday
                    ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone);
                items.Add(new TrayPopupItem(title, FormatDailyTemperature(daily, unit, feelsLike),
                    WeatherIcons.GetPath(daylight ? daily.Day?.WeatherCode : daily.Night?.WeatherCode, daylight)));
            }
        }
        else if (style == NotificationStyle.Hourly && weather is not null && location is not null)
        {
            foreach (Hourly hourly in weather.NextHourlyForecast.Take(5))
            {
                items.Add(new TrayPopupItem(DateFormatting.GetHour(hourly.Date, location.TimeZone),
                    FormatTemperature(SelectTemperature(hourly.Temperature, feelsLike), unit),
                    WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight)));
            }
        }

        string? auxiliaryText = null;
        if (weather?.Current?.AirQuality is { IsIndexValid: true } airQuality)
        {
            int? level = AirQualityIndex.GetAqiToLevel(AirQualityIndex.GetIndex(airQuality));
            if (level is { } index && Strings.GetArray("air_quality_levels").ElementAtOrDefault(index) is { } name)
            {
                auxiliaryText = Strings.Get("air_quality") + " – " + name;
            }
        }
        else if (weather?.Current?.Wind?.Speed is { } speed
            && Strings.GetArray("wind_strength_descriptions").ElementAtOrDefault(speed.InBeaufort) is { } strength)
        {
            auxiliaryText = Strings.Get("wind") + " – " + strength;
        }

        string? refreshText = style == NotificationStyle.Native && weather?.Base.RefreshTime is { } refreshTime && location is not null
            ? Strings.Get("notification_refreshed_at") + " " + DateFormatting.GetFormattedTime(refreshTime, location.TimeZone) : null;
        return new TrayPopupContent(location?.GetPlace() ?? Strings.Get("desktop_app_name"), weather?.Current?.WeatherText,
            style == NotificationStyle.Native && settings.IsWidgetNotificationTemperatureIconEnabled
                ? null : style == NotificationStyle.Native && temperature is { } value
                    ? UnitFormatting.Format(value, unit) : FormatTemperature(temperature, unit),
            WeatherIcons.GetPath(weather?.Current?.WeatherCode, daylight), auxiliaryText, refreshText,
            style == NotificationStyle.Cities, items);
    }

    private static Core.Units.Temperature? SelectTemperature(Core.Models.Temperature? temperature, bool feelsLike)
    {
        return feelsLike ? temperature?.FeelsLikeTemperature ?? temperature?.Value : temperature?.Value;
    }

    private static string? FormatTemperature(Core.Units.Temperature? temperature, TemperatureUnit unit)
    {
        return temperature is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Narrow, UnitWidth.Narrow) : null;
    }

    private static string? FormatDailyTemperature(Daily daily, TemperatureUnit unit, bool feelsLike)
    {
        Core.Units.Temperature? day = feelsLike ? daily.Day?.Temperature?.FeelsLikeTemperature : null;
        Core.Units.Temperature? night = feelsLike ? daily.Night?.Temperature?.FeelsLikeTemperature : null;
        if (day is null || night is null)
        {
            day = daily.Day?.Temperature?.Value;
            night = daily.Night?.Temperature?.Value;
        }

        return day is not null && night is not null ? FormatTemperature(day, unit) + "/" + FormatTemperature(night, unit) : null;
    }
}

public sealed record TrayPopupItem(string Title, string? Temperature, string? IconPath);
