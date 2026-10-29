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
using LibreccioWeather.Core.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace LibreccioWeather.Appearance;

public static class WeatherBackground
{
    public static Brush GetBrush(WeatherCode? code, bool daylight)
    {
        string? name = code switch
        {
            WeatherCode.Clear => daylight ? "clear_day" : "clear_night",
            WeatherCode.PartlyCloudy => "partly_cloudy",
            WeatherCode.Cloudy => "cloudy",
            WeatherCode.Rain => "rain",
            WeatherCode.Snow => "snow",
            WeatherCode.Wind => "wind",
            WeatherCode.Fog => "fog",
            WeatherCode.Haze => "haze",
            WeatherCode.Sleet => "sleet",
            WeatherCode.Hail => "hail",
            WeatherCode.Thunder or WeatherCode.Thunderstorm => "thunder",
            _ => null,
        };
        return name is null ? Brushes.Transparent
            : (Brush)System.Windows.Application.Current.FindResource("WeatherBackground." + name);
    }
}
