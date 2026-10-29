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
using LibreccioWeather.ViewModels.Forecast;

namespace LibreccioWeather.Views.Weather;

public partial class ForecastCardView : UserControl
{
    public ForecastCardView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ForecastCardViewModel card)
            {
                ForecastScroller.ScrollToHorizontalOffset(card.InitialIndex * 67);
            }
        };
    }

    private void OnForecastMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            ForecastScroller.ScrollToHorizontalOffset(ForecastScroller.HorizontalOffset - e.Delta);
        }
        else
        {
            // The nested viewer only scrolls horizontally, so vertical wheel input belongs to the page.
            RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
            });
        }
    }
}
