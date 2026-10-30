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

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateHourlyCloudCover(Core.Models.Weather weather, Location location)
    {
        List<ForecastColumn> columns = [];
        foreach (Hourly hourly in weather.NextHourlyForecast)
        {
            string description = ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_cloud_cover"));
            string? text = hourly.CloudCover is { } cloudCover
                ? UnitFormatting.FormatPercent(cloudCover, UnitWidth.Narrow) : null;
            if (text is not null)
            {
                description += Strings.GetUnit("locale_separator") + text;
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, null, null, description,
                new TrendChartData([null, null, null], null, null, null, null, null,
                    hourly.CloudCover is { } value ? (float)value.InPercent : 0f, text, false,
                    HistogramColor: ForecastColors.GetCloudCoverColor(hourly.CloudCover), HistogramUsesTitleColor: true), false));
        }

        return new ForecastTrend(Strings.Get("tag_cloud_cover"), columns, []);
    }
}
