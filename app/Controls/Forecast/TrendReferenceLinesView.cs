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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.ViewModels.Forecast;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls.Forecast;

public sealed class TrendReferenceLinesView : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(ForecastTrend), typeof(TrendReferenceLinesView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChartTopProperty = DependencyProperty.Register(
        nameof(ChartTop), typeof(int), typeof(TrendReferenceLinesView),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChartBottomProperty = DependencyProperty.Register(
        nameof(ChartBottom), typeof(int), typeof(TrendReferenceLinesView),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public ForecastTrend? Data
    {
        get => (ForecastTrend?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public int ChartTop
    {
        get => (int)GetValue(ChartTopProperty);
        set => SetValue(ChartTopProperty, value);
    }

    public int ChartBottom
    {
        get => (int)GetValue(ChartBottomProperty);
        set => SetValue(ChartBottomProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Data is not { } data || data.KeyLines.Count == 0 || ChartTop < 0 || ChartBottom < 0
            || data.Columns.FirstOrDefault()?.Chart is not { } chart)
        {
            return;
        }

        float highest = chart.Highest ?? chart.HighestHistogram;
        float lowest = chart.Lowest ?? chart.LowestHistogram;

        var pen = new Pen((Brush)FindResource("OutlineBrush"), 1)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        Brush textBrush = (Brush)FindResource("OnSurfaceVariantBrush");
        foreach (TrendKeyLine line in data.KeyLines)
        {
            if (line.Value > highest || line.Value < lowest)
            {
                continue;
            }

            int y = TrendCoordinates.ComputeKeyLineCoordinate(ChartTop, ChartBottom, line.Value, highest, lowest);
            drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
            DrawLabel(drawingContext, line.ContentLeft, textBrush, y, line.AboveLine, false);
            DrawLabel(drawingContext, line.ContentRight, textBrush, y, line.AboveLine, true);
        }
    }

    private void DrawLabel(DrawingContext drawingContext, string text, Brush brush, int y, bool aboveLine, bool right)
    {
        var typeface = new Typeface(System.Windows.SystemFonts.MessageFontFamily,
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var formatted = new FormattedText(text, Strings.Culture, FlowDirection, typeface, 12, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(formatted,
            new Point(right ? ActualWidth - 4 - formatted.Width : 4,
                aboveLine ? y - formatted.Height - 2 : y + 2));
    }
}
