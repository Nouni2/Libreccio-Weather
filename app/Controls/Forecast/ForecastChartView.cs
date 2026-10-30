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
using LibreccioWeather.Core.Localization;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace LibreccioWeather.Controls.Forecast;

public abstract class ForecastChartView : FrameworkElement
{
    private readonly ShadowedLabels _labels = new();

    protected ForecastChartView()
    {
        AddVisualChild(_labels);
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index)
    {
        return index == 0 ? _labels : throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateVisual();
    }

    protected DrawingContext OpenShadowedLabels()
    {
        _labels.SetDpi(VisualTreeHelper.GetDpi(this));
        return _labels.RenderOpen();
    }

    protected double PixelTruncatedDimension(float dip)
    {
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        return (int)(dip * (float)scale) / scale;
    }

    protected FormattedText MakeText(string text, double size, Brush brush)
    {
        var typeface = new Typeface(System.Windows.SystemFonts.MessageFontFamily,
            FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);
        return new FormattedText(text, Strings.Culture, FlowDirection, typeface, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    protected void DrawSecondaryHistogramPill(DrawingContext drawingContext, string text, Color color, bool above)
    {
        FormattedText formatted = MakeText(text, 10, Brushes.Black);
        double height = formatted.Height + 6;
        double width = Math.Max(height, formatted.Width + 12);
        double top = above ? 2 : ActualHeight - 2 - height;
        drawingContext.DrawRoundedRectangle(new SolidColorBrush(color), null,
            new Rect((ActualWidth - width) / 2, top, width, height), height / 2, height / 2);
        drawingContext.DrawText(formatted, new System.Windows.Point((ActualWidth - formatted.Width) / 2, top + 3));
    }

    private sealed class ShadowedLabels : DrawingVisual
    {
        public void SetDpi(DpiScale dpi)
        {
            // The shadow radius and offset are physical pixels, independent of display scaling.
            VisualEffect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 51 / 255.0,
                BlurRadius = 2 / dpi.DpiScaleX,
                ShadowDepth = 1 / dpi.DpiScaleY,
                Direction = 270,
                RenderingBias = RenderingBias.Quality,
            };
        }
    }
}
