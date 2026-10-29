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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using System.Net.Http;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.OpenMeteo;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// Searches places by name and hands the chosen one to the caller.
/// </summary>
public sealed partial class SearchViewModel : ObservableObject
{
    private readonly OpenMeteoService _openMeteo;
    private readonly Action<Location> _onLocationChosen;

    public SearchViewModel(OpenMeteoService openMeteo, Action<Location> onLocationChosen)
    {
        _openMeteo = openMeteo;
        _onLocationChosen = onLocationChosen;
    }

    public ObservableCollection<LocationResult> Results { get; } = [];

    public string Attribution => Strings.Format("location_results_by", OpenMeteoService.LocationSearchAttribution);

    [ObservableProperty]
    private string? _query;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string? _noResultsMessage;

    [ObservableProperty]
    private string? _noResultsAdvice;

    [RelayCommand]
    private async Task SearchAsync()
    {
        string query = Query?.Trim() ?? "";
        if (query.Length == 0)
        {
            return;
        }

        IsSearching = true;
        NoResultsMessage = null;
        NoResultsAdvice = null;
        Results.Clear();
        try
        {
            IReadOnlyList<LocationAddressInfo> addresses = await _openMeteo.RequestLocationSearchAsync(query);
            foreach (LocationAddressInfo address in addresses)
            {
                // Open-Meteo is the only source, so it takes every feature it offers. Current conditions stay empty
                // so they are taken from the current hour of the forecast, which keeps them consistent with it.
                Location location = new Location
                {
                    ForecastSource = OpenMeteoService.Id,
                    AirQualitySource = OpenMeteoService.Id,
                    PollenSource = OpenMeteoService.Id,
                    MinutelySource = OpenMeteoService.Id,
                }.ToLocationWithAddressInfo(address, overwriteCoordinates: true);
                Results.Add(new LocationResult(location));
            }

            if (Results.Count == 0)
            {
                NoResultsMessage = Strings.Format("location_search_no_results", OpenMeteoService.Name, query);
                NoResultsAdvice = Strings.Get("location_search_no_results_advice");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or LocationSearchException or TaskCanceledException)
        {
            NoResultsMessage = Strings.Get("location_message_search_failed");
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void Choose(Location? location)
    {
        if (location is not null)
        {
            _onLocationChosen(location);
        }
    }
}
