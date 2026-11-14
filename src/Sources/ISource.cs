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
using LibreccioWeather.Core.Models.Wrappers;

namespace LibreccioWeather.Core.Sources;

public interface ISource
{
    /// <summary>
    /// Unique identifier, stored with each location for the features it gives.
    /// </summary>
    string Id { get; }

    string Name { get; }
}

public interface IConfigurableSource : ISource
{
    bool IsConfigured { get; }

    bool IsRestricted { get; }

    IReadOnlyList<SourcePreference> GetPreferences();
}

public sealed record SourcePreference(string TitleKey, string Content, Action<string> OnValueChanged, string? DefaultUrl = null);

public interface IReverseGeocodingSource : IFeatureSource
{
    IReadOnlyList<string>? KnownAmbiguousCountryCodes { get; }

    Task<IReadOnlyList<LocationAddressInfo>> RequestNearestLocationAsync(
        double latitude, double longitude, CancellationToken cancellationToken = default);
}

public interface ILocationParametersSource : ISource
{
    bool NeedsLocationParametersRefresh(Location location, bool coordinatesChanged, IReadOnlyCollection<SourceFeature> features);

    Task<IReadOnlyDictionary<string, string>> RequestLocationParametersAsync(
        Location location, CancellationToken cancellationToken = default);
}

/// <summary>
/// A source reached over the network.
/// </summary>
public interface IHttpSource : ISource
{
    string PrivacyPolicyUrl { get; }

    /// <summary>
    /// The link added each time a name appears in a credit, such as Open-Meteo's website for "Open-Meteo".
    /// </summary>
    IReadOnlyDictionary<string, string> AttributionLinks { get; }

    /// <summary>
    /// The continent the source mainly covers; a national source with worldwide coverage takes its mainland's.
    /// </summary>
    SourceContinent Continent { get; }
}

/// <summary>
/// A source that gives some of the features a location can take from different sources.
/// </summary>
public interface IFeatureSource : ISource
{
    // Priorities of a source for a feature at a location; a higher one is recommended first.
    public const int PriorityHighest = 100;
    public const int PriorityHigh = 75;
    public const int PriorityMedium = 50;
    public const int PriorityLow = 25;
    public const int PriorityLowest = 0;

    /// <summary>
    /// Not recommended for the location, though it may still be chosen.
    /// </summary>
    public const int PriorityNone = -1;

    /// <summary>
    /// Each feature given, with the credit shown at the bottom of the main screen.
    /// </summary>
    IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; }

    bool IsFeatureSupportedForLocation(Location location, SourceFeature feature) => true;

    /// <summary>
    /// How strongly the source is recommended for a feature at a location: national sources usually give
    /// <see cref="PriorityHighest"/> for their country, worldwide sources <see cref="PriorityNone"/>.
    /// </summary>
    int GetFeaturePriorityForLocation(Location location, SourceFeature feature) => PriorityNone;
}

/// <summary>
/// A source of weather data.
/// </summary>
public interface IWeatherSource : IFeatureSource
{
    /// <summary>
    /// Locations that cover the cases worth testing with this source, offered to add in debug builds.
    /// </summary>
    IReadOnlyList<Location> TestingLocations => [];

    /// <summary>
    /// The requested features. A feature that fails is listed in the result's failed features instead of failing the
    /// whole request.
    /// </summary>
    Task<WeatherWrapper> RequestWeatherAsync(
        Location location, IReadOnlyCollection<SourceFeature> requestedFeatures, CancellationToken cancellationToken = default);
}
