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

using System.Text.Json;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources.Geography;

namespace LibreccioWeather.Core.Sources.NaturalEarth;

public sealed class NaturalEarthService : IReverseGeocodingSource
{
    public const string Id = "naturalearth";
    private static readonly Lazy<JsonElement[]> Features = new(() => GeoJsonFeatures.Load("ne-50m-admin-0-countries.json"));

    string ISource.Id => Id;
    public string Name => "Natural Earth";
    public IReadOnlyList<string>? KnownAmbiguousCountryCodes => [];
    public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
    {
        [SourceFeature.ReverseGeocoding] = "Natural Earth",
    };

    public Task<IReadOnlyList<LocationAddressInfo>> RequestNearestLocationAsync(double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        JsonElement[] matches = Features.Value.Where(feature => GeoJsonFeatures.Matches(feature, latitude, longitude, matchPoints: true)).ToArray();
        if (matches.Length != 1)
        {
            return Task.FromResult<IReadOnlyList<LocationAddressInfo>>([]);
        }

        string language = Strings.Culture.TwoLetterISOLanguageName.ToUpperInvariant();
        string region = Strings.Culture.Name.Split('-')[^1].ToUpperInvariant();
        if (language == "ZH" && region is "TW" or "HK" or "MO")
        {
            language += "T";
        }

        string countryCode = GeoJsonFeatures.Property(matches[0], "ISO_A2") ?? "";
        return Task.FromResult<IReadOnlyList<LocationAddressInfo>>([new LocationAddressInfo
        {
            Country = countryCode.Length > 0 ? CountryNames.GetCountryName(countryCode)
                : GeoJsonFeatures.Property(matches[0], "NAME_" + language) ?? GeoJsonFeatures.Property(matches[0], "NAME_LONG"),
            CountryCode = countryCode,
        }]);
    }
}
