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
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Point = System.Windows.Point;

namespace LibreccioWeather.Windowing;

/// <summary>
/// The window's own title bar, drawn by the app in place of the one Windows draws. <see cref="WindowChrome"/> removes
/// the system frame and keeps its resize border; this class tells Windows which parts of the drawn bar are the caption,
/// the window icon and the maximize button, so that dragging, the system menu and Windows 11's snap layouts behave as
/// on a system title bar.
/// </summary>
public sealed class WindowCaption
{
    public static readonly DependencyProperty IsHoveredProperty = DependencyProperty.RegisterAttached(
        "IsHovered", typeof(bool), typeof(WindowCaption), new PropertyMetadata(false));

    public static readonly DependencyProperty IsPressedProperty = DependencyProperty.RegisterAttached(
        "IsPressed", typeof(bool), typeof(WindowCaption), new PropertyMetadata(false));

    private const int WmNcHitTest = 0x0084;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcLButtonDblClk = 0x00A3;
    private const int WmNcMouseLeave = 0x02A2;

    private const int HtCaption = 2;
    private const int HtSysMenu = 3;
    private const int HtMaxButton = 9;

    private const uint TmeLeave = 0x00000002;
    private const uint TmeNonClient = 0x00000010;

    private const uint MonitorDefaultToNearest = 0x00000002;

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    private readonly Window _window;
    private readonly FrameworkElement _root;
    private readonly FrameworkElement _titleBar;
    private readonly FrameworkElement _icon;
    private readonly ButtonBase _maximizeButton;
    private IntPtr _handle;

    public WindowCaption(Window window, FrameworkElement root, FrameworkElement titleBar, FrameworkElement icon,
        ButtonBase maximizeButton)
    {
        _window = window;
        _root = root;
        _titleBar = titleBar;
        _icon = icon;
        _maximizeButton = maximizeButton;
    }

    public static bool GetIsHovered(DependencyObject element) => (bool)element.GetValue(IsHoveredProperty);

    public static void SetIsHovered(DependencyObject element, bool value) => element.SetValue(IsHoveredProperty, value);

    public static bool GetIsPressed(DependencyObject element) => (bool)element.GetValue(IsPressedProperty);

    public static void SetIsPressed(DependencyObject element, bool value) => element.SetValue(IsPressedProperty, value);

    /// <summary>
    /// Starts once the window has its handle. Called after <see cref="WindowChrome"/> has added its own message hook, so
    /// that this hook, added later, sees each message first.
    /// </summary>
    public void Attach()
    {
        _handle = new WindowInteropHelper(_window).Handle;
        HwndSource.FromHwnd(_handle)?.AddHook(WndProc);

        // Windows 11 does not round a window whose non-client area is removed unless asked; Windows 10 rejects the
        // attribute, which is ignored there.
        int preference = DwmwcpRound;
        _ = DwmSetWindowAttribute(_handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));

        _window.StateChanged += (_, _) => UpdateMaximizedInset();
        _window.SizeChanged += (_, _) => UpdateMaximizedInset();
        _window.DpiChanged += (_, _) => UpdateMaximizedInset();
        UpdateMaximizedInset();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WmNcHitTest:
                if (HitTest(lParam) is { } hit)
                {
                    handled = true;
                    return hit;
                }

                break;

            case WmNcMouseMove:
                bool overMaximize = (int)wParam == HtMaxButton;
                SetIsHovered(_maximizeButton, overMaximize);
                if (overMaximize)
                {
                    // Windows only reports the pointer leaving the non-client area to a window that asks for it.
                    var track = new TrackMouseEventOptions
                    {
                        Size = (uint)Marshal.SizeOf<TrackMouseEventOptions>(),
                        Flags = TmeLeave | TmeNonClient,
                        Window = hwnd,
                    };
                    _ = TrackMouseEvent(ref track);
                }
                else
                {
                    SetIsPressed(_maximizeButton, false);
                }

                break;

            case WmNcMouseLeave:
                SetIsHovered(_maximizeButton, false);
                SetIsPressed(_maximizeButton, false);
                break;

            // The default handling of a press on the maximize button draws the classic system button over the bar and
            // waits for the release itself, so the press and release are handled here.
            case WmNcLButtonDown or WmNcLButtonDblClk when (int)wParam == HtMaxButton:
                SetIsPressed(_maximizeButton, true);
                handled = true;
                return IntPtr.Zero;

            case WmNcLButtonUp when (int)wParam == HtMaxButton:
                bool wasPressed = GetIsPressed(_maximizeButton);
                SetIsPressed(_maximizeButton, false);
                if (wasPressed)
                {
                    ToggleMaximized();
                }

                handled = true;
                return IntPtr.Zero;

            case WmNcLButtonUp:
                SetIsPressed(_maximizeButton, false);
                break;
        }

        return IntPtr.Zero;
    }

    public void ToggleMaximized()
    {
        if (_window.WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(_window);
        }
        else
        {
            SystemCommands.MaximizeWindow(_window);
        }
    }

    // Returns the area under the pointer for the parts of the bar Windows must know about, or null to leave the answer to
    // WindowChrome: the resize border, and the client area for everything else, buttons included.
    private IntPtr? HitTest(IntPtr lParam)
    {
        // The coordinates are signed, as screens left of or above the main one have negative coordinates.
        long value = lParam.ToInt64();
        var screen = new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));

        // Windows 11 shows the snap layouts over the maximize button only when it is reported as such.
        if (Contains(_maximizeButton, screen))
        {
            return HtMaxButton;
        }

        // On the window icon, a click opens the system menu and a double click closes the window.
        if (Contains(_icon, screen))
        {
            return HtSysMenu;
        }

        if (!Contains(_titleBar, screen) || IsOnResizeBorder(screen))
        {
            return null;
        }

        // Empty space and text in the bar move the window; its buttons stay buttons.
        Point inRoot = _root.PointFromScreen(screen);
        if (_root.InputHitTest(inRoot) is DependencyObject element && IsInButton(element))
        {
            return null;
        }

        return HtCaption;
    }

    private static bool Contains(FrameworkElement element, Point screen)
    {
        if (!element.IsVisible)
        {
            return false;
        }

        Point point = element.PointFromScreen(screen);
        return new Rect(element.RenderSize).Contains(point);
    }

    private bool IsOnResizeBorder(Point screen)
    {
        if (_window.WindowState == WindowState.Maximized)
        {
            return false;
        }

        Thickness border = WindowChrome.GetWindowChrome(_window)?.ResizeBorderThickness ?? default;
        Point point = _window.PointFromScreen(screen);
        return point.X < border.Left
            || point.Y < border.Top
            || point.X >= _window.ActualWidth - border.Right
            || point.Y >= _window.ActualHeight - border.Bottom;
    }

    private bool IsInButton(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null && current != _root; current = Parent(current))
        {
            if (current is ButtonBase)
            {
                return true;
            }
        }

        return false;
    }

    private static DependencyObject? Parent(DependencyObject element) =>
        element is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(element)
            : LogicalTreeHelper.GetParent(element);

    // A maximized window keeps its frame size beyond the screen's edges, which the removed frame no longer covers, so
    // the content is moved back inside the screen's work area.
    private void UpdateMaximizedInset()
    {
        if (_handle == IntPtr.Zero || _window.WindowState != WindowState.Maximized)
        {
            _root.Margin = default;
            return;
        }

        var monitorInfo = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetWindowRect(_handle, out NativeRect window)
            || !GetMonitorInfo(MonitorFromWindow(_handle, MonitorDefaultToNearest), ref monitorInfo))
        {
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(_window);
        NativeRect work = monitorInfo.Work;
        _root.Margin = new Thickness(
            Math.Max(0, work.Left - window.Left) / dpi.DpiScaleX,
            Math.Max(0, work.Top - window.Top) / dpi.DpiScaleY,
            Math.Max(0, window.Right - work.Right) / dpi.DpiScaleX,
            Math.Max(0, window.Bottom - work.Bottom) / dpi.DpiScaleY);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrackMouseEventOptions
    {
        public uint Size;
        public uint Flags;
        public IntPtr Window;
        public uint HoverTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackMouseEvent(ref TrackMouseEventOptions eventTrack);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
