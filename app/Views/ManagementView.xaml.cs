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
using System.Windows.Input;
using System.Windows.Media;
using LibreccioWeather.ViewModels;

// Windows Forms, there for the tray icon, also brings System.Drawing.Point into scope.
using Point = System.Windows.Point;

namespace LibreccioWeather.Views;

/// <summary>
/// Turns pointer movements on the location cards into the gestures of Breezy Weather's list: a press opens the card,
/// sliding it past half its width towards the end deletes it, and dragging its handle moves it in the list.
/// </summary>
public partial class ManagementView : UserControl
{
    // Android's ItemTouchHelper counts a swipe once the card has moved half its width.
    private const double SwipeThreshold = 0.5;

    private Point _swipeStart;
    private bool _isSwiping;
    private int _dragFrom = -1;
    private int _dragCurrent = -1;

    public ManagementView()
    {
        InitializeComponent();

        // The list holds the capture during a drag, because moving a card can rebuild the card under the pointer.
        LocationList.MouseMove += OnListMouseMove;
        LocationList.MouseLeftButtonUp += OnListMouseUp;
    }

    private ManagementViewModel? ViewModel => DataContext as ManagementViewModel;

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        Border card = (Border)sender;
        if (card.Focusable)
        {
            card.Focus();
        }

        _swipeStart = e.GetPosition(this);
        _isSwiping = false;
        card.CaptureMouse();
        e.Handled = true;
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        Border card = (Border)sender;
        if (!card.IsMouseCaptured)
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
            SetOffset(card, offset);
        }
    }

    private void OnCardMouseUp(object sender, MouseButtonEventArgs e)
    {
        Border card = (Border)sender;
        if (!card.IsMouseCaptured)
        {
            return;
        }

        bool wasSwiping = _isSwiping;
        double offset = e.GetPosition(this).X - _swipeStart.X;
        card.ReleaseMouseCapture();
        if (card.DataContext is not LocationItemViewModel item || ViewModel is not { } viewModel)
        {
            return;
        }

        if (!wasSwiping)
        {
            viewModel.OpenCommand.Execute(item);
        }
        else if (offset >= card.ActualWidth * SwipeThreshold)
        {
            _ = viewModel.DeleteAsync(item);
        }

        // Sliding towards the start opens the location's source settings in Breezy Weather, which are not ported,
        // so the card only slides back.
    }

    private void OnCardLostCapture(object sender, MouseEventArgs e)
    {
        _isSwiping = false;
        SetOffset((Border)sender, 0);
    }

    // The side uncovered by the card shows what releasing it would do: delete at the start, source settings at the end.
    private static void SetOffset(Border card, double offset)
    {
        // A transform declared in a template is frozen, so each card gets its own the first time it moves.
        if (card.RenderTransform is not TranslateTransform { IsFrozen: false } transform)
        {
            transform = new TranslateTransform();
            card.RenderTransform = transform;
        }

        transform.X = offset;
        if (card.Parent is Grid container && container.Children.Count >= 3)
        {
            container.Children[0].Visibility = offset > 0 ? Visibility.Visible : Visibility.Collapsed;
            container.Children[1].Visibility = offset < 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (((FrameworkElement)sender).DataContext is not LocationItemViewModel item || ViewModel is not { } viewModel)
        {
            return;
        }

        _dragFrom = viewModel.Items.IndexOf(item);
        _dragCurrent = _dragFrom;
        LocationList.CaptureMouse();
    }

    private void OnListMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragFrom < 0 || ViewModel is not { } viewModel)
        {
            return;
        }

        int target = IndexAt(e.GetPosition(LocationList).Y);
        if (target >= 0 && target != _dragCurrent)
        {
            viewModel.MoveWhileDragging(_dragCurrent, target);
            _dragCurrent = target;
        }
    }

    private void OnListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragFrom < 0 || ViewModel is not { } viewModel)
        {
            return;
        }

        int from = _dragFrom;
        int to = _dragCurrent;
        _dragFrom = -1;
        _dragCurrent = -1;
        LocationList.ReleaseMouseCapture();
        _ = viewModel.EndDragAsync(from, to);
    }

    // The card whose vertical extent contains the pointer, or the nearest end of the list.
    private int IndexAt(double y)
    {
        int count = LocationList.Items.Count;
        for (int index = 0; index < count; index++)
        {
            if (LocationList.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement container)
            {
                continue;
            }

            Point top = container.TranslatePoint(new Point(0, 0), LocationList);
            if (y < top.Y + container.ActualHeight)
            {
                return index;
            }
        }

        return count - 1;
    }
}
