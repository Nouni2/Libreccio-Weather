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

using System.Text;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using UnitPrecipitation = LibreccioWeather.Core.Units.Precipitation;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// The texts of the precipitation in the next hour: whether it starts, continues or stops, and when.
/// </summary>
public static class MinutelyFormatting
{
    private static readonly UnitPrecipitation None = UnitPrecipitation.FromMillimeters(0.0);
    private static readonly UnitPrecipitation Light = UnitPrecipitation.FromMillimeters(Precipitation.PrecipitationHourlyLight);
    private static readonly UnitPrecipitation Medium = UnitPrecipitation.FromMillimeters(Precipitation.PrecipitationHourlyMedium);

    public static bool HasMinutelyPrecipitation(Weather weather)
    {
        return weather.MinutelyForecast.Any(minutely => (minutely.Dbz ?? 0) > 0);
    }

    public static string GetMinutelyTitle(Weather weather)
    {
        if (!HasMinutelyPrecipitation(weather))
        {
            return Strings.Get("precipitation");
        }

        return GetCase(weather) switch
        {
            1 => Strings.Get("notification_precipitation_starting"),
            2 => Strings.Get("notification_precipitation_continuing"),
            _ => Strings.Get("notification_precipitation_stopping"),
        };
    }

    public static string GetMinutelyDescription(Weather weather, Location location)
    {
        if (!HasMinutelyPrecipitation(weather))
        {
            return Strings.Get("precipitation_none");
        }

        int precipitationCase = GetCase(weather);
        string key = precipitationCase switch
        {
            1 => "notification_precipitation_starting_desc",
            2 => "notification_precipitation_continuing_desc",
            _ => "notification_precipitation_stopping_desc",
        };
        DateTimeOffset time = precipitationCase == 1
            ? weather.MinutelyForecast.First(minutely => (minutely.Dbz ?? 0) > 0).Date
            : weather.MinutelyForecast.Last(minutely => (minutely.Dbz ?? 0) > 0).EndingDate;
        return Strings.Format(key, DateFormatting.GetFormattedTime(time, location.TimeZone));
    }

    /// <summary>
    /// The intensity's level: none, light up to 5 mm/h, medium up to 10 mm/h, heavy above.
    /// </summary>
    public static string GetLevel(Minutely minutely)
    {
        if (minutely.PrecipitationIntensity is not { } intensity || intensity == None)
        {
            return Strings.Get("precipitation_none");
        }

        // Each range includes its end, so 5 mm/h is light and 10 mm/h medium.
        if (intensity >= None && intensity <= Light)
        {
            return Strings.Get("precipitation_intensity_light");
        }

        if (intensity >= Light && intensity <= Medium)
        {
            return Strings.Get("precipitation_intensity_medium");
        }

        return intensity >= Medium ? Strings.Get("precipitation_intensity_heavy") : Strings.Get("precipitation_none");
    }

    /// <summary>
    /// Each stretch of precipitation, from its first interval to the end of its last, with the level of its heaviest
    /// interval.
    /// </summary>
    public static string GetContentDescription(IReadOnlyList<Minutely> minutelyList, Location location)
    {
        var contentDescription = new StringBuilder();
        int? startingIndex = null;
        for (int index = 0; index < minutelyList.Count; index++)
        {
            if (minutelyList[index].PrecipitationIntensity is { } intensity && intensity.InMicrometers > 0)
            {
                startingIndex ??= index;
            }
            else if (startingIndex is { } start)
            {
                AppendStretch(contentDescription, minutelyList, start, index, location);
                startingIndex = null;
            }
        }

        if (startingIndex is { } lastStart)
        {
            AppendStretch(contentDescription, minutelyList, lastStart, minutelyList.Count, location);
        }

        return contentDescription.ToString();
    }

    // 1 when precipitation starts, 2 when it continues to the end of the hour, 3 when it stops.
    private static int GetCase(Weather weather)
    {
        if (weather.MinutelyForecast[0].Dbz is { } first && first > 0)
        {
            return weather.MinutelyForecast[^1].Dbz is { } last && last > 0 ? 2 : 3;
        }

        return 1;
    }

    private static void AppendStretch(
        StringBuilder contentDescription, IReadOnlyList<Minutely> minutelyList, int start, int end, Location location)
    {
        if (contentDescription.Length > 0)
        {
            contentDescription.Append(Strings.GetUnit("locale_separator"));
        }

        List<Minutely> slice = [.. minutelyList.Skip(start).Take(end - start)];
        contentDescription.Append(Strings.Format("precipitation_between_time",
            DateFormatting.GetFormattedTime(slice[0].Date, location.TimeZone),
            DateFormatting.GetFormattedTime(slice[^1].EndingDate, location.TimeZone)));
        contentDescription.Append(Strings.Get("colon_separator"));
        contentDescription.Append(GetLevel(slice.MaxBy(minutely => minutely.PrecipitationIntensity!.Value)!));
    }
}
