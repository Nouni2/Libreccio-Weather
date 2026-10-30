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
using LibreccioWeather.Icons;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel
{
    private static ForecastTrend CreateHourlyHumidity(Core.Models.Weather weather, Location location, TemperatureUnit unit)
    {
        IReadOnlyList<Hourly> hours = weather.NextHourlyForecast;
        float?[] dewPoints = BuildTemperatureArray(hours.Select(hour => hour.DewPoint?.Value));
        (float? highest, float? lowest) = GetBounds(null, dewPoints, []);
        List<ForecastColumn> columns = [];
        for (int position = 0; position < hours.Count; position++)
        {
            Hourly hourly = hours[position];
            List<string> description = [ForecastDescriptions.HourlyPrefix(hourly, location, "")];
            if (hourly.RelativeHumidity is { } humidity)
            {
                description.Add(Strings.Get("humidity") + Strings.Get("colon_separator") + UnitFormatting.FormatPercent(humidity));
            }

            if (hourly.DewPoint is { } dewPoint)
            {
                description.Add(Strings.Get("dew_point") + Strings.Get("colon_separator")
                    + UnitFormatting.Format(dewPoint, unit, unitWidth: UnitWidth.Long));
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null,
                string.Join(Strings.GetUnit("locale_separator"), description),
                new TrendChartData(
                    BuildTemperatureArrayForItem(dewPoints, position), null,
                    FormatTemperature(hourly.DewPoint, unit), null, highest, lowest,
                    hourly.RelativeHumidity is { } ratio ? (float)ratio.InPercent : null,
                    hourly.RelativeHumidity is { } value ? UnitFormatting.FormatPercent(value, UnitWidth.Narrow) : null, false)));
        }

        return new ForecastTrend(Strings.Get("tag_humidity_dew_point"), columns, []);
    }
}
