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

using CommunityToolkit.Mvvm.ComponentModel;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// Shows the location search until a place is chosen, then the weather for that place.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly WeatherRefresher _refresher;
    private readonly TemperatureUnit _temperatureUnit;

    [ObservableProperty]
    private ObservableObject? _currentPage;

    public MainViewModel(OpenMeteoService openMeteo, WeatherRefresher refresher, TemperatureUnit temperatureUnit)
    {
        _refresher = refresher;
        _temperatureUnit = temperatureUnit;
        CurrentPage = new SearchViewModel(openMeteo, ShowWeather);
    }

    private void ShowWeather(Location location)
    {
        WeatherViewModel weather = new(_refresher, _temperatureUnit, location);
        CurrentPage = weather;
        weather.RefreshCommand.Execute(null);
    }
}
