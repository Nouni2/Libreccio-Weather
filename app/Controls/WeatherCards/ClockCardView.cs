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

using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Threading;
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Display;
using LibreccioWeather.ViewModels.WeatherCards;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls.WeatherCards;

/// <summary>
/// The clock card, laid out in physical pixels as Android lays out its views: the scalloped shape, the hour above the
/// minutes, each text auto-sized to its half of the column, and Android's analog clock hands over them.
/// </summary>
public sealed class ClockCardView : FrameworkElement
{
    // The guidelines of container_main_clock.xml, as fractions of the square.
    private const float GuideTop = 0.1f;
    private const float GuideBottom = 0.9f;
    private const float GuideStart = 0.2f;
    private const float GuideEnd = 0.8f;

    // In sp: the text appearance's size, then the auto-size range and step of both text clocks.
    private const float InitialTextSize = 50;
    private const float MinTextSize = 50;
    private const float MaxTextSize = 130;
    private const float TextSizeStep = 1;

    // clock_shape.xml, in its 176 by 176 viewport.
    private const double ShapeViewport = 176;

    // The intrinsic size in dp of AnalogClock's dial and hands, and the viewport of their vectors.
    private const int DialSize = 380;

    // config_defaultAnalogClockSecondsHandFps: the hands' seconds are rounded to whole seconds.
    private const int SecondsHandFps = 1;

    private static readonly Geometry Shape = Frozen(Geometry.Parse(
        "F1 M77.55,6.78C81.33,4.32 83.23,3.1 85.26,2.62C87.06,2.2 88.94,2.2 90.74,2.62C92.77,3.1 94.67,4.32 98.45,6.78L102.86,9.64"
        + "C104.8,10.9 105.77,11.53 106.82,11.95C107.75,12.33 108.73,12.6 109.72,12.73C110.84,12.89 112,12.83 114.31,12.71"
        + "L119.56,12.43C124.07,12.2 126.32,12.08 128.32,12.69C130.09,13.22 131.71,14.16 133.06,15.43C134.59,16.86 135.61,18.87 137.66,22.88"
        + "L140.05,27.57C141.1,29.63 141.63,30.66 142.32,31.55C142.94,32.35 143.65,33.06 144.45,33.68C145.34,34.37 146.37,34.9 148.43,35.95"
        + "L153.12,38.34C157.13,40.39 159.14,41.41 160.57,42.94C161.84,44.29 162.78,45.91 163.31,47.68C163.92,49.68 163.8,51.93 163.57,56.44"
        + "L163.29,61.69C163.17,64 163.11,65.16 163.27,66.28C163.4,67.27 163.67,68.25 164.04,69.18C164.47,70.23 165.1,71.2 166.36,73.14"
        + "L169.22,77.55C171.68,81.33 172.9,83.23 173.38,85.26C173.8,87.06 173.8,88.94 173.38,90.74C172.9,92.77 171.68,94.67 169.22,98.45"
        + "L166.36,102.86C165.1,104.8 164.47,105.77 164.04,106.82C163.67,107.75 163.4,108.73 163.27,109.72C163.11,110.84 163.17,112 163.29,114.31"
        + "L163.57,119.56C163.8,124.07 163.92,126.32 163.31,128.32C162.78,130.09 161.84,131.71 160.57,133.06C159.14,134.59 157.13,135.61 153.12,137.66"
        + "L148.43,140.05C146.37,141.1 145.34,141.63 144.45,142.32C143.65,142.94 142.94,143.65 142.32,144.45C141.63,145.34 141.1,146.37 140.05,148.43"
        + "L137.66,153.12C135.61,157.13 134.59,159.14 133.06,160.57C131.71,161.84 130.09,162.78 128.32,163.31C126.32,163.92 124.07,163.8 119.56,163.57"
        + "L114.31,163.29C112,163.17 110.84,163.11 109.72,163.27C108.73,163.4 107.75,163.67 106.82,164.04C105.77,164.47 104.8,165.1 102.86,166.36"
        + "L98.45,169.22C94.67,171.68 92.77,172.9 90.74,173.38C88.94,173.8 87.06,173.8 85.26,173.38C83.23,172.9 81.33,171.68 77.55,169.22"
        + "L73.14,166.36C71.2,165.1 70.23,164.47 69.18,164.04C68.25,163.67 67.27,163.4 66.28,163.27C65.16,163.11 64,163.17 61.69,163.29"
        + "L56.44,163.57C51.93,163.8 49.68,163.92 47.68,163.31C45.91,162.78 44.29,161.84 42.94,160.57C41.41,159.14 40.39,157.13 38.34,153.12"
        + "L35.95,148.43C34.9,146.37 34.37,145.34 33.68,144.45C33.06,143.65 32.35,142.94 31.55,142.32C30.66,141.63 29.63,141.1 27.57,140.05"
        + "L22.88,137.66C18.87,135.61 16.86,134.59 15.43,133.06C14.16,131.71 13.22,130.09 12.69,128.32C12.08,126.32 12.2,124.07 12.43,119.56"
        + "L12.71,114.31C12.83,112 12.89,110.84 12.73,109.72C12.6,108.73 12.33,107.75 11.95,106.82C11.53,105.77 10.9,104.8 9.64,102.86"
        + "L6.78,98.45C4.32,94.67 3.1,92.77 2.62,90.74C2.2,88.94 2.2,87.06 2.62,85.26C3.1,83.23 4.32,81.33 6.78,77.55"
        + "L9.64,73.14C10.9,71.2 11.53,70.23 11.95,69.18C12.33,68.25 12.6,67.27 12.73,66.28C12.89,65.16 12.83,64 12.71,61.69"
        + "L12.43,56.44C12.2,51.93 12.08,49.68 12.69,47.68C13.22,45.91 14.16,44.29 15.43,42.94C16.86,41.41 18.87,40.39 22.88,38.34"
        + "L27.57,35.95C29.63,34.9 30.66,34.37 31.55,33.68C32.35,33.06 33.06,32.35 33.68,31.55C34.37,30.66 34.9,29.63 35.95,27.57"
        + "L38.34,22.88C40.39,18.87 41.41,16.86 42.94,15.43C44.29,14.16 45.91,13.22 47.68,12.69C49.68,12.08 51.93,12.2 56.44,12.43"
        + "L61.69,12.71C64,12.83 65.16,12.89 66.28,12.73C67.27,12.6 68.25,12.33 69.18,11.95C70.23,11.53 71.2,10.9 73.14,9.64L77.55,6.78Z"));

    // clock_hand_hour.xml and clock_hand_minute.xml, both turning around the viewport's center.
    private static readonly Geometry HourHand = Frozen(Geometry.Parse(
        "F1 M190,96L190,96A16,16 0 0 1 206,112L206,190A16,16 0 0 1 190,206L190,206A16,16 0 0 1 174,190L174,112A16,16 0 0 1 190,96z"));

    private static readonly Geometry MinuteHand = Frozen(Geometry.Parse(
        "F1 M190,60L190,60A16,16 0 0 1 206,76L206,190A16,16 0 0 1 190,206L190,206A16,16 0 0 1 174,190L174,76A16,16 0 0 1 190,60z"));

    private static readonly Lazy<ClockFont> Font = new(ClockFont.Load);

    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(ClockCardViewModel), typeof(ClockCardView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataChanged));

    private readonly DispatcherTimer _ticker = new();

    // The time both clocks show, read when they are attached or bound and at each minute, as Android's text clocks and
    // analog clock read it.
    private DateTimeOffset _shownTime = DateTimeOffset.UtcNow;

    // A text view keeps its auto-sized text size, and the column measures each text at it before giving the two texts
    // their share of the height, so the sizes carry over from one layout to the next.
    private float? _hourTextSize;
    private float? _minuteTextSize;
    private int _hourBoxHeight;
    private int _minuteBoxHeight;
    private int _measuredSize = -1;
    private float _measuredSpToPx;
    private float _sizesSpToPx;
    private int[] _textSizes = [];

    public ClockCardView()
    {
        _ticker.Tick += (_, _) => Tick();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += (_, _) => ScheduleTick();
    }

    public ClockCardViewModel? Data
    {
        get => (ClockCardViewModel?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FrameworkElementAutomationPeer(this);
    }

    private static void OnDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (ClockCardView)sender;
        AutomationProperties.SetName(view, (args.NewValue as ClockCardViewModel)?.AutomationName ?? "");

        // Binding sets the clocks' time zone, which reads the time again and lays the card out again.
        view._shownTime = DateTimeOffset.UtcNow;
        view._measuredSize = -1;
        view.ScheduleTick();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.TimeChanged += OnSystemTimeChanged;
        _shownTime = DateTimeOffset.UtcNow;
        ScheduleTick();
        InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SystemEvents.TimeChanged -= OnSystemTimeChanged;
        _ticker.Stop();
    }

    // Both clocks tick again when the system time is set.
    private void OnSystemTimeChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(Tick));
    }

    private void Tick()
    {
        _shownTime = DateTimeOffset.UtcNow;
        ScheduleTick();
        InvalidateVisual();
    }

    // Without a seconds field or a second hand, both clocks tick at the start of each minute in the location's time
    // zone, and only while the card is visible.
    private void ScheduleTick()
    {
        _ticker.Stop();
        if (!IsVisible || Data is not { } clock)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset local = TimeZoneInfo.ConvertTime(now, clock.TimeZone);
        DateTimeOffset nextMinute = new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0, local.Offset)
            .AddMinutes(1);
        TimeSpan delay = nextMinute - now;
        _ticker.Interval = delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
        _ticker.Start();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Data is not { } clock || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        float density = (float)VisualTreeHelper.GetDpi(this).PixelsPerDip;
        int size = (int)Math.Round(Math.Min(ActualWidth, ActualHeight) * density);

        // Neither the hands nor the digits mirror in a right-to-left language.
        bool mirrored = FlowDirection == FlowDirection.RightToLeft;
        if (mirrored)
        {
            drawingContext.PushTransform(new ScaleTransform(-1, 1, ActualWidth / 2, 0));
        }

        // From here on, one unit is one physical pixel.
        drawingContext.PushTransform(new ScaleTransform(1 / density, 1 / density));

        // The shape's image view fits the vector to the square.
        drawingContext.PushTransform(new ScaleTransform(size / ShapeViewport, size / ShapeViewport));
        drawingContext.DrawGeometry(ResourceBrush("SurfaceBrush"), null, Shape);
        drawingContext.Pop();

        DrawTime(drawingContext, clock, size, density);
        DrawHands(drawingContext, clock, size, density);

        drawingContext.Pop();
        if (mirrored)
        {
            drawingContext.Pop();
        }
    }

    private void DrawTime(DrawingContext drawingContext, ClockCardViewModel clock, int size, float density)
    {
        ClockFont font = Font.Value;
        float spToPx = density * TextScale.Factor;
        int top = Guide(GuideTop, size);
        int bottom = Guide(GuideBottom, size);
        int start = Guide(GuideStart, size);
        int end = Guide(GuideEnd, size);
        int columnWidth = end - start;
        int columnHeight = bottom - top;
        float initial = DimensionPixelSize(InitialTextSize * spToPx);

        if (_measuredSize != size || _measuredSpToPx != spToPx)
        {
            // Both texts are measured at their current size, then share what is left of the column's height equally,
            // the first taking the share rounded towards zero, as a vertical LinearLayout shares it by weight.
            int hourHeight = font.LineHeight(_hourTextSize ?? initial);
            int minuteHeight = font.LineHeight(_minuteTextSize ?? initial);
            int excess = columnHeight - hourHeight - minuteHeight;
            int hourShare = (int)(1f * excess / 2f);
            _hourBoxHeight = hourHeight + hourShare;
            _minuteBoxHeight = minuteHeight + excess - hourShare;
            _measuredSize = size;
            _measuredSpToPx = spToPx;
        }

        string hour = DateFormatting.FormatClockField(_shownTime, clock.TimeZone, DateFormatting.Is12Hour ? "hh" : "HH");
        string minute = DateFormatting.FormatClockField(_shownTime, clock.TimeZone, "mm");
        int[] sizes = TextSizes(spToPx);
        _hourTextSize = AutoSize(font, hour, _hourTextSize ?? initial, sizes, columnWidth, _hourBoxHeight);
        _minuteTextSize = AutoSize(font, minute, _minuteTextSize ?? initial, sizes, columnWidth, _minuteBoxHeight);

        Brush brush = ResourceBrush("PrimaryBrush");
        DrawText(drawingContext, font, hour, _hourTextSize.Value, start, top, columnWidth, _hourBoxHeight, alignBottom: true, brush);
        DrawText(drawingContext, font, minute, _minuteTextSize.Value, start, top + _hourBoxHeight, columnWidth, _minuteBoxHeight,
            alignBottom: false, brush);
    }

    // A text view's line, centered and aligned to the bottom or the top of its box, and clipped to the box.
    private static void DrawText(DrawingContext drawingContext, ClockFont font, string text, float textSize, int left, int top,
        int width, int height, bool alignBottom, Brush brush)
    {
        if (font.Outline(text, textSize) is not { } outline)
        {
            return;
        }

        int lineHeight = font.LineHeight(textSize);
        int verticalOffset = alignBottom && lineHeight < height ? height - lineHeight : 0;

        // Layout centers a line on its width truncated to an even number of pixels, with the remainder rounded down.
        int lineWidth = (int)font.Measure(text, textSize) & ~1;
        int x = left + ((width - lineWidth) >> 1);
        int baseline = top + verticalOffset + font.Ascent(textSize);
        drawingContext.PushClip(new RectangleGeometry(new Rect(left, top, Math.Max(0, width), Math.Max(0, height))));
        drawingContext.PushTransform(new TranslateTransform(x, baseline));
        drawingContext.DrawGeometry(brush, null, outline);
        drawingContext.Pop();
        drawingContext.Pop();
    }

    private void DrawHands(DrawingContext drawingContext, ClockCardViewModel clock, int size, float density)
    {
        if (clock.HourHandColor is not { } hourColor || clock.MinuteHandColor is not { } minuteColor)
        {
            return;
        }

        DateTimeOffset local = TimeZoneInfo.ConvertTime(_shownTime, clock.TimeZone);
        float rawSeconds = local.Second + local.Ticks % TimeSpan.TicksPerSecond / (float)TimeSpan.TicksPerSecond;
        float seconds = JavaRound(rawSeconds * SecondsHandFps) / (float)SecondsHandFps;
        float minutes = local.Minute + seconds / 60.0f;
        float hours = local.Hour + minutes / 60.0f;

        // The vectors' intrinsic size, truncated to whole pixels; the clock shrinks them around its center only when it
        // is smaller than the dial, by the same factor both ways since the card is square.
        int dial = (int)(DialSize * density);
        int x = size / 2;
        int y = size / 2;
        float scale = size < dial ? (float)size / dial : 1;
        DrawHand(drawingContext, HourHand, hourColor, hours / 12.0f * 360.0f, dial, x, y, scale);
        DrawHand(drawingContext, MinuteHand, minuteColor, minutes / 60.0f * 360.0f, dial, x, y, scale);
    }

    private static void DrawHand(DrawingContext drawingContext, Geometry hand, Color color, float angle, int dial,
        int x, int y, float scale)
    {
        // The hand's bounds are centered on the clock with its intrinsic size halved in whole pixels on each side.
        int half = dial / 2;
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(2.0 * half / DialSize, 2.0 * half / DialSize));
        transform.Children.Add(new TranslateTransform(x - half, y - half));
        transform.Children.Add(new RotateTransform(angle, x, y));
        if (scale != 1)
        {
            transform.Children.Add(new ScaleTransform(scale, scale, x, y));
        }

        drawingContext.PushTransform(transform);
        drawingContext.DrawGeometry(new SolidColorBrush(color), null, hand);
        drawingContext.Pop();
    }

    // A percent guideline sits at the fraction of the parent rounded to the nearest pixel.
    private static int Guide(float fraction, int size)
    {
        return (int)(0.5f + fraction * size);
    }

    // The text sizes a uniform auto-size can take, in whole pixels, ascending and without repeats.
    private int[] TextSizes(float spToPx)
    {
        if (_sizesSpToPx == spToPx && _textSizes.Length > 0)
        {
            return _textSizes;
        }

        float min = MinTextSize * spToPx;
        float max = MaxTextSize * spToPx;
        float step = TextSizeStep * spToPx;
        int count = (int)MathF.Floor((max - min) / step) + 1;
        _textSizes = Enumerable.Range(0, count)
            .Select(i => JavaRound(min + (i * step)))
            .Where(textSize => textSize > 0)
            .Distinct()
            .Order()
            .ToArray();
        _sizesSpToPx = spToPx;
        return _textSizes;
    }

    // The search of TextView.findLargestTextSizeWhichFits, kept as it is: a size that fits records the lower bound of
    // the range it searched, not the size itself.
    private static float AutoSize(ClockFont font, string text, float current, int[] sizes, int width, int height)
    {
        if (width <= 0 || height <= 0 || sizes.Length == 0)
        {
            return current;
        }

        int bestSizeIndex = 0;
        int lowIndex = bestSizeIndex + 1;
        int highIndex = sizes.Length - 1;
        while (lowIndex <= highIndex)
        {
            int sizeToTryIndex = (lowIndex + highIndex) / 2;
            if (Fits(font, text, sizes[sizeToTryIndex], width, height))
            {
                bestSizeIndex = lowIndex;
                lowIndex = sizeToTryIndex + 1;
            }
            else
            {
                highIndex = sizeToTryIndex - 1;
                bestSizeIndex = highIndex;
            }
        }

        return sizes[bestSizeIndex];
    }

    // One line only, since a wider text breaks onto a second line, and no taller than the box.
    private static bool Fits(ClockFont font, string text, int textSize, int width, int height)
    {
        return font.Measure(text, textSize) <= width && font.LineHeight(textSize) <= height;
    }

    // Resources.getDimensionPixelSize: rounded half away from zero, and at least one pixel for a non-zero size.
    private static int DimensionPixelSize(float value)
    {
        int result = (int)(value >= 0 ? value + 0.5f : value - 0.5f);
        return result != 0 ? result : value == 0 ? 0 : value > 0 ? 1 : -1;
    }

    // Java's Math.round(float): the nearest integer, halves rounded up. Adding 0.5 first would turn a float just below a
    // half into a tie.
    private static int JavaRound(float value)
    {
        float floor = MathF.Floor(value);
        return (int)(value - floor >= 0.5f ? floor + 1 : floor);
    }

    private Brush ResourceBrush(string key)
    {
        return (Brush)FindResource(key);
    }

    private static Geometry Frozen(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// Roboto Flex as the clock's text appearance sets it on Android 9 and later: weight 700, thin stroke 175 and
    /// counter width 603.
    /// </summary>
    private sealed class ClockFont
    {
        private static readonly Uri FontUri = new("pack://application:,,,/assets/fonts/robotoflex.ttf", UriKind.Absolute);

        // Without DirectWrite's variable fonts, or for a character Roboto Flex lacks, WPF draws the text at Roboto Flex's
        // default axis values in bold, or in a system font that has the character.
        private static readonly Typeface FallbackTypeface = new(
            new FontFamily(new Uri("pack://application:,,,/"), "./assets/fonts/#Roboto Flex, Global User Interface"),
            FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private readonly VariableFontInstance? _instance;
        private readonly int _unitsPerEm;
        private readonly int _ascender;
        private readonly int _descender;

        private ClockFont(VariableFontInstance? instance, int unitsPerEm, int ascender, int descender)
        {
            _instance = instance;
            _unitsPerEm = unitsPerEm;
            _ascender = ascender;
            _descender = descender;
        }

        public static ClockFont Load()
        {
            using Stream stream = Application.GetResourceStream(FontUri)?.Stream
                ?? throw new InvalidDataException("The clock font is missing from the app's resources.");
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            byte[] data = copy.ToArray();
            (int unitsPerEm, int ascender, int descender) = ReadLineMetrics(data);
            VariableFontInstance? instance = VariableFontInstance.Create(data, [("wght", 700), ("XOPQ", 175), ("XTRA", 603)]);
            return new ClockFont(instance, unitsPerEm, ascender, descender);
        }

        // StaticLayout, with line spacing from fallback fonts, takes of Paint's integer ascent, rounded by Skia's
        // floor(x + 0.5), and the line's ascent rounded by Math.round the one further from the baseline; both start from
        // the same scaled value. The result is the baseline's distance below the line's top.
        public int Ascent(float textSize)
        {
            float ascent = -textSize * _ascender / _unitsPerEm;
            return -Math.Min(SkiaRound(ascent), JavaRound(ascent));
        }

        // A single line without font padding spans the ascent and the descent, chosen the same way.
        public int LineHeight(float textSize)
        {
            float descent = textSize * -_descender / _unitsPerEm;
            return Ascent(textSize) + Math.Max(SkiaRound(descent), JavaRound(descent));
        }

        private static int SkiaRound(float value)
        {
            return (int)MathF.Floor(value + 0.5f);
        }

        public double Measure(string text, float textSize)
        {
            return _instance?.Measure(text, textSize) ?? Fallback(text, textSize).WidthIncludingTrailingWhitespace;
        }

        public Geometry? Outline(string text, float textSize)
        {
            if (_instance?.Outline(text, textSize) is { } outline)
            {
                return outline;
            }

            FormattedText formatted = Fallback(text, textSize);
            Geometry geometry = formatted.BuildGeometry(new Point(0, -formatted.Baseline));
            geometry.Freeze();
            return geometry;
        }

        private static FormattedText Fallback(string text, float textSize)
        {
            return new FormattedText(text, Strings.Culture, FlowDirection.LeftToRight, FallbackTypeface, textSize, Brushes.Black, 1);
        }

        // The line metrics come from the hhea table, which the font's variations leave unchanged, as Paint's metrics
        // read them; its typo metrics hold the same values.
        private static (int UnitsPerEm, int Ascender, int Descender) ReadLineMetrics(byte[] font)
        {
            ReadOnlySpan<byte> data = font;
            int tableCount = BinaryPrimitives.ReadUInt16BigEndian(data[4..]);
            int head = -1;
            int hhea = -1;
            for (int i = 0; i < tableCount; i++)
            {
                ReadOnlySpan<byte> record = data.Slice(12 + (16 * i), 16);
                int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                if (record[..4].SequenceEqual("head"u8))
                {
                    head = offset;
                }
                else if (record[..4].SequenceEqual("hhea"u8))
                {
                    hhea = offset;
                }
            }

            if (head < 0 || hhea < 0)
            {
                throw new InvalidDataException("The clock font has no head or hhea table.");
            }

            return (BinaryPrimitives.ReadUInt16BigEndian(data[(head + 18)..]),
                BinaryPrimitives.ReadInt16BigEndian(data[(hhea + 4)..]),
                BinaryPrimitives.ReadInt16BigEndian(data[(hhea + 6)..]));
        }
    }
}
