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
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Localization;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels.Locations;

/// <summary>
/// A source that can be offered for a feature, and whether it can be chosen.
/// </summary>
public sealed record SelectableSource(string Id, string DisplayName, bool IsEnabled);

/// <summary>
/// The source chosen for one feature of a location. Clicking it lists the sources to choose from.
/// </summary>
public sealed partial class SourceRow : ObservableObject
{
    private readonly IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> _groups;

    public SourceRow(
        SourceFeature feature,
        string selectedKey,
        IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> groups,
        Action<SourceRow> open)
    {
        Feature = feature;
        Title = Strings.Get(SecondarySourcesDialogViewModel.FeatureNameKey(feature));
        _groups = groups;
        OpenCommand = new RelayCommand(() => open(this));
        Select(selectedKey);
    }

    public SourceFeature Feature { get; }

    // The duplicate-location message sits under the forecast row, the choice that makes a location a duplicate.
    public bool IsForecast => Feature == SourceFeature.Forecast;

    public string Title { get; }

    public IRelayCommand OpenCommand { get; }

    public IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> Groups => _groups;

    [ObservableProperty]
    private string _selectedKey = "";

    [ObservableProperty]
    private string? _summary;

    public void Select(string key)
    {
        SelectedKey = key;
        Summary = _groups.SelectMany(group => group.Sources).FirstOrDefault(source => source.Id == key)?.DisplayName;
    }
}

/// <summary>
/// Chooses the source of each feature of a location: forecast, current conditions, air quality, pollen, minutely
/// precipitation, alerts, normals and, for locations that need it, reverse geocoding. Saving resets the location's
/// refresh times, so the next refresh takes every feature from its new source.
/// </summary>
public sealed partial class SecondarySourcesDialogViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Location _location;
    private readonly Action<Location?> _onClose;
    private readonly Func<Location, bool>? _locationExists;
    private bool _hasChangedASource;
    private bool _hasChangedReverseGeocodingSource;

    public SecondarySourcesDialogViewModel(
        MainViewModel main,
        Location location,
        Action<Location?> onClose,
        Func<Location, bool>? locationExists = null)
    {
        _main = main;
        _location = location;
        _onClose = onClose;
        _locationExists = locationExists;
        Title = Strings.Get("settings_weather_sources");
        SaveText = Strings.Get("action_save");
        CancelText = SystemStrings.Cancel;
        DuplicateText = Strings.Get("location_message_already_exists");
        ShowsCurrentPositionDisclaimer = location.IsCurrentPosition && !location.IsUsable;
        CurrentPositionDisclaimer = Strings.Get("settings_weather_source_current_position_disclaimer");

        List<SourceRow> rows =
        [
            new(SourceFeature.Forecast, location.ForecastSource, WithUnavailable(location.ForecastSource, SourceFeature.Forecast, null), Open),
            new(SourceFeature.Current, location.CurrentSource ?? "", WithUnavailable(location.CurrentSource ?? "", SourceFeature.Current, Strings.Get("forecast")), Open),
            Optional(SourceFeature.AirQuality, location.AirQualitySource),
            Optional(SourceFeature.Pollen, location.PollenSource),
            Optional(SourceFeature.Minutely, location.MinutelySource),
            Optional(SourceFeature.Alert, location.AlertSource),
            Optional(SourceFeature.Normals, location.NormalsSource),
        ];
        if (location.IsCurrentPosition || location.NeedsGeocodeRefresh)
        {
            rows.Add(Optional(SourceFeature.ReverseGeocoding, location.ReverseGeocodingSource));
        }

        Rows = rows;
    }

    public string Title { get; }

    public string SaveText { get; }

    public string CancelText { get; }

    public string DuplicateText { get; }

    public bool ShowsCurrentPositionDisclaimer { get; }

    public string CurrentPositionDisclaimer { get; }

    public IReadOnlyList<SourceRow> Rows { get; }

    /// <summary>
    /// Whether the chosen forecast source would make this location the same as another saved one.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isLocationDuplicate;

    internal static string FeatureNameKey(SourceFeature feature)
    {
        return feature switch
        {
            SourceFeature.Forecast => "forecast",
            SourceFeature.Current => "current_weather",
            SourceFeature.AirQuality => "air_quality",
            SourceFeature.Pollen => "pollen",
            SourceFeature.Minutely => "precipitation_nowcasting",
            SourceFeature.Alert => "alerts",
            SourceFeature.Normals => "temperature_normals",
            _ => "location_reverse_geocoding",
        };
    }

    /// <summary>
    /// The sources able to give a feature for a location, by group: recommended sources first, then worldwide ones,
    /// then the other continents by name. The stored source is always kept, so it can be shown as unavailable.
    /// </summary>
    internal static IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> GetCompatibleSources(
        Location location,
        string selectedSource,
        SourceFeature feature)
    {
        // Open-Meteo is the only source, and it sets no priority for any location, so it is grouped by its continent.
        bool supports = OpenMeteoService.SupportedFeatures.ContainsKey(feature)
            && ((location.IsCurrentPosition && !location.IsUsable) || OpenMeteoService.IsFeatureSupportedForLocation(location, feature));
        if (!supports && OpenMeteoService.Id != selectedSource)
        {
            return [];
        }

        List<SelectableSource> worldwide =
        [
            new SelectableSource(
                OpenMeteoService.Id,
                OpenMeteoService.Name,
                OpenMeteoService.IsFeatureSupportedForLocation(location, feature)),
        ];
        return [(Strings.Get("weather_source_continent_worldwide"), worldwide)];
    }

    // A feature that can be left empty: the stored source when no longer offered, "None", then the compatible sources.
    private SourceRow Optional(SourceFeature feature, string? stored)
    {
        return new SourceRow(feature, stored ?? "", WithUnavailable(stored ?? "", feature, Strings.Get("settings_weather_source_none")), Open);
    }

    private IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> WithUnavailable(
        string selected,
        SourceFeature feature,
        string? emptyName)
    {
        IReadOnlyList<(string? Group, IReadOnlyList<SelectableSource> Sources)> compatible =
            GetCompatibleSources(_location, selected, feature);
        List<SelectableSource> first = [];
        if (selected.Length > 0 && !compatible.Any(group => group.Sources.Any(source => source.Id == selected)))
        {
            first.Add(new SelectableSource(selected, Strings.Format("settings_weather_source_unavailable", selected), false));
        }

        if (emptyName is not null)
        {
            first.Add(new SelectableSource("", emptyName, true));
        }

        if (first.Count == 0)
        {
            return compatible;
        }

        List<(string? Group, IReadOnlyList<SelectableSource> Sources)> groups = [(null, first)];
        groups.AddRange(compatible);
        return groups;
    }

    private void Open(SourceRow row)
    {
        _main.ShowDialog(new GroupedListDialogViewModel(
            row.Title,
            row.Groups
                .Select(group => (group.Group, (IReadOnlyList<(string, string, bool)>)group.Sources
                    .Select(source => (source.Id, source.DisplayName, source.IsEnabled)).ToList()))
                .ToList(),
            row.SelectedKey,
            sourceId =>
            {
                Choose(row, sourceId);
                _main.ShowDialog(this);
            },
            () => _main.ShowDialog(this)));
    }

    private void Choose(SourceRow row, string sourceId)
    {
        if (row.Feature == SourceFeature.Forecast && _locationExists is not null)
        {
            IsLocationDuplicate = sourceId != _location.ForecastSource
                && _locationExists(_location with { ForecastSource = sourceId });
        }

        row.Select(sourceId);
        if (row.Feature == SourceFeature.ReverseGeocoding)
        {
            _hasChangedReverseGeocodingSource = true;
        }
        else
        {
            _hasChangedASource = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        if (!_hasChangedReverseGeocodingSource && !_hasChangedASource)
        {
            _onClose(_location);
            return;
        }

        string Selected(SourceFeature feature)
        {
            return Rows.FirstOrDefault(row => row.Feature == feature)?.SelectedKey ?? "";
        }

        Location newLocation = _location with
        {
            // City identifiers differ from one reverse geocoding source to another.
            CityId = _hasChangedReverseGeocodingSource ? "" : _location.CityId,
            ForecastSource = Selected(SourceFeature.Forecast),
            CurrentSource = Selected(SourceFeature.Current),
            AirQualitySource = Selected(SourceFeature.AirQuality),
            PollenSource = Selected(SourceFeature.Pollen),
            MinutelySource = Selected(SourceFeature.Minutely),
            AlertSource = Selected(SourceFeature.Alert),
            NormalsSource = Selected(SourceFeature.Normals),
            ReverseGeocodingSource = Rows.Any(row => row.Feature == SourceFeature.ReverseGeocoding)
                ? Selected(SourceFeature.ReverseGeocoding)
                : _location.ReverseGeocodingSource ?? "",
            NeedsGeocodeRefresh = (!_location.IsCurrentPosition && _location.NeedsGeocodeRefresh)
                || (_hasChangedReverseGeocodingSource && _location.IsCurrentPosition),
            Weather = WithoutUpdateTimes(_location.Weather),
        };
        _onClose(newLocation);
    }

    private bool CanSave()
    {
        return !IsLocationDuplicate;
    }

    [RelayCommand]
    private void Cancel()
    {
        _onClose(null);
    }

    /// <summary>
    /// The weather with every refresh time cleared, so the next refresh requests every feature again.
    /// </summary>
    internal static Weather? WithoutUpdateTimes(Weather? weather)
    {
        return weather is null
            ? null
            : weather with
            {
                Base = weather.Base with
                {
                    RefreshTime = null,
                    ForecastUpdateTime = null,
                    CurrentUpdateTime = null,
                    AirQualityUpdateTime = null,
                    PollenUpdateTime = null,
                    MinutelyUpdateTime = null,
                    AlertsUpdateTime = null,
                    NormalsUpdateTime = null,
                },
            };
    }
}
