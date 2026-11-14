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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using LibreccioWeather.Display;
using LibreccioWeather.ViewModels;

// Windows Forms, there for the tray icon, also brings System.Drawing.Point into scope.
using Point = System.Windows.Point;

namespace LibreccioWeather.Views;

/// <summary>
/// Turns pointer movements on the location cards into the list's gestures: a press opens the card,
/// sliding it past half its width towards the end deletes it, and dragging its handle moves it in the list.
/// </summary>
public partial class ManagementView : UserControl
{
    // A slide counts once the card has moved this fraction of its width.
    private const double SwipeThreshold = 0.5;

    // ItemTouchHelper's default move threshold: a dragged card must leave its slot by half its height to move.
    private const double DragMoveThreshold = 0.5;

    // RecyclerView's item move duration, which the cards making way take.
    private static readonly Duration MakeWayDuration = new(TimeSpan.FromMilliseconds(250));

    // ItemTouchHelper's drag recovery duration, which the released card takes to settle into its slot.
    private static readonly Duration SettleDuration = new(TimeSpan.FromMilliseconds(200));

    // Android's default animation curve, AccelerateDecelerateInterpolator, is this sine curve.
    private static readonly IEasingFunction AndroidDefaultEasing = CreateAndroidDefaultEasing();

    private static readonly DropShadowEffect DragShadow = CreateDragShadow();

    private Point _swipeStart;
    private bool _isSwiping;
    private LocationItemViewModel? _dragItem;
    private int _dragFrom = -1;
    private int _dragCurrent = -1;
    private double _dragStartTop;
    private double _dragStartPointer;
    private bool _isSettling;

    public ManagementView()
    {
        InitializeComponent();

        // Only the location cards take the view palette; the top bar, update card and add button keep the app's.
        Appearance.ThemeManager.ApplyViewColors(LocationList);

        // The list holds the capture during a drag, because moving a card can rebuild the card under the pointer.
        LocationList.MouseMove += OnListMouseMove;
        LocationList.MouseLeftButtonUp += OnListMouseUp;
        LocationList.LostMouseCapture += OnListLostCapture;
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
        else if (offset <= -card.ActualWidth * SwipeThreshold)
        {
            viewModel.ChooseSources(item);
        }
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
        if (_isSettling || ((FrameworkElement)sender).DataContext is not LocationItemViewModel item || ViewModel is not { } viewModel
            || ContainerOf(item) is not { } container || VisualTreeHelper.GetParent(container) is not UIElement panel)
        {
            return;
        }

        // A card grabbed while it makes way starts from its slot, as the drag takes over its position.
        TranslateTransform transform = TransformOf(container);
        transform.BeginAnimation(TranslateTransform.YProperty, null);
        transform.Y = 0;

        _dragItem = item;
        _dragFrom = viewModel.Items.IndexOf(item);
        _dragCurrent = _dragFrom;
        _dragStartTop = LayoutTop(container);
        _dragStartPointer = e.GetPosition(panel).Y;
        Raise(container);
        LocationList.CaptureMouse();
    }

    private void OnListMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragItem is not { } item || ViewModel is not { } viewModel
            || ContainerOf(item) is not { } container || VisualTreeHelper.GetParent(container) is not UIElement panel)
        {
            return;
        }

        double top = _dragStartTop + e.GetPosition(panel).Y - _dragStartPointer;
        MoveIfNecessary(viewModel, container, top);

        // Moving the item can give it a new container, which takes over the raised look and the pointer's position.
        if (ContainerOf(item) is { } current)
        {
            Raise(current);
            TransformOf(current).Y = top - LayoutTop(current);
        }
    }

    private void OnListMouseUp(object sender, MouseButtonEventArgs e) => EndDrag();

    private void OnListLostCapture(object sender, MouseEventArgs e) => EndDrag();

    // The card settles into its slot before the order is saved, because saving rebuilds the cards.
    private void EndDrag()
    {
        if (_dragItem is not { } item || ViewModel is not { } viewModel)
        {
            return;
        }

        int from = _dragFrom;
        int to = _dragCurrent;
        _dragItem = null;
        _dragFrom = -1;
        _dragCurrent = -1;
        if (LocationList.IsMouseCaptured)
        {
            LocationList.ReleaseMouseCapture();
        }

        if (ContainerOf(item) is not { } container)
        {
            _ = viewModel.EndDragAsync(from, to);
            return;
        }

        _isSettling = true;
        TranslateTransform transform = TransformOf(container);
        DoubleAnimation settle = new(transform.Y, 0, SettleDuration) { EasingFunction = AndroidDefaultEasing };
        settle.Completed += (_, _) =>
        {
            container.Effect = null;
            Panel.SetZIndex(container, 0);
            _isSettling = false;
            _ = viewModel.EndDragAsync(from, to);
        };
        AnimationEffects.Apply(settle);
        transform.BeginAnimation(TranslateTransform.YProperty, settle);
    }

    // Moves the dragged card once it has left its slot by the move threshold and wholly passed a neighbour: below, its
    // bottom beyond the neighbour's bottom; above, its top beyond the neighbour's top. Of several, the farthest wins.
    private void MoveIfNecessary(ManagementViewModel viewModel, FrameworkElement selected, double top)
    {
        double selectedTop = LayoutTop(selected);
        double height = selected.ActualHeight;
        double dy = top - selectedTop;
        if (Math.Abs(dy) < height * DragMoveThreshold)
        {
            return;
        }

        int target = -1;
        double farthest = 0;
        for (int index = 0; index < viewModel.Items.Count; index++)
        {
            if (viewModel.Items[index] == _dragItem || ContainerOf(viewModel.Items[index]) is not { } candidate)
            {
                continue;
            }

            double candidateTop = LayoutTop(candidate);
            double candidateBottom = candidateTop + candidate.ActualHeight;
            if (candidateBottom < top || candidateTop > top + height)
            {
                continue;
            }

            double distance = dy > 0
                ? candidateBottom > selectedTop + height ? top + height - candidateBottom : 0
                : candidateTop < selectedTop ? candidateTop - top : 0;
            if (distance > farthest)
            {
                farthest = distance;
                target = index;
            }
        }

        if (target >= 0)
        {
            MoveCard(viewModel, target);
        }
    }

    // The cards whose slot changes glide from where they are drawn to their new slot.
    private void MoveCard(ManagementViewModel viewModel, int to)
    {
        Dictionary<LocationItemViewModel, double> drawnTops = [];
        foreach (LocationItemViewModel item in viewModel.Items)
        {
            if (ContainerOf(item) is { } container)
            {
                drawnTops[item] = LayoutTop(container) + TransformOf(container).Y;
            }
        }

        viewModel.MoveWhileDragging(_dragCurrent, to);
        _dragCurrent = to;
        LocationList.UpdateLayout();

        foreach (LocationItemViewModel item in viewModel.Items)
        {
            if (item == _dragItem || ContainerOf(item) is not { } container || !drawnTops.TryGetValue(item, out double drawnTop))
            {
                continue;
            }

            double offset = drawnTop - LayoutTop(container);
            if (offset != 0)
            {
                DoubleAnimation makeWay = new(offset, 0, MakeWayDuration) { EasingFunction = AndroidDefaultEasing };
                AnimationEffects.Apply(makeWay);
                TransformOf(container).BeginAnimation(TranslateTransform.YProperty, makeWay);
            }
        }
    }

    private FrameworkElement? ContainerOf(LocationItemViewModel item) =>
        LocationList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

    // The card's slot in the list, without the offset it is drawn at.
    private static double LayoutTop(FrameworkElement container) => VisualTreeHelper.GetOffset(container).Y;

    private static TranslateTransform TransformOf(FrameworkElement container)
    {
        if (container.RenderTransform is not TranslateTransform { IsFrozen: false } transform)
        {
            transform = new TranslateTransform();
            container.RenderTransform = transform;
        }

        return transform;
    }

    private static void Raise(FrameworkElement container)
    {
        Panel.SetZIndex(container, 1);
        container.Effect = DragShadow;
    }

    private static IEasingFunction CreateAndroidDefaultEasing()
    {
        SineEase easing = new() { EasingMode = EasingMode.EaseInOut };
        easing.Freeze();
        return easing;
    }

    // WPF has no elevation, so a soft shadow beneath the card stands for the 10-dp touch elevation.
    private static DropShadowEffect CreateDragShadow()
    {
        DropShadowEffect shadow = new()
        {
            Color = Colors.Black,
            BlurRadius = 16,
            ShadowDepth = 4,
            Direction = 270,
            Opacity = 0.3,
        };
        shadow.Freeze();
        return shadow;
    }
}
