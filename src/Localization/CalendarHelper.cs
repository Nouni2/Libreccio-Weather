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

/// <summary>
/// The calendars whose dates can be shown beside Gregorian dates, such as the Chinese or Hebrew calendar, named with
/// CLDR's names in the selected language.
/// </summary>
public static class CalendarHelper
{
    private const string DisplayKeywordOfCalendar = "calendar";

    private static readonly string[] SupportedCalendars =
    [
        "chinese",
        "dangi",
        "hebrew",
        "indian",
        "islamic",
        "islamic-civil",
        "islamic-rgsa",
        "islamic-tbla",
        "islamic-umalqura",
        "persian",
    ];

    /// <summary>
    /// The choices of the calendar setting: none, the language's own calendar, then each calendar by name.
    /// </summary>
    public static IReadOnlyList<AlternateCalendar> GetCalendars()
    {
        CultureInfo culture = Strings.Culture;
        List<AlternateCalendar> calendars = SupportedCalendars
            .Select(id => new AlternateCalendar(
                id,
                DisplayName(id, culture),
                AdditionalParams: id == "chinese" ? "numbers=hanidays" : null,
                SpecificPattern: id == "chinese" ? "MMMd" : null))
            .OrderBy(calendar => calendar.DisplayName, StringComparer.Ordinal)
            .ToList();

        string regionalPreference = GetCalendarPreferenceForLocale(culture);
        calendars.Insert(0, new AlternateCalendar("none", Strings.Get("settings_none")));
        calendars.Insert(1, new AlternateCalendar(
            "",
            Strings.Format(
                "parenthesis",
                Strings.Get("settings_regional_preference"),
                calendars.FirstOrDefault(calendar => calendar.Id == regionalPreference)?.DisplayName
                    ?? Strings.Get("settings_none"))));
        return calendars;
    }

    /// <summary>
    /// The calendar to show dates in, from the setting: null for none, or for a calendar that is not offered.
    /// An empty setting follows the selected language.
    /// </summary>
    public static string? GetAlternateCalendarSetting(string setting)
    {
        if (setting == "none")
        {
            return null;
        }

        string alternateCalendar = setting.Length == 0 ? GetCalendarPreferenceForLocale(Strings.Culture) : setting;
        return SupportedCalendars.Contains(alternateCalendar) ? alternateCalendar : null;
    }

    /// <summary>
    /// A date as day and month in the chosen calendar, such as "十月初九" or "9 Heshvan", in the location's time zone.
    /// Null without a calendar, or without ICU to format it.
    /// </summary>
    public static string? GetFormattedMediumDayAndMonthInAdditionalCalendar(
        DateTimeOffset date,
        TimeZoneInfo timeZone,
        string setting)
    {
        if (GetAlternateCalendarSetting(setting) is not { } calendarId
            || GetCalendars().FirstOrDefault(calendar => calendar.Id == calendarId) is not { } alternateCalendar
            || !Icu.SupportsRelativeTimes)
        {
            return null;
        }

        string locale = Icu.GetLocaleId(Strings.Culture) + "@calendar=" + calendarId
            + (alternateCalendar.AdditionalParams is { } parameters ? ";" + parameters : "");
        return Icu.FormatDate(date, "d MMM", alternateCalendar.SpecificPattern, timeZone, locale);
    }

    // The calendar customary for a language or country, which is Gregorian for most of them.
    private static string GetCalendarPreferenceForLocale(CultureInfo culture)
    {
        string language = culture.TwoLetterISOLanguageName;
        string country = culture.IsNeutralCulture || !culture.Name.Contains('-')
            ? ""
            : new RegionInfo(culture.Name).TwoLetterISORegionName;
        bool Is(string value, params string[] candidates)
        {
            return candidates.Any(candidate => value.Equals(candidate, StringComparison.OrdinalIgnoreCase));
        }

        if (Is(country, "CN", "HK", "MO", "TW") || Is(language, "zh"))
        {
            return "chinese";
        }

        if (Is(country, "KP", "KR") || Is(language, "ko"))
        {
            return "dangi";
        }

        if (Is(language, "he", "iw"))
        {
            return "hebrew";
        }

        if (Is(country, "IN"))
        {
            return "indian";
        }

        if (Is(country, "SA"))
        {
            return "islamic-rgsa";
        }

        return Is(country, "IR") || Is(language, "fa") ? "persian" : "gregorian";
    }

    // CLDR's name of a calendar in a language, falling back to English, then to the identifier.
    private static string DisplayName(string calendar, CultureInfo culture)
    {
        string localeWithCalendar = "@calendar=" + calendar;
        string? result = Icu.GetDisplayKeywordValue(localeWithCalendar, DisplayKeywordOfCalendar, Icu.GetLocaleId(culture));
        if (result is not null && !result.Equals(calendar, StringComparison.Ordinal))
        {
            return Capitalize(result, CultureInfo.InvariantCulture);
        }

        string? english = Icu.GetDisplayKeywordValue(localeWithCalendar, DisplayKeywordOfCalendar, "en_001");
        return english is not null ? Capitalize(english, culture) : Capitalize(calendar, CultureInfo.InvariantCulture);
    }

    private static string Capitalize(string text, CultureInfo culture)
    {
        return text.Length == 0 || !char.IsLower(text[0])
            ? text
            : culture.TextInfo.ToUpper(text[0]) + text[1..];
    }

    public sealed record AlternateCalendar(
        string Id,
        string DisplayName,
        string? AdditionalParams = null,
        string? SpecificPattern = null);
}
