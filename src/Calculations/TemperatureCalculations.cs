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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Calculations;

public static class TemperatureCalculations
{
    /// <summary>
    /// Apparent temperature from the Bureau of Meteorology Australia approximation.
    /// See http://www.bom.gov.au/info/thermal_stress/#atapproximation
    /// </summary>
    public static Temperature? ComputeApparentTemperature(
        Temperature? temperature,
        Ratio? relativeHumidity,
        Speed? windSpeed)
    {
        if (temperature is not { } t || relativeHumidity is not { } humidity || windSpeed is not { } wind)
        {
            return null;
        }

        double vaporPressure = humidity.InFraction * 6.105 * Math.Exp(17.27 * t.InCelsius / (237.7 + t.InCelsius));
        return Temperature.FromCelsius(t.InCelsius + 0.33 * vaporPressure - 0.7 * wind.InMetersPerSecond - 4.0);
    }

    /// <summary>
    /// Wind chill from the Environment Canada formula.
    /// See https://climate.weather.gc.ca/glossary_e.html#w
    /// Only defined at or below 0 °C, or at or below 10 °C with a wind of at least 5 km/h.
    /// </summary>
    public static Temperature? ComputeWindChillTemperature(Temperature? temperature, Speed? windSpeed)
    {
        if (temperature is not { } t || windSpeed is not { } wind || t > Temperature.FromCelsius(10))
        {
            return null;
        }

        if (wind >= Speed.FromKilometersPerHour(5))
        {
            double windFactor = Math.Pow(wind.InKilometersPerHour, 0.16);
            return Temperature.FromCelsius(
                13.12 + (0.6215 * t.InCelsius) - (11.37 * windFactor) + (0.3965 * t.InCelsius * windFactor));
        }

        if (t <= Temperature.FromCelsius(0))
        {
            return Temperature.FromCelsius(
                t.InCelsius + ((-1.59 + 0.1345 * t.InCelsius) / 5.0) * wind.InKilometersPerHour);
        }

        return null;
    }

    /// <summary>
    /// Humidex from the Environment and Climate Change Canada formula. Not defined below 15 °C.
    /// </summary>
    public static Temperature? ComputeHumidex(Temperature? temperature, Temperature? dewPoint)
    {
        if (temperature is not { } t || dewPoint is not { } dew || t < Temperature.FromCelsius(15))
        {
            return null;
        }

        double vaporPressure = 6.11 * Math.Exp(5417.7530 * (1.0 / 273.15 - 1.0 / (273.15 + dew.InCelsius)));
        return Temperature.FromCelsius(t.InCelsius + 0.5555 * (vaporPressure - 10));
    }
}
