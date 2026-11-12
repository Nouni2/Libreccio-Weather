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
using System.Windows.Documents;
using System.Windows.Input;
using LibreccioWeather.Localization;
using LibreccioWeather.ViewModels.WeatherCards;

namespace LibreccioWeather.Controls;

/// <summary>
/// Text made of segments, where a segment with a command is an underlined link in the text's own color.
/// </summary>
public sealed class SegmentedText : TextBlock
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments), typeof(IReadOnlyList<TextSegment>), typeof(SegmentedText),
        new PropertyMetadata(null, (sender, _) => ((SegmentedText)sender).Rebuild()));

    public IReadOnlyList<TextSegment>? Segments
    {
        get => (IReadOnlyList<TextSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    private void Rebuild()
    {
        Inlines.Clear();
        foreach (TextSegment segment in Segments ?? [])
        {
            if (segment.Command is null)
            {
                Inlines.Add(new Run(segment.Text));
                continue;
            }

            // A hyperlink is blue by default; the text's color is kept and only the underline marks the link.
            var link = new Hyperlink(new Run(segment.Text)) { Command = segment.Command, Cursor = Cursors.Hand };
            link.SetBinding(TextElement.ForegroundProperty, new System.Windows.Data.Binding(nameof(Foreground)) { Source = this });
            Inlines.Add(link);
        }

        BidiText.EmbedInlines(Inlines, string.Concat((Segments ?? []).Select(segment => segment.Text)), FlowDirection);
    }
}
