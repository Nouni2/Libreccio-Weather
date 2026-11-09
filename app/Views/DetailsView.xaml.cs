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
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Size = System.Windows.Size;
using ToolTip = System.Windows.Controls.ToolTip;

namespace LibreccioWeather.Views;

public partial class DetailsView : UserControl
{
    // Released past half its width, a dragged page gives way to the neighbouring day.
    private const double PageSwipeThreshold = 0.5;

    private const double TooltipSpacing = 4;

    private ResourceDictionary? _palette;
    private bool? _paletteIsLight;
    private System.Windows.Point? _pageDragStart;
    private bool _isSwipingPage;

    public DetailsView()
    {
        InitializeComponent();
        Loaded += (_, _) => Dispatcher.BeginInvoke(new Action(ScrollSelectedDayIntoView), System.Windows.Threading.DispatcherPriority.Loaded);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is DetailsViewModel previous)
        {
            previous.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is DetailsViewModel current)
        {
            current.PropertyChanged += OnViewModelPropertyChanged;
            ApplyPalette(current.IsLightTheme);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetailsViewModel.IsLightTheme) && sender is DetailsViewModel viewModel)
        {
            ApplyPalette(viewModel.IsLightTheme);
        }
        else if (e.PropertyName == nameof(DetailsViewModel.SelectedDayIndex))
        {
            // A day reached by swiping may be off screen in the tab row, which follows it.
            Dispatcher.BeginInvoke(new Action(ScrollSelectedDayIntoView), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    // The screen's own palette overrides the app's, so a location can be light while the rest of the app is dark.
    // Swapping it makes every color of the screen resolve again, so it is only swapped when it changes.
    private void ApplyPalette(bool light)
    {
        if (_paletteIsLight == light)
        {
            return;
        }

        _paletteIsLight = light;
        if (_palette is not null)
        {
            Resources.MergedDictionaries.Remove(_palette);
        }

        _palette = Appearance.ThemeManager.GetScreenColors(light);
        Resources.MergedDictionaries.Add(_palette);
    }

    // A drag that starts outside the charts and buttons, which keep presses for themselves, moves the day's page once
    // it is more horizontal than vertical. There is no page before the first day or after the last, so the page does
    // not move that way.
    private void OnPageMouseDown(object sender, MouseButtonEventArgs e)
    {
        _pageDragStart = e.GetPosition(this);
        _isSwipingPage = false;
    }

    private void OnPageMouseMove(object sender, MouseEventArgs e)
    {
        if (_pageDragStart is not { } start || e.LeftButton != MouseButtonState.Pressed
            || DataContext is not DetailsViewModel viewModel)
        {
            _pageDragStart = null;
            return;
        }

        Vector moved = e.GetPosition(this) - start;
        if (!_isSwipingPage)
        {
            if (Math.Abs(moved.X) <= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(moved.X) <= Math.Abs(moved.Y))
            {
                return;
            }

            _isSwipingPage = ((UIElement)sender).CaptureMouse();
        }

        double width = ((FrameworkElement)sender).ActualWidth;
        bool blocked = (moved.X > 0 && viewModel.SelectedDayIndex <= 0)
            || (moved.X < 0 && viewModel.SelectedDayIndex >= viewModel.Days.Count - 1);
        DayPageOffset.X = blocked ? 0 : Math.Clamp(moved.X, -width, width);
        e.Handled = true;
    }

    private void OnPageMouseUp(object sender, MouseButtonEventArgs e)
    {
        _pageDragStart = null;
        if (!_isSwipingPage)
        {
            return;
        }

        double offset = DayPageOffset.X;
        double threshold = ((FrameworkElement)sender).ActualWidth * PageSwipeThreshold;
        ((UIElement)sender).ReleaseMouseCapture();
        if (DataContext is DetailsViewModel viewModel)
        {
            if (offset >= threshold)
            {
                viewModel.SelectedDayIndex--;
            }
            else if (offset <= -threshold)
            {
                viewModel.SelectedDayIndex++;
            }
        }

        e.Handled = true;
    }

    private void OnPageLostCapture(object sender, MouseEventArgs e)
    {
        _isSwipingPage = false;
        DayPageOffset.X = 0;
    }

    // An explanation opens on a click, and stays until the next click elsewhere. It takes the screen's colors, which a
    // tooltip outside the screen cannot find by itself.
    private void OnHelpClick(object sender, MouseButtonEventArgs e) => OpenHelp(sender, e, below: false);

    // The deviation from normals sits right under the temperature it explains, so its explanation prefers to open below.
    private void OnHelpBelowClick(object sender, MouseButtonEventArgs e) => OpenHelp(sender, e, below: true);

    private void OpenHelp(object sender, MouseButtonEventArgs e, bool below)
    {
        if (sender is not FrameworkElement { ToolTip: string { Length: > 0 } help } element)
        {
            return;
        }

        var toolTip = new ToolTip
        {
            Content = help,
            Style = (Style)FindResource("PlainTooltip"),
            Background = (Brush)FindResource("InverseSurfaceBrush"),
            Foreground = (Brush)FindResource("InverseOnSurfaceBrush"),
            PlacementTarget = element,
            Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = (popup, target, _) => PlaceTooltip(element, popup, target, below),
            StaysOpen = false,
        };
        toolTip.IsOpen = true;
        e.Handled = true;
    }

    // Centered on its anchor, 4 apart, on the preferred side, or on the other side when there is no room. Placement
    // sizes are in screen pixels, so the spacing is scaled to them.
    private static CustomPopupPlacement[] PlaceTooltip(Visual anchor, Size popup, Size target, bool below)
    {
        double spacing = TooltipSpacing * VisualTreeHelper.GetDpi(anchor).DpiScaleY;
        double x = (target.Width - popup.Width) / 2;
        var above = new CustomPopupPlacement(new System.Windows.Point(x, -popup.Height - spacing), PopupPrimaryAxis.Horizontal);
        var under = new CustomPopupPlacement(new System.Windows.Point(x, target.Height + spacing), PopupPrimaryAxis.Horizontal);
        return below ? [under, above] : [above, under];
    }

    // The first toggle has the leading shape and the last the trailing one.
    private void OnToggleLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DetailsToggle toggle } button
            || ItemsControl.ItemsControlFromItemContainer(VisualTreeHelper.GetParent(button) as DependencyObject) is not { } group)
        {
            return;
        }

        int index = group.Items.IndexOf(toggle);
        Controls.ConnectedPosition.SetIsFirst(button, index == 0);
        Controls.ConnectedPosition.SetIsLast(button, index == group.Items.Count - 1);
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

    // The chosen day's tab is centered in the row where the row can scroll that far, as a scrollable tab row centers
    // its selected tab.
    private void ScrollSelectedDayIntoView()
    {
        if (DayTabs.SelectedItem is not { } selected)
        {
            return;
        }

        if (DayTabs.ItemContainerGenerator.ContainerFromItem(selected) is not FrameworkElement { ActualWidth: > 0 } tab
            || FindScrollViewer(DayTabs) is not { } viewer)
        {
            DayTabs.ScrollIntoView(selected);
            return;
        }

        double tabStart = tab.TransformToAncestor(viewer).Transform(default).X + viewer.HorizontalOffset;
        double centered = tabStart + (tab.ActualWidth / 2) - (viewer.ViewportWidth / 2);
        viewer.ScrollToHorizontalOffset(Math.Clamp(centered, 0, viewer.ScrollableWidth));
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer)
            {
                return viewer;
            }

            if (FindScrollViewer(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
