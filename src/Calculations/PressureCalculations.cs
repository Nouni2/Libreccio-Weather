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

namespace LibreccioWeather.Core.Calculations;

public static class PressureCalculations
{
    /// <summary>
    /// Mean sea level pressure in hPa from barometric pressure in hPa and altitude in meters.
    /// Temperature in °C, humidity in % and latitude in degrees refine the result when known.
    /// A negative altitude converts sea level pressure back to barometric pressure.
    /// See https://integritext.net/DrKFS/correctiontosealevel.htm
    /// </summary>
    public static double? ComputeMeanSeaLevelPressure(
        double? barometricPressure,
        double? altitude,
        double? temperature = null,
        double? humidity = null,
        double? latitude = null)
    {
        if (barometricPressure is not { } pressure || altitude is not { } height)
        {
            return null;
        }

        // Vapor pressure from the Bureau of Meteorology Australia approximation.
        // See http://www.bom.gov.au/info/thermal_stress/#atapproximation
        double waterVaporPressure = humidity is { } h && temperature is { } t
            ? h / 100 * 6.105 * Math.Exp(17.27 * t / (237.7 + t))
            : 0.0;

        double temperatureTerm = 1.0 + 0.0037 * (temperature ?? 0.0);
        double humidityTerm = 1.0 / (1.0 - 0.378 * waterVaporPressure / pressure);
        double earthShapeTerm = 1.0 / (1.0 - 0.0026 * Math.Cos(2 * (latitude ?? 45.0) * Math.PI / 180));
        double gravityTerm = 1.0 + (height / 6367324);

        return Math.Pow(
            10.0,
            Math.Log10(pressure) + height / (18400.0 * temperatureTerm * humidityTerm * earthShapeTerm * gravityTerm));
    }
}
