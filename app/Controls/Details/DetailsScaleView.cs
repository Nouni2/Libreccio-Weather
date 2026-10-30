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
using LibreccioWeather.ViewModels.Details;
using Brush = System.Windows.Media.Brush;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace LibreccioWeather.Controls.Details;

/// <summary>
/// The table of a scale: a bold header row, then one row per level. Columns share the width by weight, with 8 between
/// them, and the last one is aligned to the end.
/// </summary>
public sealed class DetailsScaleView : Grid
{
    private const double Spacing = 8;
    private const double DotSize = 24;

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(DetailsScale), typeof(DetailsScaleView),
        new PropertyMetadata(null, (element, _) => ((DetailsScaleView)element).Rebuild()));

    public DetailsScale? Scale
    {
        get => (DetailsScale?)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    private void Rebuild()
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();
        if (Scale is not { } scale)
        {
            return;
        }

        for (int column = 0; column < scale.Weights.Count; column++)
        {
            if (column > 0)
            {
                ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Spacing) });
            }

            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(scale.Weights[column], GridUnitType.Star) });
        }

        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        for (int column = 0; column < scale.Header.Count; column++)
        {
            AddCell(0, column, scale.Header[column], null, bold: true, last: column == scale.Header.Count - 1);
        }

        for (int row = 0; row < scale.Rows.Count; row++)
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            IReadOnlyList<DetailsScaleCell> cells = scale.Rows[row];
            for (int column = 0; column < cells.Count; column++)
            {
                AddCell(row + 1, column, cells[column].Text, cells[column].Dot, bold: false, last: column == cells.Count - 1);
            }
        }
    }

    private void AddCell(int row, int column, string text, System.Windows.Media.Color? dot, bool bold, bool last)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = (Brush)FindResource("OnSurfaceBrush"),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = last ? TextAlignment.Right : TextAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        FrameworkElement cell = textBlock;
        if (dot is { } color)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var circle = new ContentControl
            {
                Style = (Style)FindResource("Icon960"),
                Tag = FindResource("IconCircle"),
                Width = DotSize,
                Height = DotSize,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, Spacing, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(circle);
            panel.Children.Add(textBlock);
            cell = panel;
        }

        cell.Margin = new Thickness(cell.Margin.Left, row == 0 ? 0 : Spacing, cell.Margin.Right, 0);
        cell.HorizontalAlignment = last ? HorizontalAlignment.Right : HorizontalAlignment.Stretch;
        cell.VerticalAlignment = VerticalAlignment.Center;
        SetRow(cell, row);
        SetColumn(cell, column * 2);
        Children.Add(cell);
    }
}
