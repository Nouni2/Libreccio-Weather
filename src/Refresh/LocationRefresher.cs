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

using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.BreezyTimeZone;
using LibreccioWeather.Core.Sources.Geography;
using LibreccioWeather.Core.Sources.NaturalEarth;

namespace LibreccioWeather.Core.Refresh;

public sealed class LocationRefresher(SourceManager sources, LocationRepository locationRepository)
{
    public const int ReverseGeocodingDistanceLimit = 50000;
    private static readonly string[] AmbiguousCountryCodes = ["AU", "CN", "DK", "FI", "FR", "GB", "IL", "MA", "NL", "NO", "NZ", "RS", "US"];
    private readonly BreezyTimeZoneService _timeZones = new();

    public async Task<LocationResult> GetLocationAsync(Location location, CancellationToken cancellationToken = default)
    {
        if (!location.IsUsable)
        {
            return new LocationResult(location, []);
        }

        bool needsCountryCodeRefresh = false;
        bool needsSavingToDb = false;
        List<RefreshError> errors = [];
        Location locationGeocoded = location;
        if (location.NeedsGeocodeRefresh)
        {
            IReverseGeocodingSource source = sources.GetReverseGeocodingSourceOrDefault(location.ReverseGeocodingSource ?? NaturalEarthService.Id);
            try
            {
                Location geocoded = await RequestReverseGeocodingAsync(source, location, cancellationToken).ConfigureAwait(false);
                if (SourceGeometry.ComputeDistanceBetween(new SourceCoordinate(geocoded.Latitude, geocoded.Longitude),
                    new SourceCoordinate(location.Latitude, location.Longitude)) > ReverseGeocodingDistanceLimit
                    || (source.Id != NaturalEarthService.Id && !geocoded.HasValidCountryCode))
                {
                    errors.Add(new RefreshError(RefreshErrorType.ReverseGeocodingFailed, source.Id, SourceFeature.ReverseGeocoding));
                }
                else
                {
                    needsCountryCodeRefresh = source.KnownAmbiguousCountryCodes?.Any(code =>
                        string.Equals(geocoded.CountryCode, code, StringComparison.OrdinalIgnoreCase)) != false
                        || string.Equals(geocoded.CountryCode, "AN", StringComparison.OrdinalIgnoreCase);
                    needsSavingToDb = true;
                    locationGeocoded = geocoded;
                }
            }
            catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
            {
                errors.Add(new RefreshError(RefreshError.GetTypeFromException(exception, RefreshErrorType.ReverseGeocodingFailed),
                    source.Id, SourceFeature.ReverseGeocoding));
                if (source.Id != NaturalEarthService.Id)
                {
                    IReverseGeocodingSource fallback = sources.GetReverseGeocodingSourceOrDefault(NaturalEarthService.Id);
                    try
                    {
                        locationGeocoded = (await RequestReverseGeocodingAsync(fallback, location, cancellationToken).ConfigureAwait(false))
                            with { NeedsGeocodeRefresh = true };
                        needsSavingToDb = true;
                    }
                    catch (Exception fallbackException) when (!(fallbackException is OperationCanceledException && cancellationToken.IsCancellationRequested))
                    {
                        locationGeocoded = location;
                    }
                }
            }
        }

        Location locationDisambiguated = locationGeocoded;
        if (needsCountryCodeRefresh)
        {
            needsSavingToDb = true;
            locationDisambiguated = await GetLocationWithDisambiguatedCountryCodeAsync(locationGeocoded, cancellationToken).ConfigureAwait(false);
        }

        Location locationWithTimeZone = locationDisambiguated;
        if (locationGeocoded.IsTimeZoneInvalid)
        {
            needsSavingToDb = true;
            locationWithTimeZone = locationDisambiguated with { TimeZone = GetTimeZoneForLocation(locationGeocoded) };
        }

        if (needsSavingToDb && !await locationRepository.UpdateAsync(locationWithTimeZone).ConfigureAwait(false))
        {
            throw new IOException("The refreshed location could not be saved.");
        }

        return new LocationResult(locationWithTimeZone, errors);
    }

    private static async Task<Location> RequestReverseGeocodingAsync(IReverseGeocodingSource source, Location location,
        CancellationToken cancellationToken)
    {
        if (source is IConfigurableSource configurable && !configurable.IsConfigured)
        {
            throw new ApiKeyMissingException();
        }

        IReadOnlyList<LocationAddressInfo> addresses = await source.RequestNearestLocationAsync(
            location.Latitude, location.Longitude, cancellationToken).ConfigureAwait(false);
        if (addresses.Count == 0)
        {
            throw new ReverseGeocodingException();
        }

        return location.ToLocationWithAddressInfo(addresses[0], overwriteCoordinates: false);
    }

    private async Task<Location> GetLocationWithDisambiguatedCountryCodeAsync(Location location, CancellationToken cancellationToken)
    {
        if (!AmbiguousCountryCodes.Contains(location.CountryCode, StringComparer.OrdinalIgnoreCase))
        {
            return location;
        }

        try
        {
            IReverseGeocodingSource fallback = sources.GetReverseGeocodingSourceOrDefault(NaturalEarthService.Id);
            Location disambiguated = await RequestReverseGeocodingAsync(fallback, location, cancellationToken).ConfigureAwait(false);
            return !string.Equals(disambiguated.CountryCode, location.CountryCode, StringComparison.OrdinalIgnoreCase)
                ? location with { CountryCode = disambiguated.CountryCode } : location;
        }
        catch (Exception exception) when (!(exception is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            return location;
        }
    }

    private TimeZoneInfo GetTimeZoneForLocation(Location location)
    {
        TimeZoneInfo timeZone = _timeZones.RequestTimeZone(location);
        return timeZone.Id == "GMT" ? location.IsCurrentPosition ? TimeZoneInfo.Local : GetOceanTimeZoneForLocation(location.Longitude) : timeZone;
    }

    private static TimeZoneInfo GetOceanTimeZoneForLocation(double longitude)
    {
        string zone = longitude switch
        {
            >= 172.5 and <= 180.0 => "Etc/GMT-12",
            >= 157.5 and <= 172.5 => "Etc/GMT-11",
            >= 142.5 and <= 157.5 => "Etc/GMT-10",
            >= 127.5 and <= 142.5 => "Etc/GMT-9",
            >= 112.5 and <= 127.5 => "Etc/GMT-8",
            >= 97.5 and <= 112.5 => "Etc/GMT-7",
            >= 82.5 and <= 97.5 => "Etc/GMT-6",
            >= 67.5 and <= 82.5 => "Etc/GMT-5",
            >= 52.5 and <= 67.5 => "Etc/GMT-4",
            >= 37.5 and <= 52.5 => "Etc/GMT-3",
            >= 22.5 and <= 37.5 => "Etc/GMT-2",
            >= 7.5 and <= 22.5 => "Etc/GMT-1",
            >= -7.5 and <= 7.5 => "Etc/GMT",
            >= -22.5 and <= -7.5 => "Etc/GMT+1",
            >= -37.5 and <= -22.5 => "Etc/GMT+2",
            >= -52.5 and <= -37.5 => "Etc/GMT+3",
            >= -67.5 and <= -52.5 => "Etc/GMT+4",
            >= -82.5 and <= -67.5 => "Etc/GMT+5",
            >= -97.5 and <= -82.5 => "Etc/GMT+6",
            >= -112.5 and <= -97.5 => "Etc/GMT+7",
            >= -127.5 and <= -112.5 => "Etc/GMT+8",
            >= -142.5 and <= -127.5 => "Etc/GMT+9",
            >= -157.5 and <= -142.5 => "Etc/GMT+10",
            >= -172.5 and <= -157.5 => "Etc/GMT+11",
            >= -180.0 and <= -172.5 => "Etc/GMT+12",
            _ => "GMT",
        };
        return Location.FindTimeZone(zone);
    }
}
