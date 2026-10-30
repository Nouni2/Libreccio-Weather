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
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using Microsoft.Data.Sqlite;
using UnitPrecipitation = LibreccioWeather.Core.Units.Precipitation;
using UnitTemperature = LibreccioWeather.Core.Units.Temperature;

namespace LibreccioWeather.Core.Tests.Database;

public sealed class WeatherRepositoryTests : IDisposable
{
    private static readonly Location TestLocation = new()
    {
        Latitude = 48.8566,
        Longitude = 2.3522,
        TimeZone = Location.FindTimeZone("Europe/Paris"),
        Country = "France",
        CountryCode = "FR",
        City = "Paris",
        Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["openmeteo"] = new Dictionary<string, string> { ["weatherModels"] = "best_match" },
        },
    };

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "Libreccio-Weather.Tests", Guid.NewGuid().ToString("N"));
    private readonly string _path;
    private readonly WeatherDatabase _database;
    private readonly LocationRepository _locations;
    private readonly WeatherRepository _weather;

    public WeatherRepositoryTests()
    {
        _path = Path.Combine(_directory, "weather.db");
        _database = new WeatherDatabase(_path);
        _database.EnsureCreated();
        _locations = new LocationRepository(_database);
        _weather = new WeatherRepository(_database);
    }

    [Fact]
    public async Task InsertAsync_ReopenedDatabase_PreservesQuantitiesAndForecasts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Single(await _locations.AddAllAsync([TestLocation]));
        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123);
        Models.Temperature temperature = new()
        {
            Value = UnitTemperature.FromDeciCelsius(-125L),
            SourceFeelsLike = UnitTemperature.FromDeciCelsius(-144L),
            ComputedApparent = UnitTemperature.FromDeciCelsius(-152L),
            ComputedWindChill = UnitTemperature.FromDeciCelsius(-161L),
        };
        Wind wind = new()
        {
            Degree = 247.5,
            Speed = Speed.FromCentimetersPerSecond(1234L),
            Gusts = Speed.FromCentimetersPerSecond(2345L),
        };
        Minutely minutely = new()
        {
            Date = date,
            MinuteInterval = 15,
            PrecipitationIntensity = UnitPrecipitation.FromMicrometers(1234L),
        };
        Alert alert = new()
        {
            AlertId = "wind",
            StartDate = date,
            EndDate = date.AddHours(2),
            Severity = AlertSeverity.Severe,
            Color = Alert.ColorFromSeverity(AlertSeverity.Severe),
        };
        Normals normals = new()
        {
            DaytimeTemperature = UnitTemperature.FromDeciCelsius(125L),
            NighttimeTemperature = UnitTemperature.FromDeciCelsius(32L),
        };
        Current current = new()
        {
            Temperature = temperature,
            Wind = wind,
            AirQuality = new AirQuality { PM25 = PollutantConcentration.FromMicrogramsPerCubicMeter(19L) },
            RelativeHumidity = Ratio.FromPermille(613L),
            Pressure = Pressure.FromPascals(101325L),
            Visibility = Distance.FromMeters(12345L),
        };
        Weather initial = new()
        {
            Base = new Base { RefreshTime = date, ForecastUpdateTime = date.AddMinutes(-3) },
            Current = current,
            DailyForecast =
            [
                new Daily
                {
                    Date = date,
                    Day = new HalfDay
                    {
                        PrecipitationDuration = new PrecipitationDuration { Total = TimeSpan.FromTicks(123456789L) },
                    },
                    SunshineDuration = TimeSpan.FromHours(6),
                    Pollen = new Pollen { Willow = PollenConcentration.FromPerCubicMeter(42L) },
                },
            ],
            HourlyForecast = [new Hourly { Date = date, IsDaylight = false, Temperature = temperature }],
            MinutelyForecast = [minutely],
            AlertList = [alert],
            Normals = new Dictionary<Month, Normals> { [Month.October] = normals },
        };

        await _weather.InsertAsync(TestLocation, initial, cancellationToken);
        WeatherRepository reopened = new(new WeatherDatabase(_path));
        Weather stored = Assert.IsType<Weather>(
            await reopened.GetWeatherByLocationIdAsync(TestLocation.FormattedId, cancellationToken));

        Assert.Equal(initial.Base, stored.Base);
        Assert.Equal(temperature, stored.Current?.Temperature);
        Assert.Equal(wind, stored.Current?.Wind);
        Assert.Equal(current.AirQuality, stored.Current?.AirQuality);
        Assert.Equal(current.RelativeHumidity, stored.Current?.RelativeHumidity);
        Assert.Equal(current.Pressure, stored.Current?.Pressure);
        Assert.Equal(current.Visibility, stored.Current?.Visibility);
        Daily day = Assert.Single(stored.DailyForecast);
        Assert.Equal(date, day.Date);
        Assert.Equal(initial.DailyForecast[0].Day?.PrecipitationDuration, day.Day?.PrecipitationDuration);
        Assert.Equal(TimeSpan.FromHours(6), day.SunshineDuration);
        Assert.Equal(PollenConcentration.FromPerCubicMeter(42L), day.Pollen?.Willow);
        Assert.Null(day.Sun);
        Hourly hour = Assert.Single(stored.HourlyForecast);
        Assert.Equal(date, hour.Date);
        Assert.False(hour.IsDaylight);
        Assert.Equal(temperature, hour.Temperature);
        Assert.Equal(minutely, Assert.Single(stored.MinutelyForecast));
        Assert.Equal(alert, Assert.Single(stored.AlertList));
        Assert.Equal(normals, stored.Normals[Month.October]);
    }

    [Fact]
    public async Task InsertAsync_ConstraintFailure_KeepsPreviousForecast()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Single(await _locations.AddAllAsync([TestLocation]));
        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123);
        Weather initial = new()
        {
            Base = new Base { RefreshTime = date },
            MinutelyForecast = [new Minutely { Date = date, MinuteInterval = 15 }],
        };
        await _weather.InsertAsync(TestLocation, initial, cancellationToken);
        Weather replacement = new()
        {
            Base = new Base { RefreshTime = date.AddHours(1) },
            AlertList = [new Alert { AlertId = null!, Color = 0 }],
        };

        await Assert.ThrowsAsync<SqliteException>(() => _weather.InsertAsync(TestLocation, replacement, cancellationToken));

        Weather stored = Assert.IsType<Weather>(
            await _weather.GetWeatherByLocationIdAsync(TestLocation.FormattedId, cancellationToken));
        Assert.Equal(initial.Base, stored.Base);
        Assert.Equal(initial.MinutelyForecast[0], Assert.Single(stored.MinutelyForecast));
        Assert.Empty(stored.AlertList);
    }

    [Fact]
    public async Task EnsureCreated_LocationDatabase_AddsWeatherWithoutLosingLocations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Single(await _locations.AddAllAsync([TestLocation]));
        using (SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = _path }.ToString()))
        {
            await connection.OpenAsync(cancellationToken);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE weathers;
                DROP TABLE dailys;
                DROP TABLE hourlys;
                DROP TABLE minutelys;
                DROP TABLE alerts;
                DROP TABLE normals;
                PRAGMA user_version = 1;
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _database.EnsureCreated();

        Location saved = Assert.IsType<Location>(await _locations.GetFirstLocationAsync());
        Assert.Equal(TestLocation.FormattedId, saved.FormattedId);
        Assert.Equal("best_match", saved.Parameters["openmeteo"]["weatherModels"]);
        Assert.Null(await _weather.GetWeatherByLocationIdAsync(saved.FormattedId, cancellationToken));
        await _weather.InsertAsync(saved, new Weather(), cancellationToken);
        Assert.NotNull(await _weather.GetWeatherByLocationIdAsync(saved.FormattedId, cancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsync_RenamedLocation_PreservesParametersAndEveryForecastTable(bool changeSource)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Single(await _locations.AddAllAsync([TestLocation]));
        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123);
        Weather initial = new()
        {
            Base = new Base { RefreshTime = date },
            Current = new Current { Wind = new Wind { Speed = Speed.FromMetersPerSecond(5.0) } },
            DailyForecast = [new Daily { Date = date }],
            HourlyForecast = [new Hourly { Date = date }],
            MinutelyForecast = [new Minutely { Date = date, MinuteInterval = 15 }],
            AlertList = [new Alert { AlertId = "wind", Color = 0 }],
            Normals = new Dictionary<Month, Normals> { [Month.October] = new Normals() },
        };
        await _weather.InsertAsync(TestLocation, initial, cancellationToken);
        Location renamed = changeSource
            ? TestLocation with { ForecastSource = "other-source" }
            : TestLocation with { Latitude = 49.0 };

        Assert.True(await _locations.UpdateAsync(renamed, TestLocation.FormattedId));
        Assert.Null(await _locations.GetLocationAsync(TestLocation.FormattedId));
        Location saved = Assert.IsType<Location>(await _locations.GetLocationAsync(renamed.FormattedId));
        Assert.Equal("best_match", saved.Parameters["openmeteo"]["weatherModels"]);
        Weather stored = Assert.IsType<Weather>(
            await _weather.GetWeatherByLocationIdAsync(renamed.FormattedId, cancellationToken));
        Assert.Equal(initial.Base, stored.Base);
        Assert.Equal(initial.Current?.Wind, stored.Current?.Wind);
        Assert.Single(stored.DailyForecast);
        Assert.Single(stored.HourlyForecast);
        Assert.Single(stored.MinutelyForecast);
        Assert.Single(stored.AlertList);
        Assert.Single(stored.Normals);
        using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = _path, ForeignKeys = true }.ToString());
        await connection.OpenAsync(cancellationToken);
        using SqliteCommand check = connection.CreateCommand();
        check.CommandText = "PRAGMA foreign_key_check;";
        using SqliteDataReader reader = await check.ExecuteReaderAsync(cancellationToken);
        Assert.False(await reader.ReadAsync(cancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsync_FailedRename_RollsBackParentAndChildren(bool conflictingLocation)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Location renamed = TestLocation with { Latitude = 49.0 };
        IReadOnlyList<Location> locations = conflictingLocation ? [TestLocation, renamed] : [TestLocation];
        Assert.Equal(locations.Count, (await _locations.AddAllAsync(locations)).Count);
        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(1800000000123);
        Weather initial = new()
        {
            Base = new Base { RefreshTime = date },
            HourlyForecast = [new Hourly { Date = date }],
        };
        await _weather.InsertAsync(TestLocation, initial, cancellationToken);
        if (!conflictingLocation)
        {
            using SqliteConnection connection = new(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
            await connection.OpenAsync(cancellationToken);
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TRIGGER reject_hourly_rename BEFORE UPDATE OF location_formatted_id ON hourlys
                BEGIN
                    SELECT RAISE(ABORT, 'Cannot rename hourly forecast.');
                END;
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        Assert.False(await _locations.UpdateAsync(renamed, TestLocation.FormattedId));
        Location saved = Assert.IsType<Location>(await _locations.GetLocationAsync(TestLocation.FormattedId));
        Assert.Equal("best_match", saved.Parameters["openmeteo"]["weatherModels"]);
        Weather stored = Assert.IsType<Weather>(
            await _weather.GetWeatherByLocationIdAsync(TestLocation.FormattedId, cancellationToken));
        Assert.Equal(initial.Base, stored.Base);
        Assert.Equal(date, Assert.Single(stored.HourlyForecast).Date);
        Assert.Null(await _weather.GetWeatherByLocationIdAsync(renamed.FormattedId, cancellationToken));
        Assert.Equal(conflictingLocation, await _locations.GetLocationAsync(renamed.FormattedId) is not null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InsertParametersAsync_ReplacedParameters_RemovesObsoleteSourcesAndNames(bool saveFullList)
    {
        Location initial = TestLocation with
        {
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["old-source"] = new Dictionary<string, string> { ["new-source"] = "obsolete" },
                ["new-source"] = new Dictionary<string, string> { ["new-source"] = "obsolete", ["keep"] = "old" },
            },
        };
        Location other = TestLocation with { Latitude = 49.0 };
        Assert.Equal(2, (await _locations.AddAllAsync([initial, other])).Count);
        Location replacement = initial with
        {
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                ["new-source"] = new Dictionary<string, string> { ["keep"] = "new" },
            },
        };

        if (saveFullList)
        {
            Assert.Equal(2, (await _locations.AddAllAsync([replacement, other])).Count);
        }
        else
        {
            await _locations.InsertParametersAsync(replacement.FormattedId, replacement.Parameters);
        }

        Location saved = Assert.IsType<Location>(await _locations.GetLocationAsync(initial.FormattedId));
        Assert.Equal("new-source", Assert.Single(saved.Parameters).Key);
        KeyValuePair<string, string> parameter = Assert.Single(saved.Parameters["new-source"]);
        Assert.Equal("keep", parameter.Key);
        Assert.Equal("new", parameter.Value);
        await _locations.InsertParametersAsync(initial.FormattedId, new Dictionary<string, IReadOnlyDictionary<string, string>>());
        Assert.Empty(Assert.IsType<Location>(await _locations.GetLocationAsync(initial.FormattedId)).Parameters);
        Location unchanged = Assert.IsType<Location>(await _locations.GetLocationAsync(other.FormattedId));
        Assert.Equal("best_match", unchanged.Parameters["openmeteo"]["weatherModels"]);
    }

    public void Dispose()
    {
        // Pooled SQLite handles would otherwise keep the temporary database file open on Windows.
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }
}
