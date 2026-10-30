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

public static class UVFormatting
{
    public static string? GetLevel(UV uv)
    {
        return uv.Index switch
        {
            >= 0 and < UV.UVIndexLow => Strings.Get("uv_index_0_2"),
            >= UV.UVIndexLow and < UV.UVIndexMiddle => Strings.Get("uv_index_3_5"),
            >= UV.UVIndexMiddle and < UV.UVIndexHigh => Strings.Get("uv_index_6_7"),
            >= UV.UVIndexHigh and < UV.UVIndexExcessive => Strings.Get("uv_index_8_10"),
            >= UV.UVIndexExcessive and <= double.MaxValue => Strings.Get("uv_index_11"),
            _ => null,
        };
    }

    public static string GetContentDescription(UV uv)
    {
        string description = uv.Index is { } index ? NumberFormatting.Format(index, 0, Strings.Culture) : "";
        if (GetLevel(uv) is { } level)
        {
            if (description.Length > 0)
            {
                description += Strings.GetUnit("locale_separator");
            }

            description += level;
        }

        return description;
    }
}
