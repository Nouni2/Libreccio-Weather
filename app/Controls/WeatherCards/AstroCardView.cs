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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LibreccioWeather.Controls.Details;
using LibreccioWeather.Display;
using LibreccioWeather.ViewModels.WeatherCards;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace LibreccioWeather.Controls.WeatherCards;

/// <summary>
/// The square sun or moon card: its title, a dotted arc from rise to set over a dotted horizon, the part already
/// travelled drawn solid with a shadow under it and the body at its end, the rise and set times under the arc, and a
/// description at the bottom.
/// </summary>
public sealed class AstroCardView : Grid
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(AstroCardViewModel), typeof(AstroCardView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

    private const double CornerRadius = 28;
    private const double TitleSize = 16;
    private const double TitleIconSize = 20;
    private const double LabelSize = 14;
    private const double ArcViewMargin = 8;
    private const double ArcMargin = 16;
    private const double ArcAngle = 135;
    private const double LineSize = 5;
    private const double DottedLineSize = 1;
    private const double IconSize = 24;

    public static readonly DependencyProperty PathProgressProperty = DependencyProperty.Register(
        nameof(PathProgress), typeof(double), typeof(AstroCardView),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    public static readonly DependencyProperty IndicatorRotationProperty = DependencyProperty.Register(
        nameof(IndicatorRotation), typeof(double), typeof(AstroCardView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    public static readonly DependencyProperty PhaseAngleProperty = DependencyProperty.Register(
        nameof(PhaseAngle), typeof(double), typeof(AstroCardView),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    public static readonly DependencyProperty AreBlocksSquishedProperty = DependencyProperty.RegisterAttached(
        "AreBlocksSquished", typeof(bool), typeof(AstroCardView),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender, OnAppearanceChanged));

    private readonly CardSurface _surface;
    private readonly Dictionary<(string Value, double Size, double Width, bool Bold), AstroCardText> _labels = [];
    private WeatherCardsPanel? _panel;
    private bool _hasAnimated;
    private ScrollViewer? _scrollViewer;

    public AstroCardView()
    {
        Background = System.Windows.Media.Brushes.Transparent;
        var shadow = new Border
        {
            CornerRadius = new CornerRadius(CornerRadius),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 4,
                ShadowDepth = 2,
                Direction = 270,
                Opacity = 0.24,
                RenderingBias = RenderingBias.Quality,
            },
        };
        shadow.SetResourceReference(Border.BackgroundProperty, "MainCardBackgroundBrush");
        Children.Add(shadow);
        _surface = new CardSurface(this) { IsHitTestVisible = false };
        Children.Add(_surface);
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) => Animate();
        SizeChanged += (_, _) =>
        {
            _labels.Clear();
            _surface.InvalidateVisual();
            Animate();
        };
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(this);
        while (parent is not null && parent is not ScrollViewer)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        _scrollViewer = parent as ScrollViewer;
        parent = VisualTreeHelper.GetParent(this);
        while (parent is not null && parent is not WeatherCardsPanel)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        _panel = parent as WeatherCardsPanel;
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        Animate();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }

        _panel = null;
        ClearAnimations();
        PathProgress = 1;
        IndicatorRotation = 0;
        PhaseAngle = Data?.MoonPhaseAngle ?? 0;
    }

    internal void EnterScreen() => Animate();

    private static void OnAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is AstroCardView view)
        {
            view._surface?.InvalidateVisual();
        }
    }

    private void OnScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs args) => Animate();

    public double PathProgress
    {
        get => (double)GetValue(PathProgressProperty);
        set => SetValue(PathProgressProperty, value);
    }

    public double IndicatorRotation
    {
        get => (double)GetValue(IndicatorRotationProperty);
        set => SetValue(IndicatorRotationProperty, value);
    }

    public double PhaseAngle
    {
        get => (double)GetValue(PhaseAngleProperty);
        set => SetValue(PhaseAngleProperty, value);
    }

    private void ClearAnimations()
    {
        BeginAnimation(PathProgressProperty, null);
        BeginAnimation(IndicatorRotationProperty, null);
        BeginAnimation(PhaseAngleProperty, null);
    }

    private void Animate()
    {
        if (_hasAnimated || !IsLoaded || !IsVisible || Data is not { } card)
        {
            return;
        }

        if (_panel is not null && !_panel.HasEntered(this))
        {
            return;
        }

        if (_panel is null && _scrollViewer is not null)
        {
            Rect viewport = new(0, 0, _scrollViewer.ViewportWidth, _scrollViewer.ViewportHeight);
            Rect cardBounds = TransformToAncestor(_scrollViewer).TransformBounds(new Rect(RenderSize));
            if (!viewport.IntersectsWith(cardBounds))
            {
                return;
            }
        }

        _hasAnimated = true;
        if (!card.AnimateElements)
        {
            return;
        }

        long maximum = card.EndTime - card.StartTime;
        if (maximum > 0)
        {
            double duration = Math.Clamp(1000 + 3000.0 * (card.CurrentTime - card.StartTime) / maximum, 0, 4000);
            double rotation = 360.0 * (card.IsSun ? 7 : 4) * (card.CurrentTime - card.StartTime) / maximum;
            rotation = (int)Math.Clamp(rotation - rotation % 360, int.MinValue, int.MaxValue);
            if (!card.IsSun)
            {
                rotation = -rotation;
            }

            AnimateValue(PathProgressProperty, 1, duration, new CardOvershootEase());
            AnimateValue(IndicatorRotationProperty, rotation, duration, new CardOvershootEase());
        }

        if (!card.IsSun && card.MoonPhaseAngle is { } phase && phase > 0)
        {
            double duration = Math.Clamp(phase / 360.0 * 1000 + 1000, 0, 2000);
            AnimateValue(PhaseAngleProperty, phase, duration, new QuadraticEase { EasingMode = EasingMode.EaseOut });
        }
    }

    private void AnimateValue(DependencyProperty property, double target, double milliseconds, IEasingFunction easing)
    {
        var animation = new DoubleAnimation(0, target, TimeSpan.FromMilliseconds((long)milliseconds))
        {
            EasingFunction = easing,
        };
        RefreshRate.Apply(animation, this);
        BeginAnimation(property, animation);
    }

    public AstroCardViewModel? Data
    {
        get => (AstroCardViewModel?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FrameworkElementAutomationPeer(this);
    }

    private static void OnDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (AstroCardView)sender;
        AutomationProperties.SetName(view, (args.NewValue as AstroCardViewModel)?.AutomationName ?? "");
        view.ClearAnimations();
        view.PathProgress = args.NewValue is AstroCardViewModel { AnimateElements: true } ? 0 : 1;
        view.IndicatorRotation = 0;
        view.PhaseAngle = args.NewValue is AstroCardViewModel { AnimateElements: true } ? 0
            : (args.NewValue as AstroCardViewModel)?.MoonPhaseAngle ?? 0;
        view._labels.Clear();
        view._surface.InvalidateVisual();
        view._hasAnimated = false;
        view.Animate();
    }

    private void Draw(DrawingContext drawingContext)
    {
        if (Data is not { } card || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        float density = (float)dpi.DpiScaleX;
        float textScale = TextScale.Factor;
        int size = (int)Math.Floor(Math.Min(ActualWidth * dpi.DpiScaleX, ActualHeight * dpi.DpiScaleY) + 0.5);
        if (size <= 2 * (ArcViewMargin + ArcMargin) * density)
        {
            return;
        }

        drawingContext.PushTransform(new ScaleTransform(1 / dpi.DpiScaleX, 1 / dpi.DpiScaleY));
        double cornerRadius = CornerRadius * density;
        var bounds = new Rect(0, 0, size, size);
        drawingContext.PushClip(new RectangleGeometry(bounds, cornerRadius, cornerRadius));
        var background = (SolidColorBrush)FindResource("MainCardBackgroundBrush");
        drawingContext.DrawRoundedRectangle(background, null, bounds, cornerRadius, cornerRadius);

        bool squished = (bool)GetValue(AreBlocksSquishedProperty);
        double start = Math.Floor(size * 0.1f + 0.5f);
        double end = Math.Floor(size * 0.9f + 0.5f);
        double bottom = Math.Floor(size * (!card.IsSun && squished ? 0.95f : 0.9f) + 0.5f);
        Brush titleBrush = (Brush)FindResource("MainCardTitleBrush");
        Brush textBrush = (Brush)FindResource("MainCardTextBrush");
        double titleIconSize = Math.Floor(TitleIconSize * density * textScale + 0.5f);
        double textGap = Math.Floor(4 * density + 0.5f);
        AstroCardText title = Text(card.Title, TitleSize * density * textScale,
            Math.Max(1, end - start - titleIconSize - textGap), bold: true);
        double titleTop = Math.Floor(size * (squished ? 0.05f : 0.1f) + 0.5f);
        if (TryFindResource(card.IsSun ? "IconClearDay" : "IconClearNight") is Geometry titleIcon)
        {
            double iconTop = Math.Floor(titleTop + (title.Height - titleIconSize) / 2);
            drawingContext.PushTransform(new TranslateTransform(start, iconTop));
            drawingContext.PushTransform(new ScaleTransform(titleIconSize / 24, titleIconSize / 24));
            drawingContext.DrawGeometry(titleBrush, null, titleIcon);
            drawingContext.Pop();
            drawingContext.Pop();
        }

        title.Draw(drawingContext, titleBrush, new Point(start + titleIconSize + textGap, titleTop));

        double arcTop = titleTop + title.Height;
        double outerMargin = Math.Floor(ArcViewMargin * density + 0.5f);
        double arcHeight = DrawSunMoon(drawingContext, card, outerMargin, arcTop,
            size - 2 * outerMargin, ((SolidColorBrush)FindResource("MainCardArcRootBrush")).Color, density);

        double timesTop = arcTop + arcHeight;
        double labelSize = LabelSize * density * textScale;
        double availableWidth = end - start;
        AstroCardText rise = Text(card.RiseText, labelSize, availableWidth);
        AstroCardText set = Text(card.SetText, labelSize, availableWidth);
        // Both labels shrink together so that a long localized time cannot overwrite the other.
        while (rise.Width + set.Width > availableWidth && labelSize > 12 * density * textScale)
        {
            labelSize = Math.Max(12 * density * textScale, labelSize - density * textScale);
            rise = Text(card.RiseText, labelSize, availableWidth);
            set = Text(card.SetText, labelSize, availableWidth);
        }

        if (rise.Width + set.Width > availableWidth)
        {
            rise = Text(card.RiseText, labelSize, availableWidth / 2);
            set = Text(card.SetText, labelSize, availableWidth / 2);
        }

        rise.Draw(drawingContext, textBrush, new Point(start, timesTop));
        set.Draw(drawingContext, textBrush, new Point(end - set.Width, timesTop));

        if (!string.IsNullOrEmpty(card.Description))
        {
            AstroCardText description = Text(card.Description, LabelSize * density * textScale, availableWidth);
            double phaseSize = card.MoonPhaseAngle is not null ? description.Height : 0;
            double iconGap = card.MoonPhaseAngle is not null ? textGap : 0;
            description = Text(card.Description, LabelSize * density * textScale,
                Math.Max(1, availableWidth - phaseSize - iconGap));
            double top = bottom - description.Baseline;
            double left = (size - description.Width - phaseSize - iconGap) / 2;
            if (card.MoonPhaseAngle is not null)
            {
                MoonPhaseView.Draw(drawingContext, new Rect(left, top, phaseSize, phaseSize),
                    (float)PhaseAngle, (Brush)FindResource("BodyTextBrush"), density);
            }

            description.Draw(drawingContext, textBrush, new Point(left + phaseSize + iconGap, top));
        }

        drawingContext.Pop();
        drawingContext.Pop();
    }

    // Returns the height the arc takes. The arc spans 135°, from its start on the left to its end on the right, on a
    // circle whose chord fills the width.
    private double DrawSunMoon(DrawingContext drawingContext, AstroCardViewModel card, double left, double top, double width, Color rootColor, float density)
    {
        float margin = (float)ArcMargin * density;
        int innerWidth = (int)(width - 2 * margin);
        double deltaRadians = (180 - ArcAngle) / 2.0 * Math.PI / 180.0;
        int radius = (int)(innerWidth / 2 / Math.Cos(deltaRadians));
        int height = (int)(radius - innerWidth / 2 * Math.Tan(deltaRadians));
        int measuredWidth = (int)(innerWidth + 2 * margin);
        int measuredHeight = (int)(height + 2 * margin);
        double centerX = left + measuredWidth / 2;
        double centerY = top + (int)(margin + radius);
        var rect = new Rect(centerX - radius, centerY - radius, 2 * radius, 2 * radius);

        long max = card.EndTime - card.StartTime;
        long currentTime = card.StartTime + (long)((card.CurrentTime - card.StartTime) * (float)PathProgress);
        long progress = Math.Min(Math.Max(currentTime - card.StartTime, 0), max);
        double startAngle = 270 - ArcAngle / 2;
        double sweep = max > 0 ? (float)((double)progress / max * ArcAngle) : 0;
        double progressEndAngle = startAngle + sweep;

        Color line = card.LineColor;
        if (progress > 0)
        {
            DrawShadow(drawingContext, rect, startAngle, progressEndAngle, line, rootColor, top + measuredHeight - margin);
        }

        var dotted = new Pen(new SolidColorBrush(Color.FromArgb(127, line.R, line.G, line.B)), DottedLineSize * density)
        {
            DashStyle = new DashStyle([3, 6], 0),
            DashCap = PenLineCap.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
        };
        drawingContext.DrawGeometry(null, dotted, Arc(rect, startAngle, ArcAngle, closed: false));
        drawingContext.DrawLine(dotted, new Point(left + margin, top + measuredHeight - margin),
            new Point(left + measuredWidth - margin, top + measuredHeight - margin));

        if (progress > 0)
        {
            var solid = new Pen(new SolidColorBrush(line), LineSize * density) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            drawingContext.DrawGeometry(null, solid, Arc(rect, startAngle, sweep, closed: false));

            double deltaAngle = (progressEndAngle - 180) * Math.PI / 180.0;
            double deltaWidth = Math.Abs(rect.Width / 2 * Math.Cos(deltaAngle));
            double deltaHeight = Math.Abs(rect.Width / 2 * Math.Sin(deltaAngle));
            double iconX = progressEndAngle < 270 ? rect.Left + rect.Width / 2 - deltaWidth : rect.Left + rect.Width / 2 + deltaWidth;
            double iconY = rect.Top + rect.Height / 2 - deltaHeight;
            drawingContext.PushTransform(new RotateTransform((float)IndicatorRotation, iconX, iconY));
            int iconSize = (int)((float)IconSize * density);
            DrawBody(drawingContext, card.IsSun, new Rect(iconX - iconSize / 2f, iconY - iconSize / 2f, iconSize, iconSize));
            drawingContext.Pop();
        }

        return measuredHeight;
    }

    // The part between the travelled arc and its chord, above the circle's center, fading down into the card.
    private static void DrawShadow(DrawingContext drawingContext, Rect rect, double startAngle, double progressEndAngle, Color line, Color rootColor, double bottom)
    {
        Color shadow = Color.FromArgb(51, line.R, line.G, line.B);
        Color first = Blend(shadow, rootColor);
        Color second = Blend(shadow, first);
        var brush = new LinearGradientBrush(second, rootColor, new Point(0, rect.Top), new Point(0, bottom))
        {
            MappingMode = BrushMappingMode.Absolute,
        };

        double clearFrom = rect.Left + rect.Width / 2 + rect.Width / 2 * Math.Cos((360 - progressEndAngle) * Math.PI / 180);
        var visible = new Rect(rect.Left, rect.Top, Math.Max(0, clearFrom - rect.Left), rect.Height / 2);
        drawingContext.PushClip(new RectangleGeometry(visible));
        drawingContext.DrawGeometry(brush, null, Arc(rect, startAngle, ArcAngle, closed: true));
        drawingContext.Pop();
    }

    // Opaque blend of a translucent color over another, in whole channel values.
    private static Color Blend(Color foreground, Color background)
    {
        int alpha = foreground.A;
        return Color.FromRgb(
            (byte)(background.R * (0xFF - alpha) / 0xFF + foreground.R * alpha / 0xFF),
            (byte)(background.G * (0xFF - alpha) / 0xFF + foreground.G * alpha / 0xFF),
            (byte)(background.B * (0xFF - alpha) / 0xFF + foreground.B * alpha / 0xFF));
    }

    // Angles are clockwise from three o'clock.
    private static Geometry Arc(Rect rect, double startAngle, double sweepAngle, bool closed)
    {
        double radius = rect.Width / 2;
        Point center = new(rect.Left + radius, rect.Top + radius);
        Point Along(double angle) => new(
            center.X + radius * Math.Cos(angle * Math.PI / 180), center.Y + radius * Math.Sin(angle * Math.PI / 180));

        var figure = new PathFigure { StartPoint = Along(startAngle), IsClosed = closed, IsFilled = closed };
        figure.Segments.Add(new ArcSegment(Along(startAngle + sweepAngle), new Size(radius, radius), 0, sweepAngle > 180,
            SweepDirection.Clockwise, true));
        return new PathGeometry([figure]);
    }

    private static void DrawBody(DrawingContext drawingContext, bool sun, Rect bounds)
    {
        double boundSize = Math.Min(bounds.Width, bounds.Height);
        var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        if (sun)
        {
            // Two squares turned 45° apart make the halo, under the core.
            double radius = Math.Sin(Math.PI / 4) * boundSize / 2 - 2;
            var halo = new SolidColorBrush(Color.FromRgb(249, 183, 93));
            var square = new Rect(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
            drawingContext.DrawRectangle(halo, null, square);
            drawingContext.PushTransform(new RotateTransform(45, center.X, center.Y));
            drawingContext.DrawRectangle(halo, null, square);
            drawingContext.Pop();
            drawingContext.DrawEllipse(new SolidColorBrush(Color.FromRgb(254, 214, 117)), null, center, radius, radius);
            return;
        }

        // A disc with an offset disc removed makes the crescent.
        double coreRadius = (Math.Sin(Math.PI / 4) * boundSize / 2 + boundSize / 2) / 2 - 2;
        double shaderRadius = coreRadius * 0.9050;
        var shaderCenter = new Point(center.X + coreRadius * 0.5914, center.Y - coreRadius * 0.5932);
        Geometry crescent = Geometry.Combine(
            new EllipseGeometry(center, coreRadius, coreRadius),
            new EllipseGeometry(shaderCenter, shaderRadius, shaderRadius),
            GeometryCombineMode.Exclude, null);
        drawingContext.DrawGeometry(new SolidColorBrush(Color.FromRgb(171, 202, 247)), null, crescent);
    }

    private AstroCardText Text(string value, double size, double width, bool bold = false)
    {
        var key = (value, size, width, bold);
        if (!_labels.TryGetValue(key, out AstroCardText? text))
        {
            text = new AstroCardText(value, size, width, bold);
            _labels.Add(key, text);
        }

        return text;
    }

    private sealed class CardSurface(AstroCardView owner) : FrameworkElement
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            owner.Draw(drawingContext);
        }
    }
}
