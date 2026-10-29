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

namespace LibreccioWeather.Core.Models;

/// <summary>
/// Precipitation intensity over a short interval of the next hours.
/// </summary>
public sealed record Minutely
{
    public required DateTimeOffset Date { get; init; }

    public required int MinuteInterval { get; init; }

    /// <summary>
    /// Precipitation that would fall in one hour at this interval's rate.
    /// </summary>
    public Units.Precipitation? PrecipitationIntensity { get; init; }

    /// <summary>
    /// Radar reflectivity matching the intensity.
    /// </summary>
    public int? Dbz => PrecipitationIntensityToDbz(PrecipitationIntensity?.InMillimeters);

    public DateTimeOffset EndingDate => Date.AddMinutes(MinuteInterval);

    public Minutely ToValidOrNull()
    {
        return this with { PrecipitationIntensity = PrecipitationIntensity?.ToValidHourlyOrNull() };
    }

    /// <summary>
    /// Intensity in mm/h from a radar reflectivity, with the Marshall-Palmer relation Z = 200 R^1.6.
    /// Reflectivity of 5 dBZ or less is treated as no precipitation.
    /// </summary>
    public static double? DbzToPrecipitationIntensity(double? dbz)
    {
        if (dbz is not { } value)
        {
            return null;
        }

        return value <= 5 ? 0.0 : Math.Pow(Math.Pow(10.0, value / 10.0) / 200.0, 5.0 / 8.0);
    }

    // A zero intensity gives int.MinValue, as the logarithm of zero is negative infinity.
    private static int? PrecipitationIntensityToDbz(double? intensity)
    {
        if (intensity is not { } value)
        {
            return null;
        }

        return (int)(10.0 * Math.Log10(200.0 * Math.Pow(value, 8.0 / 5.0)));
    }
}
