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
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.NaturalEarth;
using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Tests.Refresh;

public sealed class LocationRefresherTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Libreccio-Weather.Tests", Guid.NewGuid().ToString("N"));
    private readonly LocationRepository _locations;
    private static readonly Location TestLocation = new()
    {
        Latitude = 48.8566,
        Longitude = 2.3522,
        Country = "France",
        CountryCode = "FR",
        ReverseGeocodingSource = "test-geocoder",
        NeedsGeocodeRefresh = true,
    };

    public LocationRefresherTests()
    {
        WeatherDatabase database = new(Path.Combine(_directory, "weather.db"));
        database.EnsureCreated();
        _locations = new LocationRepository(database);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetLocationAsync_ProviderSuccessOrFailure_PersistsAddressAndRetainsRetryAfterFallback(bool fail)
    {
        Assert.Single(await _locations.AddAllAsync([TestLocation]));
        SourceManager sources = new([], [], [new NaturalEarthService(), new GeocodingSource(fail)]);
        LocationRefresher refresher = new(sources, _locations);

        LocationResult result = await refresher.GetLocationAsync(TestLocation, TestContext.Current.CancellationToken);

        Assert.Equal(TestLocation.Latitude, result.Location.Latitude);
        Assert.Equal(TestLocation.Longitude, result.Location.Longitude);
        Assert.Equal("FR", result.Location.CountryCode);
        Assert.Equal("Europe/Paris", result.Location.TimeZone.Id);
        Assert.Equal(fail, result.Location.NeedsGeocodeRefresh);
        if (fail)
        {
            RefreshError error = Assert.Single(result.Errors);
            Assert.Equal(RefreshErrorType.ReverseGeocodingFailed, error.Error);
            Assert.Equal("test-geocoder", error.Source);
        }
        else
        {
            Assert.Empty(result.Errors);
            Assert.Equal("Paris", result.Location.City);
        }

        Location? saved = await _locations.GetLocationAsync(TestLocation.FormattedId);
        Assert.NotNull(saved);
        Assert.Equal(result.Location.CountryCode, saved.CountryCode);
        Assert.Equal(result.Location.City, saved.City);
        Assert.Equal(result.Location.TimeZone.Id, saved.TimeZone.Id);
        Assert.Equal(fail, saved.NeedsGeocodeRefresh);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    private sealed class GeocodingSource(bool fail) : IReverseGeocodingSource
    {
        public string Id => "test-geocoder";
        public string Name => "Test geocoder";
        public IReadOnlyList<string>? KnownAmbiguousCountryCodes => [];
        public IReadOnlyDictionary<SourceFeature, string> SupportedFeatures { get; } = new Dictionary<SourceFeature, string>
        {
            [SourceFeature.ReverseGeocoding] = "Test geocoder",
        };

        public Task<IReadOnlyList<LocationAddressInfo>> RequestNearestLocationAsync(double latitude, double longitude,
            CancellationToken cancellationToken = default)
        {
            return fail ? throw new ReverseGeocodingException() : Task.FromResult<IReadOnlyList<LocationAddressInfo>>([new LocationAddressInfo
            {
                CountryCode = "FR",
                Country = "France",
                City = "Paris",
                TimeZoneId = "Europe/Paris",
            }]);
        }
    }
}
