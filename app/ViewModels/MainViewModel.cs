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
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// Opens on the first saved location, or on the location search when none is saved.
/// A place chosen in the search is saved at the end of the list and shown.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly OpenMeteoService _openMeteo;
    private readonly WeatherRefresher _refresher;
    private readonly LocationRepository _locations;
    private readonly WeatherRepository _weather;
    private readonly TemperatureUnit _temperatureUnit;

    [ObservableProperty]
    private ObservableObject? _currentPage;

    public MainViewModel(
        OpenMeteoService openMeteo,
        WeatherRefresher refresher,
        LocationRepository locations,
        WeatherRepository weather,
        TemperatureUnit temperatureUnit)
    {
        _openMeteo = openMeteo;
        _refresher = refresher;
        _locations = locations;
        _weather = weather;
        _temperatureUnit = temperatureUnit;
    }

    public async Task LoadAsync()
    {
        Location? first = await _locations.GetFirstLocationAsync();
        if (first is null)
        {
            CurrentPage = new SearchViewModel(_openMeteo, location => _ = AddAndShowAsync(location));
        }
        else
        {
            await ShowWeatherAsync(first);
        }
    }

    // A place already in the list is not added twice.
    private async Task AddAndShowAsync(Location location)
    {
        IReadOnlyList<Location> saved = await _locations.GetAllLocationsAsync();
        if (saved.All(existing => existing.FormattedId != location.FormattedId))
        {
            await _locations.AddAllAsync([.. saved, location]);
        }

        await ShowWeatherAsync(location);
    }

    private async Task ShowWeatherAsync(Location location)
    {
        location = location with { Weather = await _weather.GetWeatherByLocationIdAsync(location.FormattedId) };
        WeatherViewModel weather = new(_refresher, _temperatureUnit, location);
        CurrentPage = weather;
        weather.RefreshCommand.Execute(null);
    }
}
