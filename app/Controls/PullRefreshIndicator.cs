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
using System.Windows.Media.Effects;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls;

/// <summary>
/// The circle that follows a pull to refresh: a surface-coloured disc whose arc grows with the pull, full once the
/// pull reaches the distance that refreshes on release.
/// </summary>
public sealed class PullRefreshIndicator : FrameworkElement
{
    public const double Diameter = 40;

    // The arc's radius and stroke, and the largest share of the circle it covers.
    private const double ArcRadius = 7.5;
    private const double ArcThickness = 2.5;
    private const double MaxArcShare = 0.8;

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(PullRefreshIndicator),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ArcBrushProperty = DependencyProperty.Register(
        nameof(ArcBrush), typeof(Brush), typeof(PullRefreshIndicator),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DiscBrushProperty = DependencyProperty.Register(
        nameof(DiscBrush), typeof(Brush), typeof(PullRefreshIndicator),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public PullRefreshIndicator()
    {
        IsHitTestVisible = false;

        // The arc is drawn as it is in a right-to-left layout, and its shadow keeps falling straight down.
        FlowDirection = FlowDirection.LeftToRight;
        Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Direction = 270, Opacity = 0.3 };
    }

    /// <summary>
    /// The pull as a share of the refresh distance: 1 or more refreshes on release.
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public Brush? ArcBrush
    {
        get => (Brush?)GetValue(ArcBrushProperty);
        set => SetValue(ArcBrushProperty, value);
    }

    public Brush? DiscBrush
    {
        get => (Brush?)GetValue(DiscBrushProperty);
        set => SetValue(DiscBrushProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Diameter, Diameter);

    protected override void OnRender(DrawingContext drawingContext)
    {
        var center = new Point(Diameter / 2, Diameter / 2);
        drawingContext.DrawEllipse(DiscBrush, null, center, Diameter / 2, Diameter / 2);
        if (ArcBrush is not { } arcBrush || Progress <= 0)
        {
            return;
        }

        double share = Math.Min(Progress, 1) * MaxArcShare;

        // The arc turns as the pull goes on, and is faint until the pull is long enough to refresh.
        double start = -90 + (Progress * 144);
        double sweep = Math.Max(share * 360, 1);
        var pen = new Pen(arcBrush, ArcThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        drawingContext.PushOpacity(Progress >= 1 ? 1 : 0.3 + (0.7 * Progress));
        drawingContext.DrawGeometry(null, pen, Arc(center, start, sweep));
        drawingContext.Pop();
    }

    private static StreamGeometry Arc(Point center, double startDegrees, double sweepDegrees)
    {
        Point PointAt(double degrees)
        {
            double radians = degrees * Math.PI / 180;
            return new Point(center.X + (ArcRadius * Math.Cos(radians)), center.Y + (ArcRadius * Math.Sin(radians)));
        }

        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(PointAt(startDegrees), false, false);
            context.ArcTo(PointAt(startDegrees + sweepDegrees), new Size(ArcRadius, ArcRadius), 0,
                sweepDegrees > 180, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        return geometry;
    }
}
