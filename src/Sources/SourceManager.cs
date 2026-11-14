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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources.NaturalEarth;

namespace LibreccioWeather.Core.Sources;

/// <summary>
/// Every source the app has, in the order source choices list them: worldwide sources first, then national sources by
/// name in the selected language.
/// </summary>
public sealed class SourceManager
{
    private const string OpenMeteoId = "openmeteo";

    private readonly IReadOnlyList<ISource> _sources;

    public SourceManager(IReadOnlyList<IWeatherSource> worldwideWeatherSources, IReadOnlyList<IWeatherSource> nationalWeatherSources,
        IReadOnlyList<ISource>? otherSources = null)
    {
        StringComparer collator = StringComparer.Create(Strings.Culture, ignoreCase: false);
        _sources = [.. worldwideWeatherSources, .. nationalWeatherSources.OrderBy(source => source.Name, collator), .. (otherSources ?? [])];
    }

    public ISource? GetSource(string id)
    {
        return _sources.FirstOrDefault(source => source.Id == id);
    }

    public IReadOnlyList<IHttpSource> GetHttpSources()
    {
        return [.. _sources.OfType<IHttpSource>()];
    }

    public IReadOnlyList<IFeatureSource> GetFeatureSources()
    {
        return [.. _sources.OfType<IFeatureSource>()];
    }

    public IFeatureSource? GetFeatureSource(string id)
    {
        return GetFeatureSources().FirstOrDefault(source => source.Id == id);
    }

    public IReadOnlyList<IWeatherSource> GetWeatherSources()
    {
        return [.. _sources.OfType<IWeatherSource>()];
    }

    public IWeatherSource? GetWeatherSource(string id)
    {
        return GetWeatherSources().FirstOrDefault(source => source.Id == id);
    }

    public IReadOnlyList<IReverseGeocodingSource> GetReverseGeocodingSources()
    {
        return [.. _sources.OfType<IReverseGeocodingSource>()
            .Where(source => source.SupportedFeatures.ContainsKey(SourceFeature.ReverseGeocoding))];
    }

    public IReverseGeocodingSource? GetReverseGeocodingSource(string id)
    {
        return GetReverseGeocodingSources().FirstOrDefault(source => source.Id == id);
    }

    public IReverseGeocodingSource GetReverseGeocodingSourceOrDefault(string id)
    {
        return GetReverseGeocodingSource(id) ?? GetReverseGeocodingSource(NaturalEarthService.Id)
            ?? throw new InvalidOperationException("The default reverse-geocoding source is missing.");
    }

    /// <summary>
    /// The sources giving a feature, at a location when given; a position that is not known yet takes every source.
    /// </summary>
    /// <param name="sourceException">A source always listed, even when it does not match.</param>
    public IReadOnlyList<IFeatureSource> GetSupportedFeatureSources(
        SourceFeature? feature = null, Location? location = null, string? sourceException = null)
    {
        return [.. GetFeatureSources().Where(source =>
            source.Id == sourceException
            || feature is not { } requested
            || (source.SupportedFeatures.ContainsKey(requested)
                && (location is null
                    || (location.IsCurrentPosition && !location.IsUsable)
                    || source.IsFeatureSupportedForLocation(location, requested))))];
    }

    /// <summary>
    /// The source recommended most strongly for a feature at a location, or null when none is recommended.
    /// </summary>
    public IFeatureSource? GetBestSourceForFeature(Location location, SourceFeature feature)
    {
        return GetSupportedFeatureSources(feature, location)
            .Where(source => source.IsFeatureSupportedForLocation(location, feature)
                && source.GetFeaturePriorityForLocation(location, feature) > IFeatureSource.PriorityNone
                && (source is not IConfigurableSource configurable || (configurable.IsConfigured && !configurable.IsRestricted)))
            .MaxBy(source => source.GetFeaturePriorityForLocation(location, feature));
    }

    /// <summary>
    /// The source a feature takes when none is recommended: Open-Meteo for air quality outside India, for pollen where
    /// it covers it, and for the forecast, current conditions and minutely precipitation. Alerts, normals and reverse
    /// geocoding have no default among the sources the app has.
    /// </summary>
    public IFeatureSource? GetDefaultSourceForFeature(Location location, SourceFeature feature)
    {
        return feature switch
        {
            SourceFeature.AirQuality => !string.Equals(location.CountryCode, "IN", StringComparison.OrdinalIgnoreCase)
                ? GetWeatherSource(OpenMeteoId)
                : null,
            SourceFeature.Pollen => GetWeatherSource(OpenMeteoId) is { } openMeteo && openMeteo.IsFeatureSupportedForLocation(location, feature)
                ? openMeteo
                : null,
            SourceFeature.Alert or SourceFeature.Normals or SourceFeature.ReverseGeocoding => null,
            _ => GetWeatherSource(OpenMeteoId),
        };
    }

    public IFeatureSource? GetBestSourceForFeatureOrDefault(Location location, SourceFeature feature)
    {
        return feature != SourceFeature.ReverseGeocoding || location.IsCurrentPosition || location.NeedsGeocodeRefresh
            ? GetBestSourceForFeature(location, feature) ?? GetDefaultSourceForFeature(location, feature)
            : null;
    }
}
