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

using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Precipitation = LibreccioWeather.Core.Models.Precipitation;
using PrecipitationAmount = LibreccioWeather.Core.Units.Precipitation;

namespace LibreccioWeather.Appearance;

public static class PrecipitationColors
{
    public static Color GetHalfDayPrecipitationColor(Precipitation? precipitation)
    {
        return GetColor(precipitation?.Total,
            Precipitation.PrecipitationHalfDayLight, Precipitation.PrecipitationHalfDayMedium,
            Precipitation.PrecipitationHalfDayHeavy, Precipitation.PrecipitationHalfDayRainstorm);
    }

    public static Color GetHourlyPrecipitationColor(Precipitation? precipitation)
    {
        return GetColor(precipitation?.Total,
            Precipitation.PrecipitationHourlyLight, Precipitation.PrecipitationHourlyMedium,
            Precipitation.PrecipitationHourlyHeavy, Precipitation.PrecipitationHourlyRainstorm);
    }

    private static Color GetColor(PrecipitationAmount? amount, double light, double medium, double heavy, double rainstorm)
    {
        if (amount is not { Value: >= 0 } value)
        {
            return Colors.Transparent;
        }

        if (value <= PrecipitationAmount.FromMillimeters(light))
        {
            return Color.FromRgb(0x72, 0xD5, 0x72);
        }

        if (value <= PrecipitationAmount.FromMillimeters(medium))
        {
            return Color.FromRgb(0xFF, 0xCA, 0x28);
        }

        if (value <= PrecipitationAmount.FromMillimeters(heavy))
        {
            return Color.FromRgb(0xFF, 0xA7, 0x26);
        }

        return value <= PrecipitationAmount.FromMillimeters(rainstorm)
            ? Color.FromRgb(0xE5, 0x2F, 0x35) : Color.FromRgb(0x99, 0x00, 0x4C);
    }
}
