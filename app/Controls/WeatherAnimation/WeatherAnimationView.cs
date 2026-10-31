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
using System.Windows.Media;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Display;

namespace LibreccioWeather.Controls.WeatherAnimation;

/// <summary>
/// The animated weather behind the main screen. A change of weather fades the new scene in over the old one in
/// 300 ms. The scene fades out as the screen scrolls, and stops being drawn once it is gone or while the window is
/// not shown.
/// </summary>
public sealed class WeatherAnimationView : FrameworkElement
{
    public static readonly DependencyProperty ConditionProperty = DependencyProperty.Register(
        nameof(Condition), typeof(WeatherCode?), typeof(WeatherAnimationView), new PropertyMetadata(null, OnWeatherChanged));

    public static readonly DependencyProperty DaylightProperty = DependencyProperty.Register(
        nameof(Daylight), typeof(bool), typeof(WeatherAnimationView), new PropertyMetadata(true, OnWeatherChanged));

    public static readonly DependencyProperty DarkModeProperty = DependencyProperty.Register(
        nameof(DarkMode), typeof(bool), typeof(WeatherAnimationView), new PropertyMetadata(false, OnWeatherChanged));

    public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
        nameof(Animate), typeof(bool), typeof(WeatherAnimationView), new PropertyMetadata(true, OnWeatherChanged));

    private const double SwitchAnimationDuration = 300;

    // Without matching the monitor, frames come at most at WPF's usual 60 per second.
    private const double DefaultFrameInterval = 1000.0 / 60;

    private Painter? _current;
    private Painter? _previous;
    private double _switchProgress = 1;
    private double _scrollRate;
    private bool _drawable;
    private bool _looping;
    private TimeSpan _lastFrame = TimeSpan.MinValue;

    // The frames are drawn into this drawing, which the element shows; redrawing it needs no layout pass.
    private readonly DrawingGroup _drawing = new();

    public WeatherAnimationView()
    {
        IsHitTestVisible = false;

        // The clouds' circles reach far past the edges, and would otherwise be drawn over the location pane.
        ClipToBounds = true;
        Loaded += (_, _) => UpdateDrawable();
        Unloaded += (_, _) => SetDrawable(false);
        IsVisibleChanged += (_, _) => UpdateDrawable();
    }

    /// <summary>
    /// The weather to show, or null for no animation.
    /// </summary>
    public WeatherCode? Condition
    {
        get => (WeatherCode?)GetValue(ConditionProperty);
        set => SetValue(ConditionProperty, value);
    }

    public bool Daylight
    {
        get => (bool)GetValue(DaylightProperty);
        set => SetValue(DaylightProperty, value);
    }

    public bool DarkMode
    {
        get => (bool)GetValue(DarkModeProperty);
        set => SetValue(DarkModeProperty, value);
    }

    public bool Animate
    {
        get => (bool)GetValue(AnimateProperty);
        set => SetValue(AnimateProperty, value);
    }

    /// <summary>
    /// How far the screen has scrolled, from 0 at the top to 1 once the scene has faded out.
    /// </summary>
    public double ScrollRate
    {
        get => _scrollRate;
        set
        {
            double previous = _scrollRate;
            _scrollRate = Math.Clamp(value, 0, 1);
            if (previous == _scrollRate)
            {
                return;
            }

            // Coming back from fully faded out restarts the frames from a zero interval.
            if (previous >= 1)
            {
                _current?.ResetInterval();
                _previous?.ResetInterval();
            }

            // A still scene fades with the scroll too, so it is drawn again.
            RenderFrame();
            UpdateLoop();
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _current?.Resize(Sizes(), PixelsPerUnit());
        _previous?.Resize(Sizes(), PixelsPerUnit());
        RenderFrame();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawDrawing(_drawing);
    }

    // Moving to a monitor with another scale keeps the size in WPF units but changes the pixel grid.
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        _current?.Resize(Sizes(), newDpi.DpiScaleX);
        _previous?.Resize(Sizes(), newDpi.DpiScaleX);
        RenderFrame();
    }

    private void RenderFrame()
    {
        using DrawingContext drawingContext = _drawing.Open();
        int[] sizes = Sizes();
        if (sizes[0] <= 0 || sizes[1] <= 0)
        {
            return;
        }

        double eased = AccelerateDecelerate(_switchProgress);
        if (_previous is not null && _switchProgress < 1)
        {
            _previous.Draw(drawingContext, sizes, (float)_scrollRate, 1 - eased);
        }

        _current?.Draw(drawingContext, sizes, (float)_scrollRate, _previous is null ? 1 : eased);
    }

    private static void OnWeatherChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        ((WeatherAnimationView)sender).SetWeather();
    }

    // A painter keeps its scene until the weather, the time of day or the mode changes.
    private void SetWeather()
    {
        if (_current is { } current && current.Matches(Condition, Daylight, DarkMode, Animate))
        {
            return;
        }

        _previous = _current;
        _current = new Painter(Condition, Daylight, DarkMode, Animate);
        _current.Resize(Sizes(), PixelsPerUnit());
        _switchProgress = _previous is null ? 1 : 0;
        RenderFrame();
        UpdateLoop();
    }

    private void UpdateDrawable()
    {
        SetDrawable(IsLoaded && IsVisible);
    }

    private void SetDrawable(bool drawable)
    {
        if (_drawable == drawable)
        {
            return;
        }

        _drawable = drawable;
        _current?.ResetInterval();
        _previous?.ResetInterval();
        UpdateLoop();
    }

    // Frames are requested while a scene is visible, a still one included, as it is drawn on every frame too.
    private bool NeedsFrames => _drawable
        && (_switchProgress < 1 || (_scrollRate < 1 && (_current?.HasScene ?? false)));

    private void UpdateLoop()
    {
        bool needsFrames = NeedsFrames;
        if (needsFrames == _looping)
        {
            return;
        }

        _looping = needsFrames;
        if (needsFrames)
        {
            _lastFrame = TimeSpan.MinValue;
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!NeedsFrames)
        {
            UpdateLoop();
            return;
        }

        // Nothing is seen while the window is minimized; the scene resumes from a zero interval.
        if (Window.GetWindow(this)?.WindowState == WindowState.Minimized)
        {
            _lastFrame = TimeSpan.MinValue;
            _current?.ResetInterval();
            _previous?.ResetInterval();
            return;
        }

        if (e is RenderingEventArgs rendering)
        {
            if (rendering.RenderingTime == _lastFrame)
            {
                return;
            }

            double elapsed = _lastFrame == TimeSpan.MinValue ? 0 : (rendering.RenderingTime - _lastFrame).TotalMilliseconds;
            if (!RefreshRate.MatchesMonitor && _lastFrame != TimeSpan.MinValue && elapsed < DefaultFrameInterval - 1)
            {
                return;
            }

            _lastFrame = rendering.RenderingTime;
            if (_switchProgress < 1)
            {
                _switchProgress = Math.Min(1, _switchProgress + (elapsed / SwitchAnimationDuration));
                if (_switchProgress >= 1)
                {
                    _previous = null;
                }
            }
        }

        RenderFrame();
    }

    private double PixelsPerUnit()
    {
        return VisualTreeHelper.GetDpi(this).DpiScaleX;
    }

    private int[] Sizes()
    {
        return [(int)ActualWidth, (int)ActualHeight];
    }

    // Slow at both ends, as cos((t + 1)π) / 2 + 0.5.
    private static double AccelerateDecelerate(double progress)
    {
        return (Math.Cos((progress + 1) * Math.PI) / 2.0) + 0.5;
    }

    /// <summary>
    /// One scene: its animation, the time between its frames, and whether its single still frame is drawn.
    /// </summary>
    private sealed class Painter(WeatherCode? weatherCode, bool daylight, bool darkMode, bool animate)
    {
        private WeatherAnimationImplementor? _implementor;
        private int[] _sizes = [0, 0];
        private double _pixelsPerUnit;
        private long _lastTime = -1;
        private bool _hasDrawn;

        public bool HasScene => _implementor is not null;

        public bool Matches(WeatherCode? otherCode, bool otherDaylight, bool otherDarkMode, bool otherAnimate)
        {
            return weatherCode == otherCode && daylight == otherDaylight && darkMode == otherDarkMode && animate == otherAnimate;
        }

        public void Resize(int[] sizes, double pixelsPerUnit)
        {
            if (sizes[0] == _sizes[0] && sizes[1] == _sizes[1] && pixelsPerUnit == _pixelsPerUnit)
            {
                return;
            }

            _sizes = sizes;
            _pixelsPerUnit = pixelsPerUnit;
            _hasDrawn = false;
            _implementor = sizes[0] > 0 && sizes[1] > 0
                ? WeatherImplementorFactory.GetWeatherImplementor(weatherCode, daylight, sizes, animate, darkMode, pixelsPerUnit)
                : null;
        }

        public void ResetInterval()
        {
            _lastTime = -1;
        }

        public void Draw(DrawingContext drawingContext, int[] sizes, float scrollRate, double opacity)
        {
            if (_implementor is null)
            {
                return;
            }

            // Milliseconds from a high-resolution clock: the system tick only advances every 15.6 ms, which would make
            // motion uneven. Whole-millisecond timestamps, not intervals, keep the fractions from being lost.
            long now = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
            long interval = _lastTime == -1 ? 0 : now - _lastTime;
            _lastTime = now;
            if (!animate)
            {
                if (_hasDrawn)
                {
                    interval = 0;
                }
                else
                {
                    _hasDrawn = true;
                }
            }

            _implementor.UpdateData(sizes, interval, 0f, 0f);
            drawingContext.PushOpacity(opacity);
            AnimationCanvas canvas = new(drawingContext, sizes[0], sizes[1]);
            _implementor.Draw(sizes, canvas, scrollRate, 0f, 0f);
            canvas.Finish();
            drawingContext.Pop();
        }
    }
}
