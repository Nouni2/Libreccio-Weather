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

using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Database;

/// <summary>
/// The SQLite database holding saved locations.
/// </summary>
public sealed class WeatherDatabase
{
    // Stored in SQLite's user_version, so a later schema change can tell which version a file has.
    private const int SchemaVersion = 1;

    private const string CreateSchema = """
        CREATE TABLE locations(
            formatted_id TEXT NOT NULL PRIMARY KEY,
            list_order INTEGER NOT NULL,
            city_id TEXT,
            latitude REAL NOT NULL,
            longitude REAL NOT NULL,
            timezone TEXT NOT NULL,
            custom_name TEXT,
            country TEXT NOT NULL,
            country_code TEXT,
            admin1 TEXT,
            admin1_code TEXT,
            admin2 TEXT,
            admin2_code TEXT,
            admin3 TEXT,
            admin3_code TEXT,
            admin4 TEXT,
            admin4_code TEXT,
            city TEXT NOT NULL,
            district TEXT,
            weather_source TEXT NOT NULL,
            current_source TEXT,
            air_quality_source TEXT,
            pollen_source TEXT,
            minutely_source TEXT,
            alert_source TEXT,
            normals_source TEXT,
            reverse_geocoding_source TEXT,
            current_position INTEGER NOT NULL DEFAULT 0,
            needs_geocode_refresh INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE location_parameters(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,
            source TEXT NOT NULL,
            parameter TEXT NOT NULL,
            value TEXT NOT NULL,
            UNIQUE(location_formatted_id, source, parameter) ON CONFLICT REPLACE,
            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id) ON DELETE CASCADE
        );

        CREATE INDEX location_parameters_location_formatted_id_index ON location_parameters(location_formatted_id);
        """;

    private readonly string _path;
    private readonly string _connectionString;

    public WeatherDatabase(string path)
    {
        _path = path;

        // Foreign keys are off by default in SQLite, and parameters rely on them to go with their location.
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            ForeignKeys = true,
        }.ToString();
    }

    /// <summary>
    /// %LOCALAPPDATA%\Libreccio Weather\libreccio-weather.db, the usual place for data that belongs to one Windows user.
    /// </summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Libreccio Weather",
        "libreccio-weather.db");

    /// <summary>
    /// Creates the folder, the file and the tables when they do not exist yet.
    /// </summary>
    public void EnsureCreated()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        using SqliteConnection connection = Open();

        using SqliteCommand versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        long version = (long)(versionCommand.ExecuteScalar() ?? 0L);
        if (version >= SchemaVersion)
        {
            return;
        }

        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand createCommand = connection.CreateCommand();
        createCommand.Transaction = transaction;
        createCommand.CommandText = CreateSchema + $"PRAGMA user_version = {SchemaVersion};";
        createCommand.ExecuteNonQuery();
        transaction.Commit();
    }

    internal SqliteConnection Open()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }
}
