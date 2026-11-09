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

using System.Windows.Media;
using LibreccioWeather.Core.Colors;
using LibreccioWeather.Core.Models;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.Controls.WeatherAnimation;

/// <summary>
/// The colors each kind of weather gives the interface around its animation.
/// </summary>
public static class MaterialWeatherThemeDelegate
{
    public static uint GetBrighterColor(uint color)
    {
        float[] hsv = HsvColor.ColorToHsv(color);
        hsv[1] -= 0.25f;
        hsv[2] += 0.25f;
        return HsvColor.HsvToColor(hsv);
    }

    // Without a weather the color is transparent, which a night still brightens into an opaque grey.
    private static uint InnerGetBackgroundColor(WeatherCode? weatherCode, bool daytime) => weatherCode switch
    {
        WeatherCode.Clear => daytime ? SunImplementor.ThemeColor : MeteorShowerImplementor.ThemeColor,
        WeatherCode.PartlyCloudy => CloudImplementor.GetThemeColor(CloudImplementor.TypeCloud, daytime),
        WeatherCode.Cloudy => CloudImplementor.GetThemeColor(CloudImplementor.TypeCloudy, daytime),
        WeatherCode.Fog => CloudImplementor.GetThemeColor(CloudImplementor.TypeFog, daytime),
        WeatherCode.Hail => HailImplementor.GetThemeColor(daytime),
        WeatherCode.Haze => CloudImplementor.GetThemeColor(CloudImplementor.TypeHaze, daytime),
        WeatherCode.Rain => RainImplementor.GetThemeColor(RainImplementor.TypeRain, daytime),
        WeatherCode.Sleet => RainImplementor.GetThemeColor(RainImplementor.TypeSleet, daytime),
        WeatherCode.Snow => SnowImplementor.GetThemeColor(daytime),
        WeatherCode.Thunderstorm => RainImplementor.GetThemeColor(RainImplementor.TypeThunderstorm, daytime),
        WeatherCode.Thunder => CloudImplementor.GetThemeColor(CloudImplementor.TypeThunder, daytime),
        WeatherCode.Wind => WindImplementor.GetThemeColor(daytime),
        _ => 0x00000000,
    };

    /// <summary>
    /// The weather's color twice, then at half opacity. Night colors are brightened.
    /// </summary>
    public static Color[] GetThemeColors(WeatherCode? weatherCode, bool daylight)
    {
        uint color = InnerGetBackgroundColor(weatherCode, daylight);
        if (!daylight)
        {
            color = GetBrighterColor(color);
        }

        Color themeColor = FromArgb(color);
        return [themeColor, themeColor, Color.FromArgb((byte)(int)(0.5 * 255), themeColor.R, themeColor.G, themeColor.B)];
    }

    /// <summary>
    /// A frozen brush of the weather's first theme color.
    /// </summary>
    public static SolidColorBrush GetThemeBrush(WeatherCode? weatherCode, bool daylight)
    {
        var brush = new SolidColorBrush(GetThemeColors(weatherCode, daylight)[0]);
        brush.Freeze();
        return brush;
    }

    private static Color FromArgb(uint color)
    {
        return Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
    }
}
