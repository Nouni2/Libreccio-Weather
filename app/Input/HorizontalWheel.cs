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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LibreccioWeather.ViewModels;

namespace LibreccioWeather.Input;

/// <summary>
/// Horizontal wheel input, which a touchpad sends for a two-finger swipe sideways. Over content that scrolls sideways,
/// such as the forecast rows, it scrolls that content. Elsewhere one swipe moves through the screens like the back and
/// forward buttons of a mouse: fingers moving right go back, fingers moving left go forward.
/// </summary>
public sealed class HorizontalWheel
{
    // WPF raises no event for horizontal wheel input, so the window's messages are read directly.
    private const int WmMouseHWheel = 0x020E;

    // A swipe goes back or forward once its sideways movement adds up to two wheel notches, so a slight sideways drift
    // while scrolling does not change the screen.
    private const double NavigationDistance = 2 * ProportionalWheelScroll.NotchDelta;

    // A swipe ends once no sideways input has come for 300 ms. The input a touchpad keeps sending after the fingers
    // lift arrives without such a pause, so it belongs to the same swipe and cannot move a second screen.
    private const long SwipeEndMilliseconds = 300;

    private readonly Window _window;
    private long _lastInputAt = long.MinValue;
    private double _swipe;
    private bool _navigated;

    public HorizontalWheel(Window window)
    {
        _window = window;
        window.SourceInitialized += (_, _) =>
            HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(OnMessage);
    }

    private IntPtr OnMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmMouseHWheel)
        {
            // The delta is the high word of wParam; a positive delta scrolls the content towards its end.
            short delta = unchecked((short)((wParam.ToInt64() >> 16) & 0xFFFF));
            handled = OnHorizontalWheel(delta);
        }

        return IntPtr.Zero;
    }

    private bool OnHorizontalWheel(double delta)
    {
        long now = Environment.TickCount64;
        if (_lastInputAt == long.MinValue || now - _lastInputAt >= SwipeEndMilliseconds)
        {
            _swipe = 0;
            _navigated = false;
        }

        _lastInputAt = now;
        if (FindHorizontalViewer(Mouse.DirectlyOver as DependencyObject) is { } viewer)
        {
            ProportionalWheelScroll.ScrollBy(viewer, ProportionalWheelScroll.GetDistance(delta, viewer.ViewportWidth), horizontal: true);
            return true;
        }

        if (_navigated || _window.DataContext is not MainViewModel main)
        {
            return true;
        }

        _swipe += delta;
        if (Math.Abs(_swipe) >= NavigationDistance)
        {
            _navigated = true;
            if (_swipe < 0)
            {
                main.BrowseBack();
            }
            else
            {
                main.BrowseForward();
            }
        }

        return true;
    }

    // The nearest scroll viewer under the pointer that can scroll sideways, by pixels.
    private static ScrollViewer? FindHorizontalViewer(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ScrollViewer { ScrollableWidth: > 0, CanContentScroll: false } viewer
                && viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
            {
                return viewer;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return null;
    }
}
