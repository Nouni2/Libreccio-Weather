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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using LibreccioWeather.ViewModels.Details;
using Button = System.Windows.Controls.Button;
using ToolTip = System.Windows.Controls.ToolTip;

namespace LibreccioWeather.Views;

public partial class DetailsView : UserControl
{
    // Connected buttons are fully round at the ends of the group and nearly square between buttons.
    private const double OuterRadius = 20;
    private const double InnerRadius = 4;

    public DetailsView()
    {
        InitializeComponent();
        Loaded += (_, _) => ScrollSelectedDayIntoView();
    }

    // An explanation opens on a click, and stays until the next click elsewhere.
    private void OnHelpClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { ToolTip: string { Length: > 0 } help } element)
        {
            return;
        }

        var toolTip = new ToolTip
        {
            Content = help,
            MaxWidth = 320,
            PlacementTarget = element,
            Placement = PlacementMode.Top,
            StaysOpen = false,
        };
        toolTip.IsOpen = true;
        e.Handled = true;
    }

    private void OnToggleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DetailsToggle toggle } button
            || ItemsControl.ItemsControlFromItemContainer(VisualTreeHelper.GetParent(button) as DependencyObject) is not { } group)
        {
            return;
        }

        int index = group.Items.IndexOf(toggle);
        bool first = index == 0;
        bool last = index == group.Items.Count - 1;

        // A checked button is fully round.
        button.Tag = toggle.IsChecked
            ? new CornerRadius(OuterRadius)
            : new CornerRadius(
                first ? OuterRadius : InnerRadius, last ? OuterRadius : InnerRadius,
                last ? OuterRadius : InnerRadius, first ? OuterRadius : InnerRadius);
    }

    private void OnProgressLoaded(object sender, RoutedEventArgs e) => UpdateProgress((Grid)sender);

    private void OnProgressSizeChanged(object sender, SizeChangedEventArgs e) => UpdateProgress((Grid)sender);

    private static void UpdateProgress(Grid track)
    {
        if (track.DataContext is DetailsPollutantItem item && track.Children.Count > 1 && track.Children[1] is Border indicator)
        {
            indicator.Width = Math.Max(0, track.ActualWidth * item.Progress);
        }
    }

    private void OnDaySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ScrollSelectedDayIntoView();
    }

    private void ScrollSelectedDayIntoView()
    {
        if (DayTabs.SelectedItem is { } selected)
        {
            DayTabs.ScrollIntoView(selected);
        }
    }
}
