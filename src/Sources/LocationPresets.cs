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

using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.Core.Sources;

/// <summary>
/// The sources a new location starts with.
/// </summary>
public static class LocationPresets
{
    /// <summary>
    /// Each feature from the source recommended for the location, or its default source otherwise.
    /// </summary>
    public static Location ApplyDefaultPreset(Location location, SourceManager sources)
    {
        string forecastSource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.Forecast)!.Id;

        // Current conditions keep the default source only when it is also the forecast source; another default would
        // give current conditions that disagree with the forecast.
        string? currentSource = sources.GetBestSourceForFeature(location, SourceFeature.Current)?.Id
            ?? (sources.GetDefaultSourceForFeature(location, SourceFeature.Current)?.Id is { } defaultCurrent
                ? defaultCurrent != forecastSource ? null : defaultCurrent
                : null);

        return location with
        {
            ForecastSource = forecastSource,
            CurrentSource = currentSource,
            AirQualitySource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.AirQuality)?.Id,
            PollenSource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.Pollen)?.Id,
            MinutelySource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.Minutely)?.Id,
            AlertSource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.Alert)?.Id,
            NormalsSource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.Normals)?.Id,
            ReverseGeocodingSource = sources.GetBestSourceForFeatureOrDefault(location, SourceFeature.ReverseGeocoding)?.Id,
        };
    }

    /// <summary>
    /// The sources of a location chosen in the search: the default preset when the default forecast source is
    /// automatic or unknown, otherwise that source for every feature it gives at the location.
    /// </summary>
    public static Location GetLocationWithAppliedPreference(Location location, string defaultForecastSource, SourceManager sources)
    {
        if (defaultForecastSource == "auto" || sources.GetWeatherSource(defaultForecastSource) is not { } source)
        {
            return ApplyDefaultPreset(location, sources);
        }

        string? IfSupported(SourceFeature feature) =>
            source.SupportedFeatures.ContainsKey(feature) && source.IsFeatureSupportedForLocation(location, feature)
                ? source.Id
                : null;

        return location with
        {
            ForecastSource = source.Id,
            CurrentSource = IfSupported(SourceFeature.Current),
            AirQualitySource = IfSupported(SourceFeature.AirQuality),
            PollenSource = IfSupported(SourceFeature.Pollen),
            MinutelySource = IfSupported(SourceFeature.Minutely),
            AlertSource = IfSupported(SourceFeature.Alert),
            NormalsSource = IfSupported(SourceFeature.Normals),
        };
    }
}
