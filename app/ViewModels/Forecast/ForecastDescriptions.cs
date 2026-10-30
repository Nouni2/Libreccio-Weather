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

using System.Text;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels.Forecast;

public static class ForecastDescriptions
{
    public static string DailyPrefix(Core.Models.Weather weather, Location location, int position, string tag)
    {
        Daily daily = weather.DailyForecast[position];
        string week;
        if (weather.TodayIndex is { } today && position == today)
        {
            week = Strings.Get("daily_today");
        }
        else if (weather.TodayIndex is { } yesterday && position == yesterday - 1)
        {
            week = Strings.Get("daily_yesterday");
        }
        else if (weather.TodayIndex is { } tomorrow && position == tomorrow + 1)
        {
            week = Strings.Get("daily_tomorrow");
        }
        else
        {
            week = DateFormatting.GetWeek(daily.Date, location.TimeZone, true);
        }

        return tag + Strings.GetUnit("locale_separator") + week + Strings.GetUnit("locale_separator")
            + DateFormatting.GetFormattedFullDayAndMonth(daily.Date, location.TimeZone);
    }

    public static string HourlyPrefix(Hourly hourly, Location location, string tag)
    {
        return tag + Strings.GetUnit("locale_separator") + DateFormatting.GetHour(hourly.Date, location.TimeZone);
    }

    public static string DailyTemperature(Core.Models.Weather weather, Location location, int position, TemperatureUnit unit, bool feelsLike)
    {
        var description = new StringBuilder(DailyPrefix(weather, location, position,
            Strings.Get(feelsLike ? "tag_feels_like" : "tag_temperature")));
        Daily daily = weather.DailyForecast[position];
        AppendHalfDay(description, daily.Day, unit, feelsLike, "daytime");
        AppendHalfDay(description, daily.Night, unit, feelsLike, "nighttime");
        return description.ToString();
    }

    public static string HourlyTemperature(Hourly hourly, Location location, TemperatureUnit unit, bool feelsLike)
    {
        var description = new StringBuilder(HourlyPrefix(hourly, location,
            Strings.Get(feelsLike ? "tag_feels_like" : "tag_temperature")));
        Core.Units.Temperature? temperature = feelsLike ? hourly.Temperature?.FeelsLikeTemperature : hourly.Temperature?.Value;
        if (temperature is { } value)
        {
            description.Append(Strings.GetUnit("locale_separator"))
                .Append(UnitFormatting.Format(value, unit, unitWidth: UnitWidth.Long));
        }

        if (!feelsLike && !string.IsNullOrEmpty(hourly.WeatherText))
        {
            description.Append(Strings.GetUnit("locale_separator")).Append(hourly.WeatherText);
        }

        if (hourly.PrecipitationProbability?.Total is { } probability)
        {
            description.Append(Strings.GetUnit("locale_separator")).Append(Strings.Get("precipitation_probability"))
                .Append(Strings.Get("colon_separator")).Append(UnitFormatting.FormatPercent(probability, UnitWidth.Narrow));
        }

        return description.ToString();
    }

    private static void AppendHalfDay(StringBuilder description, HalfDay? halfDay, TemperatureUnit unit, bool feelsLike, string nameKey)
    {
        if (halfDay is null)
        {
            return;
        }

        description.Append(Strings.GetUnit("locale_separator")).Append(Strings.Get(nameKey)).Append(Strings.Get("colon_separator"));
        Core.Units.Temperature? temperature = feelsLike ? halfDay.Temperature?.FeelsLikeTemperature : halfDay.Temperature?.Value;
        if (temperature is { } value)
        {
            description.Append(UnitFormatting.Format(value, unit, unitWidth: UnitWidth.Long));
            if (!feelsLike)
            {
                description.Append(Strings.GetUnit("locale_separator"));
            }
        }

        if (!string.IsNullOrEmpty(halfDay.WeatherText))
        {
            description.Append(halfDay.WeatherText);
        }

        if (halfDay.PrecipitationProbability?.Total is { } probability)
        {
            description.Append(Strings.GetUnit("locale_separator")).Append(Strings.Get("precipitation_probability"))
                .Append(Strings.Get("colon_separator")).Append(UnitFormatting.FormatPercent(probability));
        }
    }
}
