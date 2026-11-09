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

using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LibreccioWeather.Display;

namespace LibreccioWeather.Input;

/// <summary>
/// Scrolls by as much as the wheel input asks for, gliding there. A scroll viewer otherwise jumps a whole wheel step for
/// every wheel event, whatever its size, and a touchpad sends many small events for one movement of the fingers, so the
/// page ran far ahead of them.
/// </summary>
public static class ProportionalWheelScroll
{
    // One wheel notch, in the units of the wheel's delta.
    public const double NotchDelta = 120;

    // A scrolled line, as the scroll viewer's own wheel steps count it.
    private const double LineHeight = 16;

    // A glide covers this share of the remaining distance per second as a time constant: about 95 % of a wheel notch
    // within 150 ms. A touchpad sends an event about every 10 ms, each of a different size; the events only move the
    // destination, and the page follows it frame by frame, so its speed changes smoothly however often they come.
    private const double TimeConstantSeconds = 0.05;

    // Closer than this to its destination, a glide ends there.
    private const double SettleDistance = 0.5;

    // The longest step between two frames a glide accounts for, so a frame held up by other work does not jump.
    private const double MaxFrameSeconds = 0.1;

    // An animation that changes nothing visible; it only asks WPF for frames at the monitor's rate while a glide runs.
    private static readonly DependencyProperty PaceProperty = DependencyProperty.RegisterAttached(
        "Pace", typeof(double), typeof(ProportionalWheelScroll), new PropertyMetadata(0.0));

    private static readonly ConditionalWeakTable<ScrollViewer, Glide> VerticalGlides = new();
    private static readonly ConditionalWeakTable<ScrollViewer, Glide> HorizontalGlides = new();
    private static readonly List<Glide> Gliding = [];
    private static TimeSpan _lastFrame;
    private static bool _registered;

    /// <summary>
    /// The touchpad's scrolling distance as a share of the wheel's: 1 follows Windows' wheel setting.
    /// </summary>
    public static double TouchpadSpeed { get; set; } = 1;

    /// <summary>
    /// Makes every scroll viewer of the app scroll in proportion to the wheel input. Runs once, before any window opens.
    /// </summary>
    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheel));

        // A click, such as on the scroll bar, or a key takes over from a glide on its way.
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler((sender, _) => StopGlides((ScrollViewer)sender)), true);
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewKeyDownEvent,
            new KeyEventHandler((sender, _) => StopGlides((ScrollViewer)sender)), true);
    }

    /// <summary>
    /// The distance the content moves for a wheel delta: a notch moves the lines Windows' wheel setting asks for, or a
    /// screen when it asks for a screen at a time. A touchpad sends deltas finer than a notch, and Windows has no
    /// scrolling speed of its own for it, so its deltas move that distance times the chosen touchpad speed.
    /// </summary>
    public static double GetDistance(double delta, double viewport)
    {
        int lines = SystemParameters.WheelScrollLines;
        double notch = lines < 0 ? viewport : lines * LineHeight;
        double distance = delta / NotchDelta * notch;
        return delta % NotchDelta != 0 ? distance * TouchpadSpeed : distance;
    }

    // Viewers scrolling by items, such as lists, keep their own steps, as their offsets count items rather than pixels.
    // At the start or end of the content, the viewer's own handling runs, as it did before.
    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled || sender is not ScrollViewer viewer || viewer.CanContentScroll || viewer.ScrollableHeight <= 0
            || viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled)
        {
            return;
        }

        e.Handled = ScrollBy(viewer, -GetDistance(e.Delta, viewer.ViewportHeight), horizontal: false);
    }

    /// <summary>
    /// Moves the content by a distance, held within its extent. Returns whether the viewer is scrolling for this input,
    /// which is false at the start or end of the content when no glide is on its way.
    /// </summary>
    public static bool ScrollBy(ScrollViewer viewer, double distance, bool horizontal)
    {
        // While a glide runs, a new event adds to where it is heading, so no event is lost.
        Glide glide = (horizontal ? HorizontalGlides : VerticalGlides).GetOrCreateValue(viewer);
        double offset = horizontal ? viewer.HorizontalOffset : viewer.VerticalOffset;
        double start = glide.IsActive ? glide.Target : offset;
        double target = Math.Clamp(start + distance, 0, horizontal ? viewer.ScrollableWidth : viewer.ScrollableHeight);
        if (target == start)
        {
            return glide.IsActive;
        }

        glide.Target = target;
        if (!glide.IsActive)
        {
            glide.Viewer = viewer;
            glide.IsHorizontal = horizontal;
            glide.Position = offset;
            glide.IsActive = true;
            StartPace(viewer);
            Gliding.Add(glide);
            if (Gliding.Count == 1)
            {
                _lastFrame = TimeSpan.MinValue;
                CompositionTarget.Rendering += OnRendering;
            }
        }

        return true;
    }

    // Each frame moves every glide a share of its remaining distance that depends on the time since the last frame, or
    // all of it with Windows' animation effects off.
    private static void OnRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs rendering || rendering.RenderingTime == _lastFrame)
        {
            return;
        }

        double seconds = _lastFrame == TimeSpan.MinValue
            ? 1.0 / 60 : Math.Min(MaxFrameSeconds, (rendering.RenderingTime - _lastFrame).TotalSeconds);
        _lastFrame = rendering.RenderingTime;
        double share = AnimationEffects.AreEnabled ? 1 - Math.Exp(-seconds / TimeConstantSeconds) : 1;
        foreach (Glide glide in Gliding.ToArray())
        {
            glide.Position += (glide.Target - glide.Position) * share;
            if (Math.Abs(glide.Target - glide.Position) < SettleDistance)
            {
                glide.Position = glide.Target;
                End(glide);
            }

            if (glide.Viewer is { } viewer)
            {
                if (glide.IsHorizontal)
                {
                    viewer.ScrollToHorizontalOffset(glide.Position);
                }
                else
                {
                    viewer.ScrollToVerticalOffset(glide.Position);
                }
            }
        }
    }

    private static void StopGlides(ScrollViewer viewer)
    {
        if (VerticalGlides.TryGetValue(viewer, out Glide? vertical) && vertical.IsActive)
        {
            End(vertical);
        }

        if (HorizontalGlides.TryGetValue(viewer, out Glide? horizontal) && horizontal.IsActive)
        {
            End(horizontal);
        }
    }

    private static void End(Glide glide)
    {
        glide.IsActive = false;
        Gliding.Remove(glide);
        if (glide.Viewer is { } viewer && !Gliding.Exists(other => ReferenceEquals(other.Viewer, viewer)))
        {
            viewer.BeginAnimation(PaceProperty, null);
        }

        if (Gliding.Count == 0)
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private static void StartPace(ScrollViewer viewer)
    {
        if (Gliding.Exists(other => ReferenceEquals(other.Viewer, viewer)))
        {
            return;
        }

        var pace = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever };
        RefreshRate.Apply(pace, viewer);
        viewer.BeginAnimation(PaceProperty, pace);
    }

    // A viewer's glide along one axis: where it is heading and where the content has got to.
    private sealed class Glide
    {
        public ScrollViewer? Viewer { get; set; }

        public bool IsHorizontal { get; set; }

        public bool IsActive { get; set; }

        public double Target { get; set; }

        public double Position { get; set; }
    }
}
