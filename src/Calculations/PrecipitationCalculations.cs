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

public static class PrecipitationCalculations
{
    // Density of ice divided by density of water at 0 °C (916.2 / 999.8 kg/m³). Colder ice is denser by a negligible amount.
    // See https://www.engineeringtoolbox.com/ice-thermal-properties-d_576.html
    private const double IceToLiquidRatio = 0.92;

    // Snow depth that melts into one unit of water, by temperature in °C, from the 2011 Cobb-Waldstreicher method.
    // Above freezing the ratio is 1. Values between two temperatures are interpolated.
    // See https://www.weather.gov/media/mdl/SLR.pdf
    private static readonly (double Temperature, double Ratio)[] SnowToLiquidRatios =
    [
        (-300.0, 7.2),
        (-30.0, 7.2),
        (-28.0, 6.8),
        (-26.0, 7.0),
        (-24.0, 8.8),
        (-22.0, 12.0),
        (-20.0, 18.0),
        (-18.0, 23.0),
        (-16.0, 26.0),
        (-14.0, 22.5),
        (-12.0, 17.5),
        (-10.0, 12.0),
        (-8.0, 9.5),
        (-6.0, 9.0),
        (-4.0, 8.5),
        (-2.0, 7.0),
        (0.0, 3.0),
        (1.0, 1.0),
        (300.0, 1.0),
    ];

    /// <summary>
    /// Total precipitation as liquid equivalent, with snow and ice converted to the water they melt into.
    /// </summary>
    public static Precipitation? ComputeTotalPrecipitation(
        Temperature? temperature,
        Precipitation? rain,
        Precipitation? snow,
        Precipitation? ice)
    {
        if (rain is null && snow is null && ice is null)
        {
            return null;
        }

        long snowEquivalent = snow is { } s ? UnitRounding.RoundToLong(s.Value / ComputeSnowToLiquidRatio(temperature)) : 0;
        long iceEquivalent = ice is { } i ? UnitRounding.RoundToLong(i.Value * IceToLiquidRatio) : 0;
        return Precipitation.FromMicrometers((rain?.Value ?? 0) + snowEquivalent + iceEquivalent);
    }

    /// <summary>
    /// The highest of the individual probabilities.
    /// </summary>
    public static Ratio? ComputeTotalProbabilityOfPrecipitation(
        Ratio? thunderstorm,
        Ratio? rain,
        Ratio? snow,
        Ratio? ice)
    {
        if (thunderstorm is null && rain is null && snow is null && ice is null)
        {
            return null;
        }

        long highest = Math.Max(
            Math.Max(thunderstorm?.Value ?? 0, rain?.Value ?? 0),
            Math.Max(snow?.Value ?? 0, ice?.Value ?? 0));
        return Ratio.FromPermille(highest);
    }

    // Without a temperature, the ratio of the coldest temperatures is assumed.
    private static double ComputeSnowToLiquidRatio(Temperature? temperature)
    {
        if (temperature is not { } t)
        {
            return 7.2;
        }

        double celsius = t.InCelsius;
        for (int i = 0; i < SnowToLiquidRatios.Length; i++)
        {
            (double currentTemperature, double currentRatio) = SnowToLiquidRatios[i];
            if (i >= SnowToLiquidRatios.Length - 1)
            {
                return currentRatio;
            }

            (double nextTemperature, double nextRatio) = SnowToLiquidRatios[i + 1];
            if (celsius >= currentTemperature && celsius < nextTemperature)
            {
                return currentRatio
                    + (nextRatio - currentRatio) / (nextTemperature - currentTemperature) * (celsius - currentTemperature);
            }
        }

        return 1.0;
    }
}
