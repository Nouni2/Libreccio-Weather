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
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.Locations;

/// <summary>
/// One Open-Meteo weather model with its switch.
/// </summary>
public sealed partial class WeatherModelItem : ObservableObject
{
    // Attached after the initial state is set, so building the list does not count as a change.
    private Action<WeatherModelItem, bool>? _onSwitched;

    [ObservableProperty]
    private bool _isEnabled;

    public WeatherModelItem(OpenMeteoWeatherModel model, bool enabled, Action<WeatherModelItem, bool> onSwitched)
    {
        Model = model;
        Name = model.GetName();
        Description = model.GetDescription();
        IsEnabled = enabled;
        _onSwitched = onSwitched;
    }

    public OpenMeteoWeatherModel Model { get; }

    public string Name { get; }

    public string? Description { get; }

    // Seamless models start a provider's group, and the best match is set apart from all the others.
    public bool HasDividerBefore => Model.Id.EndsWith("_seamless", StringComparison.Ordinal);

    public bool HasDividerAfter => Model == OpenMeteoWeatherModel.BestMatch;

    partial void OnIsEnabledChanged(bool value) => _onSwitched?.Invoke(this, value);

    [RelayCommand]
    private void Toggle()
    {
        IsEnabled = !IsEnabled;
    }
}

/// <summary>
/// The Open-Meteo weather models of one location. Turning a model on turns every other model off, so one model is
/// chosen at a time. Confirming saves a change; cancelling returns to the location's preferences.
/// </summary>
public sealed class WeatherModelsDialogViewModel : ObservableObject
{
    private readonly ObservableObject _parent;
    private readonly MainViewModel _main;
    private readonly Action<string> _onSave;
    private bool _hasChanged;
    private bool _isSwitching;

    public WeatherModelsDialogViewModel(MainViewModel main, Location location, ObservableObject parent, Action<string> onSave)
    {
        _main = main;
        _parent = parent;
        _onSave = onSave;
        IReadOnlyList<OpenMeteoWeatherModel> chosen = OpenMeteoService.GetWeatherModels(location);
        Models = [.. OpenMeteoWeatherModel.All.Select(model => new WeatherModelItem(model, chosen.Contains(model), OnSwitched))];
        Title = Strings.Get("settings_weather_source_open_meteo_weather_models");
        ConfirmText = Strings.Get("action_confirm");
        CancelText = SystemStrings.Cancel;
        ConfirmCommand = new RelayCommand(Confirm);
        CancelCommand = new RelayCommand(() => _main.ShowDialog(_parent));
    }

    public string Title { get; }

    public string ConfirmText { get; }

    public string CancelText { get; }

    public IReadOnlyList<WeatherModelItem> Models { get; }

    public IRelayCommand ConfirmCommand { get; }

    public IRelayCommand CancelCommand { get; }

    private void OnSwitched(WeatherModelItem item, bool enabled)
    {
        if (_isSwitching)
        {
            return;
        }

        _isSwitching = true;
        if (enabled)
        {
            foreach (WeatherModelItem other in Models.Where(other => other != item))
            {
                other.IsEnabled = false;
            }
        }

        _isSwitching = false;
        _hasChanged = true;
    }

    private void Confirm()
    {
        if (!_hasChanged)
        {
            _main.ShowDialog(_parent);
            return;
        }

        string models = string.Join(",", Models.Where(item => item.IsEnabled).Select(item => item.Model.Id));
        _onSave(models.Length == 0 ? OpenMeteoWeatherModel.BestMatch.Id : models);
    }
}
