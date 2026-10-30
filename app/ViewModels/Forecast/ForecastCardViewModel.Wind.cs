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

using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateDailyWind(Core.Models.Weather weather, Location location, SpeedUnit unit)
    {
        IEnumerable<Wind?> winds = weather.DailyForecast.SelectMany(daily => new[] { daily.Day?.Wind, daily.Night?.Wind });
        float highestSpeed = GetHighestWindSpeed(winds);
        bool hasNotableDayGusts = weather.DailyForecast.Any(daily => HasNotableGusts(daily.Day?.Wind));
        bool hasNotableNightGusts = weather.DailyForecast.Any(daily => HasNotableGusts(daily.Night?.Wind));
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            Wind? day = daily.Day?.Wind;
            Wind? night = daily.Night?.Wind;
            string description = ForecastDescriptions.DailyPrefix(weather, location, position, "");
            if (day is { IsValid: true })
            {
                description += Strings.GetUnit("locale_separator") + Strings.Get("daytime") + Strings.Get("colon_separator")
                    + WindFormatting.GetContentDescription(day, unit, true);
            }

            if (night is { IsValid: true })
            {
                description += Strings.GetUnit("locale_separator") + Strings.Get("nighttime") + Strings.Get("colon_separator")
                    + WindFormatting.GetContentDescription(night, unit, true);
            }

            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                null, null, description,
                new TrendChartData([null, null, null], null, null, null, null, null, null, null, true,
                    DoubleHistogram: new DoubleHistogramData(
                        day?.Speed is { } daySpeed ? (float)daySpeed.Value : 0f,
                        night?.Speed is { } nightSpeed ? (float)nightSpeed.Value : 0f,
                        day?.Speed is { } daytime ? UnitFormatting.FormatValue(daytime, unit) : null,
                        night?.Speed is { } nighttime ? UnitFormatting.FormatValue(nighttime, unit) : null,
                        highestSpeed, ForecastColors.GetWindColor(day?.Speed), ForecastColors.GetWindColor(night?.Speed),
                        BuildWindSecondary(day, hasNotableDayGusts, unit), BuildWindSecondary(night, hasNotableNightGusts, unit))),
                DayWindIcon: WindIcons.GetDrawableArrow(day, ForecastColors.GetWindColor(day?.Speed)),
                NightWindIcon: WindIcons.GetDrawableArrow(night, ForecastColors.GetWindColor(night?.Speed))));
        }

        return new ForecastTrend(Strings.Get("tag_wind"), columns, []);
    }

    private static ForecastTrend CreateHourlyWind(Core.Models.Weather weather, Location location, SpeedUnit unit)
    {
        float highestSpeed = GetHighestWindSpeed(weather.NextHourlyForecast.Select(hour => hour.Wind));
        bool hasNotableGusts = weather.NextHourlyForecast.Any(hour => HasNotableGusts(hour.Wind));
        List<ForecastColumn> columns = [];
        foreach (Hourly hourly in weather.NextHourlyForecast)
        {
            Wind? wind = hourly.Wind;
            string description = ForecastDescriptions.HourlyPrefix(hourly, location, "");
            if (wind is { IsValid: true })
            {
                description += Strings.GetUnit("locale_separator") + WindFormatting.GetContentDescription(wind, unit, true);
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, null, null, description,
                new TrendChartData([null, null, null], null, null, null, null, null,
                    wind?.Speed is { } speed ? (float)speed.Value : 0f,
                    wind?.Speed is { } value ? UnitFormatting.FormatValue(value, unit) : null, false,
                    HighestHistogram: highestSpeed, HistogramColor: ForecastColors.GetWindColor(wind?.Speed),
                    HistogramUsesTitleColor: true, SecondaryHistogram: BuildWindSecondary(wind, hasNotableGusts, unit),
                    HistogramAlpha: 255),
                DayWindIcon: WindIcons.GetDrawableArrow(wind, ForecastColors.GetWindColor(wind?.Speed))));
        }

        return new ForecastTrend(Strings.Get("tag_wind"), columns, []);
    }

    private static float GetHighestWindSpeed(IEnumerable<Wind?> winds)
    {
        float highestSpeed = (float)Speed.FromMetersPerSecond(15).InCentimetersPerSecond;
        long? maximum = winds.SelectMany(wind => new[] { wind?.Speed?.Value, wind?.Gusts?.Value }).Max();
        if (maximum is { } value && value > highestSpeed)
        {
            highestSpeed = value;
        }

        return highestSpeed;
    }

    private static bool HasNotableGusts(Wind? wind)
    {
        return wind?.Speed is { } speed && wind.Gusts is { } gusts && gusts > speed;
    }

    private static SecondaryHistogramData BuildWindSecondary(Wind? wind, bool reserveSpace, SpeedUnit unit)
    {
        string? text = HasNotableGusts(wind) ? UnitFormatting.FormatValue(wind!.Gusts!.Value, unit)
            : reserveSpace ? "" : null;
        return new SecondaryHistogramData(wind?.Gusts is { } gusts ? (float)gusts.Value : null,
            text, ForecastColors.GetWindColor(wind?.Gusts));
    }
}
