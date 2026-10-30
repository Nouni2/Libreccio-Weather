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
using System.Windows.Media.Animation;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls;

/// <summary>
/// A pane beside the content: its first child is the pane, its second the content. Unfolding slides the pane in from
/// the start edge and narrows the content to the remaining width; folding slides it out again. Right-to-left layout
/// mirrors it to the other edge.
/// </summary>
public sealed class DrawerPanel : Panel
{
    private const double MinDrawerWidth = 280;
    private const double MaxDrawerWidth = 320;

    // The width a list keeps on a wide screen; the pane takes what is left, within its own limits.
    private const double AdaptiveListWidth = 600;

    // A full slide lasts 450 ms, and a partial one the matching share of it.
    private const double FullSlideMilliseconds = 450;

    public static readonly DependencyProperty IsAvailableProperty = DependencyProperty.Register(
        nameof(IsAvailable), typeof(bool), typeof(DrawerPanel), new PropertyMetadata(false, OnAvailableChanged));

    public static readonly DependencyProperty IsUnfoldProperty = DependencyProperty.Register(
        nameof(IsUnfold), typeof(bool), typeof(DrawerPanel), new PropertyMetadata(false, OnUnfoldChanged));

    private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(DrawerPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    /// Whether the pane can be shown at all. While it cannot, the content takes the whole width at once.
    /// </summary>
    public bool IsAvailable
    {
        get => (bool)GetValue(IsAvailableProperty);
        set => SetValue(IsAvailableProperty, value);
    }

    public bool IsUnfold
    {
        get => (bool)GetValue(IsUnfoldProperty);
        set => SetValue(IsUnfoldProperty, value);
    }

    // 0 while folded, 1 while unfolded.
    private double Progress => (double)GetValue(ProgressProperty);

    private UIElement? Drawer => InternalChildren.Count > 0 ? InternalChildren[0] : null;

    private UIElement? Content => InternalChildren.Count > 1 ? InternalChildren[1] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        double drawerWidth = DrawerWidth(width);
        if (Drawer is { } drawer)
        {
            // A folded pane is hidden, so it takes no keyboard focus and no clicks.
            drawer.Visibility = Progress > 0 ? Visibility.Visible : Visibility.Hidden;
            drawer.Measure(new Size(drawerWidth, availableSize.Height));
        }

        Content?.Measure(new Size(ContentWidth(width, drawerWidth), availableSize.Height));
        return new Size(width, double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double drawerWidth = DrawerWidth(finalSize.Width);
        double drawerLeft = drawerWidth * (Progress - 1);
        Drawer?.Arrange(new Rect(drawerLeft, 0, drawerWidth, finalSize.Height));
        Content?.Arrange(new Rect(drawerLeft + drawerWidth, 0, ContentWidth(finalSize.Width, drawerWidth), finalSize.Height));
        return finalSize;
    }

    // A pane as wide as the panel covers the content instead of narrowing it.
    private double ContentWidth(double width, double drawerWidth)
    {
        return drawerWidth >= width ? width : Math.Max(0, width - drawerWidth * Progress);
    }

    private static double DrawerWidth(double width)
    {
        double remaining = width - Math.Min(width, AdaptiveListWidth);
        return remaining == 0 ? width : Math.Clamp(remaining, MinDrawerWidth, MaxDrawerWidth);
    }

    private static void OnAvailableChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var panel = (DrawerPanel)sender;
        panel.BeginAnimation(ProgressProperty, null);
        panel.SetValue(ProgressProperty, panel.IsAvailable && panel.IsUnfold ? 1.0 : 0.0);
    }

    private static void OnUnfoldChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var panel = (DrawerPanel)sender;
        if (!panel.IsAvailable)
        {
            return;
        }

        double target = panel.IsUnfold ? 1 : 0;
        var slide = new DoubleAnimation(target, TimeSpan.FromMilliseconds(Math.Abs(panel.Progress - target) * FullSlideMilliseconds))
        {
            // Slows down towards the end, as 1 − (1 − t)⁴.
            EasingFunction = new PowerEase { Power = 4, EasingMode = EasingMode.EaseOut },
        };
        panel.BeginAnimation(ProgressProperty, slide);
    }
}
