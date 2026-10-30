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
using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.Core.Settings.Options;

/// <summary>
/// A page of the detail screen of a day.
/// </summary>
public sealed class DetailScreen
{
    // A chart needs at least two hours of data; with fewer, a message replaces it.
    public const int ChartMinCount = 2;

    public static readonly DetailScreen Conditions = new("conditions", "conditions");

    // Never listed: it is the feels-like half of the conditions page.
    public static readonly DetailScreen FeelsLike = new("feels_like", "conditions");
    public static readonly DetailScreen Wind = new("wind", "wind");
    public static readonly DetailScreen AirQuality = new("air_quality", "air_quality");
    public static readonly DetailScreen Pollen = new("pollen", "pollen");
    public static readonly DetailScreen UVIndex = new("uv_index", "uv_index");
    public static readonly DetailScreen Precipitation = new("precipitation", "precipitation");
    public static readonly DetailScreen Humidity = new("humidity", "humidity");
    public static readonly DetailScreen Pressure = new("pressure", "pressure");
    public static readonly DetailScreen CloudCover = new("cloud_cover", "cloud_cover");
    public static readonly DetailScreen Visibility = new("visibility", "visibility");
    public static readonly DetailScreen SunMoon = new("sun_moon", "ephemeris");

    public static IReadOnlyList<DetailScreen> Entries { get; } =
    [
        Conditions,
        FeelsLike,
        Wind,
        AirQuality,
        Pollen,
        UVIndex,
        Precipitation,
        Humidity,
        Pressure,
        CloudCover,
        Visibility,
        SunMoon,
    ];

    private readonly string _nameKey;

    private DetailScreen(string id, string nameKey)
    {
        Id = id;
        _nameKey = nameKey;
    }

    public string Id { get; }

    public string Name => Strings.Get(_nameKey);

    /// <summary>
    /// The pages offered in the page menu, in order, for the weather of a location.
    /// </summary>
    public static IReadOnlyList<DetailScreen> ToDetailScreenList(Location location)
    {
        Models.Weather? weather = location.Weather;
        return Entries.Where(screen =>
        {
            if (screen == Conditions || screen == Precipitation || screen == SunMoon)
            {
                return true;
            }

            if (screen == FeelsLike)
            {
                return false;
            }

            if (screen == Wind)
            {
                return weather?.DailyForecast.Any(day => day.Day?.Wind?.Speed is not null || day.Night?.Wind?.Speed is not null) == true
                    || weather?.HourlyForecast.Any(hour => hour.Wind?.Speed is not null) == true;
            }

            if (screen == AirQuality)
            {
                return !string.IsNullOrEmpty(location.AirQualitySource);
            }

            if (screen == Pollen)
            {
                return !string.IsNullOrEmpty(location.PollenSource);
            }

            if (screen == UVIndex)
            {
                return weather?.DailyForecast.Any(day => (day.UV?.Index ?? 0.0) > 0.0) == true
                    || weather?.HourlyForecast.Any(hour => (hour.UV?.Index ?? 0.0) > 0.0) == true;
            }

            if (screen == Humidity)
            {
                return weather?.DailyForecast.Any(day => day.RelativeHumidity?.Min is not null || day.RelativeHumidity?.Max is not null) == true
                    || weather?.HourlyForecast.Any(hour => hour.RelativeHumidity is not null || hour.DewPoint is not null) == true;
            }

            if (screen == Pressure)
            {
                return weather?.DailyForecast.Any(day => day.Pressure?.Average is not null) == true
                    || weather?.HourlyForecast.Any(hour => hour.Pressure is not null) == true;
            }

            if (screen == CloudCover)
            {
                return weather?.DailyForecast.Any(day => day.CloudCover?.Min is not null || day.CloudCover?.Max is not null) == true
                    || weather?.HourlyForecast.Any(hour => hour.CloudCover is not null) == true;
            }

            return weather?.DailyForecast.Any(day => day.Visibility?.Min is not null || day.Visibility?.Max is not null) == true
                || weather?.HourlyForecast.Any(hour => hour.Visibility is not null) == true;
        }).ToList();
    }

    public override string ToString() => Id;
}
