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
using LibreccioWeather.Core.Charts;
using LibreccioWeather.ViewModels.Details;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FlowDirection = System.Windows.FlowDirection;
using FontFamily = System.Windows.Media.FontFamily;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.Details;

/// <summary>
/// A chart of one day with the hours along the bottom and values on the end side. Each hour takes an equal segment of
/// the width, with the point at its center. Lines are straight unless the chart sets a curvature. Pressing on the
/// chart shows a guideline at the nearest hour.
/// </summary>
public sealed class DetailsChartView : FrameworkElement
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(DetailsChartData), typeof(DetailsChartView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double MinChartHeight = 100;
    private const double LabelSize = 12;
    private const double LabelHorizontalPadding = 4;
    private const double LabelVerticalPadding = 4;
    private const double TickLength = 4;
    private const double LineThickness = 5;
    private const double IconSize = 18;
    private const double IconPadding = 5;
    private const double DashLength = 4;
    private const double DashGap = 2;

    // A column is 500 units thick with 16 units between columns, scaled to fit its hour.
    private const double BarFill = 500.0 / 516.0;
    private const double BarCornerFraction = 0.15;

    // Text labels are grey on the dark theme, and lines take the outline color.
    private static readonly Brush LabelBrush = Freeze(new SolidColorBrush(Color.FromRgb(0xB2, 0xB2, 0xB2)));

    private long? _activeX;
    private Window? _window;

    public DetailsChartView()
    {
        ClipToBounds = true;
        Focusable = false;
        IsManipulationEnabled = true;
        Loaded += (_, _) =>
        {
            _window = Window.GetWindow(this);
            if (_window is not null)
            {
                _window.SizeChanged += OnWindowSizeChanged;
            }

            InvalidateMeasure();
        };
        Unloaded += (_, _) =>
        {
            if (_window is not null)
            {
                _window.SizeChanged -= OnWindowSizeChanged;
            }

            SetActive(null);
        };
    }

    public DetailsChartData? Data
    {
        get => (DetailsChartData?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        InvalidateMeasure();
    }

    // The chart takes a quarter of the window height, and never less than 100.
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = Math.Max(MinChartHeight, (_window?.ActualHeight ?? 0) / 4);
        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        if (Data is not { } data || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        Brush lineBrush = (Brush)FindResource("OutlineBrush");
        var axisPen = new Pen(lineBrush, 1);
        var guidelinePen = new Pen(lineBrush, 1) { DashStyle = new DashStyle([DashLength, DashGap], 0) };
        Rect layer = GetLayerBounds(data);
        if (layer.Width <= 0 || layer.Height <= 0)
        {
            return;
        }

        // Guidelines behind the data.
        foreach (ChartAxisLabel label in data.EndAxisLabels)
        {
            double y = ToY(data, layer, label.Value);
            drawingContext.DrawLine(guidelinePen, new Point(layer.Left, y), new Point(layer.Right, y));
        }

        foreach (ChartAxisLabel label in data.BottomAxisLabels)
        {
            double x = ToX(data, layer, (long)label.Value);
            drawingContext.DrawLine(guidelinePen, new Point(x, layer.Top), new Point(x, layer.Bottom));
        }

        drawingContext.PushClip(new RectangleGeometry(layer));
        if (data.Kind == DetailsChartKind.Bar)
        {
            DrawBars(drawingContext, data, layer);
        }
        else
        {
            foreach (DetailsChartSeries series in data.Series)
            {
                DrawLine(drawingContext, data, layer, series);
            }
        }

        drawingContext.Pop();

        // End axis on the right, with its labels centered on their values.
        drawingContext.DrawLine(axisPen, new Point(layer.Right, layer.Top), new Point(layer.Right, layer.Bottom));
        foreach (ChartAxisLabel label in data.EndAxisLabels)
        {
            double y = ToY(data, layer, label.Value);
            drawingContext.DrawLine(axisPen, new Point(layer.Right, y), new Point(layer.Right + TickLength, y));
            FormattedText text = MakeText(label.Text, LabelBrush);
            drawingContext.DrawText(text, new Point(layer.Right + TickLength + LabelHorizontalPadding, y - text.Height / 2));
        }

        // Bottom axis with the hours.
        drawingContext.DrawLine(axisPen, new Point(layer.Left, layer.Bottom), new Point(layer.Right, layer.Bottom));
        foreach (ChartAxisLabel label in data.BottomAxisLabels)
        {
            double x = ToX(data, layer, (long)label.Value);
            drawingContext.DrawLine(axisPen, new Point(x, layer.Bottom), new Point(x, layer.Bottom + TickLength));
            FormattedText text = MakeText(label.Text, LabelBrush);
            drawingContext.DrawText(text, new Point(x - text.Width / 2, layer.Bottom + TickLength + LabelVerticalPadding));
        }

        // Reference lines, labelled below the line at the start.
        foreach (ChartAxisLabel line in data.HorizontalLines)
        {
            double y = ToY(data, layer, line.Value);
            if (y < layer.Top || y > layer.Bottom)
            {
                continue;
            }

            drawingContext.DrawLine(axisPen, new Point(layer.Left, y), new Point(layer.Right, y));
            FormattedText text = MakeText(line.Text, LabelBrush);
            drawingContext.DrawText(text, new Point(layer.Left + LabelHorizontalPadding, y + LabelVerticalPadding));
        }

        DrawTop(drawingContext, data, layer, axisPen);

        if (_activeX is { } activeX)
        {
            double x = ToX(data, layer, activeX);
            drawingContext.DrawLine(new Pen(LabelBrush, 1), new Point(x, layer.Top), new Point(x, layer.Bottom));
        }
    }

    private void DrawTop(DrawingContext drawingContext, DetailsChartData data, Rect layer, Pen axisPen)
    {
        if (data.TopLabel is { } topLabel)
        {
            drawingContext.DrawLine(axisPen, new Point(layer.Left, layer.Top), new Point(layer.Right, layer.Top));
            double maxLabelWidth = data.TopValues.Select(value => MakeText(topLabel(value), LabelBrush).Width)
                .DefaultIfEmpty(0).Max() + 2 * LabelHorizontalPadding;
            foreach (long value in DetailsChartAxes.GetTopLabelValues(
                data.TopValues, data.MinX, data.MaxX, (float)layer.Width, (float)maxLabelWidth))
            {
                FormattedText text = MakeText(topLabel(value), LabelBrush);
                double x = ToX(data, layer, value);
                drawingContext.DrawText(text, new Point(x - text.Width / 2, layer.Top - LabelVerticalPadding - text.Height));
            }
        }

        if (data.TopIcon is { } topIcon && data.TopIconValues.Count > 0)
        {
            foreach (long value in DetailsChartAxes.GetTopLabelValues(
                data.TopIconValues, data.MinX, data.MaxX, (float)layer.Width, (float)IconSize))
            {
                if (topIcon(value) is not { } icon)
                {
                    continue;
                }

                double x = ToX(data, layer, value);
                drawingContext.DrawImage(icon,
                    new Rect(Math.Round(x - IconSize / 2), Math.Round(layer.Top - IconSize - IconPadding), IconSize, IconSize));
            }
        }
    }

    private void DrawLine(DrawingContext drawingContext, DetailsChartData data, Rect layer, DetailsChartSeries series)
    {
        if (series.X.Count == 0)
        {
            return;
        }

        Point[] points = new Point[series.X.Count];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Point(ToX(data, layer, series.X[i]), ToY(data, layer, series.Y[i]));
        }

        Brush stroke = MakeColorScale(data, layer, series.Colors);
        var line = new StreamGeometry();
        using (StreamGeometryContext context = line.Open())
        {
            context.BeginFigure(points[0], false, false);
            AddSegments(context, points, data.Curvature, layer);
        }

        if (series.AreaColors is { } areaColors)
        {
            // The area lies between the line and zero.
            double baseline = ToY(data, layer, Math.Clamp(0, data.MinY, data.MaxY));
            var area = new StreamGeometry();
            using (StreamGeometryContext context = area.Open())
            {
                context.BeginFigure(new Point(points[0].X, baseline), true, true);
                context.LineTo(points[0], false, false);
                AddSegments(context, points, data.Curvature, layer);
                context.LineTo(new Point(points[^1].X, baseline), false, false);
            }

            drawingContext.DrawGeometry(MakeColorScale(data, layer, areaColors), null, area);
        }

        drawingContext.DrawGeometry(null, new Pen(stroke, LineThickness) { LineJoin = PenLineJoin.Round }, line);
    }

    // Without curvature the points are joined by straight lines. With it, each curve leaves and arrives horizontally,
    // more strongly where the line rises or falls more: four times the height change over the chart height, at most
    // 1, times the curvature, of the horizontal distance.
    private static void AddSegments(StreamGeometryContext context, Point[] points, double curvature, Rect layer)
    {
        for (int i = 1; i < points.Length; i++)
        {
            Point previous = points[i - 1];
            Point current = points[i];
            if (curvature <= 0)
            {
                context.LineTo(current, true, true);
                continue;
            }

            double xDelta = Math.Min(4 * Math.Abs(current.Y - previous.Y) / layer.Height, 1.0) * curvature * (current.X - previous.X);
            context.BezierTo(new Point(previous.X + xDelta, previous.Y), new Point(current.X - xDelta, current.Y), current, true, true);
        }
    }

    private static void DrawBars(DrawingContext drawingContext, DetailsChartData data, Rect layer)
    {
        if (data.Series.Count == 0)
        {
            return;
        }

        DetailsChartSeries series = data.Series[0];
        double segment = GetSegmentWidth(data, layer);
        double width = segment * BarFill;
        double baseline = ToY(data, layer, Math.Clamp(0, data.MinY, data.MaxY));
        Brush fill = Freeze(new SolidColorBrush(data.BarColor));
        for (int i = 0; i < series.X.Count; i++)
        {
            double x = ToX(data, layer, series.X[i]);
            double y = ToY(data, layer, series.Y[i]);
            Rect bar = new(x - width / 2, Math.Min(y, baseline), width, Math.Abs(baseline - y));
            double radius = Math.Min(bar.Width, bar.Height) * BarCornerFraction;
            drawingContext.DrawRoundedRectangle(fill, null, bar, radius, radius);
        }
    }

    // A gradient from the top of the chart to its bottom, with each color at the height of its value. Stops above or
    // below the chart are pinned to its edge, each at least at the position of the one before, as Skia draws
    // gradients, so a color beyond the range takes over the edge instead of being blended there.
    private static Brush MakeColorScale(DetailsChartData data, Rect layer, IReadOnlyList<ChartColorStop> colors)
    {
        List<ChartColorStop> stops = [.. colors.OrderByDescending(stop => stop.Value)];
        if (stops.Count == 0)
        {
            return Brushes.Transparent;
        }

        if (stops.Count == 1)
        {
            return Freeze(new SolidColorBrush(stops[0].Color));
        }

        double range = data.MaxY - data.MinY;
        var brush = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.Absolute,
            StartPoint = new Point(0, layer.Top),
            EndPoint = new Point(0, layer.Bottom),
        };
        double previous = 0;
        for (int i = 0; i < stops.Count; i++)
        {
            double position = range == 0 ? (double)i / (stops.Count - 1) : 1 - (stops[i].Value - data.MinY) / range;
            position = Math.Clamp(position, previous, 1);
            brush.GradientStops.Add(new GradientStop(stops[i].Color, position));
            previous = position;
        }

        return Freeze(brush);
    }

    private Rect GetLayerBounds(DetailsChartData data)
    {
        double labelHeight = MakeText("0", LabelBrush).Height;
        double endAxisWidth = TickLength + 2 * LabelHorizontalPadding
            + data.EndAxisLabels.Select(label => MakeText(label.Text, LabelBrush).Width).DefaultIfEmpty(0).Max();
        double bottom = TickLength + labelHeight + 2 * LabelVerticalPadding;

        // Labels centered on the top and bottom values need half their height inside the chart.
        double top = labelHeight / 2;
        if (data.TopLabel is not null)
        {
            top = Math.Max(top, labelHeight + 2 * LabelVerticalPadding);
        }

        if (data.TopIcon is not null && data.TopIconValues.Count > 0)
        {
            top = Math.Max(top, IconSize + IconPadding);
        }

        return new Rect(0, top, Math.Max(0, ActualWidth - endAxisWidth), Math.Max(0, ActualHeight - top - bottom));
    }

    private static double GetSegmentWidth(DetailsChartData data, Rect layer)
    {
        long step = GetXStep(data);
        double segments = (double)(data.MaxX - data.MinX) / step + 1;
        return layer.Width / segments;
    }

    // The spacing of the data: the greatest common divisor of the gaps between its hours.
    private static long GetXStep(DetailsChartData data)
    {
        long step = 0;
        foreach (DetailsChartSeries series in data.Series)
        {
            for (int i = 1; i < series.X.Count; i++)
            {
                step = Gcd(step, Math.Abs(series.X[i] - series.X[i - 1]));
            }
        }

        return step > 0 ? step : 3_600_000L;
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }

    private static double ToX(DetailsChartData data, Rect layer, long x)
    {
        long step = GetXStep(data);
        double segment = GetSegmentWidth(data, layer);
        return layer.Left + ((double)(x - data.MinX) / step + 0.5) * segment;
    }

    private static double ToY(DetailsChartData data, Rect layer, double y)
    {
        double range = data.MaxY - data.MinY;
        return range == 0 ? layer.Bottom : layer.Bottom - (y - data.MinY) / range * layer.Height;
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

    // The chart keeps press-and-drag for itself, so a parent cannot scroll or swipe from it.
    protected override void OnManipulationStarting(ManipulationStartingEventArgs e)
    {
        base.OnManipulationStarting(e);
        e.Handled = true;
    }

    private long? FindNearestX(double pointerX)
    {
        if (Data is not { } data || data.XValues.Count == 0)
        {
            return null;
        }

        Rect layer = GetLayerBounds(data);
        return data.XValues.MinBy(x => Math.Abs(ToX(data, layer, x) - pointerX));
    }

    private void SetActive(long? x)
    {
        if (_activeX == x)
        {
            return;
        }

        _activeX = x;
        InvalidateVisual();
        Data?.SetActiveX(x);
    }
}
