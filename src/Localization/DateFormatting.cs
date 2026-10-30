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

    private static string Capitalize(string value)
    {
        return value.Length > 0 && char.IsLower(value[0])
            ? value[..1].ToUpper(Strings.Culture) + value[1..] : value;
    }
}

