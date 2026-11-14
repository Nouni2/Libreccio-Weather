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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.Nws;

public sealed partial class NwsService
{
    private static string? GetWeatherText(NwsValueWeatherValue? weather, Speed? windSpeed, int? cloudCover)
    {
        string? key = weather?.Weather switch
        {
            "blowing_dust" => "common_weather_text_dust_storm",
            "blowing_sand" => "common_weather_text_sand_storm",
            "blowing_snow" => "common_weather_text_blowing_snow",
            "drizzle" => "common_weather_text_drizzle",
            "fog" => "common_weather_text_fog",
            "freezing_drizzle" => "common_weather_text_drizzle_freezing",
            "freezing_rain" => "common_weather_text_rain_freezing",
            "freezing_spray" => "nws_weather_text_freezing_spray",
            "frost" => "common_weather_text_frost",
            "hail" => "weather_kind_hail",
            "haze" => "weather_kind_haze",
            "ice_crystals" => "nws_weather_text_ice_crystals",
            "ice_fog" => "nws_weather_text_ice_fog",
            "rain" => "common_weather_text_rain",
            "rain_showers" => "common_weather_text_rain_showers",
            "sleet" => "common_weather_text_rain_snow_mixed",
            "smoke" => "common_weather_text_smoke",
            "snow" => "common_weather_text_snow",
            "snow_showers" => "common_weather_text_snow_showers",
            "thunderstorms" => "weather_kind_thunderstorm",
            "volcanic_ash" => "nws_weather_text_volcanic_ash",
            "water_spouts" => "nws_weather_text_waterspout",
            _ => null,
        };
        string? weatherText = key is null ? null : Strings.Get(key);
        if (!string.IsNullOrEmpty(weatherText))
        {
            weatherText = weather?.Intensity switch
            {
                "very_light" or "light" => Strings.Format("nws_weather_text_intensity_light", weatherText),
                "heavy" => Strings.Format("nws_weather_text_intensity_heavy", weatherText),
                _ => weatherText,
            };
            string? coverageKey = weather?.Coverage switch
            {
                "areas" => "nws_weather_text_coverage_areas_of",
                "brief" => "nws_weather_text_coverage_brief",
                "chance" => "nws_weather_text_coverage_chance_of",
                "few" => "nws_weather_text_coverage_few",
                "frequent" => "nws_weather_text_coverage_frequent",
                "intermittent" => "nws_weather_text_coverage_intermittent",
                "isolated" => "nws_weather_text_coverage_isolated",
                "likely" => "nws_weather_text_coverage_likely",
                "numerous" => "nws_weather_text_coverage_numerous",
                "occasional" => "nws_weather_text_coverage_occasional",
                "patchy" => "nws_weather_text_coverage_patchy",
                "periods" => "nws_weather_text_coverage_periods_of",
                "scattered" => "nws_weather_text_coverage_scattered",
                "slight_chance" => "nws_weather_text_coverage_slight_chance_of",
                "widespread" => "nws_weather_text_coverage_widespread",
                _ => null,
            };
            if (coverageKey is not null)
            {
                weatherText = Strings.Format(coverageKey, weatherText);
            }
        }
        else if (cloudCover is { } cover)
        {
            weatherText = Strings.Get((double)cover switch
            {
                >= 87.5 => "common_weather_text_cloudy",
                >= 67.5 => "common_weather_text_cloudy",
                >= 37.5 => "common_weather_text_partly_cloudy",
                >= 12.5 => "common_weather_text_mostly_clear",
                _ => "common_weather_text_clear_sky",
            });
        }

        string[] attributes = weather?.Attributes ?? [];
        if (attributes.Length == 0 || (!attributes.Contains("gusty_wind") && !attributes.Contains("damaging_wind")))
        {
            string? windKey = windSpeed is not { } speed ? null
                : speed >= Speed.FromMilesPerHour(40.0) ? "nws_weather_text_wind_high_wind"
                : speed >= Speed.FromMilesPerHour(30.0) ? "nws_weather_text_wind_very_windy"
                : speed >= Speed.FromMilesPerHour(20.0) ? "nws_weather_text_wind_windy"
                : speed >= Speed.FromMilesPerHour(15.0) ? "nws_weather_text_wind_breezy" : null;
            string? windDescription = windKey is null ? null : Strings.Get(windKey);
            if (!string.IsNullOrEmpty(windDescription))
            {
                weatherText = !string.IsNullOrEmpty(weatherText)
                    ? Strings.Format("nws_weather_text_condition_and_wind", weatherText, windDescription) : windDescription;
            }
        }

        string attributesText = "";
        for (int i = 0; i < attributes.Length; i++)
        {
            string separator = i == 0 ? "" : i < attributes.Length - 1
                ? Strings.GetUnit("locale_separator") : Strings.Get("nws_weather_text_separator_and");
            string? attributeKey = attributes[i] switch
            {
                "damaging_wind" => "nws_weather_text_attribute_wind_damaging",
                "dry_thunderstorms" => "weather_kind_thunder",
                "flooding" => "nws_weather_text_attribute_flooding",
                "gusty_wind" => "nws_weather_text_attribute_wind_gusty",
                "heavy_rain" => "common_weather_text_rain_heavy",
                "large_hail" => "nws_weather_text_attribute_hail_large",
                "small_hail" => "nws_weather_text_attribute_hail_small",
                "tornadoes" => "common_weather_text_tornado",
                _ => null,
            };
            if (attributeKey is not null)
            {
                attributesText += separator + Strings.Get(attributeKey);
            }
        }

        if (attributesText.Length > 0)
        {
            weatherText = !string.IsNullOrEmpty(weatherText)
                ? Strings.Format("nws_weather_text_condition_with_attribute", weatherText, attributesText) : attributesText;
        }

        return weatherText;
    }

    private static WeatherCode? GetWeatherCode(NwsValueWeatherValue? weather, Speed? windSpeed, int? cloudCover)
    {
        string[] attributes = weather?.Attributes ?? [];
        if (attributes.Contains("tornadoes"))
        {
            return WeatherCode.Wind;
        }

        if (attributes.Contains("large_hail") || attributes.Contains("small_hail"))
        {
            return WeatherCode.Hail;
        }

        if (!string.IsNullOrEmpty(weather?.Coverage) && weather.Coverage != "slight_chance")
        {
            WeatherCode? code = weather.Weather switch
            {
                "blowing_dust" or "blowing_sand" => WeatherCode.Wind,
                "blowing_snow" => WeatherCode.Snow,
                "drizzle" => WeatherCode.Rain,
                "fog" => WeatherCode.Fog,
                "freezing_drizzle" or "freezing_rain" or "freezing_spray" => WeatherCode.Sleet,
                "frost" => WeatherCode.Snow,
                "hail" => WeatherCode.Hail,
                "haze" => WeatherCode.Haze,
                "ice_crystals" or "ice_fog" => WeatherCode.Fog,
                "rain" or "rain_showers" => WeatherCode.Rain,
                "sleet" => WeatherCode.Sleet,
                "smoke" => WeatherCode.Haze,
                "snow" or "snow_showers" => WeatherCode.Snow,
                "thunderstorms" => WeatherCode.Thunderstorm,
                "volcanic_ash" => WeatherCode.Haze,
                "water_spouts" => WeatherCode.Wind,
                _ => null,
            };
            if (code is not null)
            {
                return code;
            }
        }

        if (attributes.Contains("dry_thunderstorms"))
        {
            return WeatherCode.Thunder;
        }

        if (attributes.Contains("heavy_rain"))
        {
            return WeatherCode.Rain;
        }

        if (attributes.Contains("damaging_wind") || attributes.Contains("gusty_wind"))
        {
            return WeatherCode.Wind;
        }

        if (windSpeed is { } speed && speed >= Speed.FromMilesPerHour(40.0))
        {
            return WeatherCode.Wind;
        }

        return (double?)cloudCover switch
        {
            null => null,
            >= 67.5 => WeatherCode.Cloudy,
            >= 37.5 => WeatherCode.PartlyCloudy,
            _ => WeatherCode.Clear,
        };
    }

    private static WeatherCode? GetWeatherCode(string? icon)
    {
        if (icon is null)
        {
            return null;
        }

        (string Pattern, WeatherCode Code)[] codes =
        [
            ("tornado", WeatherCode.Wind), ("hurricane", WeatherCode.Wind), ("tropical_storm", WeatherCode.Wind),
            ("sleet", WeatherCode.Sleet), ("fzra", WeatherCode.Sleet), ("rain_snow", WeatherCode.Sleet),
            ("snow", WeatherCode.Snow), ("blizzard", WeatherCode.Snow), ("tsra", WeatherCode.Thunderstorm),
            ("rain", WeatherCode.Rain), ("dust", WeatherCode.Haze), ("smoke", WeatherCode.Haze), ("haze", WeatherCode.Haze),
            ("fog", WeatherCode.Fog), ("wind", WeatherCode.Wind), ("bkn", WeatherCode.Cloudy), ("ovc", WeatherCode.Cloudy),
            ("few", WeatherCode.PartlyCloudy), ("sct", WeatherCode.Clear), ("skc", WeatherCode.Clear),
        ];
        foreach ((string pattern, WeatherCode code) in codes)
        {
            if (icon.Contains(pattern, StringComparison.Ordinal))
            {
                return code;
            }
        }

        return null;
    }
}
