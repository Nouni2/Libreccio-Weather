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
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Input;
using LibreccioWeather.Localization;
using LibreccioWeather.ViewModels;
using LibreccioWeather.Windowing;

namespace LibreccioWeather;

public partial class MainWindow : Window
{
    private ResourceDictionary? _desktopTheme;
    private ResourceDictionary? _lightColors;
    private IInputElement? _focusBeforeDialog;
    private readonly WindowCaption _caption;

    public MainWindow()
    {
        InitializeComponent();
        // The window's event handlers keep the autoscroll and the sideways wheel alive for as long as the window.
        _ = new AutoScroll(this);
        _ = new HorizontalWheel(this);
        DataContextChanged += OnViewModelChanged;
        DialogHost.IsVisibleChanged += OnDialogVisibilityChanged;
        InterfaceHost.SizeChanged += (_, _) => UpdateWindowShape();
        _caption = new WindowCaption(this, WindowRoot, TitleBar, TitleBarIcon, MaximizeButton);
        StateChanged += (_, _) => UpdateCaptionButtonNames();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _caption.Attach();
        UpdateCaptionButtonNames();
    }

    // Minimize, maximize and restore are named with Windows' own words for them, from the window's system menu.
    private void UpdateCaptionButtonNames()
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetCaptionButtonName(MinimizeButton, SystemStrings.Minimize(handle));
        SetCaptionButtonName(MaximizeButton,
            WindowState == WindowState.Maximized ? SystemStrings.Restore(handle) : SystemStrings.Maximize(handle));
    }

    private static void SetCaptionButtonName(Button button, string name)
    {
        button.ToolTip = name;
        AutomationProperties.SetName(button, name);
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    // A mouse click on the maximize button is handled by WindowCaption, since Windows takes the pointer there; this one
    // comes from the keyboard or a screen reader.
    private void OnMaximizeClick(object sender, RoutedEventArgs e) => _caption.ToggleMaximized();

    private void OnCloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);

    // The main screen's overflow menu opens below its button on a click, as a toolbar's overflow menu does.
    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    private void UpdateWindowShape()
    {
        if (DataContext is MainViewModel main && InterfaceHost.ActualWidth > 0)
        {
            main.UpdateWindowShape(InterfaceHost.ActualWidth, InterfaceHost.ActualHeight);
        }
    }

    // The side buttons of a mouse move through the screens like a browser's back and forward buttons.
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Handled || DataContext is not MainViewModel main)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.XButton1)
        {
            main.BrowseBack();
            e.Handled = true;
        }
        else if (e.ChangedButton == MouseButton.XButton2)
        {
            main.BrowseForward();
            e.Handled = true;
        }
    }

    private void OnDialogVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not MainViewModel { IsDesktopInterfaceEnabled: true })
        {
            return;
        }

        if (DialogHost.IsVisible)
        {
            _focusBeforeDialog = Keyboard.FocusedElement;
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (DialogHost.IsVisible)
                {
                    DialogHost.Focus();
                    DialogHost.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
            });
        }
        else if (_focusBeforeDialog is UIElement { IsVisible: true, IsEnabled: true } previous)
        {
            previous.Focus();
            _focusBeforeDialog = null;
        }
    }

    private void OnViewModelChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel previous)
        {
            previous.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel current)
        {
            current.PropertyChanged += OnViewModelPropertyChanged;
            ApplyInterface(current);
            UpdateWindowShape();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel main
            && e.PropertyName is nameof(MainViewModel.IsDesktopInterfaceEnabled) or nameof(MainViewModel.InterfaceCulture)
                or nameof(MainViewModel.IsAppDark))
        {
            ApplyInterface(main);
        }
    }

    private void ApplyInterface(MainViewModel main)
    {
        // Controls resolve static resources during construction, before they have a parent window.
        ResourceDictionary applicationResources = System.Windows.Application.Current.Resources;
        if (main.IsDesktopInterfaceEnabled && _desktopTheme is null)
        {
            _desktopTheme = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Theme/DesktopTheme.xaml", UriKind.Absolute),
            };
            applicationResources.MergedDictionaries.Add(_desktopTheme);
        }
        else if (!main.IsDesktopInterfaceEnabled && _desktopTheme is not null)
        {
            applicationResources.MergedDictionaries.Remove(_desktopTheme);
            _desktopTheme = null;
        }

        // The light palette replaces the dark one of the application's resources while the app is light.
        if (!main.IsAppDark && _lightColors is null)
        {
            _lightColors = Appearance.ThemeManager.GetLightColors();
            applicationResources.MergedDictionaries.Add(_lightColors);
        }
        else if (main.IsAppDark && _lightColors is not null)
        {
            applicationResources.MergedDictionaries.Remove(_lightColors);
            _lightColors = null;
        }

        LayoutDirection.Apply(applicationResources);
        NativeDigits.Apply(applicationResources);
        FlowDirection = LayoutDirection.Current;
        Title = Strings.Get("desktop_app_name");
#if DEBUG
        Title += " — " + Strings.Get("settings_debug");
#endif

        SetCaptionButtonName(CloseButton, Strings.Get("action_close"));

        // Recreating the templates also reloads markup translations after a language change.
        InterfaceHost.ContentTemplate = null;
        InterfaceHost.ContentTemplate = (DataTemplate)FindResource("InterfacePages");
        TitleBarPage.ContentTemplate = null;
        TitleBarPage.ContentTemplate = (DataTemplate)FindResource("WeatherTitleBar");
    }
}
