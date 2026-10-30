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
    private static ForecastTrend CreateHourlyVisibility(Core.Models.Weather weather, Location location, DistanceUnit unit)
    {
        IReadOnlyList<Hourly> hours = weather.NextHourlyForecast;
        float?[] visibilities = BuildTemperatureArray(hours.Select(hour => hour.Visibility?.Value));
        float? highestVisibility = null;
        foreach (Hourly hourly in hours)
        {
            if (hourly.Visibility is { } visibility && (highestVisibility is null || visibility.Value > highestVisibility))
            {
                highestVisibility = visibility.Value;
            }
        }

        List<ForecastColumn> columns = [];
        for (int position = 0; position < hours.Count; position++)
        {
            Hourly hourly = hours[position];
            string description = ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_visibility"));
            if (hourly.Visibility is { } visibility)
            {
                description += Strings.GetUnit("locale_separator") + UnitFormatting.Format(visibility, unit, unitWidth: UnitWidth.Long);
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null, description,
                new TrendChartData(
                    BuildTemperatureArrayForItem(visibilities, position), null,
                    hourly.Visibility is { } value ? UnitFormatting.FormatValue(value, unit) : null, null,
                    highestVisibility, 0f, null, null, false)));
        }

        return new ForecastTrend(Strings.Get("tag_visibility"), columns, []);
    }
}
