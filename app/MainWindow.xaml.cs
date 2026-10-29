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
using System.Windows.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.ViewModels;

namespace LibreccioWeather;

public partial class MainWindow : Window
{
    private ResourceDictionary? _desktopTheme;
    private IInputElement? _focusBeforeDialog;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnViewModelChanged;
        DialogHost.IsVisibleChanged += OnDialogVisibilityChanged;
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
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MainViewModel main
            && e.PropertyName is nameof(MainViewModel.IsDesktopInterfaceEnabled) or nameof(MainViewModel.InterfaceCulture))
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

        Title = Strings.Get("desktop_app_name");

        // Recreating the template also reloads markup translations after a language change.
        InterfaceHost.ContentTemplate = null;
        InterfaceHost.ContentTemplate = (DataTemplate)FindResource("InterfacePages");
    }
}
