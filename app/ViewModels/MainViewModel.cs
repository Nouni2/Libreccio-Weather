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

using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibreccioWeather.Core.Background;
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// The screens of the main window and the saved locations they show. The main screen shows the selected location; the
/// location list, the search and the settings open on top of it and go back to it.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    // Breezy Weather shows each message for three seconds.
    private static readonly TimeSpan SnackbarDuration = TimeSpan.FromSeconds(3);

    private readonly OpenMeteoService _openMeteo;
    private readonly WeatherRefresher _refresher;
    private readonly LocationRepository _locationRepository;
    private readonly WeatherRepository _weatherRepository;
    private readonly SettingsManager _settings;
    private readonly string _regionCode;
    private readonly Stack<ObservableObject> _backStack = new();
    private readonly DispatcherTimer _snackbarTimer;
    private List<Location> _locations = [];
    private string? _currentFormattedId;

    [ObservableProperty]
    private ObservableObject? _currentPage;

    [ObservableProperty]
    private ObservableObject? _dialog;

    [ObservableProperty]
    private SnackbarViewModel? _snackbar;

    public MainViewModel(
        OpenMeteoService openMeteo,
        WeatherRefresher refresher,
        LocationRepository locationRepository,
        WeatherRepository weatherRepository,
        WeatherUpdateJob updateJob,
        SettingsManager settings,
        string regionCode)
    {
        _openMeteo = openMeteo;
        _refresher = refresher;
        _locationRepository = locationRepository;
        _weatherRepository = weatherRepository;
        _settings = settings;
        _regionCode = regionCode;

        _snackbarTimer = new DispatcherTimer { Interval = SnackbarDuration };
        _snackbarTimer.Tick += (_, _) =>
        {
            _snackbarTimer.Stop();
            Snackbar = null;
        };

        // Background updates finish on another thread, so the screen is updated on the thread that built this view model.
        SynchronizationContext? uiContext = SynchronizationContext.Current;
        updateJob.LocationUpdated += (_, location) =>
        {
            if (uiContext is null)
            {
                UpdateFromBackground(location);
            }
            else
            {
                uiContext.Post(state => UpdateFromBackground(location), null);
            }
        };

        // Units and other settings change what the main screen shows, so it is built again with them.
        settings.SettingsChanged += (_, _) => RebuildMainScreen();
    }

    /// <summary>
    /// Raised with the location now on screen, or null when no location is shown.
    /// </summary>
    public event EventHandler<Location?>? ShownLocationChanged;

    public IReadOnlyList<Location> Locations => _locations;

    public string? CurrentFormattedId => _currentFormattedId;

    public SettingsManager Settings => _settings;

    public string RegionCode => _regionCode;

    /// <summary>
    /// Opens on the first saved location, or on the empty location list when none is saved.
    /// </summary>
    public async Task LoadAsync()
    {
        _locations = await LoadLocationsWithWeatherAsync();
        if (_locations.Count == 0)
        {
            CurrentPage = new ManagementViewModel(this);
            ShownLocationChanged?.Invoke(this, null);
        }
        else
        {
            ShowLocation(_locations[0]);
        }
    }

    public void NavigateTo(ObservableObject page)
    {
        if (CurrentPage is not null)
        {
            _backStack.Push(CurrentPage);
        }

        CurrentPage = page;
    }

    /// <summary>
    /// Goes back to the previous screen. The location list opened at first launch goes to the main screen once a
    /// location is selected.
    /// </summary>
    public void GoBack()
    {
        if (_backStack.Count > 0)
        {
            CurrentPage = _backStack.Pop();
        }
        else if (CurrentLocation() is { } current)
        {
            ShowLocation(current);
        }
    }

    public bool CanGoBack => _backStack.Count > 0 || _currentFormattedId is not null;

    public void OpenLocationList()
    {
        _ = OpenLocationListAsync();
    }

    // Refreshes since the list was read changed the stored weather that each card summarises.
    private async Task OpenLocationListAsync()
    {
        await ReloadLocationsAsync();
        NavigateTo(new ManagementViewModel(this));
    }

    public void OpenSearch()
    {
        NavigateTo(new SearchViewModel(_openMeteo, location => _ = AddFromSearchAsync(location), GoBack));
    }

    public void OpenSettings()
    {
        NavigateTo(SettingsPages.Root(this));
    }

    /// <summary>
    /// Shows a location on the main screen, replacing every screen opened on top of it.
    /// </summary>
    public void ShowLocation(Location location)
    {
        _backStack.Clear();
        _currentFormattedId = location.FormattedId;
        WeatherViewModel weather = new(_refresher, this, location);
        CurrentPage = weather;
        ShownLocationChanged?.Invoke(this, location);
        weather.RefreshCommand.Execute(null);
    }

    public void ShowSnackbar(string text, string? actionText = null, Action? action = null)
    {
        _snackbarTimer.Stop();
        Snackbar = new SnackbarViewModel(text, actionText, action is null ? null : () =>
        {
            _snackbarTimer.Stop();
            Snackbar = null;
            action();
        });
        _snackbarTimer.Start();
    }

    public void ShowDialog(ObservableObject dialog)
    {
        Dialog = dialog;
    }

    public void CloseDialog()
    {
        Dialog = null;
    }

    /// <summary>
    /// Whether the location on the main screen is being refreshed, during which the list cannot change.
    /// </summary>
    public bool IsRefreshing => FindMainScreen()?.IsLoading == true;

    /// <summary>
    /// The same place from the same forecast source is only saved once.
    /// </summary>
    public bool LocationExists(Location location)
    {
        return _locations.Any(item => item.Longitude == location.Longitude
            && item.Latitude == location.Latitude
            && item.ForecastSource == location.ForecastSource);
    }

    public async Task MoveLocationAsync(int from, int to)
    {
        if (from == to)
        {
            return;
        }

        List<Location> locations = [.. _locations];
        Location moved = locations[from];
        locations.RemoveAt(from);
        locations.Insert(to, moved);
        _locations = locations;
        await _locationRepository.AddAllAsync(_locations);
    }

    /// <summary>
    /// Deletes a location. When it was on the main screen, the first remaining location takes its place.
    /// </summary>
    public async Task<Location> DeleteLocationAsync(int index)
    {
        Location location = _locations[index];
        _locations = [.. _locations.Where((_, position) => position != index)];
        if (_currentFormattedId == location.FormattedId)
        {
            _currentFormattedId = _locations.FirstOrDefault()?.FormattedId;
            ReplaceMainScreen();
        }

        await _locationRepository.DeleteAsync(location.FormattedId);
        return location;
    }

    public async Task AddLocationAsync(Location location, int? index = null)
    {
        if (_locations.Any(item => item.FormattedId == location.FormattedId))
        {
            return;
        }

        List<Location> locations = [.. _locations];
        locations.Insert(index ?? locations.Count, location);
        _locations = locations;
        if (_currentFormattedId is null)
        {
            _currentFormattedId = location.FormattedId;
            ReplaceMainScreen();
        }

        await _locationRepository.AddAllAsync(_locations);
    }

    /// <summary>
    /// Reads every saved location again with its stored weather, which a refresh may have changed.
    /// </summary>
    public async Task ReloadLocationsAsync()
    {
        _locations = await LoadLocationsWithWeatherAsync();
    }

    /// <summary>
    /// Switches to another language at once, or back to the Windows language with null. Translated text is read when
    /// each screen is built, so every open screen is built again.
    /// </summary>
    public void ChangeLanguage(string? languageCode)
    {
        Strings.Culture = AppLanguages.Resolve(languageCode, System.Globalization.CultureInfo.CurrentUICulture);

        // The setting raises SettingsChanged, which builds the main screen again in the new language.
        _settings.Language = languageCode;

        List<ObservableObject> screens = [.. _backStack.Reverse().Select(Rebuilt)];
        _backStack.Clear();
        foreach (ObservableObject screen in screens)
        {
            _backStack.Push(screen);
        }

        if (CurrentPage is not null)
        {
            CurrentPage = Rebuilt(CurrentPage);
        }
    }

    private ObservableObject Rebuilt(ObservableObject screen)
    {
        switch (screen)
        {
            case SettingsPageViewModel settingsPage:
                return settingsPage.Rebuild();
            case ManagementViewModel management:
                management.Refresh();
                return management;
            default:
                return screen;
        }
    }

    private async Task AddFromSearchAsync(Location location)
    {
        GoBack();
        if (LocationExists(location))
        {
            ShowSnackbar(Strings.Get("location_message_already_exists"));
        }
        else
        {
            await AddLocationAsync(location);
            if (CurrentPage is ManagementViewModel management)
            {
                management.Refresh();
            }

            ShowSnackbar(Strings.Get("location_message_added"));
        }
    }

    private void UpdateFromBackground(Location location)
    {
        int index = _locations.FindIndex(item => item.FormattedId == location.FormattedId);
        if (index >= 0)
        {
            _locations[index] = location;
        }

        if (FindMainScreen() is { } shown && shown.LocationFormattedId == location.FormattedId)
        {
            shown.UpdateFromBackground(location);
        }

        if (CurrentPage is ManagementViewModel management)
        {
            management.Refresh();
        }
    }

    private void RebuildMainScreen()
    {
        if (CurrentLocation() is null)
        {
            return;
        }

        ReplaceMainScreen();
    }

    // The main screen is the bottom of the back stack, or the current screen when nothing is open on top of it.
    private void ReplaceMainScreen()
    {
        Location? current = CurrentLocation();
        WeatherViewModel? replacement = current is null ? null : new WeatherViewModel(_refresher, this, current);
        List<ObservableObject> screens = [.. _backStack.Reverse()];
        if (screens.Count > 0 && screens[0] is WeatherViewModel)
        {
            screens.RemoveAt(0);
        }

        if (CurrentPage is WeatherViewModel)
        {
            CurrentPage = replacement;
        }
        else if (replacement is not null)
        {
            screens.Insert(0, replacement);
        }

        _backStack.Clear();
        foreach (ObservableObject screen in screens)
        {
            _backStack.Push(screen);
        }

        ShownLocationChanged?.Invoke(this, current);
        replacement?.RefreshCommand.Execute(null);
    }

    private WeatherViewModel? FindMainScreen()
    {
        return CurrentPage as WeatherViewModel ?? _backStack.OfType<WeatherViewModel>().FirstOrDefault();
    }

    private Location? CurrentLocation()
    {
        return _locations.FirstOrDefault(location => location.FormattedId == _currentFormattedId);
    }

    private async Task<List<Location>> LoadLocationsWithWeatherAsync()
    {
        List<Location> locations = [];
        foreach (Location location in await _locationRepository.GetAllLocationsAsync())
        {
            locations.Add(location with { Weather = await _weatherRepository.GetWeatherByLocationIdAsync(location.FormattedId) });
        }

        return locations;
    }
}
