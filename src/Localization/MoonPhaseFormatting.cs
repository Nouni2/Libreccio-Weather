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

using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Localization;

public static class MoonPhaseFormatting
{
    /// <summary>
    /// The name of the closest of the eight phases, each 45° wide around its angle, from new moon at 0°.
    /// </summary>
    public static string? GetDescription(MoonPhase moonPhase)
    {
        if (moonPhase.Angle is not { } angle)
        {
            return null;
        }

        double normalized = angle % 360.0;
        if (normalized < 0.0)
        {
            normalized += 360.0;
        }

        string phase = normalized switch
        {
            < 22.5 => "new_moon",
            < 67.5 => "waxing_crescent",
            < 112.5 => "first_quarter",
            < 157.5 => "waxing_gibbous",
            < 202.5 => "full_moon",
            < 247.5 => "waning_gibbous",
            < 292.5 => "last_quarter",
            < 337.5 => "waning_crescent",
            _ => "new_moon",
        };
        return Strings.Get("ephemeris_moon_phase_" + phase);
    }
}
