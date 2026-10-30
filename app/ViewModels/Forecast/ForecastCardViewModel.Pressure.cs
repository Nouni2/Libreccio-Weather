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
    private static ForecastTrend? CreateHourlyPressure(
        Core.Models.Weather weather, Location location, PressureUnit unit, bool showReferenceLines)
    {
        IReadOnlyList<Hourly> hours = weather.NextHourlyForecast;
        float?[] pressures = BuildTemperatureArray(hours.Select(hour => hour.Pressure?.Value));
        float highestPressure = PressureUnit.Standard;
        float lowestPressure = PressureUnit.Standard;
        foreach (Hourly hourly in hours)
        {
            if (hourly.Pressure is { } pressure)
            {
                if (pressure.Value > highestPressure)
                {
                    highestPressure = pressure.Value;
                }

                if (pressure.Value < lowestPressure)
                {
                    lowestPressure = pressure.Value;
                }
            }
        }

        if (highestPressure == PressureUnit.Standard && lowestPressure == PressureUnit.Standard)
        {
            return null;
        }

        List<ForecastColumn> columns = [];
        for (int position = 0; position < hours.Count; position++)
        {
            Hourly hourly = hours[position];
            string description = ForecastDescriptions.HourlyPrefix(hourly, location, Strings.Get("tag_pressure"));
            if (hourly.Pressure is { } pressure)
            {
                description += Strings.GetUnit("locale_separator") + UnitFormatting.Format(pressure, unit, unitWidth: UnitWidth.Long);
            }

            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null, description,
                new TrendChartData(
                    BuildTemperatureArrayForItem(pressures, position), null,
                    hourly.Pressure is { } value ? UnitFormatting.FormatValue(value, unit, UnitWidth.Narrow) : null, null,
                    highestPressure, lowestPressure, null, null, false)));
        }

        IReadOnlyList<TrendKeyLine> keyLines = showReferenceLines
            ? [new TrendKeyLine(PressureUnit.Standard,
                UnitFormatting.FormatValue(Pressure.FromPascals(PressureUnit.Standard), unit, UnitWidth.Narrow),
                Strings.Get("pressure_standard"), true)]
            : [];
        return new ForecastTrend(Strings.Get("tag_pressure"), columns, keyLines);
    }
}
