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
using System.Windows.Threading;
using LibreccioWeather.ViewModels;
using Point = System.Windows.Point;

namespace LibreccioWeather.Views;

public partial class AlertsView : UserControl
{
    private ResourceDictionary? _palette;

    public AlertsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // The screen's own palette overrides the app's, so the screen follows the location's daylight, then the list
    // starts at the alert that was opened once it is laid out.
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not AlertsViewModel alerts)
        {
            return;
        }

        if (_palette is not null)
        {
            Resources.MergedDictionaries.Remove(_palette);
        }

        _palette = Appearance.ThemeManager.GetScreenColors(alerts.IsLightTheme);
        Resources.MergedDictionaries.Add(_palette);
        Dispatcher.BeginInvoke(new Action(() => ScrollTo(alerts.ScrollIndex)), DispatcherPriority.Loaded);
    }

    private void ScrollTo(int index)
    {
        if (Alerts.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
        {
            AlertsScroll.ScrollToVerticalOffset(container.TranslatePoint(new Point(0, 0), Alerts).Y);
        }
    }
}
