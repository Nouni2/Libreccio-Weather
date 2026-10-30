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
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace LibreccioWeather.Display;

/// <summary>
/// The refresh rate of the monitor a window is on. WPF runs animations at 60 frames per second unless told
/// otherwise, which looks choppy on faster monitors.
/// </summary>
public static class RefreshRate
{
    private const uint MonitorDefaultToNearest = 2;
    private const int EnumCurrentSettings = -1;

    /// <summary>
    /// Whether animations follow the monitor's refresh rate. When off, they keep WPF's 60 frames per second.
    /// </summary>
    public static bool MatchesMonitor { get; set; } = true;

    /// <summary>
    /// The refresh rate, in hertz, of the monitor showing most of the element's window, or null when Windows does not
    /// report one.
    /// </summary>
    public static int? Of(DependencyObject element)
    {
        if (PresentationSource.FromDependencyObject(element) is not HwndSource source)
        {
            return null;
        }

        IntPtr monitor = MonitorFromWindow(source.Handle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            return null;
        }

        var mode = new DevMode { Size = (ushort)Marshal.SizeOf<DevMode>() };
        if (!EnumDisplaySettings(monitorInfo.Device, EnumCurrentSettings, ref mode))
        {
            return null;
        }

        // Windows reports 0 or 1 when the display uses its hardware's default rate, which it does not name.
        return mode.DisplayFrequency > 1 ? (int)mode.DisplayFrequency : null;
    }

    /// <summary>
    /// Runs the animation at the refresh rate of the monitor showing the element, when that setting is on and Windows
    /// reports one.
    /// </summary>
    public static void Apply(Timeline animation, DependencyObject element)
    {
        if (MatchesMonitor && Of(element) is { } rate)
        {
            Timeline.SetDesiredFrameRate(animation, rate);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(string deviceName, int modeNumber, ref DevMode mode);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Device;
    }

    // The display form of Windows' DEVMODEW, field for field, so Windows can fill it in.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;
        public uint Fields;
        public int PositionX;
        public int PositionY;
        public uint DisplayOrientation;
        public uint DisplayFixedOutput;
        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FormName;

        public ushort LogPixels;
        public uint BitsPerPel;
        public uint PelsWidth;
        public uint PelsHeight;
        public uint DisplayFlags;
        public uint DisplayFrequency;
        public uint IcmMethod;
        public uint IcmIntent;
        public uint MediaType;
        public uint DitherType;
        public uint Reserved1;
        public uint Reserved2;
        public uint PanningWidth;
        public uint PanningHeight;
    }
}
