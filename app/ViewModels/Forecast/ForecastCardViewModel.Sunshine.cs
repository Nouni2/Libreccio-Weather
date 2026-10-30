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
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateDailySunshine(Core.Models.Weather weather, Location location)
    {
        TimeSpan highestDuration = weather.DailyForecast.Select(daily => daily.Sun?.Duration).Max()
            ?? weather.DailyForecast.Select(daily => daily.SunshineDuration).Max() ?? TimeSpan.Zero;
        float highestMinutes = highestDuration.Ticks / TimeSpan.TicksPerMinute;
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            TimeSpan? sunshine = daily.SunshineDuration is { } duration
                ? duration > highestDuration ? highestDuration : duration : null;
            string description = ForecastDescriptions.DailyPrefix(weather, location, position, Strings.Get("tag_sunshine"));
            if (daily.SunshineDuration is { } time)
            {
                description += Strings.GetUnit("locale_separator") + DurationFormatting.FormatTime(time, UnitWidth.Long);
            }

            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                null, null, description,
                new TrendChartData([null, null, null], null, null, null, null, null,
                    sunshine is { } value ? value.Ticks / TimeSpan.TicksPerMinute : null,
                    daily.SunshineDuration is { } label ? DurationFormatting.FormatHours(label) : null, false,
                    HighestHistogram: highestMinutes,
                    HistogramColor: sunshine is not null ? Color.FromRgb(0xFD, 0xBC, 0x4C) : Colors.Transparent,
                    HistogramUsesTitleColor: true), false));
        }

        return new ForecastTrend(Strings.Get("tag_sunshine"), columns, []);
    }
}
