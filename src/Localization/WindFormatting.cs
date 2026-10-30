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
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

public static class WindFormatting
{
    public static string? GetDirection(Wind wind, bool shortName = true)
    {
        string? direction = wind.Degree switch
        {
            null => null,
            >= 0 and <= 22.5 => "N",
            >= 22.5 and <= 67.5 => "NE",
            >= 67.5 and <= 112.5 => "E",
            >= 112.5 and <= 157.5 => "SE",
            >= 157.5 and <= 202.5 => "S",
            >= 202.5 and <= 247.5 => "SW",
            >= 247.5 and <= 292.5 => "W",
            >= 292.5 and <= 337.5 => "NW",
            >= 337.5 and <= 360 => "N",
            _ => "variable",
        };
        return direction is null ? null : Strings.Get("wind_direction_" + direction
            + (shortName && direction != "variable" ? "_short" : ""));
    }

    public static string GetContentDescription(Wind wind, SpeedUnit unit, bool withGusts = false)
    {
        string separator = Strings.GetUnit("locale_separator");
        List<string> description = [];
        if (wind.Speed is { } speed)
        {
            description.Add(UnitFormatting.Format(speed, unit, unitWidth: UnitWidth.Long));
            if (Strings.GetArray("wind_strength_descriptions").ElementAtOrDefault(speed.InBeaufort) is { Length: > 0 } strength)
            {
                description.Add(strength);
            }
        }

        if (GetDirection(wind, false) is { Length: > 0 } direction)
        {
            description.Add(wind.Degree is >= 0 and <= 360 ? Strings.Format("wind_origin", direction) : direction);
        }

        if (withGusts && wind.Gusts is { } gusts && gusts > (wind.Speed ?? Speed.FromCentimetersPerSecond(0)))
        {
            description.Add(Strings.Get("wind_gusts_short") + Strings.Get("colon_separator")
                + UnitFormatting.Format(gusts, unit, unitWidth: UnitWidth.Long));
        }

        return string.Join(separator, description);
    }
}
