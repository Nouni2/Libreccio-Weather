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
using LibreccioWeather.ViewModels.Details;
using Brush = System.Windows.Media.Brush;

namespace LibreccioWeather.Controls.Details;

/// <summary>
/// Text made of runs in different styles, such as a large value with a smaller unit.
/// </summary>
public sealed class DetailsRunsText : TextBlock
{
    // The size of a small headline, which units and levels take after a large value.
    private const double SmallerSize = 24;

    public static readonly DependencyProperty RunsProperty = DependencyProperty.Register(
        nameof(Runs), typeof(IReadOnlyList<DetailsTextRun>), typeof(DetailsRunsText),
        new PropertyMetadata(null, (element, _) => ((DetailsRunsText)element).Rebuild()));

    public IReadOnlyList<DetailsTextRun>? Runs
    {
        get => (IReadOnlyList<DetailsTextRun>?)GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    private void Rebuild()
    {
        Inlines.Clear();
        foreach (DetailsTextRun textRun in Runs ?? [])
        {
            var run = new Run(textRun.Text);
            switch (textRun.Style)
            {
                case DetailsRunStyle.Smaller:
                    run.FontSize = SmallerSize;
                    break;
                case DetailsRunStyle.SmallerVariant:
                    run.FontSize = SmallerSize;
                    run.Foreground = (Brush)FindResource("OnSurfaceVariantBrush");
                    break;
                case DetailsRunStyle.Subscript:
                    run.BaselineAlignment = BaselineAlignment.Subscript;
                    run.SetBinding(TextElement.FontSizeProperty, new System.Windows.Data.Binding(nameof(FontSize))
                    {
                        Source = this,
                        Converter = SubscriptSize.Instance,
                    });
                    break;
            }

            Inlines.Add(run);
        }
    }

    // A subscript is drawn at 80 % of the text size.
    private sealed class SubscriptSize : System.Windows.Data.IValueConverter
    {
        public static readonly SubscriptSize Instance = new();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value * 0.8;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
