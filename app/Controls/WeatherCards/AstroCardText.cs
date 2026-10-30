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
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using LibreccioWeather.Core.Localization;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;

namespace LibreccioWeather.Controls.WeatherCards;

internal sealed class AstroCardText
{
    private static readonly FontFamily Family = new(new Uri("pack://application:,,,/"), "./assets/fonts/#Roboto");
    private readonly List<GlyphRun> _runs = [];

    public double Width { get; private set; }
    public double Height { get; private set; }
    public double Baseline { get; private set; }

    public AstroCardText(string value, double size, double maximumWidth, bool bold)
    {
        var typeface = new Typeface(Family, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Medium, FontStretches.Normal);
        var properties = new RunProperties(typeface, size);
        var paragraph = new ParagraphProperties(properties, size * (bold ? 20.0 / 16 : 20.0 / 14));
        using TextFormatter formatter = TextFormatter.Create();
        string normalized = value.Normalize();
        string text = normalized;
        int[] elements = StringInfo.ParseCombiningCharacters(text);
        int remaining = elements.Length;
        while (true)
        {
            Shape(formatter, text, properties, paragraph, size);
            if (Width <= maximumWidth || remaining == 0)
            {
                break;
            }

            remaining--;
            text = normalized[..elements[remaining]] + "\u2026";
        }

        if (Width > maximumWidth)
        {
            _runs.Clear();
            Width = 0;
        }
    }

    private void Shape(TextFormatter formatter, string text, RunProperties properties, ParagraphProperties paragraph, double size)
    {
        _runs.Clear();
        Width = 0;
        using TextLine line = formatter.FormatLine(new Source(text, properties), 0, 1_000_000, paragraph, null);
        Height = Math.Ceiling(line.Height);
        Baseline = line.Baseline;
        double addedTracking = 0;
        IEnumerable<IndexedGlyphRun> glyphRuns = line.GetIndexedGlyphRuns() ?? [];
        foreach (IndexedGlyphRun indexed in glyphRuns)
        {
            GlyphRun run = indexed.GlyphRun;
            double[] advances = run.AdvanceWidths.ToArray();
            var starts = new SortedSet<int>();
            if (run.ClusterMap is { Count: > 0 })
            {
                foreach (ushort start in run.ClusterMap)
                {
                    starts.Add(start);
                }
            }
            else
            {
                for (int i = 0; i < advances.Length; i++)
                {
                    starts.Add(i);
                }
            }

            // Keep shaping and combining marks intact when applying the label's letter spacing.
            int[] clusters = starts.ToArray();
            double tracking = size * 0.00714286;
            for (int i = 0; i < clusters.Length; i++)
            {
                int lastGlyph = (i + 1 < clusters.Length ? clusters[i + 1] : advances.Length) - 1;
                advances[lastGlyph] += tracking;
            }

            Point baseline = new(run.BaselineOrigin.X + addedTracking, run.BaselineOrigin.Y);
            _runs.Add(new GlyphRun(run.GlyphTypeface, run.BidiLevel, run.IsSideways, run.FontRenderingEmSize,
                1f, run.GlyphIndices, baseline, advances, run.GlyphOffsets, run.Characters, run.DeviceFontName,
                run.ClusterMap, run.CaretStops, run.Language));
            addedTracking += clusters.Length * tracking;
        }

        Width = line.WidthIncludingTrailingWhitespace + addedTracking;
    }

    public void Draw(DrawingContext drawingContext, Brush brush, Point topLeft)
    {
        // Glyph-run origins are relative to the baseline, while callers position the top of the line.
        drawingContext.PushTransform(new TranslateTransform(topLeft.X, topLeft.Y + Baseline));
        foreach (GlyphRun run in _runs)
        {
            drawingContext.DrawGlyphRun(brush, run);
        }

        drawingContext.Pop();
    }

    private sealed class RunProperties(Typeface typeface, double size) : TextRunProperties
    {
        public override Typeface Typeface => typeface;
        public override double FontRenderingEmSize => size;
        public override double FontHintingEmSize => size;
        public override TextDecorationCollection TextDecorations { get; } = new();
        public override Brush ForegroundBrush => Brushes.Black;
        public override Brush BackgroundBrush => null!;
        public override CultureInfo CultureInfo => Strings.Culture;
        public override TextEffectCollection TextEffects { get; } = new();
    }

    private sealed class ParagraphProperties(RunProperties properties, double lineHeight) : TextParagraphProperties
    {
        public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
        public override TextAlignment TextAlignment => TextAlignment.Left;
        public override double LineHeight => lineHeight;
        public override bool FirstLineInParagraph => true;
        public override TextRunProperties DefaultTextRunProperties => properties;
        public override TextWrapping TextWrapping => TextWrapping.NoWrap;
        public override TextMarkerProperties TextMarkerProperties => null!;
        public override double Indent => 0;
    }

    private sealed class Source(string text, RunProperties properties) : TextSource
    {
        public override TextRun GetTextRun(int textSourceCharacterIndex) =>
            textSourceCharacterIndex < text.Length
                ? new TextCharacters(text, textSourceCharacterIndex, text.Length - textSourceCharacterIndex, properties)
                : new TextEndOfParagraph(1);

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
            new(textSourceCharacterIndexLimit,
                new CultureSpecificCharacterBufferRange(Strings.Culture, new CharacterBufferRange(text, 0, textSourceCharacterIndexLimit)));

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) =>
            textSourceCharacterIndex;
    }
}
