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
using System.Windows.Controls;
using System.Windows.Media;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.Forecast;

/// <summary>
/// Forecast columns side by side. When every column fits at its minimum width, the columns share the available width
/// in whole physical pixels, so they fill the card exactly and no scrollbar appears. Otherwise each column keeps the
/// minimum width and the row is wider than the card, to be scrolled.
/// </summary>
public sealed class ForecastColumnsPanel : Panel
{
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
        nameof(MinColumnWidth), typeof(double), typeof(ForecastColumnsPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty AvailableWidthProperty = DependencyProperty.Register(
        nameof(AvailableWidth), typeof(double), typeof(ForecastColumnsPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double MinColumnWidth
    {
        get => (double)GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    /// <summary>
    /// The visible width the columns may fill, which a scrolling parent does not pass on when it measures them.
    /// </summary>
    public double AvailableWidth
    {
        get => (double)GetValue(AvailableWidthProperty);
        set => SetValue(AvailableWidthProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double[] widths = ColumnWidths();
        double width = 0;
        double height = 0;
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            UIElement child = InternalChildren[i];
            child.Measure(new Size(widths[i], availableSize.Height));
            width += widths[i];
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double[] widths = ColumnWidths();
        double x = 0;
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            InternalChildren[i].Arrange(new Rect(x, 0, widths[i], finalSize.Height));
            x += widths[i];
        }

        return finalSize;
    }

    private double[] ColumnWidths()
    {
        int count = InternalChildren.Count;
        double[] widths = new double[count];
        if (count == 0)
        {
            return widths;
        }

        if (count * MinColumnWidth > AvailableWidth)
        {
            Array.Fill(widths, MinColumnWidth);
            return widths;
        }

        // The width is split in whole physical pixels, rounded down so the row is never wider than the card; the
        // leftover pixels go one each to columns spread evenly along the row.
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        int totalPixels = (int)Math.Floor(AvailableWidth * scale);
        int basePixels = totalPixels / count;
        int remainder = totalPixels % count;
        for (int i = 0; i < count; i++)
        {
            int extra = ((i + 1) * remainder / count) - (i * remainder / count);
            widths[i] = (basePixels + extra) / scale;
        }

        return widths;
    }
}
