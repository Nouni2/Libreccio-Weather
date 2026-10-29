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
/// Conversions between volume and mass concentrations of a gas.
/// The same formulas convert ppm to mg/m³ and back.
/// Temperature defaults to 25 °C and pressure to 1013.25 hPa, the reference conditions of US air quality standards.
/// See https://en.wikipedia.org/wiki/Useful_conversions_and_formulas_for_air_dispersion_modeling
/// and https://www.ecfr.gov/current/title-40/chapter-I/subchapter-C/part-50/section-50.3
/// </summary>
public static class PollutantCalculations
{
    private const double GasConstant = 8.31446261815324;

    public static double? ComputePollutantInUgm3FromPpb(
        double? molecularMass,
        double? concentrationInPpb,
        Temperature? temperature = null,
        Pressure? barometricPressure = null)
    {
        if (concentrationInPpb is not { } concentration || molecularMass is not { } mass)
        {
            return null;
        }

        return concentration * mass / MolarVolumeFactor(barometricPressure) / AbsoluteTemperature(temperature);
    }

    public static double? ComputePollutantInPpbFromUgm3(
        double? molecularMass,
        double? concentrationInUgm3,
        Temperature? temperature = null,
        Pressure? barometricPressure = null)
    {
        if (concentrationInUgm3 is not { } concentration || molecularMass is not { } mass)
        {
            return null;
        }

        return concentration / mass * MolarVolumeFactor(barometricPressure) * AbsoluteTemperature(temperature);
    }

    private static double MolarVolumeFactor(Pressure? barometricPressure)
    {
        return GasConstant / (barometricPressure?.InHectopascals ?? 1013.25) * 10;
    }

    private static double AbsoluteTemperature(Temperature? temperature)
    {
        return 273.15 + (temperature?.InCelsius ?? 25.0);
    }
}
