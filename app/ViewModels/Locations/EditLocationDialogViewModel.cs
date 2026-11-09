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
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Localization;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels.Locations;

/// <summary>
/// An entry of the edit location dialog: an icon, a title and a summary.
/// </summary>
public sealed class EditLocationEntry(string iconKey, string title, string summary, Action onClick)
{
    public Geometry Icon { get; } = (Geometry)Application.Current.FindResource(iconKey);

    // Icons come in two viewport sizes, which the view scales to the same size.
    public double IconViewport => Icon.Bounds.Right > 24 || Icon.Bounds.Bottom > 24 ? 960 : 24;

    public string Title { get; } = title;

    public string Summary { get; } = summary;

    public IRelayCommand ClickCommand { get; } = new RelayCommand(onClick);
}

/// <summary>
/// The edit location dialog of the main screen: the location's sources, its own preferences such as a custom name,
/// and the global settings, with a button to delete it while other locations remain.
/// </summary>
public sealed class EditLocationDialogViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Location _location;

    public EditLocationDialogViewModel(MainViewModel main, Location location)
    {
        _main = main;
        _location = location;
        Title = Strings.Get("action_edit_location");
        CloseText = Strings.Get("action_close");
        DeleteText = Strings.Get("action_delete");
        CanDelete = main.Locations.Count > 1;

        List<EditLocationEntry> entries =
        [
            new("IconFactory", Strings.Get("settings_weather_sources"), Strings.Get("settings_weather_sources_per_location_summary"), OpenSources),
        ];

        // Open-Meteo's per-location preferences, its forecast models, are not offered, so only the custom name is.
        if (!location.IsCurrentPosition)
        {
            entries.Add(new("IconSettings", Strings.Get("settings_per_location"), Strings.Get("settings_per_location_summary"), OpenPerLocation));
        }

        entries.Add(new("IconHome", Strings.Get("settings_global"), Strings.Get("settings_main_summary"), () =>
        {
            main.CloseDialog();
            main.NavigateTo(SettingsPages.MainScreen(main));
        }));
        Entries = entries;
        CloseCommand = new RelayCommand(main.CloseDialog);
        DeleteCommand = new RelayCommand(ConfirmDelete);
    }

    public string Title { get; }

    public string CloseText { get; }

    public string DeleteText { get; }

    public bool CanDelete { get; }

    public IReadOnlyList<EditLocationEntry> Entries { get; }

    public IRelayCommand CloseCommand { get; }

    public IRelayCommand DeleteCommand { get; }

    private void OpenSources()
    {
        _main.ShowDialog(new SecondarySourcesDialogViewModel(_main, _location, newLocation =>
        {
            if (newLocation is null)
            {
                _main.ShowDialog(this);
                return;
            }

            _main.CloseDialog();

            // A new forecast source that matches an existing location is not saved.
            if (_location.ForecastSource != newLocation.ForecastSource && _main.LocationExists(newLocation))
            {
                _main.ShowSnackbar(Strings.Get("location_message_already_exists"));
            }
            else
            {
                _ = _main.UpdateLocationPreferencesAsync(newLocation, _location);
            }
        }, _main.LocationExists));
    }

    private void OpenPerLocation()
    {
        _main.ShowDialog(new PerLocationDialogViewModel(_main, _location, this));
    }

    private void ConfirmDelete()
    {
        string message = _location.City.Length > 0
            ? Strings.Format("location_delete_location_dialog_message", _location.City)
            : Strings.Get("location_delete_location_dialog_message_no_name");
        _main.ShowDialog(new ConfirmDialogViewModel(
            Strings.Get("location_delete_location_dialog_title"),
            message,
            Strings.Get("action_confirm"),
            () =>
            {
                _main.CloseDialog();
                _ = DeleteAsync();
            },
            () => _main.ShowDialog(this)));
    }

    private async Task DeleteAsync()
    {
        int index = _main.Locations.ToList().FindIndex(item => item.FormattedId == _location.FormattedId);
        if (_main.Locations.Count > 1)
        {
            if (index >= 0)
            {
                await _main.DeleteLocationAsync(index);
                _main.ShowSnackbar(Strings.Get("location_message_deleted"));
            }
        }
        else
        {
            _main.ShowSnackbar(Strings.Get("location_message_list_cannot_be_empty"));
        }
    }
}

/// <summary>
/// The preferences of one location: its custom name, which replaces the city and district in the interface, and
/// Open-Meteo's weather models. Closing saves a changed name.
/// </summary>
public sealed partial class PerLocationDialogViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Location _location;
    private readonly ObservableObject _parent;
    private bool _hasChangedPreferences;
    private string? _customName;
    private string _content;

    public PerLocationDialogViewModel(MainViewModel main, Location location, ObservableObject parent)
    {
        _main = main;
        _location = location;
        _parent = parent;
        _customName = location.CustomName;
        _content = string.IsNullOrEmpty(location.CustomName) ? location.CityAndDistrict : location.CustomName;
        Title = Strings.Get("settings_per_location");
        SectionTitle = Strings.Get("settings_section_general");
        CustomNameTitle = Strings.Get("location_custom_name");

        // Closing saves a changed custom name, so the button says Done rather than Close.
        CloseText = Strings.Get("action_done");
        CustomNameSummary = _content.Length == 0 ? location.CityAndDistrict : _content;

        // Open-Meteo has a section when it gives the forecast, the current conditions or the minutely precipitation;
        // a feature without a source of its own takes the forecast source.
        HasOpenMeteoSection = new[]
        {
            location.ForecastSource,
            location.CurrentSource ?? location.ForecastSource,
            location.MinutelySource ?? location.ForecastSource,
        }.Contains(OpenMeteoService.Id);
        OpenMeteoSectionTitle = OpenMeteoService.Name;
        WeatherModelsTitle = Strings.Get("settings_weather_source_open_meteo_weather_models");

        // The chosen models, sorted by name in the selected language.
        StringComparer collator = StringComparer.Create(Strings.Culture, ignoreCase: false);
        WeatherModelsSummary = string.Join(Strings.GetUnit("locale_separator"),
            OpenMeteoService.GetWeatherModels(location).Select(model => model.GetName()).OrderBy(name => name, collator));
    }

    public bool HasOpenMeteoSection { get; }

    public string OpenMeteoSectionTitle { get; }

    public string WeatherModelsTitle { get; }

    public string WeatherModelsSummary { get; }

    // Saving models leaves the dialog at once with the location's other preferences as they were saved, so a custom
    // name changed but not yet saved is dropped.
    [RelayCommand]
    private void OpenWeatherModels()
    {
        _main.ShowDialog(new WeatherModelsDialogViewModel(_main, _location, this, models =>
        {
            Dictionary<string, IReadOnlyDictionary<string, string>> parameters = _location.Parameters
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            Dictionary<string, string> openMeteo = parameters.TryGetValue(OpenMeteoService.Id, out IReadOnlyDictionary<string, string>? saved)
                ? saved.ToDictionary(pair => pair.Key, pair => pair.Value)
                : [];
            openMeteo["weatherModels"] = models;
            parameters[OpenMeteoService.Id] = openMeteo;
            _main.CloseDialog();
            _ = _main.UpdateLocationPreferencesAsync(_location with
            {
                Parameters = parameters,
                Weather = SecondarySourcesDialogViewModel.WithoutUpdateTimes(_location.Weather),
            }, _location);
        }));
    }

    public string Title { get; }

    public string SectionTitle { get; }

    public string CustomNameTitle { get; }

    public string CloseText { get; }

    [ObservableProperty]
    private string _customNameSummary = "";

    [RelayCommand]
    private void EditCustomName()
    {
        _main.ShowDialog(new EditTextDialogViewModel(CustomNameTitle, _content, null, null, null, value =>
        {
            _content = value;
            CustomNameSummary = value.Length == 0 ? _location.CityAndDistrict : value;
            if (value != _location.CityAndDistrict && value != _location.CustomName)
            {
                _customName = value;
                _hasChangedPreferences = true;
            }
            else if ((value == _location.CityAndDistrict || value.Length == 0) && !string.IsNullOrEmpty(_location.CustomName))
            {
                _customName = "";
                _hasChangedPreferences = true;
            }

            _main.ShowDialog(this);
        }, () => _main.ShowDialog(this)));
    }

    [RelayCommand]
    private void Close()
    {
        if (_hasChangedPreferences)
        {
            _main.CloseDialog();
            _ = _main.UpdateLocationPreferencesAsync(_location with { CustomName = _customName }, _location);
        }
        else
        {
            _main.ShowDialog(_parent);
        }
    }
}

/// <summary>
/// A question with a confirm button and Cancel.
/// </summary>
public sealed class ConfirmDialogViewModel(string title, string message, string confirmText, Action onConfirm, Action onCancel)
    : ObservableObject
{
    public string Title { get; } = title;

    public string Message { get; } = message;

    public string ConfirmText { get; } = confirmText;

    public string CancelText { get; } = SystemStrings.Cancel;

    public IRelayCommand ConfirmCommand { get; } = new RelayCommand(onConfirm);

    public IRelayCommand CancelCommand { get; } = new RelayCommand(onCancel);
}
