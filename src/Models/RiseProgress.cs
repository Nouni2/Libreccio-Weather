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

using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.Core.Models;

/// <summary>
/// How far the current time of day is between a rise and the following set, in the location's time zone.
/// </summary>
public static class RiseProgress
{
    private const int DefaultRiseHour = 6;
    private const int DefaultDurationHour = 12;

    /// <summary>
    /// 0 at the rise and 1 at the set, below 0 before the rise and above 1 after the set. Only the time of day counts,
    /// to the minute. Without a rise or a set, the day is taken to run from 06:00 to 18:00.
    /// </summary>
    public static double GetRiseProgress(Astro? astro, Location location, DateTimeOffset now)
    {
        DateTime localNow = TimeZoneInfo.ConvertTime(now, location.TimeZone).DateTime;
        long currentTime = ((localNow.Hour * 60L) + localNow.Minute) * 60_000;

        if (astro?.RiseDate is not { } riseDate || astro.SetDate is not { } setDate)
        {
            long defaultRise = DefaultRiseHour * 3_600_000L;
            long defaultSet = defaultRise + (DefaultDurationHour * 3_600_000L);
            if (defaultSet == defaultRise)
            {
                return -1.0;
            }

            return (currentTime - defaultRise) / (double)(defaultSet - defaultRise);
        }

        DateTime riseTime = WallClock(riseDate, location.TimeZone);
        DateTime setTime = WallClock(setDate, location.TimeZone);
        long riseHourMinuteTime = ((riseTime.Hour * 60L) + riseTime.Minute) * 60_000;

        // A set that is not after the rise belongs to the following day.
        DateTime safeSetTime = setTime;
        while (safeSetTime <= riseTime)
        {
            safeSetTime = safeSetTime.AddDays(1);
        }

        long setHourMinuteTime = riseHourMinuteTime + ((safeSetTime - riseTime).Ticks / TimeSpan.TicksPerMillisecond);
        return (currentTime - riseHourMinuteTime) / (double)(setHourMinuteTime - riseHourMinuteTime);
    }

    /// <summary>
    /// Whether the sun is up at the location now, by today's sunrise and sunset.
    /// </summary>
    public static bool IsDaylight(Location location, DateTimeOffset now)
    {
        double sunRiseProgress = GetRiseProgress(location.Weather?.Today?.Sun, location, now);
        return sunRiseProgress > 0 && sunRiseProgress < 1;
    }

    // The date's clock time in the time zone, to the second, compared as wall-clock values so a daylight saving
    // change between the rise and the set does not count.
    private static DateTime WallClock(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        DateTime local = TimeZoneInfo.ConvertTime(date, timeZone).DateTime;
        return new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second, DateTimeKind.Unspecified);
    }
}
