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
/// Unicode CLDR formatting through the ICU library that Windows ships as icu.dll from Windows 10 version 1903, the
/// same library Android formats units, durations and relative times with. Each feature checks that the functions it
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

    private static readonly IntPtr Handle = NativeLibrary.TryLoad(Library, out IntPtr handle) ? handle : IntPtr.Zero;

    public static bool SupportsUnitNames { get; } = HasExports(
        "unumf_openForSkeletonAndLocale", "unumf_openResult", "unumf_formatDouble", "unumf_resultToString",
        "unumf_resultNextFieldPosition", "unumf_close", "unumf_closeResult");

    public static bool SupportsDurations { get; } = SupportsUnitNames
        && HasExports("ulistfmt_openForType", "ulistfmt_format", "ulistfmt_close");

    public static bool SupportsRelativeTimes { get; } = HasExports(
        "ureldatefmt_open", "ureldatefmt_format", "ureldatefmt_formatNumeric", "ureldatefmt_close",
        "udatpg_open", "udatpg_getBestPattern", "udatpg_close", "udat_open", "udat_format", "udat_close");

    /// <summary>
    /// Formats a value with a number skeleton, such as "measure-unit/duration-hour unit-width-full-name".
    /// Returns the whole text and the unit part of it, or null when ICU cannot format it.
    /// </summary>
    public static (string Text, string? Unit)? FormatNumber(string skeleton, double value, CultureInfo culture)
    {
        int status = 0;
        IntPtr formatter = unumf_openForSkeletonAndLocale(skeleton, skeleton.Length, LocaleId(culture), ref status);
        if (Failed(status) || formatter == IntPtr.Zero)
        {
            return null;
        }

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
            unumf_close(formatter);
        }
    }

    /// <summary>
    /// Joins items the way MeasureFormat joins measures, such as "1 hour, 30 minutes".
    /// </summary>
    public static string? FormatUnitList(IReadOnlyList<string> items, CultureInfo culture)
    {
        int status = 0;
        IntPtr formatter = ulistfmt_openForType(LocaleId(culture), ListTypeUnits, ListWidthWide, ref status);
        if (Failed(status) || formatter == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            string[] strings = [.. items];
            int[] lengths = [.. items.Select(item => item.Length)];
            return ReadString((char[] buffer, ref int error) =>
                ulistfmt_format(formatter, strings, lengths, strings.Length, buffer, buffer.Length, ref error));
        }
        finally
        {
            ulistfmt_close(formatter);
        }
    }

    /// <summary>
    /// A relative time in the short style, starting with a capital as at the beginning of a sentence.
    /// Numeric gives "5 min. ago"; otherwise ICU may use a word such as "yesterday". A negative offset is in the past,
    /// and -0.0 counts as past.
    /// </summary>
    public static string? FormatRelative(double offset, RelativeUnit unit, bool numeric, CultureInfo culture)
    {
        int status = 0;
        IntPtr formatter = ureldatefmt_open(
            LocaleId(culture), IntPtr.Zero, RelativeStyleShort, CapitalizationForBeginningOfSentence, ref status);
        if (Failed(status) || formatter == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return ReadString((char[] buffer, ref int error) => numeric
                ? ureldatefmt_formatNumeric(formatter, offset, (int)unit, buffer, buffer.Length, ref error)
                : ureldatefmt_format(formatter, offset, (int)unit, buffer, buffer.Length, ref error));
        }
        finally
        {
            ureldatefmt_close(formatter);
        }
    }

    /// <summary>
    /// A date formatted with the best pattern for a skeleton, such as "MMMMd" for "October 12", in a time zone.
    /// </summary>
    public static string? FormatDate(DateTimeOffset date, string skeleton, TimeZoneInfo timeZone, CultureInfo culture)
    {
        string locale = LocaleId(culture);
        int status = 0;
        IntPtr generator = udatpg_open(locale, ref status);
        if (Failed(status) || generator == IntPtr.Zero)
        {
            return null;
        }

        string? pattern;
        try
        {
            pattern = ReadString((char[] buffer, ref int error) =>
                udatpg_getBestPattern(generator, skeleton, skeleton.Length, buffer, buffer.Length, ref error));
        }
        finally
        {
            udatpg_close(generator);
        }

        if (pattern is null)
        {
            return null;
        }

        string zone = TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out string? iana) ? iana : timeZone.Id;
        IntPtr formatter = udat_open(
            DatePatternStyle, DatePatternStyle, locale, zone, zone.Length, pattern, pattern.Length, ref status);
        if (Failed(status) || formatter == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            double milliseconds = date.ToUnixTimeMilliseconds();
            return ReadString((char[] buffer, ref int error) =>
                udat_format(formatter, milliseconds, buffer, buffer.Length, IntPtr.Zero, ref error));
        }
        finally
        {
            udat_close(formatter);
        }
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

    [DllImport(Library)]
    private static extern void udat_close(IntPtr formatter);
}
