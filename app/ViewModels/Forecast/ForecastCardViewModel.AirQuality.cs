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

using System.Globalization;
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateDailyAirQuality(Core.Models.Weather weather, Location location, bool showReferenceLines)
    {
        float highestIndex = GetHighestAirQualityIndex(weather.DailyForecast.Select(daily => daily.AirQuality));
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            int? index = daily.AirQuality is { } airQuality ? AirQualityIndex.GetIndex(airQuality) : null;
            string description = AppendAirQualityDescription(
                ForecastDescriptions.DailyPrefix(weather, location, position, Strings.Get("tag_aqi")), index);
            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                null, null, description, BuildAirQualityChart(index, highestIndex), false));
        }

        return new ForecastTrend(Strings.Get("tag_aqi"), columns, BuildAirQualityReferenceLines(showReferenceLines));
    }

    private static ForecastTrend CreateHourlyAirQuality(Core.Models.Weather weather, Location location, bool showReferenceLines)
    {
        float highestIndex = GetHighestAirQualityIndex(weather.NextHourlyForecast.Select(hourly => hourly.AirQuality));
        List<ForecastColumn> columns = [];
        foreach (Hourly hourly in weather.NextHourlyForecast)
        {
            int? index = hourly.AirQuality is { } airQuality ? AirQualityIndex.GetIndex(airQuality) : null;
            string description = AppendAirQualityDescription(
                ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_aqi")), index);
            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, null, null, description, BuildAirQualityChart(index, highestIndex), false));
        }

        return new ForecastTrend(Strings.Get("tag_aqi"), columns, BuildAirQualityReferenceLines(showReferenceLines));
    }

    private static float GetHighestAirQualityIndex(IEnumerable<AirQuality?> values)
    {
        int highestIndex = AirQualityIndex.AqiThresholds[4];
        int? maximum = values.Select(airQuality => airQuality is not null ? AirQualityIndex.GetIndex(airQuality) : null).Max();
        if (maximum is { } index && index > highestIndex)
        {
            highestIndex = index;
        }

        return highestIndex;
    }

    private static TrendChartData BuildAirQualityChart(int? index, float highestIndex)
    {
        return new TrendChartData([null, null, null], null, null, null, null, null,
            index, index is { } value ? NumberFormatting.Format(value, 0, Strings.Culture) : null, false,
            HighestHistogram: highestIndex, HistogramColor: ForecastColors.GetAirQualityColor(index), HistogramUsesTitleColor: true);
    }

    private static string AppendAirQualityDescription(string description, int? index)
    {
        if (index is { } value)
        {
            description += Strings.GetUnit("locale_separator") + NumberFormatting.Format(value, 0, Strings.Culture)
                + Strings.GetUnit("locale_separator");
            if (AirQualityIndex.GetAqiToLevel(value) is { } level)
            {
                description += Strings.GetArray("air_quality_levels").ElementAtOrDefault(level);
            }
        }

        return description;
    }

    private static IReadOnlyList<TrendKeyLine> BuildAirQualityReferenceLines(bool showReferenceLines)
    {
        if (!showReferenceLines)
        {
            return [];
        }

        IReadOnlyList<string> levels = Strings.GetArray("air_quality_levels");
        return new[] { 1, 3, 5 }.Select(level => new TrendKeyLine(
            AirQualityIndex.AqiThresholds[level], AirQualityIndex.AqiThresholds[level].ToString(CultureInfo.InvariantCulture),
            levels.ElementAtOrDefault(level) ?? "", true)).ToArray();
    }
}
