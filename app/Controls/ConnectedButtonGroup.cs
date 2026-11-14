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

using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Button = System.Windows.Controls.Button;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls;

/// <summary>
/// One choice among items, shown as a row of connected toggle buttons. Buttons that do not fit go into a menu opened by
/// a button at the end of the row.
/// </summary>
public sealed class ConnectedButtonGroup : Panel
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ConnectedButtonGroup),
        new PropertyMetadata(null, (sender, e) => ((ConnectedButtonGroup)sender).OnItemsSourceChanged(e)));

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(ConnectedButtonGroup),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (sender, _) => ((ConnectedButtonGroup)sender).UpdateChecked()));

    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(ConnectedButtonGroup),
        new PropertyMetadata(null, (sender, _) => ((ConnectedButtonGroup)sender).Rebuild()));

    public static readonly DependencyProperty OverflowLabelProperty = DependencyProperty.Register(
        nameof(OverflowLabel), typeof(string), typeof(ConnectedButtonGroup), new PropertyMetadata(null));

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(ConnectedButtonGroup),
        new PropertyMetadata(null, (sender, _) => ((ConnectedButtonGroup)sender).Rebuild()));

    public static readonly DependencyProperty IsCheckedPathProperty = DependencyProperty.Register(
        nameof(IsCheckedPath), typeof(string), typeof(ConnectedButtonGroup),
        new PropertyMetadata(null, (sender, _) => ((ConnectedButtonGroup)sender).Rebuild()));

    public static readonly DependencyProperty CommandPathProperty = DependencyProperty.Register(
        nameof(CommandPath), typeof(string), typeof(ConnectedButtonGroup),
        new PropertyMetadata(null, (sender, _) => ((ConnectedButtonGroup)sender).Rebuild()));

    public static readonly DependencyProperty OverflowRuleProperty = DependencyProperty.Register(
        nameof(OverflowRule), typeof(ConnectedOverflowRule), typeof(ConnectedButtonGroup),
        new FrameworkPropertyMetadata(ConnectedOverflowRule.MaterialComponents, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty RoundsLastItemProperty = DependencyProperty.Register(
        nameof(RoundsLastItem), typeof(bool), typeof(ConnectedButtonGroup),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MenuUsesLocalResourcesProperty = DependencyProperty.Register(
        nameof(MenuUsesLocalResources), typeof(bool), typeof(ConnectedButtonGroup), new PropertyMetadata(false));

    // Neighbouring buttons are 2 apart.
    private const double Spacing = 2;

    private readonly List<ConnectedToggle> _buttons = [];
    private readonly Button _overflowButton;
    private readonly List<ConnectedToggle> _overflowed = [];

    public ConnectedButtonGroup()
    {
        _overflowButton = new Button { Visibility = Visibility.Hidden };
        _overflowButton.SetResourceReference(StyleProperty, "ConnectedGroupOverflowButton");
        _overflowButton.SetBinding(System.Windows.Automation.AutomationProperties.NameProperty,
            new Binding(nameof(OverflowLabel)) { Source = this });
        _overflowButton.Click += (_, _) => OpenOverflowMenu();
        InternalChildren.Add(_overflowButton);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => (string?)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string? OverflowLabel
    {
        get => (string?)GetValue(OverflowLabelProperty);
        set => SetValue(OverflowLabelProperty, value);
    }

    /// <summary>
    /// How each item is shown, in its button and in the menu.
    /// </summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// With a path, each item says whether it is chosen, and the selected item is not used.
    /// </summary>
    public string? IsCheckedPath
    {
        get => (string?)GetValue(IsCheckedPathProperty);
        set => SetValue(IsCheckedPathProperty, value);
    }

    /// <summary>
    /// With a path, choosing an item runs its command.
    /// </summary>
    public string? CommandPath
    {
        get => (string?)GetValue(CommandPathProperty);
        set => SetValue(CommandPathProperty, value);
    }

    public ConnectedOverflowRule OverflowRule
    {
        get => (ConnectedOverflowRule)GetValue(OverflowRuleProperty);
        set => SetValue(OverflowRuleProperty, value);
    }

    /// <summary>
    /// Whether the last item rounds the row's end when nothing overflows. Without it, the last item keeps a small
    /// corner there, as every item after the first has the shape of a middle button.
    /// </summary>
    public bool RoundsLastItem
    {
        get => (bool)GetValue(RoundsLastItemProperty);
        set => SetValue(RoundsLastItemProperty, value);
    }

    /// <summary>
    /// Whether the menu takes its colors from the group's place, such as a screen with its own palette, instead of
    /// from the application.
    /// </summary>
    public bool MenuUsesLocalResources
    {
        get => (bool)GetValue(MenuUsesLocalResourcesProperty);
        set => SetValue(MenuUsesLocalResourcesProperty, value);
    }

    private void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnCollectionChanged;
        }

        Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        foreach (ConnectedToggle button in _buttons)
        {
            // A resource reference is looked up again when its element leaves the tree, and the view that defines the
            // style is then out of reach; the style found is kept as a plain value instead.
            button.Style = button.Style;
            InternalChildren.Remove(button);
        }

        _buttons.Clear();
        _overflowed.Clear();
        foreach (object item in ItemsSource ?? Array.Empty<object>())
        {
            var button = new ConnectedToggle { DataContext = item };
            if (ItemTemplate is { } template)
            {
                button.Content = item;
                button.ContentTemplate = template;
            }
            else if (DisplayMemberPath is { } path)
            {
                button.SetBinding(ContentControl.ContentProperty, new Binding(path));
            }
            else
            {
                button.Content = item;
            }

            if (IsCheckedPath is { } checkedPath)
            {
                button.SetBinding(ToggleButton.IsCheckedProperty, new Binding(checkedPath) { Mode = BindingMode.OneWay });
            }

            if (CommandPath is { } commandPath)
            {
                button.SetBinding(ButtonBase.CommandProperty, new Binding(commandPath));
            }
            else
            {
                button.Click += (_, _) => Select(button);
            }

            // Added before its style is looked up, so the lookup reaches the view that defines the style. The content
            // comes first, as a style can bind to it and a null name is rejected.
            _buttons.Add(button);
            InternalChildren.Insert(InternalChildren.Count - 1, button);
            button.SetResourceReference(StyleProperty, "ConnectedGroupButton");
        }

        UpdateChecked();
        InvalidateMeasure();
    }

    private void Select(ConnectedToggle button)
    {
        SelectedItem = button.DataContext;
        UpdateChecked();
    }

    private void UpdateChecked()
    {
        if (IsCheckedPath is null)
        {
            foreach (ConnectedToggle button in _buttons)
            {
                button.IsChecked = Equals(button.DataContext, SelectedItem);
            }
        }

        UpdateCorners();
    }

    // The menu lists the buttons that did not fit, the chosen one checked; choosing one presses its button.
    private void OpenOverflowMenu()
    {
        var menu = new ContextMenu { PlacementTarget = _overflowButton, Placement = PlacementMode.Bottom };
        menu.SetResourceReference(StyleProperty, "AppContextMenu");
        foreach (ConnectedToggle button in _overflowed)
        {
            MenuItem item;
            if (ItemTemplate is { } template)
            {
                // The item's own template shows whether it is chosen.
                item = new MenuItem { Header = button.DataContext, HeaderTemplate = template, DataContext = button.DataContext };
            }
            else
            {
                item = new MenuItem { Header = button.Content, IsCheckable = true, IsChecked = button.IsChecked == true };
            }

            if (CommandPath is { } commandPath)
            {
                item.SetBinding(MenuItem.CommandProperty, new Binding(commandPath));
            }
            else
            {
                item.Click += (_, _) => Select(button);
            }

            menu.Items.Add(item);
        }

        if (MenuUsesLocalResources)
        {
            // A menu has no parent of its own, so its resources would come from the application alone.
            AddLogicalChild(menu);
            menu.Closed += (_, _) => RemoveLogicalChild(menu);
        }

        menu.IsOpen = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double available = availableSize.Width;
        _overflowButton.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        foreach (ConnectedToggle button in _buttons)
        {
            button.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        }

        (List<ConnectedToggle> overflowed, bool showOverflow) = OverflowRule == ConnectedOverflowRule.Compose
            ? FindComposeOverflow(available)
            : FindMaterialComponentsOverflow(available);

        // Buttons left out are hidden, not collapsed: a collapsed element measures as 0 wide, which would make it fit
        // on the next measure and the row would alternate between its two states.
        _overflowed.Clear();
        _overflowed.AddRange(overflowed);
        foreach (ConnectedToggle button in _buttons)
        {
            button.Visibility = _overflowed.Contains(button) ? Visibility.Hidden : Visibility.Visible;
        }

        _overflowButton.Visibility = showOverflow ? Visibility.Visible : Visibility.Hidden;
        UpdateCorners();

        double width = 0;
        double height = 0;
        int index = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility != Visibility.Visible)
            {
                continue;
            }

            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width + (index++ > 0 ? Spacing : 0);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    // Buttons are placed in order. Once one does not fit, it and every later one go into the menu, and so does any
    // earlier button that only fits without the menu's button.
    private (List<ConnectedToggle> Overflowed, bool ShowOverflow) FindMaterialComponentsOverflow(double available)
    {
        double overflowSize = _overflowButton.DesiredSize.Width + Spacing;
        List<ConnectedToggle> overflowed = [];
        double displayed = 0;
        for (int i = 0; i < _buttons.Count; i++)
        {
            double size = _buttons[i].DesiredSize.Width + (i > 0 ? Spacing : 0);
            if (displayed + size + overflowSize > available)
            {
                overflowed.Add(_buttons[i]);
            }

            if (displayed + size > available)
            {
                overflowed.AddRange(_buttons.Skip(i + 1));
                return (overflowed, true);
            }

            displayed += size;
        }

        return ([], false);
    }

    // Everything shows when it all fits. Otherwise the menu's button is set aside first, and buttons are shown while
    // each still fits in what remains.
    private (List<ConnectedToggle> Overflowed, bool ShowOverflow) FindComposeOverflow(double available)
    {
        double desired = _buttons.Sum(button => button.DesiredSize.Width) + Spacing * Math.Max(0, _buttons.Count - 1);
        if (desired <= available)
        {
            return ([], false);
        }

        double remaining = available - _overflowButton.DesiredSize.Width;
        int shown = 0;
        while (shown < _buttons.Count && _buttons[shown].DesiredSize.Width <= remaining)
        {
            remaining -= _buttons[shown++].DesiredSize.Width + Spacing;
        }

        return ([.. _buttons.Skip(shown)], true);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility != Visibility.Visible)
            {
                child.Arrange(new Rect());
                continue;
            }

            child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
            x += child.DesiredSize.Width + Spacing;
        }

        return finalSize;
    }

    // The row's two ends are fully round and the corners between buttons small; the chosen button is fully round.
    private void UpdateCorners()
    {
        List<FrameworkElement> shown = [.. InternalChildren.OfType<FrameworkElement>().Where(child => child.Visibility == Visibility.Visible)];
        for (int i = 0; i < shown.Count; i++)
        {
            bool last = i == shown.Count - 1 && (shown[i] == _overflowButton || RoundsLastItem);
            ConnectedPosition.SetIsFirst(shown[i], i == 0);
            ConnectedPosition.SetIsLast(shown[i], last);
        }
    }
}

/// <summary>
/// Which rule decides the buttons that go into the menu: the button group of Material Components for views, or the
/// one of Compose.
/// </summary>
public enum ConnectedOverflowRule
{
    MaterialComponents,
    Compose,
}

/// <summary>
/// A toggle whose checked state comes only from its group or its item, so a click does not toggle it by itself.
/// </summary>
public sealed class ConnectedToggle : ToggleButton
{
    protected override void OnToggle()
    {
    }
}

/// <summary>
/// Where a button sits in a connected row, which its template turns into its corners.
/// </summary>
public static class ConnectedPosition
{
    public static readonly DependencyProperty IsFirstProperty = DependencyProperty.RegisterAttached(
        "IsFirst", typeof(bool), typeof(ConnectedPosition), new PropertyMetadata(false));

    public static readonly DependencyProperty IsLastProperty = DependencyProperty.RegisterAttached(
        "IsLast", typeof(bool), typeof(ConnectedPosition), new PropertyMetadata(false));

    public static bool GetIsFirst(DependencyObject element) => (bool)element.GetValue(IsFirstProperty);

    public static void SetIsFirst(DependencyObject element, bool value) => element.SetValue(IsFirstProperty, value);

    public static bool GetIsLast(DependencyObject element) => (bool)element.GetValue(IsLastProperty);

    public static void SetIsLast(DependencyObject element, bool value) => element.SetValue(IsLastProperty, value);
}

/// <summary>
/// The corners of a 40-high button in a connected row: 20, fully round, at the row's ends; 8 between buttons, 4 while
/// a toggle is pressed, and fully round on the chosen toggle.
/// </summary>
public sealed class ConnectedCornersConverter : System.Windows.Data.IMultiValueConverter
{
    public static readonly ConnectedCornersConverter Instance = new();

    private const double Full = 20;
    private const double Inner = 8;
    private const double PressedInner = 4;

    // Values: is first, is last, is checked, is pressed. The parameter "Toggle" marks a checkable button.
    public object Convert(object[] values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        bool first = values.Length > 0 && values[0] is true;
        bool last = values.Length > 1 && values[1] is true;
        bool isChecked = values.Length > 2 && values[2] is true;
        bool pressed = values.Length > 3 && values[3] is true;
        bool checkable = parameter as string == "Toggle";
        double inner = checkable && pressed ? PressedInner : checkable && isChecked ? Full : Inner;
        double left = first ? Full : inner;
        double right = last ? Full : inner;
        return new CornerRadius(left, right, right, left);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
