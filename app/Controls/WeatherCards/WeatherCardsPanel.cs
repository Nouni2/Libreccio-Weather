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
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LibreccioWeather.Display;
using LibreccioWeather.ViewModels.WeatherCards;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace LibreccioWeather.Controls.WeatherCards;

public sealed class WeatherCardsPanel : Panel
{
    public static readonly DependencyProperty AnimateCardsProperty = DependencyProperty.Register(
        nameof(AnimateCards), typeof(bool), typeof(WeatherCardsPanel),
        new FrameworkPropertyMetadata(false, OnAnimateCardsChanged));

    private readonly HashSet<UIElement> _startedCards = [];
    private readonly HashSet<UIElement> _enteredCards = [];
    private readonly Dictionary<UIElement, DispatcherTimer> _pendingCards = [];
    private ScrollViewer? _scrollViewer;
    private UIElement? _header;
    private bool _checkQueued;

    public WeatherCardsPanel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += (_, _) => QueueEnterCheck();
        UseLayoutRounding = true;
    }

    public bool AnimateCards
    {
        get => (bool)GetValue(AnimateCardsProperty);
        set => SetValue(AnimateCardsProperty, value);
    }

    internal void AttachHeader(UIElement header)
    {
        if (_header == header)
        {
            return;
        }

        if (_header is not null)
        {
            StopEntrance(_header);
            _startedCards.Remove(_header);
            _enteredCards.Remove(_header);
        }

        _header = header;
        header.Opacity = AnimateCards ? 0 : 1;
        QueueEnterCheck();
    }

    internal bool HasEntered(UIElement card)
    {
        DependencyObject? child = card;
        while (child is not null && VisualTreeHelper.GetParent(child) != this)
        {
            child = VisualTreeHelper.GetParent(child);
        }

        return child is UIElement item && _enteredCards.Contains(item);
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        DependencyObject? parent = VisualTreeHelper.GetParent(this);
        while (parent is not null && parent is not ScrollViewer)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        _scrollViewer = parent as ScrollViewer;
        foreach (UIElement child in InternalChildren)
        {
            if (!_startedCards.Contains(child))
            {
                child.Opacity = AnimateCards ? 0 : 1;
            }
        }

        if (_header is not null && !_startedCards.Contains(_header))
        {
            _header.Opacity = AnimateCards ? 0 : 1;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        QueueEnterCheck();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }

        foreach (UIElement child in InternalChildren)
        {
            StopEntrance(child);
        }

        if (_header is not null)
        {
            StopEntrance(_header);
        }

        _startedCards.Clear();
        _enteredCards.Clear();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs args) => QueueEnterCheck();

    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
        base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        if (visualRemoved is UIElement removed)
        {
            StopEntrance(removed);
            _startedCards.Remove(removed);
            _enteredCards.Remove(removed);
        }

        if (visualAdded is UIElement added)
        {
            added.Opacity = AnimateCards ? 0 : 1;
        }

        QueueEnterCheck();
    }

    private static void OnAnimateCardsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var panel = (WeatherCardsPanel)sender;
        foreach (UIElement child in panel.InternalChildren)
        {
            panel.StopEntrance(child);
            child.Opacity = (bool)args.NewValue ? 0 : 1;
        }

        if (panel._header is not null)
        {
            panel.StopEntrance(panel._header);
            panel._header.Opacity = (bool)args.NewValue ? 0 : 1;
        }

        panel._startedCards.Clear();
        panel._enteredCards.Clear();
        panel.QueueEnterCheck();
    }

    private void QueueEnterCheck()
    {
        if (_checkQueued || !IsLoaded)
        {
            return;
        }

        _checkQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            _checkQueued = false;
            if (IsLoaded && IsVisible)
            {
                CheckEnterScreen();
            }
        }));
    }

    private void CheckEnterScreen()
    {
        if (_header is not null)
        {
            CheckEnterScreen(_header, true);
        }

        foreach (UIElement child in InternalChildren)
        {
            CheckEnterScreen(child, false);
        }
    }

    private void CheckEnterScreen(UIElement child, bool header)
    {
        if (_startedCards.Contains(child) || !child.IsArrangeValid)
        {
            return;
        }

        if (_scrollViewer is not null)
        {
            Rect bounds = child.TransformToAncestor(_scrollViewer).TransformBounds(new Rect(child.RenderSize));
            if (bounds.Top >= _scrollViewer.ViewportHeight || bounds.Bottom <= 0)
            {
                return;
            }
        }

        _startedCards.Add(child);
        if (AnimateCards)
        {
            StartEntrance(child, header);
        }
        else
        {
            child.Opacity = 1;
            EnterScreen(child);
        }
    }

    private void StartEntrance(UIElement child, bool header)
    {
        int pendingCount = _pendingCards.Count;
        double duration = header ? 300 : Math.Max(500 - pendingCount * 50, 250);
        var delay = TimeSpan.FromMilliseconds(header ? 100 : pendingCount * 200);
        if (header)
        {
            var alpha = new DoubleAnimationUsingKeyFrames
            {
                BeginTime = delay,
                Duration = TimeSpan.FromMilliseconds(duration),
            };
            alpha.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            alpha.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration)),
                new KeySpline(0.4, 0, 0.2, 1)));
            RefreshRate.Apply(alpha, this);
            child.BeginAnimation(OpacityProperty, alpha);
        }
        else
        {
            var scale = new ScaleTransform(1.025f, 1.025f);
            var translation = new TranslateTransform(0, 120);
            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(translation);
            child.RenderTransformOrigin = new Point(0.5, 0.5);
            child.RenderTransform = transforms;

            Animate(child, OpacityProperty, 0, 1, duration, delay, new SineEase { EasingMode = EasingMode.EaseInOut });
            Animate(translation, TranslateTransform.YProperty, 120, 0, duration, delay,
                new CardOvershootEase { Tension = 0.4f + 0.2f * pendingCount });
            Animate(scale, ScaleTransform.ScaleXProperty, 1.025f, 1, duration, delay,
                new QuadraticEase { EasingMode = EasingMode.EaseOut });
            Animate(scale, ScaleTransform.ScaleYProperty, 1.025f, 1, duration, delay,
                new QuadraticEase { EasingMode = EasingMode.EaseOut });
        }

        // The queue counts cards waiting for their delay, not cards already moving.
        var timer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher) { Interval = delay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _pendingCards.Remove(child);
            EnterScreen(child);
        };
        _pendingCards.Add(child, timer);
        timer.Start();
    }

    private void Animate(Animatable target, DependencyProperty property, double from, double to,
        double milliseconds, TimeSpan delay, IEasingFunction easing)
    {
        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            BeginTime = delay,
            EasingFunction = easing,
        };
        RefreshRate.Apply(animation, this);
        target.BeginAnimation(property, animation);
    }

    private void Animate(UIElement target, DependencyProperty property, double from, double to,
        double milliseconds, TimeSpan delay, IEasingFunction easing)
    {
        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            BeginTime = delay,
            EasingFunction = easing,
        };
        RefreshRate.Apply(animation, this);
        target.BeginAnimation(property, animation);
    }

    private void EnterScreen(UIElement child)
    {
        _enteredCards.Add(child);
        StartElementAnimations(child);
    }

    private static void StartElementAnimations(DependencyObject element)
    {
        if (element is AstroCardView astro)
        {
            astro.EnterScreen();
            return;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            StartElementAnimations(VisualTreeHelper.GetChild(element, i));
        }
    }

    private void StopEntrance(UIElement child)
    {
        if (_pendingCards.Remove(child, out DispatcherTimer? timer))
        {
            timer.Stop();
        }

        if (child.RenderTransform is TransformGroup transforms)
        {
            foreach (Transform transform in transforms.Children)
            {
                if (transform is ScaleTransform scale)
                {
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                }
                else if (transform is TranslateTransform translation)
                {
                    translation.BeginAnimation(TranslateTransform.YProperty, null);
                }
            }
        }

        child.BeginAnimation(OpacityProperty, null);
        child.Opacity = 1;
        child.RenderTransform = Transform.Identity;
    }

    protected override Size MeasureOverride(Size availableSize) => Layout(availableSize.Width, false);

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize.Width, true);
        QueueEnterCheck();
        return finalSize;
    }

    private Size Layout(double width, bool arrange)
    {
        float density = (float)VisualTreeHelper.GetDpi(this).DpiScaleX;
        float minBlockWidth = 160f * (0.5f + TextScale.Factor / 2);
        int blocks = Math.Clamp((int)Math.Floor((float)width / minBlockWidth), 1, 5);
        if (blocks > 2)
        {
            blocks = Math.Clamp((int)Math.Floor((float)width / (minBlockWidth * 1.1f)), 1, 5);
        }

        int totalWidth = (int)Math.Floor(width * density + 0.5);
        int spanWidth = totalWidth / blocks;
        int remainder = totalWidth % blocks;
        int additionalSize = 0;
        int[] borders = new int[blocks + 1];
        for (int i = 1; i <= blocks; i++)
        {
            int itemSize = spanWidth;
            additionalSize += remainder;
            if (additionalSize > 0 && blocks - additionalSize < remainder)
            {
                itemSize++;
                additionalSize -= blocks;
            }

            borders[i] = borders[i - 1] + itemSize;
        }

        double rowHeight = (spanWidth + (remainder > 0 ? 1 : 0)) / (double)density;
        bool squished = blocks <= 2 && (float)width / (minBlockWidth * 1.1f) < blocks;
        double y = 0;
        int column = 0;
        foreach (UIElement child in InternalChildren)
        {
            bool square = child is ContentPresenter { Content: ConditionsCardViewModel or AstroCardViewModel };
            child.SetValue(AstroCardView.AreBlocksSquishedProperty, squished);
            if (!square && column > 0)
            {
                y += rowHeight;
                column = 0;
            }

            if (square)
            {
                double left = borders[column] / (double)density;
                double blockWidth = (borders[column + 1] - borders[column]) / (double)density;
                if (arrange)
                {
                    child.Arrange(new Rect(left, y, blockWidth, rowHeight));
                }
                else
                {
                    child.Measure(new Size(blockWidth, rowHeight));
                }

                column++;
                if (column == blocks)
                {
                    column = 0;
                    y += rowHeight;
                }
            }
            else
            {
                if (arrange)
                {
                    child.Arrange(new Rect(0, y, width, child.DesiredSize.Height));
                }
                else
                {
                    child.Measure(new Size(width, double.PositiveInfinity));
                }

                y += child.DesiredSize.Height;
            }
        }

        return new Size(width, y + (column > 0 ? rowHeight : 0));
    }
}
