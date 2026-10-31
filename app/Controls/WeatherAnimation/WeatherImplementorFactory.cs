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

namespace LibreccioWeather.Controls.WeatherAnimation;

/// <summary>
/// The animation of each kind of weather.
/// </summary>
public static class WeatherImplementorFactory
{
    /// <param name="darkMode">Whether the app is dark, which dims the sun and the clouds' colors.</param>
    public static WeatherAnimationImplementor? GetWeatherImplementor(
        WeatherCode? weatherCode,
        bool daytime,
        int[] sizes,
        bool animate,
        bool darkMode)
    {
        return weatherCode switch
        {
            WeatherCode.Clear => daytime
                ? (WeatherAnimationImplementor)new SunImplementor(sizes, darkMode)
                : new MeteorShowerImplementor(sizes, animate),
            WeatherCode.PartlyCloudy => new CloudImplementor(sizes, animate, CloudImplementor.TypeCloud, daytime, darkMode),
            WeatherCode.Cloudy => new CloudImplementor(sizes, animate, CloudImplementor.TypeCloudy, daytime, darkMode),
            WeatherCode.Fog => new CloudImplementor(sizes, animate, CloudImplementor.TypeFog, daytime, darkMode),
            WeatherCode.Haze => new CloudImplementor(sizes, animate, CloudImplementor.TypeHaze, daytime, darkMode),
            WeatherCode.Rain => new RainImplementor(sizes, animate, RainImplementor.TypeRain, daytime),
            WeatherCode.Sleet => new RainImplementor(sizes, animate, RainImplementor.TypeSleet, daytime),
            WeatherCode.Snow => new SnowImplementor(sizes, daytime),
            WeatherCode.Hail => new HailImplementor(sizes, daytime),
            WeatherCode.Thunderstorm => new RainImplementor(sizes, animate, RainImplementor.TypeThunderstorm, daytime),

            // Thunder clouds take their dark colors from the night, not from the app's mode.
            WeatherCode.Thunder => new CloudImplementor(sizes, animate, CloudImplementor.TypeThunder, daytime),
            WeatherCode.Wind => new WindImplementor(sizes, daytime),
            _ => null,
        };
    }
}
