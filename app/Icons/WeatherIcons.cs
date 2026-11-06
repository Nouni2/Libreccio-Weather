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
using System.Windows.Media.Imaging;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Icons;

public static class WeatherIcons
{
    // One decoded image per icon for the whole app: screens are rebuilt often, and each icon is a 256-pixel PNG.
    private static readonly Dictionary<string, ImageSource> Images = [];
    private static readonly object ImagesGate = new();

    /// <summary>
    /// The image of an icon path from <see cref="GetPath"/>, decoded once and shared.
    /// </summary>
    public static ImageSource? GetImage(string? path)
    {
        if (path is null)
        {
            return null;
        }

        lock (ImagesGate)
        {
            if (!Images.TryGetValue(path, out ImageSource? image))
            {
                var bitmap = new BitmapImage(new Uri(path));
                bitmap.Freeze();
                image = bitmap;
                Images[path] = image;
            }

            return image;
        }
    }

    public static string? GetPath(WeatherCode? code, bool daylight)
    {
        string? name = code switch
        {
            WeatherCode.Clear => daylight ? "clear_day" : "clear_night",
            WeatherCode.PartlyCloudy => daylight ? "partly_cloudy_day" : "partly_cloudy_night",
            WeatherCode.Cloudy => "cloudy",
            WeatherCode.Rain => "rain",
            WeatherCode.Snow => "snow",
            WeatherCode.Wind => "wind",
            WeatherCode.Fog => "fog",
            WeatherCode.Haze => "haze",
            WeatherCode.Sleet => "sleet",
            WeatherCode.Hail => "hail",
            WeatherCode.Thunder => "thunder",
            WeatherCode.Thunderstorm => "thunderstorm",
            _ => null,
        };
        return name is null ? null : $"pack://application:,,,/assets/weather/weather_{name}.png";
    }
}
