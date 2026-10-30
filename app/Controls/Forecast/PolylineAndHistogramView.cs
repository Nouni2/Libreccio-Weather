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
using LibreccioWeather.Core.Charts;
using LibreccioWeather.ViewModels.Forecast;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls.Forecast;

public sealed class PolylineAndHistogramView : ForecastChartView
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(TrendChartData), typeof(PolylineAndHistogramView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public TrendChartData? Data
    {
        get => (TrendChartData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        using DrawingContext labels = OpenShadowedLabels();
        if (Data is not { } data || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        int measuredHeight = (int)ActualHeight;
        int marginTop = data.HasTopPolylineLabel ? 24 : 12;
        const int MarginBottom = 36;
        double bottom = measuredHeight - MarginBottom;
        Brush primary = (Brush)FindResource("PrimaryBrush");
        Brush highText = (Brush)FindResource("OnSurfaceBrush");
        Brush lowText = (Brush)FindResource("OnSurfaceVariantBrush");
        Brush outline = (Brush)FindResource("OutlineBrush");

        drawingContext.DrawLine(new Pen(outline, 1),
            new Point(ActualWidth / 2, marginTop), new Point(ActualWidth / 2, bottom));

        if (data.Histogram is { } histogram && data.HistogramText is { } histogramText
            && (histogram != 0f || (data.Highest is null && data.Lowest is null)))
        {
            int y = TrendCoordinates.ComputeSingleCoordinate(measuredHeight, marginTop, MarginBottom,
                histogram, data.HighestHistogram, data.LowestHistogram);
            Brush histogramBrush = data.HistogramColor is { } color ? new SolidColorBrush(color) : primary;
            if (y <= bottom)
            {
                double halfWidth = PixelTruncatedDimension(4.5f);
                drawingContext.DrawRoundedRectangle(WithAlpha(histogramBrush, 127), null,
                    new Rect(ActualWidth / 2 - halfWidth, y, 2 * halfWidth, bottom - y), halfWidth, halfWidth);
            }

            DrawText(drawingContext, histogramText, 12, data.HistogramUsesTitleColor ? highText : primary, bottom + 18);
        }

        if (data.Highest is not { } highest || data.Lowest is not { } lowest)
        {
            return;
        }

        int[] highY = Coordinates(data.HighValues, measuredHeight, marginTop, highest, lowest);
        int[] lowY = data.LowValues is { } low
            ? Coordinates(low, measuredHeight, marginTop, highest, lowest) : new int[3];
        if (data.HighText is { } highLabel)
        {
            DrawPolyline(drawingContext, data.HighValues, highY, primary, marginTop, bottom, shadow: true);
            DrawLabel(labels, highLabel, highText, highY[1], highY[1] > lowY[1]);
        }

        if (data.LowValues is { } lowValues && data.LowText is { } lowLabel)
        {
            DrawPolyline(drawingContext, lowValues, lowY, WithAlpha(primary, 127), marginTop, bottom, shadow: false);
            DrawLabel(labels, lowLabel, lowText, lowY[1], lowY[1] >= highY[1]);
        }
    }

    private static int[] Coordinates(float?[] values, int height, int marginTop, float highest, float lowest)
    {
        return values.Select(value => value is { } number
            ? TrendCoordinates.ComputeSingleCoordinate(height, marginTop, 36, number, highest, lowest)
            : 0).ToArray();
    }

    private void DrawPolyline(
        DrawingContext drawingContext, float?[] values, int[] y, Brush color, int marginTop, double bottom, bool shadow)
    {
        List<Point> points = [];
        if (values[0] is not null)
        {
            points.Add(new Point(GetRTLCompactX(0), y[0]));
        }

        points.Add(new Point(GetRTLCompactX(ActualWidth / 2), y[1]));
        if (values[2] is not null || values[0] is null)
        {
            points.Add(new Point(GetRTLCompactX(ActualWidth), y[2]));
        }

        if (shadow)
        {
            var area = new StreamGeometry();
            using (StreamGeometryContext context = area.Open())
            {
                context.BeginFigure(points[0], true, true);
                context.PolyLineTo(points.Skip(1).ToArray(), true, false);
                context.LineTo(new Point(points[^1].X, bottom), true, false);
                context.LineTo(new Point(points[0].X, bottom), true, false);
            }

            Color shade = ((SolidColorBrush)WithAlpha(color, 76)).Color;
            var gradient = new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, marginTop),
                EndPoint = new Point(0, bottom),
                GradientStops =
                [
                    new GradientStop(shade, 0),
                    new GradientStop(Color.FromArgb(0, shade.R, shade.G, shade.B), 1),
                ],
            };
            drawingContext.DrawGeometry(gradient, null, area);
        }

        var pen = new Pen(color, 5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var polyline = new StreamGeometry();
        using (StreamGeometryContext context = polyline.Open())
        {
            context.BeginFigure(points[0], false, false);
            context.PolyLineTo(points.Skip(1).ToArray(), true, false);
        }

        drawingContext.DrawGeometry(null, pen, polyline);
    }

    private void DrawLabel(DrawingContext drawingContext, string text, Brush brush, double y, bool below)
    {
        FormattedText formatted = MakeText(text, 14, brush);
        drawingContext.DrawText(formatted, new Point((ActualWidth - formatted.Width) / 2,
            below ? y + 2 : y - formatted.Height - 2));
    }

    private void DrawText(DrawingContext drawingContext, string text, double size, Brush brush, double y)
    {
        FormattedText formatted = MakeText(text, size, brush);
        drawingContext.DrawText(formatted, new Point((ActualWidth - formatted.Width) / 2, y));
    }

    private double GetRTLCompactX(double x)
    {
        return FlowDirection == System.Windows.FlowDirection.RightToLeft ? ActualWidth - x : x;
    }

    private static Brush WithAlpha(Brush brush, byte alpha)
    {
        Color color = ((SolidColorBrush)brush).Color;
        return new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
    }
}
