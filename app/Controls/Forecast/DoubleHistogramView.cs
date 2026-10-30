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

public sealed class DoubleHistogramView : ForecastChartView
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(DoubleHistogramData), typeof(DoubleHistogramView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public DoubleHistogramData? Data
    {
        get => (DoubleHistogramData?)GetValue(DataProperty);
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
        int marginTop = data.HighSecondary?.Text is not null ? 46 : 24;
        int marginBottom = data.LowSecondary?.Text is not null ? 46 : 24;
        double cx = ActualWidth / 2;
        Brush outline = (Brush)FindResource("OutlineBrush");
        drawingContext.DrawLine(new Pen(outline, 1), new Point(cx, marginTop), new Point(cx, measuredHeight - marginBottom));
        DrawSecondaryHistogram(drawingContext, data.HighSecondary, data.HighValue, data.Highest, marginTop, marginBottom, false);
        DrawSecondaryHistogram(drawingContext, data.LowSecondary, data.LowValue, data.Highest, marginTop, marginBottom, true);
        if (data.HighValue is { } high && high != 0f && data.HighText is { } highText)
        {
            int y = TrendCoordinates.ComputeDoubleHistogramCoordinate(measuredHeight, marginTop, marginBottom, 4, high, data.Highest, false);
            double cy = measuredHeight / 2f - 2;
            if (y <= cy)
            {
                drawingContext.DrawRoundedRectangle(new SolidColorBrush(data.HighColor), null,
                    new Rect(cx - 4, y, 8, cy - y), 4, 4);
            }

            DrawLabel(labels, highText, marginTop, true);
        }

        if (data.LowValue is { } low && low != 0f && data.LowText is { } lowText)
        {
            int y = TrendCoordinates.ComputeDoubleHistogramCoordinate(measuredHeight, marginTop, marginBottom, 4, low, data.Highest, true);
            double cy = measuredHeight / 2f + 2;
            if (y >= cy)
            {
                Color color = data.LowColor;
                var brush = new SolidColorBrush(Color.FromArgb(127, color.R, color.G, color.B));
                drawingContext.DrawRoundedRectangle(brush, null, new Rect(cx - 4, cy, 8, y - cy), 4, 4);
            }

            DrawLabel(labels, lowText, measuredHeight - marginBottom, false);
        }

        if (data.HighSecondary is { Text: { Length: > 0 } highPillText } highPill)
        {
            DrawSecondaryHistogramPill(drawingContext, highPillText, highPill.Color, true);
        }

        if (data.LowSecondary is { Text: { Length: > 0 } lowPillText } lowPill)
        {
            DrawSecondaryHistogramPill(drawingContext, lowPillText, lowPill.Color, false);
        }
    }

    private void DrawSecondaryHistogram(
        DrawingContext drawingContext, SecondaryHistogramData? secondary, float? value, float highest,
        int marginTop, int marginBottom, bool lower)
    {
        if (secondary is not { Value: { } gusts, Text: not null } || value is not { } speed || gusts <= speed)
        {
            return;
        }

        int y = TrendCoordinates.ComputeDoubleHistogramCoordinate((int)ActualHeight, marginTop, marginBottom, 4, gusts, highest, lower);
        double center = (int)ActualHeight / 2f + (lower ? 2 : -2);
        if (lower ? y >= center : y <= center)
        {
            var brush = new SolidColorBrush(Color.FromArgb(76, secondary.Color.R, secondary.Color.G, secondary.Color.B));
            drawingContext.DrawRoundedRectangle(brush, null,
                new Rect(ActualWidth / 2 - 4, Math.Min(y, center), 8, Math.Abs(y - center)), 4, 4);
        }
    }

    private void DrawLabel(DrawingContext drawingContext, string text, double y, bool above)
    {
        Brush brush = (Brush)FindResource("OnSurfaceVariantBrush");
        FormattedText formatted = MakeText(text, 14, brush);
        drawingContext.DrawText(formatted,
            new Point((ActualWidth - formatted.Width) / 2, above ? y - formatted.Height - 2 : y + 2));
    }
}
