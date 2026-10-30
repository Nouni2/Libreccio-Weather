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

using System.Runtime.InteropServices;

namespace LibreccioWeather.Core.Solar;

/// <summary>
/// An observer on the WGS84 ellipsoid: latitude North positive, longitude East positive, elevation in meters.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SppObserver
{
    public double LatDeg;
    public double LonDeg;
    public double ElevM;
}

/// <summary>
/// One solar day as spp.dll lays it out: six 32-bit integers, five 64-bit instants and a double, 72 bytes.
/// An event's time is only meaningful when its flag is 1.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SppSolarDay
{
    public int StructSize;
    public int DayType;
    public int HasRise;
    public int HasSet;
    public int HasCivilDawn;
    public int HasCivilDusk;
    public long TransitUtcMs;
    public long RiseUtcMs;
    public long SetUtcMs;
    public long CivilDawnUtcMs;
    public long CivilDuskUtcMs;
    public double DurationHours;
}

/// <summary>
/// The C interface of spp.dll, version 1. Every call returns a status, 0 when it succeeded.
/// </summary>
internal static class SppNative
{
    private const string Library = "spp";

    public const int AbiVersion = 1;
    public const int SolarDaySize = 72;
    public const int Ok = 0;
    public const int Normal = 0;
    public const int PolarDay = 1;
    public const int PolarNight = 2;

    [DllImport(Library, EntryPoint = "spp_c_abi_version", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int GetAbiVersion();

    // The string has static storage in the library and is never freed.
    [DllImport(Library, EntryPoint = "spp_c_library_version", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern IntPtr GetLibraryVersion();

    [DllImport(Library, EntryPoint = "spp_c_solar_day_size", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int GetSolarDaySize();

    [DllImport(Library, EntryPoint = "spp_solar_day_utc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int SolarDayUtc(ref SppObserver observer, int year, int month, int day, ref SppSolarDay result);

    [DllImport(Library, EntryPoint = "spp_upper_limb_altitude", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int UpperLimbAltitude(ref SppObserver observer, long utcMs, out double altitudeDeg);

    [DllImport(Library, EntryPoint = "spp_upper_limb_altitudes", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern int UpperLimbAltitudes(
        ref SppObserver observer, long startUtcMs, long stepMs, int count, [Out] double[] altitudesDeg);
}
