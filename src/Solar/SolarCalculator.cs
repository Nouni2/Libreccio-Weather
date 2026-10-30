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

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibreccioWeather.Core.Solar;

public enum SolarDayType
{
    Normal,

    // The Sun's upper limb stays above the horizon over the whole day.
    PolarDay,

    // The Sun's upper limb stays below the horizon over the whole day.
    PolarNight,
}

/// <summary>
/// The Sun on one day: its transit, and its rise, set, civil dawn and civil dusk when it crosses those altitudes. A
/// normal day can still lack a sunrise or a sunset next to polar day or close to a pole.
/// </summary>
public sealed record SolarDay(
    SolarDayType Type,
    DateTimeOffset Transit,
    DateTimeOffset? Rise,
    DateTimeOffset? Set,
    DateTimeOffset? CivilDawn,
    DateTimeOffset? CivilDusk,
    double DurationHours);

/// <summary>
/// Sun times and altitudes from spp.dll. Sunrise and sunset are when the refracted upper limb of the Sun is at 0°,
/// civil dawn and dusk when its center is at −6°. When the library cannot be loaded, as in a process that is not x64,
/// or a calculation fails, the result is null and callers keep what the weather source gave.
/// </summary>
public static class SolarCalculator
{
    private const int MinYear = -1000;
    private const int MaxYear = 3000;
    private const int MaxAltitudes = 1_000_000;

    private static readonly Lazy<bool> Available = new(Load);

    public static bool IsAvailable => Available.Value;

    public static string? LibraryVersion { get; private set; }

    /// <summary>
    /// The solar day whose transit falls on a local date. Spp gives a day for a UTC date, centered on its transit, so
    /// the day before, the day itself and the day after are tried; exactly one of them should match.
    /// </summary>
    public static SolarDay? GetSolarDay(
        double latitude, double longitude, DateOnly localDate, TimeZoneInfo timeZone, double elevation = 0)
    {
        if (!IsAvailable || !IsValidObserver(latitude, longitude, elevation))
        {
            return null;
        }

        SolarDay? match = null;
        for (int offset = -1; offset <= 1; offset++)
        {
            int dayNumber = localDate.DayNumber + offset;
            if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
            {
                continue;
            }

            DateOnly utcDate = DateOnly.FromDayNumber(dayNumber);
            if (GetSolarDayUtc(latitude, longitude, elevation, utcDate) is not { } day)
            {
                continue;
            }

            if (DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(day.Transit, timeZone).DateTime) != localDate)
            {
                continue;
            }

            if (match is not null)
            {
                Trace.TraceWarning($"Two solar days match {localDate:yyyy-MM-dd} at {latitude}, {longitude}.");
                return null;
            }

            match = day;
        }

        if (match is null)
        {
            Trace.TraceWarning($"No solar day matches {localDate:yyyy-MM-dd} at {latitude}, {longitude}.");
        }

        return match;
    }

    /// <summary>
    /// The apparent altitude of the Sun's upper limb at each instant, in degrees: positive means the Sun is up. Evenly
    /// spaced instants are computed in one call.
    /// </summary>
    public static double[]? GetUpperLimbAltitudes(
        double latitude, double longitude, IReadOnlyList<DateTimeOffset> instants, double elevation = 0)
    {
        if (!IsAvailable || !IsValidObserver(latitude, longitude, elevation) || instants.Count > MaxAltitudes)
        {
            return null;
        }

        if (instants.Count == 0)
        {
            return [];
        }

        if (instants.Any(instant => instant.Year is < MinYear or > MaxYear))
        {
            return null;
        }

        SppObserver observer = Observer(latitude, longitude, elevation);
        long start = instants[0].ToUnixTimeMilliseconds();
        long step = instants.Count > 1 ? instants[1].ToUnixTimeMilliseconds() - start : 0;
        bool evenlySpaced = step > 0;
        for (int i = 2; i < instants.Count && evenlySpaced; i++)
        {
            evenlySpaced = instants[i].ToUnixTimeMilliseconds() - instants[i - 1].ToUnixTimeMilliseconds() == step;
        }

        try
        {
            double[] altitudes = new double[instants.Count];
            if (evenlySpaced)
            {
                int status = SppNative.UpperLimbAltitudes(ref observer, start, step, instants.Count, altitudes);
                return status == SppNative.Ok ? altitudes : Fail<double[]>(status);
            }

            for (int i = 0; i < instants.Count; i++)
            {
                int status = SppNative.UpperLimbAltitude(ref observer, instants[i].ToUnixTimeMilliseconds(), out altitudes[i]);
                if (status != SppNative.Ok)
                {
                    return Fail<double[]>(status);
                }
            }

            return altitudes;
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// The apparent altitude of the Sun's upper limb at one instant, in degrees.
    /// </summary>
    public static double? GetUpperLimbAltitude(double latitude, double longitude, DateTimeOffset instant)
    {
        return GetUpperLimbAltitudes(latitude, longitude, [instant]) is [var altitude] ? altitude : null;
    }

    private static SolarDay? GetSolarDayUtc(double latitude, double longitude, double elevation, DateOnly utcDate)
    {
        if (utcDate.Year is < MinYear or > MaxYear)
        {
            return null;
        }

        SppObserver observer = Observer(latitude, longitude, elevation);
        var result = new SppSolarDay { StructSize = SppNative.SolarDaySize };
        int status;
        try
        {
            status = SppNative.SolarDayUtc(ref observer, utcDate.Year, utcDate.Month, utcDate.Day, ref result);
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            return null;
        }

        if (status != SppNative.Ok)
        {
            return Fail<SolarDay>(status);
        }

        SolarDayType type = result.DayType switch
        {
            SppNative.PolarDay => SolarDayType.PolarDay,
            SppNative.PolarNight => SolarDayType.PolarNight,
            _ => SolarDayType.Normal,
        };
        try
        {
            return new SolarDay(
                type,
                DateTimeOffset.FromUnixTimeMilliseconds(result.TransitUtcMs),
                Instant(result.HasRise, result.RiseUtcMs),
                Instant(result.HasSet, result.SetUtcMs),
                Instant(result.HasCivilDawn, result.CivilDawnUtcMs),
                Instant(result.HasCivilDusk, result.CivilDuskUtcMs),
                result.DurationHours);
        }
        catch (ArgumentOutOfRangeException)
        {
            // A native calendar date can resolve to an instant before the first date .NET can represent.
            return null;
        }
    }

    // A missing event has a time of zero, so the flag is read rather than the time.
    private static DateTimeOffset? Instant(int present, long utcMs)
    {
        return present != 0 ? DateTimeOffset.FromUnixTimeMilliseconds(utcMs) : null;
    }

    // Locations carry no elevation, so the app uses 0 m, which moves sunrise and sunset by at most about a minute.
    private static SppObserver Observer(double latitude, double longitude, double elevation)
    {
        return new SppObserver { LatDeg = latitude, LonDeg = longitude, ElevM = elevation };
    }

    private static bool IsValidObserver(double latitude, double longitude, double elevation)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180 && elevation is >= -1000 and <= 11000;
    }

    // A failed calculation is never read as polar day or night.
    private static T? Fail<T>(int status) where T : class
    {
        Trace.TraceWarning($"spp.dll returned status {status}.");
        return null;
    }

    private static bool Load()
    {
        try
        {
            if (SppNative.GetAbiVersion() != SppNative.AbiVersion || SppNative.GetSolarDaySize() != SppNative.SolarDaySize
                || Marshal.SizeOf<SppSolarDay>() != SppNative.SolarDaySize)
            {
                Trace.TraceWarning("spp.dll has an incompatible interface.");
                return false;
            }

            LibraryVersion = Marshal.PtrToStringUTF8(SppNative.GetLibraryVersion());
            Trace.TraceInformation($"Loaded spp.dll {LibraryVersion}.");
            return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            Trace.TraceWarning($"spp.dll is unavailable: {exception.Message}");
            return false;
        }
    }
}
