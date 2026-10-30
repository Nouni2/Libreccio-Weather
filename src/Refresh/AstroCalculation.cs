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

using LibreccioWeather.Core.Astronomy;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Solar;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Refresh;

/// <summary>
/// Sun, civil twilight, moon and moon phase of a day that starts at midnight. A body that stays up all day rises at
/// midnight and sets a millisecond before the next midnight; one that is already up at midnight rises at midnight.
/// </summary>
internal static class AstroCalculation
{
    // A set that does not follow its rise within the day is searched for over a year.
    private const int FullCycleDays = 365;

    // The Sun's mean angular radius. Spp gives the upper limb's altitude; civil twilight is the center's.
    private const double SunSemidiameterDegrees = 0.2666;

    private const double CivilTwilightDegrees = -6.0;

    /// <summary>
    /// Sunrise and sunset, or null when Spp is unavailable or fails, which leaves the day without sun times.
    /// </summary>
    public static Astro? GetSun(Daily daily, Location location)
    {
        DateOnly localDate = LocalDate(daily.Date, location.TimeZone);
        if (SolarCalculator.GetSolarDay(location.Latitude, location.Longitude, localDate, location.TimeZone) is not { } day)
        {
            return null;
        }

        if (day.Type == SolarDayType.PolarDay)
        {
            return AllDay(daily.Date);
        }

        if (day.Type == SolarDayType.PolarNight)
        {
            return new Astro();
        }

        return Complete(daily.Date, day.Rise, day.Set, location, localDate, next => next.Set);
    }

    /// <summary>
    /// Civil dawn and dusk, when the Sun's center is 6° below the horizon, or null when Spp is unavailable or fails.
    /// </summary>
    public static Astro? GetTwilight(Daily daily, Location location)
    {
        DateOnly localDate = LocalDate(daily.Date, location.TimeZone);
        if (SolarCalculator.GetSolarDay(location.Latitude, location.Longitude, localDate, location.TimeZone) is not { } day)
        {
            return null;
        }

        if (day.CivilDawn is null && day.CivilDusk is null)
        {
            // Without dawn or dusk, the Sun's center stays above −6° all day, as in white nights, or below.
            if (SolarCalculator.GetUpperLimbAltitude(location.Latitude, location.Longitude, day.Transit.AddHours(12)) is not { } lowest)
            {
                return null;
            }

            return lowest - SunSemidiameterDegrees > CivilTwilightDegrees ? AllDay(daily.Date) : new Astro();
        }

        return Complete(daily.Date, day.CivilDawn, day.CivilDusk, location, localDate, next => next.CivilDusk);
    }

    public static Astro GetMoon(Daily daily, Location location)
    {
        DateTimeOffset date = daily.Date;
        MoonTimes riseTimes = MoonCalculator.GetTimes(date, location.Latitude, location.Longitude);

        if (riseTimes.IsAlwaysUp)
        {
            return AllDay(date);
        }

        // Without a rise, the Moon was already up before midnight.
        if (riseTimes.Rise is null && riseTimes.Set is { } set)
        {
            return new Astro { RiseDate = date, SetDate = set };
        }

        if (riseTimes.Rise is { } rise && riseTimes.Set is { } earlierSet && earlierSet < rise)
        {
            MoonTimes setTimes = MoonCalculator.GetTimes(rise, location.Latitude, location.Longitude);
            if (setTimes.Set is { } nextSet)
            {
                return new Astro { RiseDate = rise, SetDate = nextSet };
            }

            // Rarely needed, so the longer search is only done when the day has no set.
            MoonTimes setTimes2 = MoonCalculator.GetTimes(rise, location.Latitude, location.Longitude, fullCycle: true);
            return new Astro { RiseDate = rise, SetDate = setTimes2.Set };
        }

        if (riseTimes.Rise is { } lateRise && riseTimes.Set is null)
        {
            MoonTimes times2 = MoonCalculator.GetTimes(lateRise, location.Latitude, location.Longitude, fullCycle: true);
            return new Astro { RiseDate = times2.Rise, SetDate = times2.Set };
        }

        return new Astro { RiseDate = riseTimes.Rise, SetDate = riseTimes.Set };
    }

    /// <summary>
    /// The phase at noon of a day starting at midnight, as an angle from 0° at new moon through 180° at full moon.
    /// </summary>
    public static MoonPhase GetMoonPhase(Daily daily)
    {
        MoonIllumination illumination = MoonCalculator.GetIllumination(daily.Date.AddHours(12));
        return new MoonPhase { Angle = (int)Math.Clamp(UnitRounding.RoundToLong(illumination.Phase + 180), int.MinValue, int.MaxValue) };
    }

    // A day without a rise starts at midnight. A rise without a set after it takes the first set of the next days.
    private static Astro Complete(
        DateTimeOffset date, DateTimeOffset? rise, DateTimeOffset? set, Location location, DateOnly localDate,
        Func<SolarDay, DateTimeOffset?> nextEvent)
    {
        if (rise is null && set is not null)
        {
            return new Astro { RiseDate = date, SetDate = set };
        }

        if (rise is { } riseDate && (set is null || set < riseDate))
        {
            return new Astro { RiseDate = riseDate, SetDate = FindNext(riseDate, location, localDate, nextEvent) };
        }

        return new Astro { RiseDate = rise, SetDate = set };
    }

    private static DateTimeOffset? FindNext(
        DateTimeOffset after, Location location, DateOnly localDate, Func<SolarDay, DateTimeOffset?> nextEvent)
    {
        for (int offset = 0; offset <= FullCycleDays; offset++)
        {
            SolarDay? day = SolarCalculator.GetSolarDay(location.Latitude, location.Longitude, localDate.AddDays(offset), location.TimeZone);
            if (day is null)
            {
                return null;
            }

            if (nextEvent(day) is { } next && next > after)
            {
                return next;
            }
        }

        return null;
    }

    private static Astro AllDay(DateTimeOffset date)
    {
        return new Astro { RiseDate = date, SetDate = date.AddDays(1).AddMilliseconds(-1) };
    }

    private static DateOnly LocalDate(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(date, timeZone).DateTime);
    }
}
