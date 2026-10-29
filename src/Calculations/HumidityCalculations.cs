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

/// <summary>
/// Relative humidity and dew point from the Magnus approximation with the Arden Buck constants,
/// which differ above and below freezing.
/// </summary>
public static class HumidityCalculations
{
    public static Ratio? ComputeRelativeHumidity(Temperature? temperature, Temperature? dewPoint)
    {
        if (temperature is not { } t || dewPoint is not { } dew)
        {
            return null;
        }

        (double b, double c) = MagnusConstants(t);
        return Ratio.FromFraction(
            Math.Exp(b * dew.InCelsius / (c + dew.InCelsius)) / Math.Exp(b * t.InCelsius / (c + t.InCelsius)));
    }

    public static Temperature? ComputeDewPoint(Temperature? temperature, Ratio? relativeHumidity)
    {
        // The logarithm of a zero humidity is undefined.
        if (temperature is not { } t || relativeHumidity is not { } humidity || humidity.Value == 0)
        {
            return null;
        }

        (double b, double c) = MagnusConstants(t);
        double magnus = Math.Log(humidity.InFraction) + (b * t.InCelsius) / (c + t.InCelsius);
        return Temperature.FromCelsius((c * magnus) / (b - magnus));
    }

    // c is in degrees Celsius.
    private static (double B, double C) MagnusConstants(Temperature temperature)
    {
        return temperature < Temperature.FromCelsius(0) ? (17.966, 227.15) : (17.368, 238.88);
    }
}
