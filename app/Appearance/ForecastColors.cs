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

using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace LibreccioWeather.Appearance;

public static class ForecastColors
{
    public static Color GetWindColor(Speed? speed)
    {
        return speed?.InBeaufort switch
        {
            >= 0 and < 4 => Color.FromRgb(0x72, 0xD5, 0x72),
            >= 4 and < 6 => Color.FromRgb(0xFF, 0xCA, 0x28),
            >= 6 and < 8 => Color.FromRgb(0xFF, 0xA7, 0x26),
            >= 8 and < 10 => Color.FromRgb(0xE5, 0x2F, 0x35),
            >= 10 and < 12 => Color.FromRgb(0x99, 0x00, 0x4C),
            >= 12 => Color.FromRgb(0x7E, 0x00, 0x23),
            _ => Colors.Transparent,
        };
    }

    public static Color GetAirQualityColor(int? index)
    {
        return AirQualityIndex.GetAqiToLevel(index) switch
        {
            0 => Color.FromRgb(0x00, 0xE5, 0x9B),
            1 => Color.FromRgb(0xFF, 0xC3, 0x02),
            2 => Color.FromRgb(0xFF, 0x71, 0x2B),
            3 => Color.FromRgb(0xF6, 0x2A, 0x55),
            4 => Color.FromRgb(0xC7, 0x2E, 0xAA),
            5 => Color.FromRgb(0x99, 0x30, 0xFF),
            _ => Colors.Transparent,
        };
    }

    public static Color GetUVColor(UV? uv)
    {
        return uv?.Index switch
        {
            >= 0 and < UV.UVIndexLow => Color.FromRgb(0x72, 0xD5, 0x72),
            >= UV.UVIndexLow and < UV.UVIndexMiddle => Color.FromRgb(0xFF, 0xCA, 0x28),
            >= UV.UVIndexMiddle and < UV.UVIndexHigh => Color.FromRgb(0xFF, 0xA7, 0x26),
            >= UV.UVIndexHigh and < UV.UVIndexExcessive => Color.FromRgb(0xE5, 0x2F, 0x35),
            >= UV.UVIndexExcessive and <= double.MaxValue => Color.FromRgb(0x99, 0x00, 0x4C),
            _ => Colors.Transparent,
        };
    }

    public static Color GetCloudCoverColor(Ratio? cloudCover)
    {
        return cloudCover?.InPercent switch
        {
            >= 0 and < 37.5 => Color.FromRgb(0x72, 0xD5, 0x72),
            >= 37.5 and <= 62.5 => Color.FromRgb(0xFF, 0xCA, 0x28),
            >= 62.5 and <= 100 => Color.FromRgb(0xFF, 0xA7, 0x26),
            _ => Colors.Transparent,
        };
    }
}
