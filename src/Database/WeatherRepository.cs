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
using LibreccioWeather.Core.Models;
using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Database;

public sealed class WeatherRepository
{
    private readonly WeatherDatabase _database;

    public WeatherRepository(WeatherDatabase database)
    {
        _database = database;
    }

    public async Task<Weather?> GetWeatherByLocationIdAsync(
        string locationFormattedId,
        CancellationToken cancellationToken = default)
    {
        using SqliteConnection connection = _database.Open();

        // A refresh replaces several tables, so all parts must be read from the same database snapshot.
        using SqliteTransaction transaction = connection.BeginTransaction();
        Weather? weather;
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT * FROM weathers WHERE location_formatted_id = $locationId;";
            command.Parameters.AddWithValue("$locationId", locationFormattedId);
            using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            weather = await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? new WeatherReader(reader).ReadWeather()
                : null;
        }

        if (weather is null)
        {
            return null;
        }

        IReadOnlyList<Daily> daily = await ReadListAsync(
            connection, transaction, "dailys", "date", locationFormattedId,
            reader => reader.ReadDaily(), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Hourly> hourly = await ReadListAsync(
            connection, transaction, "hourlys", "date", locationFormattedId,
            reader => reader.ReadHourly(), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Minutely> minutely = await ReadListAsync(
            connection, transaction, "minutelys", "date", locationFormattedId,
            reader => reader.ReadMinutely(), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Alert> alerts = await ReadListAsync(
            connection, transaction, "alerts", "severity DESC, start_date ASC", locationFormattedId,
            reader => reader.ReadAlert(), cancellationToken).ConfigureAwait(false);
        IReadOnlyList<KeyValuePair<Month, Normals>> normals = await ReadListAsync(
            connection, transaction, "normals", "month", locationFormattedId,
            reader => reader.ReadNormals(), cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return weather with
        {
            DailyForecast = daily,
            HourlyForecast = hourly,
            MinutelyForecast = minutely,
            AlertList = alerts,
            Normals = normals.ToDictionary(pair => pair.Key, pair => pair.Value),
        };
    }

    public async Task InsertAsync(Location location, Weather weather, CancellationToken cancellationToken = default)
    {
        using SqliteConnection connection = _database.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();
        string locationId = location.FormattedId;

        await InsertRowAsync(
            connection, transaction, "weathers", locationId, weather,
            (writer, value) => writer.WriteWeather(value), cancellationToken).ConfigureAwait(false);
        await ReplaceListAsync(
            connection, transaction, "dailys", locationId, weather.DailyForecast,
            (writer, value) => writer.WriteDaily(value), cancellationToken).ConfigureAwait(false);
        await ReplaceListAsync(
            connection, transaction, "hourlys", locationId, weather.HourlyForecast,
            (writer, value) => writer.WriteHourly(value), cancellationToken).ConfigureAwait(false);
        await ReplaceListAsync(
            connection, transaction, "minutelys", locationId, weather.MinutelyForecast,
            (writer, value) => writer.WriteMinutely(value), cancellationToken).ConfigureAwait(false);
        await ReplaceListAsync(
            connection, transaction, "alerts", locationId, weather.AlertList,
            (writer, value) => writer.WriteAlert(value), cancellationToken).ConfigureAwait(false);
        await ReplaceListAsync(
            connection, transaction, "normals", locationId, weather.Normals,
            (writer, value) => writer.WriteNormals(value), cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<T>> ReadListAsync<T>(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table,
        string orderBy,
        string locationId,
        Func<WeatherReader, T> read,
        CancellationToken cancellationToken)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT * FROM {table} WHERE location_formatted_id = $locationId ORDER BY {orderBy};";
        command.Parameters.AddWithValue("$locationId", locationId);
        using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        WeatherReader weatherReader = new(reader);
        List<T> result = [];
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(read(weatherReader));
        }

        return result;
    }

    private static async Task ReplaceListAsync<T>(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table,
        string locationId,
        IEnumerable<T> items,
        Action<WeatherWriter, T> write,
        CancellationToken cancellationToken)
    {
        using (SqliteCommand delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = $"DELETE FROM {table} WHERE location_formatted_id = $locationId;";
            delete.Parameters.AddWithValue("$locationId", locationId);
            await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (T item in items)
        {
            await InsertRowAsync(connection, transaction, table, locationId, item, write, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InsertRowAsync<T>(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string table,
        string locationId,
        T item,
        Action<WeatherWriter, T> write,
        CancellationToken cancellationToken)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$location_formatted_id", locationId);
        write(new WeatherWriter(command), item);

        List<string> names = command.Parameters.Cast<SqliteParameter>().Select(parameter => parameter.ParameterName).ToList();
        string columns = string.Join(", ", names.Select(name => name[1..]));
        command.CommandText = $"INSERT INTO {table}({columns}) VALUES ({string.Join(", ", names)});";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
