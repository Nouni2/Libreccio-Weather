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

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateDailyUV(Core.Models.Weather weather, Location location, bool showReferenceLines)
    {
        float highestIndex = GetHighestUVIndex(weather.DailyForecast.Select(daily => daily.UV));
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            string description = ForecastDescriptions.DailyPrefix(weather, location, position, Strings.Get("tag_uv"));
            if (daily.UV is { Index: not null } uv)
            {
                description += Strings.GetUnit("locale_separator") + UVFormatting.GetContentDescription(uv);
            }

            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                null, null, description, BuildUVChart(daily.UV, highestIndex), false));
        }

        return new ForecastTrend(Strings.Get("tag_uv"), columns, BuildUVReferenceLines(showReferenceLines));
    }

    private static ForecastTrend CreateHourlyUV(Core.Models.Weather weather, Location location, bool showReferenceLines)
    {
        float highestIndex = GetHighestUVIndex(weather.NextHourlyForecast.Select(hourly => hourly.UV));
        List<ForecastColumn> columns = [];
        foreach (Hourly hourly in weather.NextHourlyForecast)
        {
            string description = ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_uv"));
            if (hourly.UV is { Index: not null } uv)
            {
                description += Strings.GetUnit("locale_separator") + UVFormatting.GetContentDescription(uv);
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, null, null, description, BuildUVChart(hourly.UV, highestIndex), false));
        }

        return new ForecastTrend(Strings.Get("tag_uv"), columns, BuildUVReferenceLines(showReferenceLines));
    }

    private static float GetHighestUVIndex(IEnumerable<UV?> values)
    {
        float highestIndex = (float)UV.UVIndexHigh;
        double? maximum = values.Select(uv => uv?.Index).Max();
        if (maximum is { } index && index > highestIndex)
        {
            highestIndex = (float)index;
        }

        return highestIndex;
    }

    private static TrendChartData BuildUVChart(UV? uv, float highestIndex)
    {
        return new TrendChartData([null, null, null], null, null, null, null, null,
            uv?.Index is { } index ? UnitFormatting.RoundToInt(index) : 0f,
            uv?.Index is { } value ? NumberFormatting.Format(value, 0, Strings.Culture) : null, false,
            HighestHistogram: highestIndex, HistogramColor: ForecastColors.GetUVColor(uv), HistogramUsesTitleColor: true);
    }

    private static IReadOnlyList<TrendKeyLine> BuildUVReferenceLines(bool showReferenceLines)
    {
        return showReferenceLines
            ? [new TrendKeyLine((float)UV.UVIndexMiddle, NumberFormatting.Format(UV.UVIndexMiddle, 0, Strings.Culture),
                Strings.Get("uv_alert_level"), true)]
            : [];
    }
}
