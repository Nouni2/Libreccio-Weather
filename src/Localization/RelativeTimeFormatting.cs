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

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// How long ago a moment was, such as "5 min. ago", "Yesterday" or "October 12", following Android's relative time
/// rules with a resolution of one minute and the abbreviated style, in the computer's time zone.
/// </summary>
public static class RelativeTimeFormatting
{
    /// <summary>
    /// The relative time, starting with a capital, or null when Windows has no ICU to write it with.
    /// </summary>
    public static string? GetRelativeTime(DateTimeOffset time)
    {
        return Icu.SupportsRelativeTimes ? GetRelativeTime(time, DateTimeOffset.UtcNow, TimeZoneInfo.Local) : null;
    }

    /// <summary>
    /// The same text starting with a lowercase letter, to be placed inside a sentence such as "Last updated 5 min. ago".
    /// </summary>
    public static string? GetRelativeTimeInSentence(DateTimeOffset time)
    {
        string? text = GetRelativeTime(time);
        return string.IsNullOrEmpty(text) || !char.IsUpper(text[0])
            ? text
            : Strings.Culture.TextInfo.ToLower(text[0]) + text[1..];
    }

    private static string? GetRelativeTime(DateTimeOffset time, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        TimeSpan duration = (now - time).Duration();
        bool past = now >= time;

        // Under an hour counts minutes, so less than a minute is "0 min. ago".
        if (duration < TimeSpan.FromHours(1))
        {
            return Numeric((long)duration.TotalMinutes, past, Icu.RelativeUnit.Minute);
        }

        // Under a day counts hours, even when the moment was on the previous date.
        if (duration < TimeSpan.FromDays(1))
        {
            return Numeric((long)duration.TotalHours, past, Icu.RelativeUnit.Hour);
        }

        if (duration < TimeSpan.FromDays(7))
        {
            int days = Math.Abs(DayDistance(time, now, timeZone));

            // One day uses "Yesterday" or "Tomorrow", and none "Today". Two days uses a word such as French
            // "avant-hier" when the language has one, and a number otherwise.
            return days switch
            {
                0 => Icu.FormatRelative(0, Icu.RelativeUnit.Day, numeric: false, Strings.Culture),
                1 or 2 => Icu.FormatRelative(past ? -days : days, Icu.RelativeUnit.Day, numeric: false, Strings.Culture),
                _ => Numeric(days, past, Icu.RelativeUnit.Day),
            };
        }

        // A week or more shows the date itself, with its year only when it is not the current year.
        DateTimeOffset localTime = TimeZoneInfo.ConvertTime(time, timeZone);
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        string skeleton = localTime.Year == localNow.Year ? "MMMMd" : "yMMMMd";
        return Icu.FormatDate(time, skeleton, timeZone, Strings.Culture);
    }

    // A count with its unit, such as "5 min. ago" or "in 5 min.". Negating zero gives -0.0, which ICU reads as past,
    // so zero in the past gives "0 min. ago".
    private static string? Numeric(long count, bool past, Icu.RelativeUnit unit)
    {
        double offset = past ? -(double)count : count;
        return Icu.FormatRelative(offset, unit, numeric: true, Strings.Culture);
    }

    // The number of calendar days between two moments in a time zone, positive when the end is later.
    private static int DayDistance(DateTimeOffset start, DateTimeOffset end, TimeZoneInfo timeZone)
    {
        DateOnly startDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, timeZone).DateTime);
        DateOnly endDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(end, timeZone).DateTime);
        return endDay.DayNumber - startDay.DayNumber;
    }
}
