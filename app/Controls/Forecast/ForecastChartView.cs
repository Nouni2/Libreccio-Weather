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
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Localization;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;

namespace LibreccioWeather.Controls.Forecast;

public abstract class ForecastChartView : FrameworkElement
{
    private readonly ShadowedLabels _labels = new();

    // Trend charts draw on an unmirrored canvas in a right-to-left layout; the columns change places with the layout,
    // and a chart flips only the positions it chooses itself.
    protected ForecastChartView()
    {
        FlowDirection = FlowDirection.LeftToRight;
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

    // Roboto Medium keeps the labels' size and their placement above and below the curve independent of the Windows
    // font, as the placement is computed from the text box's height. Canvas text is ordered by its first strong letter,
    // left to right without one.
    protected FormattedText MakeText(string text, double size, Brush brush)
    {
        return new FormattedText(BidiText.Embed(text, FlowDirection, FlowDirection.LeftToRight), Strings.Culture, FlowDirection,
            BundledFonts.RobotoMedium, size, brush, NativeDigits.Substitution, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    // Brushes, pens and geometries made while drawing are frozen, so WPF does not track them for changes.
    protected static T Frozen<T>(T freezable) where T : Freezable
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }

        return freezable;
    }

    protected void DrawSecondaryHistogramPill(DrawingContext drawingContext, string text, Color color, bool above)
    {
        FormattedText formatted = MakeText(text, 10, Brushes.Black);
        double height = formatted.Height + 6;
        double width = Math.Max(height, formatted.Width + 12);
        double top = above ? 2 : ActualHeight - 2 - height;
        drawingContext.DrawRoundedRectangle(Frozen(new SolidColorBrush(color)), null,
            new Rect((ActualWidth - width) / 2, top, width, height), height / 2, height / 2);
        drawingContext.DrawText(formatted, new System.Windows.Point((ActualWidth - formatted.Width) / 2, top + 3));
    }

    private sealed class ShadowedLabels : DrawingVisual
    {
        private DpiScale? _dpi;

        // The labels are kept as a bitmap at the screen's resolution, so the weather background redrawing beneath them
        // on every frame does not make them render again. The shadow effect already draws them into a bitmap without
        // ClearType, so the cache changes nothing visible.
        public ShadowedLabels()
        {
            VisualCacheMode = new BitmapCache { SnapsToDevicePixels = true };
        }

        public void SetDpi(DpiScale dpi)
        {
            // A new effect is only made when the scale changes, as a chart is drawn again on every data change.
            if (_dpi is { } current && current.DpiScaleX == dpi.DpiScaleX && current.DpiScaleY == dpi.DpiScaleY)
            {
                return;
            }

            _dpi = dpi;
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
