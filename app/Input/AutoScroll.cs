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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace LibreccioWeather.Input;

/// <summary>
/// Middle-button autoscroll, as in web browsers. A middle click sets an anchor, and the content then scrolls towards the
/// pointer, faster the further the pointer is from the anchor. A press without movement keeps scrolling until the next
/// click; a press held while moving stops on release.
/// </summary>
public sealed class AutoScroll
{
    // Distance from the anchor, in device-independent pixels, within which nothing scrolls.
    private const double DeadZone = 12;

    // Scrolling speed, in pixels per second, for each pixel beyond the dead zone.
    private const double SpeedPerPixel = 8;

    private const double AnchorSize = 28;

    private readonly Window _window;
    private readonly Popup _anchor;
    private readonly Stopwatch _clock = new();
    private ScrollViewer? _horizontal;
    private ScrollViewer? _vertical;
    private Point _origin;
    private TimeSpan _lastFrame;
    private bool _active;
    private bool _moved;

    public AutoScroll(Window window)
    {
        _window = window;
        _anchor = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Relative,
            IsHitTestVisible = false,
            Focusable = false,
        };
        _window.PreviewMouseDown += OnPreviewMouseDown;
        _window.PreviewMouseUp += OnPreviewMouseUp;
        _window.PreviewMouseWheel += OnPreviewMouseWheel;
        _window.PreviewKeyDown += OnPreviewKeyDown;
        _window.LostMouseCapture += OnLostMouseCapture;
        _window.Deactivated += (_, _) => Stop();
    }

    private UIElement? Root => _window.Content as UIElement;

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Any click ends a running autoscroll, and does nothing else.
        if (_active)
        {
            Stop();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Middle || Root is not { } root)
        {
            return;
        }

        var source = e.OriginalSource as DependencyObject;
        _horizontal = FindScrollable(source, horizontal: true);
        _vertical = FindScrollable(source, horizontal: false);
        if (_horizontal is null && _vertical is null)
        {
            return;
        }

        Start(e.GetPosition(root));
        e.Handled = true;
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_active && e.ChangedButton == MouseButton.Middle)
        {
            e.Handled = true;
            if (_moved)
            {
                Stop();
            }
        }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_active)
        {
            Stop();
        }
    }

    // The event also bubbles up from elements inside the window, which must not end the autoscroll.
    private void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, _window))
        {
            Stop();
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_active && e.Key == Key.Escape)
        {
            Stop();
            e.Handled = true;
        }
    }

    // Each axis scrolls the nearest enclosing viewer that can move along it, so a forecast scrolls sideways while the
    // page around it scrolls down.
    private static ScrollViewer? FindScrollable(DependencyObject? element, bool horizontal)
    {
        while (element is not null)
        {
            if (element is ScrollViewer viewer
                && (horizontal
                    ? viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && viewer.ScrollableWidth > 0
                    : viewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && viewer.ScrollableHeight > 0))
            {
                return viewer;
            }

            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return null;
    }

    private void Start(Point origin)
    {
        _active = true;
        _moved = false;
        _origin = origin;
        _anchor.PlacementTarget = Root;
        _anchor.HorizontalOffset = origin.X - AnchorSize / 2;
        _anchor.VerticalOffset = origin.Y - AnchorSize / 2;
        _anchor.Child = CreateAnchor(_horizontal is not null, _vertical is not null);
        _anchor.IsOpen = true;
        _window.CaptureMouse();
        Mouse.OverrideCursor = CursorFor(default);
        _clock.Restart();
        _lastFrame = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void Stop()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        CompositionTarget.Rendering -= OnRendering;
        _clock.Stop();
        _anchor.IsOpen = false;
        Mouse.OverrideCursor = null;
        _horizontal = null;
        _vertical = null;
        _window.ReleaseMouseCapture();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (Root is not { } root)
        {
            Stop();
            return;
        }

        TimeSpan now = _clock.Elapsed;
        double seconds = (now - _lastFrame).TotalSeconds;
        _lastFrame = now;
        Vector offset = Mouse.GetPosition(root) - _origin;
        if (Math.Abs(offset.X) > DeadZone || Math.Abs(offset.Y) > DeadZone)
        {
            _moved = true;
        }

        double dx = Speed(offset.X);
        if (_horizontal is { } horizontal && dx != 0)
        {
            horizontal.ScrollToHorizontalOffset(horizontal.HorizontalOffset + dx * seconds);
        }

        double dy = Speed(offset.Y);
        if (_vertical is { } vertical && dy != 0)
        {
            vertical.ScrollToVerticalOffset(vertical.VerticalOffset + dy * seconds);
        }

        Mouse.OverrideCursor = CursorFor(offset);
    }

    private static double Speed(double distance)
    {
        double beyond = Math.Abs(distance) - DeadZone;
        return beyond <= 0 ? 0 : Math.Sign(distance) * beyond * SpeedPerPixel;
    }

    // Windows' own autoscroll cursors, pointing where the content moves.
    private Cursor CursorFor(Vector offset)
    {
        int x = _horizontal is null ? 0 : Math.Sign(Speed(offset.X));
        int y = _vertical is null ? 0 : Math.Sign(Speed(offset.Y));
        return (x, y) switch
        {
            (0, -1) => Cursors.ScrollN,
            (0, 1) => Cursors.ScrollS,
            (-1, 0) => Cursors.ScrollW,
            (1, 0) => Cursors.ScrollE,
            (-1, -1) => Cursors.ScrollNW,
            (1, -1) => Cursors.ScrollNE,
            (-1, 1) => Cursors.ScrollSW,
            (1, 1) => Cursors.ScrollSE,
            _ when _horizontal is null => Cursors.ScrollNS,
            _ when _vertical is null => Cursors.ScrollWE,
            _ => Cursors.ScrollAll,
        };
    }

    // A small disc at the anchor, with an arrow for each direction that can scroll.
    private static Grid CreateAnchor(bool horizontal, bool vertical)
    {
        var fill = (Brush)Application.Current.FindResource("SurfaceContainerHighBrush");
        var outline = (Brush)Application.Current.FindResource("OutlineBrush");
        var arrows = (Brush)Application.Current.FindResource("OnSurfaceVariantBrush");
        var anchor = new Grid { Width = AnchorSize, Height = AnchorSize };
        anchor.Children.Add(new Ellipse { Fill = fill, Stroke = outline, StrokeThickness = 1 });
        anchor.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = arrows });
        if (vertical)
        {
            anchor.Children.Add(Arrow("M0,4 L4,0 L8,4 Z", arrows, HorizontalAlignment.Center, VerticalAlignment.Top));
            anchor.Children.Add(Arrow("M0,0 L8,0 L4,4 Z", arrows, HorizontalAlignment.Center, VerticalAlignment.Bottom));
        }

        if (horizontal)
        {
            anchor.Children.Add(Arrow("M4,0 L0,4 L4,8 Z", arrows, HorizontalAlignment.Left, VerticalAlignment.Center));
            anchor.Children.Add(Arrow("M0,0 L4,4 L0,8 Z", arrows, HorizontalAlignment.Right, VerticalAlignment.Center));
        }

        return anchor;
    }

    private static Path Arrow(string data, Brush fill, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        return new Path
        {
            Data = Geometry.Parse(data),
            Fill = fill,
            Margin = new Thickness(5),
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
        };
    }
}
