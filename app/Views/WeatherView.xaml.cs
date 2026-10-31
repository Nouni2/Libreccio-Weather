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
using System.Windows.Threading;
using LibreccioWeather.Controls.WeatherCards;
using LibreccioWeather.ViewModels;
using LibreccioWeather.ViewModels.WeatherCards;

namespace LibreccioWeather.Views;

public partial class WeatherView : UserControl
{
    // The time since the last refresh is written again every minute, and only while the screen is shown.
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(1) };

    public WeatherView()
    {
        InitializeComponent();
        _clock.Tick += (_, _) => (DataContext as WeatherViewModel)?.UpdateRefreshTime();
        Loaded += (_, _) =>
        {
            (DataContext as WeatherViewModel)?.UpdateRefreshTime();
            _clock.Start();
            FindCardsPanel(WeatherScroll)?.AttachHeader(WeatherHeader);
        };
        Unloaded += (_, _) => _clock.Stop();
        DataContextChanged += OnDataContextChanged;
    }

    private ResourceDictionary? _palette;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is WeatherViewModel previous)
        {
            previous.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is WeatherViewModel current)
        {
            current.PropertyChanged += OnViewModelPropertyChanged;
            ApplyPalette(current.IsLightTheme);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WeatherViewModel.IsLightTheme) && sender is WeatherViewModel viewModel)
        {
            ApplyPalette(viewModel.IsLightTheme);
        }
    }

    // The screen's own palette overrides the app's, so a location can be light while the rest of the app is dark.
    private void ApplyPalette(bool light)
    {
        if (_palette is not null)
        {
            Resources.MergedDictionaries.Remove(_palette);
        }

        _palette = Appearance.ThemeManager.GetViewColors(light);
        Resources.MergedDictionaries.Add(_palette);
    }

    private static WeatherCardsPanel? FindCardsPanel(DependencyObject parent)
    {
        if (parent is WeatherCardsPanel panel)
        {
            return panel;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            if (FindCardsPanel(VisualTreeHelper.GetChild(parent, i)) is { } child)
            {
                return child;
            }
        }

        return null;
    }

    private void OnAstroCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AstroCardViewModel } && DataContext is WeatherViewModel weather)
        {
            weather.OpenAstroDetailsCommand.Execute(null);
        }
    }

    private void OnConditionsCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ConditionsCardViewModel card } && DataContext is WeatherViewModel weather)
        {
            weather.OpenCardDetailsCommand.Execute(card);
        }
    }
}
