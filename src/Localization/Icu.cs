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
using System.Runtime.InteropServices;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Unicode CLDR formatting of units, durations, relative times and dates through the ICU library that Windows ships
/// as icu.dll from Windows 10 version 1903. Each feature checks that the functions it
/// needs exist, as older Windows versions have an older ICU or none; callers then use the translated strings instead.
/// </summary>
internal static class Icu
{
    private const string Library = "icu.dll";

    // ICU writes this error when the output buffer is too small, with the needed length as the return value.
    private const int BufferOverflowError = 15;

    // UNUM_MEASURE_UNIT_FIELD: the unit part of a formatted number, such as "kilometers" in "2 kilometers".
    private const int MeasureUnitField = 11;

    // ULISTFMT_TYPE_UNITS and ULISTFMT_WIDTH_WIDE: the list style MeasureFormat joins measures with.
    private const int ListTypeUnits = 2;
    private const int ListWidthWide = 0;

    // UDAT_STYLE_SHORT, and UDISPCTX_CAPITALIZATION_FOR_BEGINNING_OF_SENTENCE as (type 1 << 8) + 2.
    private const int RelativeStyleShort = 1;
    private const int CapitalizationForBeginningOfSentence = 258;

    // UDAT_PATTERN for both the date and the time style, so the given pattern is used.
    private const int DatePatternStyle = -2;

    // Windows' own icu.dll is loaded from System32 only, never from the current directory or PATH. The imports below
    // name the same module, which Windows then finds already loaded and uses again.
    private static readonly IntPtr Handle =
        NativeLibrary.TryLoad(Library, typeof(Icu).Assembly, DllImportSearchPath.System32, out IntPtr handle) ? handle : IntPtr.Zero;

    public static bool SupportsUnitNames { get; } = HasExports(
        "unumf_openForSkeletonAndLocale", "unumf_openResult", "unumf_formatDouble", "unumf_resultToString",
        "unumf_resultNextFieldPosition", "unumf_close", "unumf_closeResult");

    public static bool SupportsDurations { get; } = SupportsUnitNames
        && HasExports("ulistfmt_openForType", "ulistfmt_format", "ulistfmt_close");

    public static bool SupportsRelativeTimes { get; } = HasExports(
        "ureldatefmt_open", "ureldatefmt_format", "ureldatefmt_formatNumeric", "ureldatefmt_close",
        "udatpg_open", "udatpg_getBestPattern", "udatpg_close", "udat_open", "udat_format", "udat_close");

    public static bool SupportsDateParsing { get; } = HasExports("udat_open", "udat_parse", "udat_setLenient", "udat_close");

    // Opening a formatter loads the language's CLDR data and parses its pattern, which costs far more than formatting
    // with it, so each formatter is opened once and kept for the life of the process. A screen formats hundreds of
    // values with a handful of formatters. ICU's date and relative-time formatters are not safe to use from two
    // threads at once, so every call holds the same lock.
    private static readonly object Gate = new();
    private static readonly Dictionary<(string Skeleton, string Locale), IntPtr> NumberFormatters = [];
    private static readonly Dictionary<string, IntPtr> ListFormatters = [];
    private static readonly Dictionary<string, IntPtr> RelativeFormatters = [];
    private static readonly Dictionary<string, IntPtr> PatternGenerators = [];
    private static readonly Dictionary<(string Skeleton, string Locale), string?> DatePatterns = [];
    private static readonly Dictionary<(string Pattern, string Locale, string Zone), IntPtr> DateFormatters = [];
    private static readonly Dictionary<string, IntPtr> PluralRuleSets = [];

    /// <summary>
    /// Formats a value with a number skeleton, such as "measure-unit/duration-hour unit-width-full-name".
    /// Returns the whole text and the unit part of it, or null when ICU cannot format it.
    /// </summary>
    public static (string Text, string? Unit)? FormatNumber(string skeleton, double value, CultureInfo culture)
    {
        lock (Gate)
        {
            string locale = LocaleId(culture);
            IntPtr formatter = Cached(NumberFormatters, (skeleton, locale), () =>
            {
                int openStatus = 0;
                IntPtr opened = unumf_openForSkeletonAndLocale(skeleton, skeleton.Length, locale, ref openStatus);
                return Opened(opened, openStatus, unumf_close);
            });
            if (formatter == IntPtr.Zero)
            {
                return null;
            }

            int status = 0;
            IntPtr result = unumf_openResult(ref status);
            try
            {
                unumf_formatDouble(formatter, value, result, ref status);
                if (Failed(status))
                {
                    return null;
                }

                string? text = ReadString((char[] buffer, ref int error) => unumf_resultToString(result, buffer, buffer.Length, ref error));
                if (text is null)
                {
                    return null;
                }

                FieldPosition position = new() { Field = MeasureUnitField };
                int fieldStatus = 0;
                string? unit = unumf_resultNextFieldPosition(result, ref position, ref fieldStatus) != 0 && !Failed(fieldStatus)
                    ? text[position.BeginIndex..position.EndIndex]
                    : null;
                return (text, unit);
            }
            finally
            {
                unumf_closeResult(result);
            }
        }
    }

    /// <summary>
    /// Joins items the way MeasureFormat joins measures, such as "1 hour, 30 minutes".
    /// </summary>
    public static string? FormatUnitList(IReadOnlyList<string> items, CultureInfo culture)
    {
        lock (Gate)
        {
            string locale = LocaleId(culture);
            IntPtr formatter = Cached(ListFormatters, locale, () =>
            {
                int openStatus = 0;
                IntPtr opened = ulistfmt_openForType(locale, ListTypeUnits, ListWidthWide, ref openStatus);
                return Opened(opened, openStatus, ulistfmt_close);
            });
            if (formatter == IntPtr.Zero)
            {
                return null;
            }

            string[] strings = [.. items];
            int[] lengths = [.. items.Select(item => item.Length)];
            return ReadString((char[] buffer, ref int error) =>
                ulistfmt_format(formatter, strings, lengths, strings.Length, buffer, buffer.Length, ref error));
        }
    }

    /// <summary>
    /// A relative time in the short style, starting with a capital as at the beginning of a sentence.
    /// Numeric gives "5 min. ago"; otherwise ICU may use a word such as "yesterday". A negative offset is in the past,
    /// and -0.0 counts as past.
    /// </summary>
    public static string? FormatRelative(double offset, RelativeUnit unit, bool numeric, CultureInfo culture)
    {
        lock (Gate)
        {
            string locale = LocaleId(culture);
            IntPtr formatter = Cached(RelativeFormatters, locale, () =>
            {
                int openStatus = 0;
                IntPtr opened = ureldatefmt_open(
                    locale, IntPtr.Zero, RelativeStyleShort, CapitalizationForBeginningOfSentence, ref openStatus);
                return Opened(opened, openStatus, ureldatefmt_close);
            });
            if (formatter == IntPtr.Zero)
            {
                return null;
            }

            return ReadString((char[] buffer, ref int error) => numeric
                ? ureldatefmt_formatNumeric(formatter, offset, (int)unit, buffer, buffer.Length, ref error)
                : ureldatefmt_format(formatter, offset, (int)unit, buffer, buffer.Length, ref error));
        }
    }

    public static bool SupportsPluralRules { get; } = HasExports("uplrules_open", "uplrules_select", "uplrules_close");

    /// <summary>
    /// The CLDR plural category of a number in a language, such as "few", or null without ICU.
    /// </summary>
    public static string? SelectPlural(double number, CultureInfo culture)
    {
        if (!SupportsPluralRules)
        {
            return null;
        }

        lock (Gate)
        {
            string locale = LocaleId(culture);
            IntPtr rules = Cached(PluralRuleSets, locale, () =>
            {
                int openStatus = 0;
                IntPtr opened = uplrules_open(locale, ref openStatus);
                return Opened(opened, openStatus, uplrules_close);
            });
            return rules == IntPtr.Zero
                ? null
                : ReadString((char[] buffer, ref int error) => uplrules_select(rules, number, buffer, buffer.Length, ref error));
        }
    }

    public static bool SupportsLikelyScripts { get; } = HasExports("uloc_addLikelySubtags", "uloc_getScript");

    /// <summary>
    /// The script a language is most likely written in, such as "Cyrl" for sr and "Latn" for sr-Latn-RS, or null
    /// without ICU.
    /// </summary>
    public static string? GetLikelyScript(CultureInfo culture)
    {
        if (!SupportsLikelyScripts)
        {
            return null;
        }

        lock (Gate)
        {
            byte[] maximized = new byte[157];
            int status = 0;
            int length = uloc_addLikelySubtags(LocaleId(culture), maximized, maximized.Length, ref status);
            if (Failed(status) || length >= maximized.Length)
            {
                return null;
            }

            byte[] script = new byte[12];
            status = 0;
            int scriptLength = uloc_getScript(maximized, script, script.Length, ref status);
            return Failed(status) || scriptLength == 0 ? null : System.Text.Encoding.ASCII.GetString(script, 0, scriptLength);
        }
    }

    public static bool SupportsNumberingSystems { get; } = HasExports("unumsys_open", "unumsys_getName", "unumsys_close");

    /// <summary>
    /// The CLDR default numbering system of a language, such as "arab" for Arabic or "latn", or null without ICU.
    /// </summary>
    public static string? GetNumberingSystem(CultureInfo culture)
    {
        if (!SupportsNumberingSystems)
        {
            return null;
        }

        lock (Gate)
        {
            int status = 0;
            IntPtr system = unumsys_open(LocaleId(culture), ref status);
            if (system == IntPtr.Zero || Failed(status))
            {
                if (system != IntPtr.Zero)
                {
                    unumsys_close(system);
                }

                return null;
            }

            try
            {
                return Marshal.PtrToStringAnsi(unumsys_getName(system));
            }
            finally
            {
                unumsys_close(system);
            }
        }
    }

    public static bool SupportsLanguageNames { get; } = HasExports("uloc_getDisplayLanguage", "uloc_getDisplayCountry");

    /// <summary>
    /// A language's name in that language, such as "English (Australia)" for en-AU, or null without ICU. The language
    /// and country names are joined with " (" and ")" in every language, as Android's Locale.getDisplayName does, where
    /// ICU's own display name follows each language's pattern.
    /// </summary>
    public static string? GetOwnDisplayName(string languageCode)
    {
        if (!SupportsLanguageNames)
        {
            return null;
        }

        lock (Gate)
        {
            string locale = languageCode.Replace('-', '_');
            string[] parts = languageCode.Split('-');
            string? language = ReadString((char[] buffer, ref int error) =>
                uloc_getDisplayLanguage(locale, locale, buffer, buffer.Length, ref error));
            if (language is null)
            {
                return null;
            }

            string name = language.Length == 0 ? parts[0] : language;
            if (parts.Length < 2)
            {
                return name;
            }

            string? country = ReadString((char[] buffer, ref int error) =>
                uloc_getDisplayCountry(locale, locale, buffer, buffer.Length, ref error));
            return country is { Length: > 0 } ? $"{name} ({country})" : name;
        }
    }

    public static bool SupportsKeywordNames { get; } = HasExports("uloc_getDisplayKeywordValue");

    public static bool SupportsCountryNames { get; } = HasExports("uloc_getDisplayCountry");

    /// <summary>
    /// A country's name in a language, such as "Germany" for DE in English, or null without ICU.
    /// </summary>
    public static string? GetDisplayCountry(string countryCode, CultureInfo displayCulture)
    {
        if (!SupportsCountryNames)
        {
            return null;
        }

        lock (Gate)
        {
            string locale = "und_" + countryCode;
            string displayLocale = LocaleId(displayCulture);
            return ReadString((char[] buffer, ref int error) =>
                uloc_getDisplayCountry(locale, displayLocale, buffer, buffer.Length, ref error));
        }
    }

    /// <summary>
    /// Lenient parsing lets out-of-range calendar fields roll into the surrounding date.
    /// </summary>
    public static DateTimeOffset? ParseDate(string text, string pattern, TimeZoneInfo timeZone, CultureInfo culture)
    {
        if (!SupportsDateParsing)
        {
            return null;
        }

        lock (Gate)
        {
            string zone = TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out string? iana) ? iana : timeZone.Id;
            int status = 0;
            IntPtr formatter = udat_open(DatePatternStyle, DatePatternStyle, LocaleId(culture), zone, zone.Length,
                pattern, pattern.Length, ref status);
            if (formatter == IntPtr.Zero || Failed(status))
            {
                if (formatter != IntPtr.Zero)
                {
                    udat_close(formatter);
                }

                return null;
            }

            try
            {
                udat_setLenient(formatter, 1);
                int position = 0;
                double milliseconds = udat_parse(formatter, text, text.Length, ref position, ref status);
                return Failed(status) || position == 0 || !double.IsFinite(milliseconds)
                    ? null : DateTimeOffset.FromUnixTimeMilliseconds(checked((long)milliseconds));
            }
            finally
            {
                udat_close(formatter);
            }
        }
    }

    /// <summary>
    /// A date formatted with the best pattern for a skeleton, such as "MMMMd" for "October 12", in a time zone.
    /// </summary>
    public static string? FormatDate(DateTimeOffset date, string skeleton, TimeZoneInfo timeZone, CultureInfo culture)
    {
        return FormatDate(date, skeleton, pattern: null, timeZone, LocaleId(culture));
    }

    /// <summary>
    /// A date in a calendar other than the Gregorian one, such as the Hebrew calendar, given as an ICU locale with
    /// keywords such as "fr@calendar=hebrew". The pattern is used as it is when given, and the skeleton's best pattern
    /// for that calendar otherwise.
    /// </summary>
    public static string? FormatDate(DateTimeOffset date, string skeleton, string? pattern, TimeZoneInfo timeZone, string locale)
    {
        lock (Gate)
        {
            if (pattern is null && !DatePatterns.TryGetValue((skeleton, locale), out pattern))
            {
                pattern = BestPattern(skeleton, locale);
                DatePatterns[(skeleton, locale)] = pattern;
            }

            if (pattern is not { } bestPattern)
            {
                return null;
            }

            string zone = TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out string? iana) ? iana : timeZone.Id;
            IntPtr formatter = Cached(DateFormatters, (bestPattern, locale, zone), () =>
            {
                int openStatus = 0;
                IntPtr opened = udat_open(
                    DatePatternStyle, DatePatternStyle, locale, zone, zone.Length, bestPattern, bestPattern.Length, ref openStatus);
                return Opened(opened, openStatus, udat_close);
            });
            if (formatter == IntPtr.Zero)
            {
                return null;
            }

            double milliseconds = date.ToUnixTimeMilliseconds();
            return ReadString((char[] buffer, ref int error) =>
                udat_format(formatter, milliseconds, buffer, buffer.Length, IntPtr.Zero, ref error));
        }
    }

    /// <summary>
    /// The name of a locale keyword's value in a language, such as "Hebrew Calendar" for the calendar "hebrew" in
    /// English. ICU gives the value itself back when it has no name for it.
    /// </summary>
    public static string? GetDisplayKeywordValue(string locale, string keyword, string displayLocale)
    {
        if (!SupportsKeywordNames)
        {
            return null;
        }

        lock (Gate)
        {
            return ReadString((char[] buffer, ref int error) =>
                uloc_getDisplayKeywordValue(locale, keyword, displayLocale, buffer, buffer.Length, ref error));
        }
    }

    /// <summary>
    /// The ICU name of a culture, such as fr_FR.
    /// </summary>
    public static string GetLocaleId(CultureInfo culture)
    {
        return LocaleId(culture);
    }

    // Called with the lock held.
    private static string? BestPattern(string skeleton, string locale)
    {
        IntPtr generator = Cached(PatternGenerators, locale, () =>
        {
            int openStatus = 0;
            IntPtr opened = udatpg_open(locale, ref openStatus);
            return Opened(opened, openStatus, udatpg_close);
        });
        return generator == IntPtr.Zero
            ? null
            : ReadString((char[] buffer, ref int error) =>
                udatpg_getBestPattern(generator, skeleton, skeleton.Length, buffer, buffer.Length, ref error));
    }

    // Called with the lock held. A formatter that failed to open is not kept, so the next call tries again.
    private static IntPtr Cached<TKey>(Dictionary<TKey, IntPtr> cache, TKey key, Func<IntPtr> open)
        where TKey : notnull
    {
        if (cache.TryGetValue(key, out IntPtr existing))
        {
            return existing;
        }

        IntPtr opened = open();
        if (opened != IntPtr.Zero)
        {
            cache[key] = opened;
        }

        return opened;
    }

    // ICU can return a handle along with a failure status; that handle is closed and not used.
    private static IntPtr Opened(IntPtr handle, int status, Action<IntPtr> close)
    {
        if (!Failed(status) && handle != IntPtr.Zero)
        {
            return handle;
        }

        if (handle != IntPtr.Zero)
        {
            close(handle);
        }

        return IntPtr.Zero;
    }

    private static bool HasExports(params string[] names)
    {
        return Handle != IntPtr.Zero && names.All(name => NativeLibrary.TryGetExport(Handle, name, out _));
    }

    // ICU names locales with underscores, such as fr_FR.
    private static string LocaleId(CultureInfo culture)
    {
        return culture.Name.Replace('-', '_');
    }

    private static bool Failed(int status)
    {
        return status > 0;
    }

    private delegate int StringWriter(char[] buffer, ref int status);

    // Writes into a buffer, and once more into a larger one when ICU reports it was too small.
    private static string? ReadString(StringWriter write)
    {
        char[] buffer = new char[128];
        int status = 0;
        int length = write(buffer, ref status);
        if (status == BufferOverflowError)
        {
            buffer = new char[length + 1];
            status = 0;
            length = write(buffer, ref status);
        }

        return Failed(status) ? null : new string(buffer, 0, Math.Min(length, buffer.Length));
    }

    public enum RelativeUnit
    {
        Week = 3,
        Day = 4,
        Hour = 5,
        Minute = 6,
        Second = 7,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FieldPosition
    {
        public int Field;
        public int BeginIndex;
        public int EndIndex;
    }

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int uloc_addLikelySubtags(
        [MarshalAs(UnmanagedType.LPStr)] string locale, [Out] byte[] maximized, int capacity, ref int status);

    [DllImport(Library)]
    private static extern int uloc_getScript(byte[] locale, [Out] byte[] script, int capacity, ref int status);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr unumsys_open([MarshalAs(UnmanagedType.LPStr)] string locale, ref int status);

    [DllImport(Library)]
    private static extern IntPtr unumsys_getName(IntPtr system);

    [DllImport(Library)]
    private static extern void unumsys_close(IntPtr system);

    [DllImport(Library, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int uloc_getDisplayLanguage(
        [MarshalAs(UnmanagedType.LPStr)] string locale,
        [MarshalAs(UnmanagedType.LPStr)] string displayLocale,
        [Out] char[] language,
        int languageCapacity,
        ref int status);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr uplrules_open([MarshalAs(UnmanagedType.LPStr)] string locale, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int uplrules_select(IntPtr rules, double number, [Out] char[] keyword, int capacity, ref int status);

    [DllImport(Library)]
    private static extern void uplrules_close(IntPtr rules);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr unumf_openForSkeletonAndLocale(
        [MarshalAs(UnmanagedType.LPWStr)] string skeleton, int skeletonLength,
        [MarshalAs(UnmanagedType.LPStr)] string locale, ref int status);

    [DllImport(Library)]
    private static extern IntPtr unumf_openResult(ref int status);

    [DllImport(Library)]
    private static extern void unumf_formatDouble(IntPtr formatter, double value, IntPtr result, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int unumf_resultToString(IntPtr result, [Out] char[] buffer, int capacity, ref int status);

    [DllImport(Library)]
    private static extern sbyte unumf_resultNextFieldPosition(IntPtr result, ref FieldPosition position, ref int status);

    [DllImport(Library)]
    private static extern void unumf_close(IntPtr formatter);

    [DllImport(Library)]
    private static extern void unumf_closeResult(IntPtr result);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr ulistfmt_openForType(
        [MarshalAs(UnmanagedType.LPStr)] string locale, int type, int width, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int ulistfmt_format(
        IntPtr formatter,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] strings,
        int[] lengths,
        int count,
        [Out] char[] result,
        int capacity,
        ref int status);

    [DllImport(Library)]
    private static extern void ulistfmt_close(IntPtr formatter);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr ureldatefmt_open(
        [MarshalAs(UnmanagedType.LPStr)] string locale, IntPtr numberFormat, int width, int capitalization, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int ureldatefmt_format(
        IntPtr formatter, double offset, int unit, [Out] char[] result, int capacity, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int ureldatefmt_formatNumeric(
        IntPtr formatter, double offset, int unit, [Out] char[] result, int capacity, ref int status);

    [DllImport(Library)]
    private static extern void ureldatefmt_close(IntPtr formatter);

    [DllImport(Library, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr udatpg_open([MarshalAs(UnmanagedType.LPStr)] string locale, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int udatpg_getBestPattern(
        IntPtr generator,
        [MarshalAs(UnmanagedType.LPWStr)] string skeleton,
        int length,
        [Out] char[] pattern,
        int capacity,
        ref int status);

    [DllImport(Library)]
    private static extern void udatpg_close(IntPtr generator);

    [DllImport(Library, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr udat_open(
        int timeStyle,
        int dateStyle,
        [MarshalAs(UnmanagedType.LPStr)] string locale,
        [MarshalAs(UnmanagedType.LPWStr)] string timeZone,
        int timeZoneLength,
        [MarshalAs(UnmanagedType.LPWStr)] string pattern,
        int patternLength,
        ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern int udat_format(
        IntPtr formatter, double date, [Out] char[] result, int capacity, IntPtr position, ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode)]
    private static extern double udat_parse(IntPtr formatter, string text, int length, ref int position, ref int status);

    [DllImport(Library)]
    private static extern void udat_setLenient(IntPtr formatter, sbyte isLenient);

    [DllImport(Library)]
    private static extern void udat_close(IntPtr formatter);

    [DllImport(Library, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int uloc_getDisplayCountry(
        [MarshalAs(UnmanagedType.LPStr)] string locale,
        [MarshalAs(UnmanagedType.LPStr)] string displayLocale,
        [Out] char[] country,
        int countryCapacity,
        ref int status);

    [DllImport(Library, CharSet = CharSet.Unicode, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern int uloc_getDisplayKeywordValue(
        [MarshalAs(UnmanagedType.LPStr)] string locale,
        [MarshalAs(UnmanagedType.LPStr)] string keyword,
        [MarshalAs(UnmanagedType.LPStr)] string displayLocale,
        [Out] char[] dest,
        int destCapacity,
        ref int status);
}
