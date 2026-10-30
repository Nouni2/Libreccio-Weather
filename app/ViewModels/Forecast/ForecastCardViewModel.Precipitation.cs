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
using PrecipitationAmount = LibreccioWeather.Core.Units.Precipitation;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateDailyPrecipitation(Core.Models.Weather weather, Location location, PrecipitationUnit unit)
    {
        float highestPrecipitation = (float)PrecipitationAmount.FromMillimeters(
            Core.Models.Precipitation.PrecipitationHalfDayHeavy).InMicrometers;
        long? maxDailyPrecipitation = weather.DailyForecast.Select(day => day.Day?.Precipitation?.Total?.Value).Max();
        long? maxNightPrecipitation = weather.DailyForecast.Select(day => day.Night?.Precipitation?.Total?.Value).Max();
        if (maxDailyPrecipitation is not null || maxNightPrecipitation is not null)
        {
            long max = Math.Max(maxDailyPrecipitation ?? 0, maxNightPrecipitation ?? 0);
            if (max > highestPrecipitation)
            {
                highestPrecipitation = max;
            }
        }

        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            PrecipitationAmount? day = daily.Day?.Precipitation?.Total;
            PrecipitationAmount? night = daily.Night?.Precipitation?.Total;
            string description = day is { Value: > 0 } || night is { Value: > 0 }
                ? string.Join(Strings.GetUnit("locale_separator"),
                    Strings.Get("daytime") + Strings.Get("colon_separator") + DescribePrecipitation(day, unit),
                    Strings.Get("nighttime") + Strings.Get("colon_separator") + DescribePrecipitation(night, unit))
                : Strings.Get("precipitation_none");
            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                WeatherIcons.GetPath(daily.Day?.WeatherCode, true),
                WeatherIcons.GetPath(daily.Night?.WeatherCode, false),
                ForecastDescriptions.DailyPrefix(weather, location, position, Strings.Get("tag_precipitation"))
                    + Strings.GetUnit("locale_separator") + description,
                new TrendChartData([null, null, null], null, null, null, null, null, null, null, true,
                    DoubleHistogram: new DoubleHistogramData(
                        day is { } d ? (float)d.Value : null, night is { } n ? (float)n.Value : null,
                        day is { } daytime ? UnitFormatting.FormatValue(daytime, unit) : null,
                        night is { } nighttime ? UnitFormatting.FormatValue(nighttime, unit) : null,
                        highestPrecipitation,
                        PrecipitationColors.GetHalfDayPrecipitationColor(daily.Day?.Precipitation),
                        PrecipitationColors.GetHalfDayPrecipitationColor(daily.Night?.Precipitation)))));
        }

        return new ForecastTrend(Strings.Get("tag_precipitation"), columns, []);
    }

    private static ForecastTrend CreateHourlyPrecipitation(Core.Models.Weather weather, Location location, PrecipitationUnit unit)
    {
        float highestPrecipitation = (float)PrecipitationAmount.FromMillimeters(
            Core.Models.Precipitation.PrecipitationHourlyHeavy).InMicrometers;
        long? max = weather.NextHourlyForecast.Select(hour => hour.Precipitation?.Total?.Value).Max();
        if (max is { } maximum && maximum > highestPrecipitation)
        {
            highestPrecipitation = maximum;
        }

        List<ForecastColumn> columns = [];
        foreach (Hourly hourly in weather.NextHourlyForecast)
        {
            PrecipitationAmount? precipitation = hourly.Precipitation?.Total;
            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null,
                ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_precipitation"))
                    + Strings.GetUnit("locale_separator") + DescribePrecipitation(precipitation, unit),
                new TrendChartData([null, null, null], null, null, null, null, null,
                    precipitation is { } amount ? (float)amount.Value : 0f,
                    precipitation is { } value ? UnitFormatting.FormatValue(value, unit) : null, false,
                    HighestHistogram: highestPrecipitation,
                    HistogramColor: PrecipitationColors.GetHourlyPrecipitationColor(hourly.Precipitation),
                    HistogramUsesTitleColor: true)));
        }

        return new ForecastTrend(Strings.Get("tag_precipitation"), columns, []);
    }

    private static string DescribePrecipitation(PrecipitationAmount? precipitation, PrecipitationUnit unit)
    {
        return precipitation is { Value: > 0 } value
            ? UnitFormatting.Format(value, unit, unitWidth: UnitWidth.Long)
            : Strings.Get("precipitation_none");
    }
}
