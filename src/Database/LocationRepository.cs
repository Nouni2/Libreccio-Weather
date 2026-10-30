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
using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Database;

/// <summary>
/// Saved locations, in the order the user arranged them, with the parameters each source needs for them.
/// </summary>
public sealed class LocationRepository
{
    private const string LocationColumns =
        "city_id, latitude, longitude, timezone, custom_name, country, country_code, admin1, admin1_code, admin2, "
        + "admin2_code, admin3, admin3_code, admin4, admin4_code, city, district, weather_source, current_source, "
        + "air_quality_source, pollen_source, minutely_source, alert_source, normals_source, reverse_geocoding_source, "
        + "current_position, needs_geocode_refresh";

    // An existing location keeps its stored value for any column given as null.
    private const string InsertLocation = """
        INSERT INTO locations(formatted_id, list_order, city_id, latitude, longitude, timezone, custom_name, country,
            country_code, admin1, admin1_code, admin2, admin2_code, admin3, admin3_code, admin4, admin4_code, city,
            district, weather_source, current_source, air_quality_source, pollen_source, minutely_source, alert_source,
            normals_source, reverse_geocoding_source, current_position, needs_geocode_refresh)
        VALUES ($formattedId, $listOrder, $cityId, $latitude, $longitude, $timezone, $customName, $country,
            $countryCode, $admin1, $admin1Code, $admin2, $admin2Code, $admin3, $admin3Code, $admin4, $admin4Code, $city,
            $district, $weatherSource, $currentSource, $airQualitySource, $pollenSource, $minutelySource, $alertSource,
            $normalsSource, $reverseGeocodingSource, $currentPosition, $needsGeocodeRefresh)
        ON CONFLICT(formatted_id) DO UPDATE SET
            list_order = coalesce($listOrder, list_order),
            city_id = coalesce($cityId, city_id),
            latitude = coalesce($latitude, latitude),
            longitude = coalesce($longitude, longitude),
            timezone = coalesce($timezone, timezone),
            custom_name = coalesce($customName, custom_name),
            country = coalesce($country, country),
            country_code = coalesce($countryCode, country_code),
            admin1 = coalesce($admin1, admin1),
            admin1_code = coalesce($admin1Code, admin1_code),
            admin2 = coalesce($admin2, admin2),
            admin2_code = coalesce($admin2Code, admin2_code),
            admin3 = coalesce($admin3, admin3),
            admin3_code = coalesce($admin3Code, admin3_code),
            admin4 = coalesce($admin4, admin4),
            admin4_code = coalesce($admin4Code, admin4_code),
            city = coalesce($city, city),
            district = coalesce($district, district),
            weather_source = coalesce($weatherSource, weather_source),
            current_source = coalesce($currentSource, current_source),
            air_quality_source = coalesce($airQualitySource, air_quality_source),
            pollen_source = coalesce($pollenSource, pollen_source),
            minutely_source = coalesce($minutelySource, minutely_source),
            alert_source = coalesce($alertSource, alert_source),
            normals_source = coalesce($normalsSource, normals_source),
            reverse_geocoding_source = coalesce($reverseGeocodingSource, reverse_geocoding_source),
            current_position = coalesce($currentPosition, current_position),
            needs_geocode_refresh = coalesce($needsGeocodeRefresh, needs_geocode_refresh);
        """;

    private const string UpdateLocation = """
        UPDATE locations
        SET city_id = coalesce($cityId, city_id),
            latitude = coalesce($latitude, latitude),
            longitude = coalesce($longitude, longitude),
            timezone = coalesce($timezone, timezone),
            custom_name = coalesce($customName, custom_name),
            country = coalesce($country, country),
            country_code = coalesce($countryCode, country_code),
            admin1 = coalesce($admin1, admin1),
            admin1_code = coalesce($admin1Code, admin1_code),
            admin2 = coalesce($admin2, admin2),
            admin2_code = coalesce($admin2Code, admin2_code),
            admin3 = coalesce($admin3, admin3),
            admin3_code = coalesce($admin3Code, admin3_code),
            admin4 = coalesce($admin4, admin4),
            admin4_code = coalesce($admin4Code, admin4_code),
            city = coalesce($city, city),
            district = coalesce($district, district),
            weather_source = coalesce($weatherSource, weather_source),
            current_source = coalesce($currentSource, current_source),
            air_quality_source = coalesce($airQualitySource, air_quality_source),
            pollen_source = coalesce($pollenSource, pollen_source),
            minutely_source = coalesce($minutelySource, minutely_source),
            alert_source = coalesce($alertSource, alert_source),
            normals_source = coalesce($normalsSource, normals_source),
            reverse_geocoding_source = coalesce($reverseGeocodingSource, reverse_geocoding_source),
            current_position = coalesce($currentPosition, current_position),
            needs_geocode_refresh = coalesce($needsGeocodeRefresh, needs_geocode_refresh)
        WHERE formatted_id = coalesce($formattedId, formatted_id);
        """;

    private readonly WeatherDatabase _database;

    public LocationRepository(WeatherDatabase database)
    {
        _database = database;
    }

    public async Task<Location?> GetLocationAsync(string formattedId, bool withParameters = true)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {LocationColumns} FROM locations WHERE formatted_id = $formattedId;";
        command.Parameters.AddWithValue("$formattedId", formattedId);
        List<Location> locations = await ReadLocationsAsync(command).ConfigureAwait(false);
        return locations.Count == 0 ? null : await WithParametersIfAsked(connection, locations[0], withParameters).ConfigureAwait(false);
    }

    public async Task<Location?> GetFirstLocationAsync(bool withParameters = true)
    {
        IReadOnlyList<Location> locations = await GetXLocationsAsync(1, withParameters).ConfigureAwait(false);
        return locations.Count == 0 ? null : locations[0];
    }

    public async Task<IReadOnlyList<Location>> GetXLocationsAsync(int limit, bool withParameters = true)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {LocationColumns} FROM locations ORDER BY list_order LIMIT $limit;";
        command.Parameters.AddWithValue("$limit", limit);
        return await WithParametersIfAsked(connection, await ReadLocationsAsync(command).ConfigureAwait(false), withParameters)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Location>> GetAllLocationsAsync(bool withParameters = true)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT {LocationColumns} FROM locations ORDER BY list_order;";
        return await WithParametersIfAsked(connection, await ReadLocationsAsync(command).ConfigureAwait(false), withParameters)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Saves the full list of locations in this order. Locations missing from the list are deleted.
    /// </summary>
    /// <returns>The saved locations, or an empty list when saving failed.</returns>
    public async Task<IReadOnlyList<Location>> AddAllAsync(IReadOnlyList<Location> locations)
    {
        try
        {
            using SqliteConnection connection = _database.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = $"DELETE FROM locations WHERE formatted_id NOT IN ({InList(delete, "$id", locations.Select(location => location.FormattedId))});";
                await delete.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            for (int i = 0; i < locations.Count; i++)
            {
                Location location = locations[i];
                using (SqliteCommand insert = connection.CreateCommand())
                {
                    insert.Transaction = transaction;
                    insert.CommandText = InsertLocation;
                    insert.Parameters.AddWithValue("$listOrder", i + 1L);
                    AddLocationValues(insert, location);
                    await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await ReplaceParametersAsync(connection, transaction, location.FormattedId, location.Parameters).ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return locations;
        }
        catch (SqliteException)
        {
            return [];
        }
    }

    public async Task InsertParametersAsync(
        string locationFormattedId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> locationParameters)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();
        await ReplaceParametersAsync(connection, transaction, locationFormattedId, locationParameters).ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces old values of a source parameter with new ones, in every location.
    /// </summary>
    /// <param name="values">New values by old value.</param>
    public async Task UpdateParametersAsync(string source, string parameter, IReadOnlyDictionary<string, string> values)
    {
        using SqliteConnection connection = _database.Open();
        foreach ((string oldValue, string newValue) in values)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                UPDATE location_parameters
                SET value = $newValue
                WHERE source = $source AND parameter = $parameter AND value = $oldValue;
                """;
            command.Parameters.AddWithValue("$newValue", newValue);
            command.Parameters.AddWithValue("$source", source);
            command.Parameters.AddWithValue("$parameter", parameter);
            command.Parameters.AddWithValue("$oldValue", oldValue);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Deletes a source parameter wherever it has one of the given values.
    /// </summary>
    public async Task DeleteParametersAsync(string source, string parameter, IReadOnlyList<string> values)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM location_parameters WHERE source = $source AND parameter = $parameter "
            + $"AND value IN ({InList(command, "$value", values)});";
        command.Parameters.AddWithValue("$source", source);
        command.Parameters.AddWithValue("$parameter", parameter);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Saves changes to a location, renaming it first when its formatted id changed.
    /// </summary>
    /// <returns>Whether saving succeeded.</returns>
    public async Task<bool> UpdateAsync(Location location, string? oldFormattedId = null)
    {
        try
        {
            using SqliteConnection connection = _database.Open();
            using SqliteTransaction transaction = connection.BeginTransaction();
            if (oldFormattedId is not null && oldFormattedId != location.FormattedId)
            {
                // Parent and child identifiers must change before foreign keys are checked at commit.
                using (SqliteCommand defer = connection.CreateCommand())
                {
                    defer.Transaction = transaction;
                    defer.CommandText = "PRAGMA defer_foreign_keys = ON;";
                    await defer.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                using SqliteCommand rename = connection.CreateCommand();
                rename.Transaction = transaction;
                rename.CommandText = """
                    UPDATE locations
                    SET formatted_id = coalesce($newFormattedId, formatted_id)
                    WHERE formatted_id = coalesce($oldFormattedId, formatted_id);
                    """;
                rename.Parameters.AddWithValue("$newFormattedId", location.FormattedId);
                rename.Parameters.AddWithValue("$oldFormattedId", oldFormattedId);
                if (await rename.ExecuteNonQueryAsync().ConfigureAwait(false) == 0)
                {
                    return false;
                }

                foreach (string table in new[] { "location_parameters", "weathers", "dailys", "hourlys", "minutelys", "alerts", "normals" })
                {
                    using SqliteCommand child = connection.CreateCommand();
                    child.Transaction = transaction;
                    child.CommandText = $"UPDATE {table} SET location_formatted_id = $newId WHERE location_formatted_id = $oldId;";
                    child.Parameters.AddWithValue("$newId", location.FormattedId);
                    child.Parameters.AddWithValue("$oldId", oldFormattedId);
                    await child.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            using (SqliteCommand update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText = UpdateLocation;
                AddLocationValues(update, location);
                await update.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
            return true;
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string formattedId)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM locations WHERE formatted_id = $formattedId;";
        command.Parameters.AddWithValue("$formattedId", formattedId);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAllAsync()
    {
        using SqliteConnection connection = _database.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM locations;";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task ReplaceParametersAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string locationFormattedId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> parameters)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM location_parameters WHERE location_formatted_id = $locationFormattedId;";
            delete.Parameters.AddWithValue("$locationFormattedId", locationFormattedId);
            await delete.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        foreach ((string source, IReadOnlyDictionary<string, string> values) in parameters)
        {
            foreach ((string parameter, string value) in values)
            {
                using SqliteCommand insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO location_parameters(location_formatted_id, source, parameter, value)
                    VALUES ($locationFormattedId, $source, $parameter, $value);
                    """;
                insert.Parameters.AddWithValue("$locationFormattedId", locationFormattedId);
                insert.Parameters.AddWithValue("$source", source);
                insert.Parameters.AddWithValue("$parameter", parameter);
                insert.Parameters.AddWithValue("$value", value);
                await insert.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<Location> WithParametersIfAsked(SqliteConnection connection, Location location, bool withParameters)
    {
        return withParameters ? location with { Parameters = await ReadParametersAsync(connection, location.FormattedId).ConfigureAwait(false) } : location;
    }

    private static async Task<IReadOnlyList<Location>> WithParametersIfAsked(
        SqliteConnection connection,
        List<Location> locations,
        bool withParameters)
    {
        if (!withParameters)
        {
            return locations;
        }

        List<Location> result = new(locations.Count);
        foreach (Location location in locations)
        {
            result.Add(await WithParametersIfAsked(connection, location, true).ConfigureAwait(false));
        }

        return result;
    }

    private static async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> ReadParametersAsync(
        SqliteConnection connection,
        string locationFormattedId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT source, parameter, value FROM location_parameters WHERE location_formatted_id = $locationFormattedId;";
        command.Parameters.AddWithValue("$locationFormattedId", locationFormattedId);

        Dictionary<string, Dictionary<string, string>> bySource = [];
        using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            string source = reader.GetString(0);
            if (!bySource.TryGetValue(source, out Dictionary<string, string>? values))
            {
                values = [];
                bySource[source] = values;
            }

            values[reader.GetString(1)] = reader.GetString(2);
        }

        return bySource.ToDictionary(pair => pair.Key, pair => (IReadOnlyDictionary<string, string>)pair.Value);
    }

    // Columns are read in the order of LocationColumns.
    private static async Task<List<Location>> ReadLocationsAsync(SqliteCommand command)
    {
        List<Location> locations = [];
        using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            string? Text(int column) => reader.IsDBNull(column) ? null : reader.GetString(column);

            locations.Add(new Location
            {
                CityId = Text(0),
                Latitude = reader.GetDouble(1),
                Longitude = reader.GetDouble(2),
                TimeZone = Location.FindTimeZone(reader.GetString(3)),
                CustomName = Text(4),
                Country = reader.GetString(5),
                CountryCode = Text(6),
                Admin1 = Text(7),
                Admin1Code = Text(8),
                Admin2 = Text(9),
                Admin2Code = Text(10),
                Admin3 = Text(11),
                Admin3Code = Text(12),
                Admin4 = Text(13),
                Admin4Code = Text(14),
                City = reader.GetString(15),
                District = Text(16),
                ForecastSource = reader.GetString(17),
                CurrentSource = Text(18),
                AirQualitySource = Text(19),
                PollenSource = Text(20),
                MinutelySource = Text(21),
                AlertSource = Text(22),
                NormalsSource = Text(23),
                ReverseGeocodingSource = Text(24),
                IsCurrentPosition = reader.GetInt64(25) != 0,
                NeedsGeocodeRefresh = reader.GetInt64(26) != 0,
            });
        }

        return locations;
    }

    private static void AddLocationValues(SqliteCommand command, Location location)
    {
        void Add(string name, object? value) => command.Parameters.AddWithValue(name, value ?? DBNull.Value);

        Add("$formattedId", location.FormattedId);
        Add("$cityId", location.CityId);
        Add("$latitude", location.Latitude);
        Add("$longitude", location.Longitude);
        Add("$timezone", location.TimeZone.Id);
        Add("$customName", location.CustomName);
        Add("$country", location.Country);
        Add("$countryCode", location.CountryCode);
        Add("$admin1", location.Admin1);
        Add("$admin1Code", location.Admin1Code);
        Add("$admin2", location.Admin2);
        Add("$admin2Code", location.Admin2Code);
        Add("$admin3", location.Admin3);
        Add("$admin3Code", location.Admin3Code);
        Add("$admin4", location.Admin4);
        Add("$admin4Code", location.Admin4Code);
        Add("$city", location.City);
        Add("$district", location.District);
        Add("$weatherSource", location.ForecastSource);
        Add("$currentSource", location.CurrentSource);
        Add("$airQualitySource", location.AirQualitySource);
        Add("$pollenSource", location.PollenSource);
        Add("$minutelySource", location.MinutelySource);
        Add("$alertSource", location.AlertSource);
        Add("$normalsSource", location.NormalsSource);
        Add("$reverseGeocodingSource", location.ReverseGeocodingSource);
        Add("$currentPosition", location.IsCurrentPosition ? 1L : 0L);
        Add("$needsGeocodeRefresh", location.NeedsGeocodeRefresh ? 1L : 0L);
    }

    // Writes one parameter per value and returns their names joined for an IN list. SQLite accepts an empty list.
    private static string InList(SqliteCommand command, string prefix, IEnumerable<string> values)
    {
        List<string> names = [];
        foreach (string value in values)
        {
            string name = prefix + names.Count;
            command.Parameters.AddWithValue(name, value);
            names.Add(name);
        }

        return string.Join(", ", names);
    }
}
