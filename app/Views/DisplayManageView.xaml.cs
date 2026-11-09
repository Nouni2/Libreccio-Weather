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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LibreccioWeather.Display;
using LibreccioWeather.ViewModels.Settings;

// Windows Forms, there for the tray icon, also brings System.Drawing.Point into scope.
using Point = System.Windows.Point;

namespace LibreccioWeather.Views;

/// <summary>
/// Turns pointer movements on an order editor's rows into its gestures: sliding a row past half its width either way
/// removes it, and dragging its handle moves it. It also slides the bar of chips in and out.
/// </summary>
public partial class DisplayManageView : UserControl
{
    // A slide counts once the row has moved this fraction of its width.
    private const double SwipeThreshold = 0.5;

    private Point _swipeStart;
    private bool _isSwiping;
    private int _dragCurrent = -1;
    private DisplayManageViewModel? _viewModel;

    // The bar starts shown without animating, and animates only when whether entries are left out changes.
    private bool _bottomBarVisibility;

    public DisplayManageView()
    {
        InitializeComponent();
        Appearance.ThemeManager.ApplyViewColors(this);

        // The list holds the capture during a drag, because moving a row rebuilds the row under the pointer.
        DisplayList.MouseMove += OnListMouseMove;
        DisplayList.MouseLeftButtonUp += OnListMouseUp;
        DisplayList.LostMouseCapture += (_, _) => _dragCurrent = -1;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as DisplayManageViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _bottomBarVisibility = false;
            ResetBottomBarVisibility();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DisplayManageViewModel.HasOtherItems))
        {
            ResetBottomBarVisibility();
        }
    }

    // Showing fades in and rises over 350 ms, slowing down as 1 − (1 − t)⁴; hiding fades out and sinks by the bar's
    // height over 150 ms, speeding up as t⁴. Each starts from wherever the previous one left the bar.
    private void ResetBottomBarVisibility()
    {
        bool visible = _viewModel?.HasOtherItems == true;
        if (_bottomBarVisibility == visible)
        {
            return;
        }

        _bottomBarVisibility = visible;
        TimeSpan duration = TimeSpan.FromMilliseconds(visible ? 350 : 150);
        IEasingFunction easing = new PowerEase { Power = 4, EasingMode = visible ? EasingMode.EaseOut : EasingMode.EaseIn };

        var alpha = new DoubleAnimation(visible ? 1 : 0, duration) { EasingFunction = easing };
        var translation = new DoubleAnimation(visible ? 0 : BottomBar.ActualHeight, duration) { EasingFunction = easing };
        RefreshRate.Apply(alpha, this);
        AnimationEffects.Apply(alpha);
        RefreshRate.Apply(translation, this);
        AnimationEffects.Apply(translation);

        // A transform declared in XAML may be frozen, so the bar gets its own before it moves.
        if (BottomBar.RenderTransform is not TranslateTransform { IsFrozen: false } transform)
        {
            transform = new TranslateTransform();
            BottomBar.RenderTransform = transform;
        }

        BottomBar.BeginAnimation(OpacityProperty, alpha);
        transform.BeginAnimation(TranslateTransform.YProperty, translation);
        BottomBar.IsHitTestVisible = visible;
    }

    private void OnRowMouseDown(object sender, MouseButtonEventArgs e)
    {
        _swipeStart = e.GetPosition(this);
        _isSwiping = false;
        ((FrameworkElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void OnRowMouseMove(object sender, MouseEventArgs e)
    {
        var row = (FrameworkElement)sender;
        if (!row.IsMouseCaptured)
        {
            return;
        }

        double offset = e.GetPosition(this).X - _swipeStart.X;
        if (!_isSwiping && Math.Abs(offset) > SystemParameters.MinimumHorizontalDragDistance)
        {
            _isSwiping = true;
        }

        if (_isSwiping)
        {
            SetOffset(row, offset);
        }
    }

    private void OnRowMouseUp(object sender, MouseButtonEventArgs e)
    {
        var row = (FrameworkElement)sender;
        if (!row.IsMouseCaptured)
        {
            return;
        }

        bool wasSwiping = _isSwiping;
        double offset = e.GetPosition(this).X - _swipeStart.X;
        row.ReleaseMouseCapture();
        if (wasSwiping && Math.Abs(offset) >= row.ActualWidth * SwipeThreshold
            && row.DataContext is DisplayItem item && _viewModel is { } viewModel)
        {
            viewModel.RemoveItemCommand.Execute(item);
        }
    }

    private void OnRowLostCapture(object sender, MouseEventArgs e)
    {
        _isSwiping = false;
        SetOffset((FrameworkElement)sender, 0);
    }

    // The side uncovered by the row shows the delete icon at that end.
    private static void SetOffset(FrameworkElement row, double offset)
    {
        if (row.RenderTransform is not TranslateTransform { IsFrozen: false } transform)
        {
            transform = new TranslateTransform();
            row.RenderTransform = transform;
        }

        transform.X = offset;
        if (row.Parent is Grid container && container.Children.Count >= 3)
        {
            container.Children[0].Visibility = offset > 0 ? Visibility.Visible : Visibility.Collapsed;
            container.Children[1].Visibility = offset < 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (((FrameworkElement)sender).DataContext is not DisplayItem item || _viewModel is not { } viewModel)
        {
            return;
        }

        _dragCurrent = viewModel.DisplayedItems.IndexOf(item);
        DisplayList.CaptureMouse();
    }

    private void OnListMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCurrent < 0 || _viewModel is not { } viewModel)
        {
            return;
        }

        int target = IndexAt(e.GetPosition(DisplayList).Y);
        if (target >= 0 && target != _dragCurrent)
        {
            viewModel.MoveItem(_dragCurrent, target);
            _dragCurrent = target;
        }
    }

    private void OnListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragCurrent >= 0)
        {
            DisplayList.ReleaseMouseCapture();
        }
    }

    // The row whose vertical extent contains the pointer, or the nearest end of the list.
    private int IndexAt(double y)
    {
        int count = DisplayList.Items.Count;
        for (int index = 0; index < count; index++)
        {
            if (DisplayList.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
            {
                continue;
            }

            Point top = container.TranslatePoint(new Point(0, 0), DisplayList);
            if (y < top.Y + container.ActualHeight)
            {
                return index;
            }
        }

        return count - 1;
    }
}
