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
using System.Windows.Threading;
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
        };
        Unloaded += (_, _) => _clock.Stop();
    }

    private void OnConditionsCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ConditionsCardViewModel card } && DataContext is WeatherViewModel weather)
        {
            weather.OpenCardDetailsCommand.Execute(card);
        }
    }
}
