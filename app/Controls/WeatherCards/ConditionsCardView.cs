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
using System.Windows.Media;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.ViewModels.WeatherCards;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.WeatherCards;

public sealed class ConditionsCardView : FrameworkElement
{
    private const string Digits = "0123456789٠١٢٣٤٥٦٧٨٩০১২৩৪৫৬৭৮৯०१२३४५६७८९۰۱۲۳۴۵۶۷۸۹௦௧௨௩௪௫௬௭௮௯";

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(ConditionsCardViewModel), typeof(ConditionsCardView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

    public ConditionsCardViewModel? Data
    {
        get => (ConditionsCardViewModel?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FrameworkElementAutomationPeer(this);
    }

    private static void OnDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (ConditionsCardView)sender;
        AutomationProperties.SetName(view, (args.NewValue as ConditionsCardViewModel)?.Description ?? "");
        view.ToolTip = (args.NewValue as ConditionsCardViewModel)?.Description;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Data is not { } card || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        double size = Math.Min(ActualWidth, ActualHeight);
        bool rectangular = card.Kind is ConditionsCardKind.Humidity or ConditionsCardKind.Precipitation or ConditionsCardKind.Pollen;
        double corner = rectangular ? 28 : size / 2;
        var bounds = new Rect(0, 0, size, size);
        drawingContext.PushClip(new RectangleGeometry(bounds, corner, corner));
        if (card.Kind != ConditionsCardKind.UV)
        {
            drawingContext.DrawRoundedRectangle(ResourceBrush("SurfaceContainerBrush"), null, bounds, corner, corner);
        }

        if (card.ArtworkKey is { } key && TryFindResource(key) is ImageSource artwork)
        {
            drawingContext.PushTransform(new RotateTransform(card.Rotation, size / 2, size / 2));
            drawingContext.DrawImage(artwork, bounds);
            drawingContext.Pop();
        }

        if (card.GaugeProgress is { } progress)
        {
            Color color = card.GaugeColor ?? ((SolidColorBrush)ResourceBrush("PrimaryBrush")).Color;
            var background = new SolidColorBrush(Color.FromArgb(25, color.R, color.G, color.B));
            DrawArc(drawingContext, size, 270, background);
            if (progress > 0)
            {
                DrawArc(drawingContext, size, progress / card.GaugeMaximum * 270, new SolidColorBrush(color));
            }
        }

        double inset = size * (card.Kind == ConditionsCardKind.Pollen ? 0.1 : rectangular ? 0.08 : 0.2);
        double contentWidth = size - 2 * inset;
        Brush foreground = ResourceBrush("OnSurfaceBrush");
        FormattedText title = Text(card.Title, 16, foreground, true);
        double iconSize = title.Height;
        title.MaxTextWidth = Math.Max(1, contentWidth - iconSize - 4);
        title.Trimming = TextTrimming.CharacterEllipsis;
        double titleX = rectangular ? inset + iconSize + 4 : (size - title.Width - iconSize - 4) / 2 + iconSize + 4;
        double titleY = size * (card.Kind == ConditionsCardKind.Pollen ? 0.1 : rectangular ? 0.08 : 0.2);
        if (TryFindResource(card.IconKey) is ImageSource icon)
        {
            drawingContext.DrawImage(icon, new Rect(titleX - iconSize - 4, titleY, iconSize, iconSize));
        }

        drawingContext.DrawText(title, new Point(titleX, titleY));
        if (card.PollenItems is { } pollens)
        {
            for (int i = 0; i < pollens.Count; i++)
            {
                PollenCardItem pollen = pollens[i];
                double top = size * (i == 0 ? 0.3 : 0.6) + 8;
                drawingContext.DrawEllipse(new SolidColorBrush(pollen.Color), null, new Point(inset + 12, top + 20), 10, 10);
                if (pollen.Name is { } name)
                {
                    FormattedText label = Text(name, 16, foreground);
                    label.MaxTextWidth = Math.Max(1, size - 2 * inset - 32);
                    label.MaxLineCount = 1;
                    label.Trimming = TextTrimming.CharacterEllipsis;
                    drawingContext.DrawText(label, new Point(inset + 32, top));
                }

                FormattedText level = Text(pollen.Level ?? "", 14, ResourceBrush("OnSurfaceVariantBrush"));
                level.MaxTextWidth = Math.Max(1, size - 2 * inset - 32);
                level.MaxLineCount = 1;
                level.Trimming = TextTrimming.CharacterEllipsis;
                drawingContext.DrawText(level, new Point(inset + 32, top + (pollen.Name is null ? 10 : 20)));
            }

            drawingContext.Pop();
            return;
        }

        double valueSize = card.Kind == ConditionsCardKind.Visibility ? 48 : 45;
        double valueInset = card.Kind is ConditionsCardKind.Visibility or ConditionsCardKind.Pressure ? 0 : inset;
        FormattedText value = Text(card.ValueText, valueSize, foreground);
        if (card.HalfSizeUnits)
        {
            int first = card.ValueText.IndexOfAny(Digits.ToCharArray());
            int last = card.ValueText.LastIndexOfAny(Digits.ToCharArray());
            if (first >= 0 && last >= 0)
            {
                if (first > 0)
                {
                    value.SetFontSize(valueSize / 2, 0, first);
                }

                if (last < card.ValueText.Length - 1)
                {
                    value.SetFontSize(valueSize / 2, last + 1, card.ValueText.Length - last - 1);
                }
            }
        }

        if (card.Kind is ConditionsCardKind.Visibility or ConditionsCardKind.Precipitation
            && value.Width > size - 2 * valueInset)
        {
            double scale = (size - 2 * valueInset) / value.Width;
            drawingContext.PushTransform(new ScaleTransform(scale, scale, size / 2, size / 2));
            drawingContext.DrawText(value, new Point((size - value.Width) / 2, (size - value.Height) / 2));
            drawingContext.Pop();
        }
        else
        {
            value.MaxTextWidth = Math.Max(1, size - 2 * valueInset);
            value.MaxLineCount = 1;
            value.TextAlignment = rectangular ? TextAlignment.Left : TextAlignment.Center;
            value.Trimming = TextTrimming.CharacterEllipsis;
            drawingContext.DrawText(value, new Point(valueInset, (size - value.Height) / 2));
        }

        if (card.Kind == ConditionsCardKind.Humidity)
        {
            double radius = size * 0.1;
            var center = new Point(inset + radius, size * 0.82);
            drawingContext.DrawEllipse(ResourceBrush("TertiaryBrush"), null, center, radius, radius);
            FormattedText dewPoint = Text(card.DewPointText ?? "", 16, ResourceBrush("OnTertiaryBrush"));
            drawingContext.DrawText(dewPoint, new Point(center.X - dewPoint.Width / 2, center.Y - dewPoint.Height / 2));
            FormattedText label = Text(card.SupportingText ?? "", 14, ResourceBrush("OnTertiaryContainerBrush"));
            label.MaxTextWidth = Math.Max(1, size - inset - center.X - radius - 8);
            drawingContext.DrawText(label, new Point(center.X + radius + 8, center.Y - label.Height / 2));
        }
        else if (!string.IsNullOrEmpty(card.SupportingText))
        {
            FormattedText detail = Text(card.SupportingText, 14,
                ResourceBrush(card.Kind == ConditionsCardKind.Precipitation ? "OnSurfaceVariantBrush" : "OnSurfaceBrush"));
            detail.MaxTextWidth = Math.Max(1, contentWidth);
            detail.TextAlignment = rectangular ? TextAlignment.Left : TextAlignment.Center;
            detail.MaxLineCount = card.Kind == ConditionsCardKind.Precipitation ? 3 : 2;
            detail.Trimming = TextTrimming.CharacterEllipsis;
            drawingContext.DrawText(detail, new Point(inset, size * (card.Kind == ConditionsCardKind.UV ? 0.77 : 0.8) - detail.Height));
        }

        drawingContext.Pop();
    }

    private void DrawArc(DrawingContext context, double size, double sweep, Brush brush)
    {
        double radius = (size - 10) / 2 - 8;
        var center = new Point(size / 2, size / 2);
        var pen = new Pen(brush, 8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (sweep >= 360)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        const double Start = 135 * Math.PI / 180;
        double end = (135 + sweep) * Math.PI / 180;
        var startPoint = new Point(center.X + radius * Math.Cos(Start), center.Y + radius * Math.Sin(Start));
        var endPoint = new Point(center.X + radius * Math.Cos(end), center.Y + radius * Math.Sin(end));
        var geometry = new StreamGeometry();
        using (StreamGeometryContext path = geometry.Open())
        {
            path.BeginFigure(startPoint, false, false);
            path.ArcTo(endPoint, new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true, false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    private Brush ResourceBrush(string key)
    {
        return (Brush)FindResource(key);
    }

    private FormattedText Text(string value, double size, Brush brush, bool bold = false)
    {
        var typeface = new Typeface(System.Windows.SystemFonts.MessageFontFamily,
            FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        return new FormattedText(value, Strings.Culture, FlowDirection, typeface, size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }
}
