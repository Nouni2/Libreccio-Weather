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
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls;

/// <summary>
/// The stop indicators drawn over a <see cref="MaterialSlider"/>'s track: one at the end of the inactive track and one
/// at the slider's stop value. A stop under the handle or its gaps is not drawn, as the handle covers it.
/// </summary>
public sealed class SliderStops : FrameworkElement
{
    // The handle is 4 pixels wide with a 6-pixel gap on each side, as the slider template draws it.
    private const double HandleSpan = 16;

    private const double StopSize = 4;

    // Space between the end stop and the end of the track.
    private const double TrailingSpace = 4;

    public static readonly DependencyProperty MinimumProperty = Register(nameof(Minimum), 0);
    public static readonly DependencyProperty MaximumProperty = Register(nameof(Maximum), 1);
    public static readonly DependencyProperty ValueProperty = Register(nameof(Value), 0);
    public static readonly DependencyProperty StopValueProperty = Register(nameof(StopValue), double.NaN);

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double StopValue
    {
        get => (double)GetValue(StopValueProperty);
        set => SetValue(StopValueProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        double range = Maximum - Minimum;
        if (range <= 0 || ActualWidth <= HandleSpan)
        {
            return;
        }

        double handle = Position(Value, range);
        DrawStop(drawingContext, ActualWidth - TrailingSpace - StopSize / 2, handle, active: false);
        if (!double.IsNaN(StopValue) && StopValue >= Minimum && StopValue <= Maximum)
        {
            DrawStop(drawingContext, Position(StopValue, range), handle, active: StopValue < Value);
        }
    }

    private static DependencyProperty Register(string name, double defaultValue)
    {
        return DependencyProperty.Register(name, typeof(double), typeof(SliderStops),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    // The track places the handle's centre between half a handle span from each end.
    private double Position(double value, double range)
    {
        double fraction = (Math.Clamp(value, Minimum, Maximum) - Minimum) / range;
        return HandleSpan / 2 + fraction * (ActualWidth - HandleSpan);
    }

    private void DrawStop(DrawingContext drawingContext, double x, double handle, bool active)
    {
        if (Math.Abs(x - handle) < HandleSpan / 2 + StopSize / 2)
        {
            return;
        }

        if (TryFindResource(active ? "OnPrimaryBrush" : "OnSecondaryContainerBrush") is Brush brush)
        {
            drawingContext.DrawEllipse(brush, null, new Point(x, ActualHeight / 2), StopSize / 2, StopSize / 2);
        }
    }
}
