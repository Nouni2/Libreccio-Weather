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

using System.Globalization;

namespace LibreccioWeather.Core.Localization;

public static class DateFormatting
{
    public static string GetFormattedFullDayAndMonth(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        string formatted = Icu.SupportsRelativeTimes
            ? Icu.FormatDate(date, "MMMMd", timeZone, Strings.Culture)
                ?? TimeZoneInfo.ConvertTime(date, timeZone).ToString("M", Strings.Culture)
            : TimeZoneInfo.ConvertTime(date, timeZone).ToString("M", Strings.Culture);
        return Capitalize(formatted);
    }

    public static string GetWeek(DateTimeOffset date, TimeZoneInfo timeZone, bool full = false)
    {
        return Capitalize(TimeZoneInfo.ConvertTime(date, timeZone).ToString(full ? "dddd" : "ddd", Strings.Culture));
    }

    public static string GetHour(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        bool twelveHour = !CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('H');
        if (twelveHour && Icu.SupportsRelativeTimes
            && Icu.FormatDate(date, "ah", timeZone, Strings.Culture) is { } formatted)
        {
            return formatted;
        }

        return TimeZoneInfo.ConvertTime(date, timeZone).ToString(twelveHour ? "h tt" : "H:mm", Strings.Culture);
    }

    public static string GetFormattedTime(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        bool twelveHour = !CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('H');
        if (twelveHour && Icu.SupportsRelativeTimes
            && Icu.FormatDate(date, "ahmm", timeZone, Strings.Culture) is { } formatted)
        {
            return formatted;
        }

        return TimeZoneInfo.ConvertTime(date, timeZone).ToString(twelveHour ? "h:mm tt" : "HH:mm", Strings.Culture);
    }

    public static string GetFormattedShortDayAndMonth(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        if (Icu.SupportsRelativeTimes
            && Icu.FormatDate(date, "MMdd", timeZone, Strings.Culture) is { } formatted)
        {
            return formatted;
        }

        return TimeZoneInfo.ConvertTime(date, timeZone).ToString("MM-dd", Strings.Culture);
    }

    public static string GetFormattedDayOfTheMonth(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        return FormatBestPattern(date, timeZone, "dd", "dd");
    }

    /// <summary>
    /// The abbreviated month, such as "Oct".
    /// </summary>
    public static string GetFormattedAbbreviatedMonth(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        return FormatBestPattern(date, timeZone, "MMM", "MMM");
    }

    /// <summary>
    /// "Today", "Yesterday" or "Tomorrow", the weekday within a week, then the weekday with its date, such as
    /// "Monday 3 November".
    /// </summary>
    public static string GetFullLabel(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (date > now.AddDays(6))
        {
            return Capitalize(FormatBestPattern(date, timeZone, "EEEEdMMMM", "dddd d MMMM"));
        }

        DateTime current = TimeZoneInfo.ConvertTime(now, timeZone).DateTime;
        DateTime thisDay = TimeZoneInfo.ConvertTime(date, timeZone).DateTime;
        if (current.Year == thisDay.Year && current.DayOfYear == thisDay.DayOfYear)
        {
            return Strings.Get("daily_today");
        }

        if ((current.Year == thisDay.Year && current.DayOfYear - 1 == thisDay.DayOfYear)
            || (current.Year - 1 == thisDay.Year && current.DayOfYear == 1 && thisDay.DayOfYear is >= 365 and <= 366))
        {
            return Strings.Get("daily_yesterday");
        }

        if ((current.Year == thisDay.Year && current.DayOfYear + 1 == thisDay.DayOfYear)
            || (current.Year + 1 == thisDay.Year && thisDay.DayOfYear == 1 && current.DayOfYear is >= 365 and <= 366))
        {
            return Strings.Get("daily_tomorrow");
        }

        return GetWeek(date, timeZone, full: true);
    }

    // The ICU best pattern for a skeleton in the selected language, or the closest .NET pattern without ICU.
    private static string FormatBestPattern(DateTimeOffset date, TimeZoneInfo timeZone, string skeleton, string fallbackPattern)
    {
        if (Icu.SupportsRelativeTimes && Icu.FormatDate(date, skeleton, timeZone, Strings.Culture) is { } formatted)
        {
            return formatted;
        }

        return TimeZoneInfo.ConvertTime(date, timeZone).ToString(fallbackPattern, Strings.Culture);
    }

    private static string Capitalize(string value)
    {
        return value.Length > 0 && char.IsLower(value[0])
            ? value[..1].ToUpper(Strings.Culture) + value[1..] : value;
    }
}

