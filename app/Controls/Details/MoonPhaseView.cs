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
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.Details;

/// <summary>
/// The Moon's lit and dark parts for a phase angle: 0° new, 90° first quarter, 180° full, 270° last quarter. The lit
/// part grows from the right, and a half ellipse marks the terminator.
/// </summary>
public sealed class MoonPhaseView : FrameworkElement
{
    public static readonly DependencyProperty SurfaceAngleProperty = DependencyProperty.Register(
        nameof(SurfaceAngle), typeof(double), typeof(MoonPhaseView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double Padding = 4;

    // The lit and unlit surfaces use the secondary text colors, independently of the outline.
    private static readonly Brush LightBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));
    private static readonly Brush DarkBrush = Freeze(new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));

    public double SurfaceAngle
    {
        get => (double)GetValue(SurfaceAngleProperty);
        set => SetValue(SurfaceAngleProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(24, 24);

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        drawingContext.PushTransform(new ScaleTransform(1 / dpi.DpiScaleX, 1 / dpi.DpiScaleY));
        Draw(drawingContext, new Rect(0, 0, Math.Floor(ActualWidth * dpi.DpiScaleX + 0.5), Math.Floor(ActualHeight * dpi.DpiScaleY + 0.5)),
            SurfaceAngle, (Brush)FindResource("BodyTextBrush"), (float)dpi.DpiScaleX);
        drawingContext.Pop();
    }

    /// <summary>
    /// Draws the phase inside physical-pixel bounds, with density-scaled padding and outline.
    /// </summary>
    public static void Draw(DrawingContext drawingContext, Rect bounds, double surfaceAngle, Brush strokeBrush, float density)
    {
        int padding = (int)((float)Padding * density);
        double lineWidth = density;
        Rect background = new(bounds.Left + padding, bounds.Top + padding,
            Math.Max(0, bounds.Width - 2 * padding), Math.Max(0, bounds.Height - 2 * padding));
        double angle = (float)surfaceAngle >= 360 ? (float)surfaceAngle % 360 : (float)surfaceAngle;

        if (angle == 0)
        {
            DrawCircle(drawingContext, background, DarkBrush);
        }
        else if (angle < 90)
        {
            DrawCircle(drawingContext, background, LightBrush);
            drawingContext.DrawGeometry(DarkBrush, null, HalfEllipse(background, background.Width / 2, left: true));
            double halfWidth = background.Width / 2 * Math.Cos(ToRadians(angle));
            drawingContext.DrawGeometry(DarkBrush, null, HalfEllipse(background, halfWidth, left: false));
        }
        else if (angle == 90)
        {
            DrawCircle(drawingContext, background, DarkBrush);
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, background.Width / 2, left: false));
        }
        else if (angle < 180)
        {
            DrawCircle(drawingContext, background, DarkBrush);
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, background.Width / 2, left: false));
            double halfWidth = background.Width / 2 * Math.Sin(ToRadians(angle - 90));
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, halfWidth, left: true));
        }
        else if (angle == 180)
        {
            DrawCircle(drawingContext, background, LightBrush);
        }
        else if (angle < 270)
        {
            DrawCircle(drawingContext, background, DarkBrush);
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, background.Width / 2, left: true));
            double halfWidth = background.Width / 2 * Math.Cos(ToRadians(angle - 180));
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, halfWidth, left: false));
        }
        else if (angle == 270)
        {
            DrawCircle(drawingContext, background, DarkBrush);
            drawingContext.DrawGeometry(LightBrush, null, HalfEllipse(background, background.Width / 2, left: true));
        }
        else
        {
            DrawCircle(drawingContext, background, LightBrush);
            drawingContext.DrawGeometry(DarkBrush, null, HalfEllipse(background, background.Width / 2, left: false));
            double halfWidth = background.Width / 2 * Math.Cos(ToRadians(360 - angle));
            drawingContext.DrawGeometry(DarkBrush, null, HalfEllipse(background, halfWidth, left: true));
        }

        Point top = new(background.Left + background.Width / 2, background.Top);
        Point bottom = new(background.Left + background.Width / 2, background.Bottom);
        if (angle < 90 || angle > 270)
        {
            drawingContext.DrawLine(new Pen(DarkBrush, lineWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, top, bottom);
        }
        else if (angle > 90 && angle < 270)
        {
            drawingContext.DrawLine(new Pen(LightBrush, lineWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, top, bottom);
        }

        drawingContext.DrawEllipse(null, new Pen(strokeBrush, lineWidth), Center(background), background.Width / 2, background.Height / 2);
    }

    private static void DrawCircle(DrawingContext drawingContext, Rect bounds, Brush brush)
    {
        drawingContext.DrawEllipse(brush, null, Center(bounds), bounds.Width / 2, bounds.Height / 2);
    }

    // Half of an ellipse as wide as the bounds are high, split along its vertical axis, closed through its center.
    private static Geometry HalfEllipse(Rect bounds, double halfWidth, bool left)
    {
        Point center = Center(bounds);
        Point top = new(center.X, bounds.Top);
        Point bottom = new(center.X, bounds.Bottom);
        var figure = new PathFigure { StartPoint = top, IsClosed = true, IsFilled = true };
        figure.Segments.Add(new ArcSegment(bottom, new Size(Math.Max(halfWidth, 0), bounds.Height / 2), 0, false,
            left ? SweepDirection.Counterclockwise : SweepDirection.Clockwise, true));
        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }

    private static Point Center(Rect bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
