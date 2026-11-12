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

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace LibreccioWeather.Localization;

/// <summary>
/// Orders text as Android does: a text takes its base direction from its first strong letter, so English text in an
/// Arabic layout reads "Follow system (Dark)". Without a strong letter, a text view takes the layout's direction and
/// canvas and chart text is left to right.
/// </summary>
/// <remarks>
/// WPF orders text by its element's flow direction alone, and its text engine ignores Unicode's embedding characters.
/// Text whose first strong letter runs the other way is put between two marks of that direction, which are strong
/// letters themselves: the punctuation at its ends takes its direction, as Android orders it, while it stays aligned
/// with the layout.
/// </remarks>
public static class BidiText
{
    private const char LeftToRightMark = (char)0x200E;
    private const char RightToLeftMark = (char)0x200F;

    // A text block's text and direction, bound on the block itself, so each change orders the text again. The binding
    // lives on the block, which keeps nothing alive once the block is gone.
    private static readonly DependencyProperty WatchedProperty = DependencyProperty.RegisterAttached(
        "Watched", typeof(string), typeof(BidiText),
        new PropertyMetadata(null, (element, _) => Apply((TextBlock)element)));

    /// <summary>
    /// The text as it is shown in a paragraph of the given direction, between two marks of its own direction when that
    /// direction, or the fallback without a strong letter, differs.
    /// </summary>
    public static string Embed(string text, FlowDirection paragraph, FlowDirection fallback)
    {
        FlowDirection direction = FirstStrong(text) ?? fallback;
        if (direction == paragraph || text.Length == 0)
        {
            return text;
        }

        char mark = direction == FlowDirection.LeftToRight ? LeftToRightMark : RightToLeftMark;
        return mark + text + mark;
    }

    /// <summary>
    /// The direction of the first strong letter: right to left for Hebrew, Arabic, Syriac, Thaana, N'Ko and the other
    /// right-to-left scripts, left to right for any other letter, or null when there is none.
    /// </summary>
    public static FlowDirection? FirstStrong(string text)
    {
        foreach (Rune rune in text.EnumerateRunes())
        {
            int value = rune.Value;
            if (value is 0x200E)
            {
                return FlowDirection.LeftToRight;
            }

            if (value is 0x200F or 0x061C)
            {
                return FlowDirection.RightToLeft;
            }

            if (!Rune.IsLetter(rune))
            {
                continue;
            }

            return IsRightToLeftScript(value) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        return null;
    }

    /// <summary>
    /// Puts the runs that make up one text between the marks its first strong letter asks for, as Embed does for a
    /// string.
    /// </summary>
    public static void EmbedInlines(InlineCollection inlines, string text, FlowDirection paragraph)
    {
        string ordered = Embed(text, paragraph, paragraph);
        if (ordered.Length == text.Length || inlines.FirstInline is not { } first)
        {
            return;
        }

        inlines.InsertBefore(first, new Run(ordered[0].ToString()));
        inlines.Add(new Run(ordered[0].ToString()));
    }

    /// <summary>
    /// Orders every plain text block of the app by its first strong letter. Text blocks built from runs order their
    /// runs themselves.
    /// </summary>
    /// <remarks>
    /// WPF raises Loaded only on elements with a Loaded handler of their own, so a class handler misses most text
    /// blocks; every element raises SizeChanged once it is laid out.
    /// </remarks>
    public static void Register()
    {
        EventManager.RegisterClassHandler(typeof(TextBlock), FrameworkElement.SizeChangedEvent,
            new SizeChangedEventHandler(OnSizeChanged), true);
    }

    private static bool IsRightToLeftScript(int value)
    {
        return value is (>= 0x0590 and <= 0x08FF) or (>= 0xFB1D and <= 0xFDFF) or (>= 0xFE70 and <= 0xFEFF)
            or (>= 0x10800 and <= 0x10FFF) or (>= 0x1E800 and <= 0x1EFFF);
    }

    // Only plain text blocks: a derived block, or the one an access key text builds, manages its runs itself.
    private static bool IsPlain(TextBlock block)
    {
        return block.GetType() == typeof(TextBlock) && block.TemplatedParent is not AccessText
            && System.Windows.Media.VisualTreeHelper.GetParent(block) is not AccessText;
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not TextBlock block || !IsPlain(block) || BindingOperations.IsDataBound(block, WatchedProperty))
        {
            return;
        }

        var watch = new MultiBinding { Converter = WatchedText.Instance };
        watch.Bindings.Add(new Binding(nameof(TextBlock.Text)) { Source = block });
        watch.Bindings.Add(new Binding(nameof(FrameworkElement.FlowDirection)) { Source = block });
        BindingOperations.SetBinding(block, WatchedProperty, watch);
    }

    // The current value is set, so a binding keeps feeding the block and each new text is ordered again.
    private static void Apply(TextBlock block)
    {
        string shown = block.Text;
        string text = shown.Length >= 2 && shown[0] is LeftToRightMark or RightToLeftMark && shown[^1] == shown[0]
            ? shown[1..^1]
            : shown;
        string ordered = Embed(text, block.FlowDirection, block.FlowDirection);
        if (ordered != shown)
        {
            block.SetCurrentValue(TextBlock.TextProperty, ordered);
        }
    }

    private sealed class WatchedText : IMultiValueConverter
    {
        public static readonly WatchedText Instance = new();

        public object Convert(object[] values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return $"{values[1]}|{values[0]}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
