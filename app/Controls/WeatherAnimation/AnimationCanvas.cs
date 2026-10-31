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

// Windows Forms, there for the tray icon, also brings System.Drawing into scope.
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls.WeatherAnimation;

/// <summary>
/// Draws shapes through a current transform that rotations and translations add to, each one applying before those
/// already set, so a rotation set after a translation turns the shape around the translated origin.
/// </summary>
public sealed class AnimationCanvas(DrawingContext context, double width, double height)
{
    private Matrix _matrix = Matrix.Identity;

    public void Translate(double dx, double dy)
    {
        _matrix.TranslatePrepend(dx, dy);
    }

    public void Rotate(double degrees)
    {
        _matrix.RotatePrepend(degrees);
    }

    public void Rotate(double degrees, double centerX, double centerY)
    {
        _matrix.RotateAtPrepend(degrees, centerX, centerY);
    }

    /// <summary>
    /// Fills the whole area with a color, whatever the current transform.
    /// </summary>
    public void DrawColor(Color color)
    {
        context.DrawRectangle(Brush(color), null, new Rect(0, 0, width, height));
    }

    public void DrawRect(double left, double top, double right, double bottom, Color color)
    {
        Draw(() => context.DrawRectangle(Brush(color), null, Normalized(left, top, right, bottom)));
    }

    public void DrawRoundRect(double left, double top, double right, double bottom, double radius, Color color)
    {
        Draw(() => context.DrawRoundedRectangle(Brush(color), null, Normalized(left, top, right, bottom), radius, radius));
    }

    public void DrawCircle(double centerX, double centerY, double radius, Color color)
    {
        if (radius <= 0)
        {
            return;
        }

        Draw(() => context.DrawEllipse(Brush(color), null, new Point(centerX, centerY), radius, radius));
    }

    /// <summary>
    /// A round point, as a zero-length line with a round cap of the given width.
    /// </summary>
    public void DrawPoint(double x, double y, double strokeWidth, Color color)
    {
        DrawCircle(x, y, strokeWidth / 2, color);
    }

    /// <summary>
    /// A line with round caps.
    /// </summary>
    public void DrawLine(double x1, double y1, double x2, double y2, double strokeWidth, Color color)
    {
        if (strokeWidth <= 0)
        {
            return;
        }

        Pen pen = new(Brush(color), strokeWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        Draw(() => context.DrawLine(pen, new Point(x1, y1), new Point(x2, y2)));
    }

    private void Draw(Action draw)
    {
        if (_matrix.IsIdentity)
        {
            draw();
            return;
        }

        MatrixTransform transform = new(_matrix);
        transform.Freeze();
        context.PushTransform(transform);
        draw();
        context.Pop();
    }

    // A rectangle whose sides are given in any order, as its corners can be.
    private static Rect Normalized(double left, double top, double right, double bottom)
    {
        return new Rect(new Point(left, top), new Point(right, bottom));
    }

    private static SolidColorBrush Brush(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
