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

public enum WeatherCode
{
    Clear,
    PartlyCloudy,
    Cloudy,
    Rain,
    Snow,
    Wind,
    Fog,
    Haze,
    Sleet,
    Hail,
    Thunder,
    Thunderstorm,
}

public static class WeatherCodeExtensions
{
    extension(WeatherCode code)
    {
        /// <summary>
        /// Identifier used when the code is stored.
        /// </summary>
        public string Id => code switch
        {
            WeatherCode.Clear => "clear",
            WeatherCode.PartlyCloudy => "partly_cloudy",
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
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
        };
    }

    extension(WeatherCode)
    {
        public static WeatherCode? GetInstance(string? value)
        {
            foreach (WeatherCode code in Enum.GetValues<WeatherCode>())
            {
                if (string.Equals(code.Id, value, StringComparison.OrdinalIgnoreCase))
                {
                    return code;
                }
            }

            return null;
        }
    }
}
