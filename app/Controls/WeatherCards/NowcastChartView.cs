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

using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LibreccioWeather.ViewModels.Details;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.WeatherCards;

/// <summary>
/// The precipitation of the next hour as columns. X values are Unix milliseconds.
/// </summary>
public sealed record NowcastChartData(
    long MinX,
    long MaxX,
    long XStep,
    IReadOnlyList<long> X,
    IReadOnlyList<double> Y,
    double MaxY,
    Color BarColor,
    IReadOnlyList<ChartAxisLabel> BottomLabels,
    IReadOnlyList<ChartAxisLabel> HorizontalLines,
    Func<long, string> MarkerText);

/// <summary>
/// Columns from zero, a bottom axis with time labels, labelled reference lines over the columns, and while pressed a
/// guideline and a pill above the chart with the pressed interval's text.
/// </summary>
public sealed class NowcastChartView : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(NowcastChartData), typeof(NowcastChartView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double ChartHeight = 150;
    private const double BottomPadding = 16;
    private const double LabelSize = 12;
    private const double AxisLabelHorizontalPadding = 4;
    private const double AxisLabelVerticalPadding = 2;
    private const double LineThickness = 1;

    // Columns are 500 units thick with 16 between them, and half a column of padding at each end, all scaled to fit.
    private const double ColumnThickness = 500;
    private const double ColumnSpacing = 16;
    private const double ColumnCornerFraction = 0.15;

    private const double MarkerHorizontalPadding = 16;
    private const double MarkerVerticalPadding = 8;
    private const double MarkerMinWidth = 40;

    private long? _activeX;

    public NowcastChartView()
    {
        ClipToBounds = true;
        Focusable = false;
        IsManipulationEnabled = true;
        Unloaded += (_, _) => _activeX = null;
    }

    public NowcastChartData? Data
    {
        get => (NowcastChartData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, ChartHeight);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        if (Data is not { } data || ActualWidth <= 0 || ActualHeight <= 0 || data.MaxX <= data.MinX)
        {
            return;
        }

        Brush lineBrush = (Brush)FindResource("OutlineBrush");
        Brush labelBrush = (Brush)FindResource("ChartLabelBrush");
        var linePen = Freeze(new Pen(lineBrush, LineThickness));
        Layout(data, labelBrush, out Rect layer, out double startPadding, out double spacing);

        // Columns from zero up to their value.
        double width = ColumnThickness / (ColumnThickness + ColumnSpacing) * spacing;
        Brush fill = Freeze(new SolidColorBrush(data.BarColor));
        for (int i = 0; i < data.X.Count; i++)
        {
            double x = ToX(data, layer, startPadding, spacing, data.X[i]);
            double top = ToY(data, layer, data.Y[i]);
            var column = new Rect(x - width / 2, Math.Min(top, layer.Bottom), width, Math.Abs(layer.Bottom - top));
            double radius = Math.Min(column.Width, column.Height) * ColumnCornerFraction;
            drawingContext.DrawRoundedRectangle(fill, null, column, radius, radius);
        }

        // The axis line runs one line thickness past the plot at each end.
        double axisY = layer.Bottom + LineThickness / 2;
        drawingContext.DrawLine(linePen, new Point(layer.Left - LineThickness, axisY), new Point(layer.Right + LineThickness, axisY));
        foreach (ChartAxisLabel label in data.BottomLabels)
        {
            FormattedText text = MakeText(label.Text, labelBrush);
            double x = ToX(data, layer, startPadding, spacing, (long)label.Value);
            drawingContext.DrawText(text, new Point(x - text.Width / 2, layer.Bottom + LineThickness + AxisLabelVerticalPadding));
        }

        // Reference lines over the columns, each labelled at the start below the line, or above when there is no room.
        // The labels sit in their own column before the plot, so a tall column never covers them.
        foreach (ChartAxisLabel line in data.HorizontalLines)
        {
            double y = ToY(data, layer, line.Value);
            drawingContext.DrawLine(linePen, new Point(0, y), new Point(layer.Right, y));
            FormattedText text = MakeText(line.Text, labelBrush);
            bool below = y + LineThickness / 2 + text.Height <= layer.Bottom;
            drawingContext.DrawText(text, new Point(0, below ? y + LineThickness / 2 : y - LineThickness / 2 - text.Height));
        }

        if (_activeX is { } active)
        {
            DrawMarker(drawingContext, data, layer, startPadding, spacing, active, labelBrush);
        }
    }

    // The plot sits under the marker pill's height and above the axis, after the reference labels' column. The first
    // and last labels each add half their padded width as fixed padding, and the columns with their half-column end
    // padding are scaled into the rest.
    private void Layout(NowcastChartData data, Brush labelBrush, out Rect layer, out double startPadding, out double spacing)
    {
        double markerHeight = MakeText("0", labelBrush).Height + 2 * MarkerVerticalPadding;
        double axisHeight = LineThickness + MakeText("0", labelBrush).Height + 2 * AxisLabelVerticalPadding;
        double bottom = ActualHeight - BottomPadding - axisHeight;
        double labelColumn = data.HorizontalLines.Count == 0 ? 0
            : data.HorizontalLines.Max(line => MakeText(line.Text, labelBrush).Width) + 2 * AxisLabelHorizontalPadding;
        layer = new Rect(labelColumn, markerHeight, Math.Max(0, ActualWidth - labelColumn), Math.Max(0, bottom - markerHeight));

        double fixedStart = data.BottomLabels.Count > 0
            ? (MakeText(data.BottomLabels[0].Text, labelBrush).Width + 2 * AxisLabelHorizontalPadding) / 2 : 0;
        double fixedEnd = data.BottomLabels.Count > 0
            ? (MakeText(data.BottomLabels[^1].Text, labelBrush).Width + 2 * AxisLabelHorizontalPadding) / 2 : 0;
        double steps = (double)(data.MaxX - data.MinX) / data.XStep;
        double scalableWidth = ColumnThickness / 2 + steps * (ColumnThickness + ColumnSpacing) + ColumnThickness / 2;
        double zoom = Math.Max(0, layer.Width - fixedStart - fixedEnd) / scalableWidth;
        spacing = (ColumnThickness + ColumnSpacing) * zoom;
        startPadding = fixedStart + ColumnThickness / 2 * zoom;
    }

    private static double ToX(NowcastChartData data, Rect layer, double startPadding, double spacing, long x)
    {
        return layer.Left + startPadding + (double)(x - data.MinX) / data.XStep * spacing;
    }

    private static double ToY(NowcastChartData data, Rect layer, double y)
    {
        return data.MaxY <= 0 ? layer.Bottom : layer.Bottom - y / data.MaxY * layer.Height;
    }

    // A guideline through the pressed column, and a pill above the plot centered on it but kept inside the plot.
    private void DrawMarker(DrawingContext drawingContext, NowcastChartData data, Rect layer, double startPadding,
        double spacing, long active, Brush labelBrush)
    {
        double x = ToX(data, layer, startPadding, spacing, active);
        drawingContext.DrawLine(Freeze(new Pen(labelBrush, LineThickness)), new Point(x, layer.Top), new Point(x, layer.Bottom));

        FormattedText text = MakeText(data.MarkerText(active), (Brush)FindResource("OnPrimaryBrush"));
        double width = Math.Max(MarkerMinWidth, text.Width + 2 * MarkerHorizontalPadding);
        double height = text.Height + 2 * MarkerVerticalPadding;
        double center = Math.Clamp(x, layer.Left + width / 2, Math.Max(layer.Left + width / 2, layer.Right - width / 2));
        var pill = new Rect(center - width / 2, layer.Top - height, width, height);
        drawingContext.DrawRoundedRectangle((Brush)FindResource("PrimaryBrush"), null, pill, height / 2, height / 2);
        drawingContext.DrawText(text, new Point(pill.X + (width - text.Width) / 2, pill.Y + MarkerVerticalPadding));
    }

    private FormattedText MakeText(string text, Brush brush)
    {
        return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), LabelSize, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    private FontFamily FontFamily => (FontFamily)GetValue(System.Windows.Documents.TextElement.FontFamilyProperty);

    private static T Freeze<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (CaptureMouse())
        {
            SetActive(FindNearestX(e.GetPosition(this).X));
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (IsMouseCaptured)
        {
            SetActive(FindNearestX(e.GetPosition(this).X));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        SetActive(null);
    }

    protected override void OnTouchDown(TouchEventArgs e)
    {
        base.OnTouchDown(e);
        if (CaptureTouch(e.TouchDevice))
        {
            SetActive(FindNearestX(e.GetTouchPoint(this).Position.X));
            e.Handled = true;
        }
    }

    protected override void OnTouchMove(TouchEventArgs e)
    {
        base.OnTouchMove(e);
        if (e.TouchDevice.Captured == this)
        {
            SetActive(FindNearestX(e.GetTouchPoint(this).Position.X));
            e.Handled = true;
        }
    }

    protected override void OnTouchUp(TouchEventArgs e)
    {
        base.OnTouchUp(e);
        ReleaseTouchCapture(e.TouchDevice);
        SetActive(null);
        e.Handled = true;
    }

    // The chart keeps press-and-drag for itself, so the main screen cannot scroll from it.
    protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
    {
        base.OnManipulationStarting(e);
        e.Handled = true;
    }

    private long? FindNearestX(double pointerX)
    {
        if (Data is not { } data || data.X.Count == 0 || data.MaxX <= data.MinX)
        {
            return null;
        }

        Layout(data, (Brush)FindResource("ChartLabelBrush"), out Rect layer, out double startPadding, out double spacing);
        return data.X.MinBy(x => Math.Abs(ToX(data, layer, startPadding, spacing, x) - pointerX));
    }

    private void SetActive(long? x)
    {
        if (_activeX == x)
        {
            return;
        }

        _activeX = x;
        InvalidateVisual();
    }
}
