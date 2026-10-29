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
using System.Text.Json.Nodes;

namespace LibreccioWeather.Core.Settings;

/// <summary>
/// Settings stored as key-value pairs in a JSON file. Each change is written to the file at once.
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly Lock _lock = new();
    private readonly JsonObject _values;

    public ConfigStore(string path)
    {
        _path = path;
        _values = Load(path);
    }

    public string? GetString(string key, string? defaultValue)
    {
        return Read(key, node => node.GetValue<string>(), defaultValue);
    }

    public int GetInt(string key, int defaultValue)
    {
        return Read(key, node => node.GetValue<int>(), defaultValue);
    }

    public long GetLong(string key, long defaultValue)
    {
        return Read(key, node => node.GetValue<long>(), defaultValue);
    }

    public bool GetBoolean(string key, bool defaultValue)
    {
        return Read(key, node => node.GetValue<bool>(), defaultValue);
    }

    public bool Contains(string key)
    {
        lock (_lock)
        {
            return _values.ContainsKey(key);
        }
    }

    public void PutString(string key, string? value)
    {
        // A null string removes the key, so the default applies again.
        Write(key, value is null ? null : JsonValue.Create(value));
    }

    public void PutInt(string key, int value)
    {
        Write(key, JsonValue.Create(value));
    }

    public void PutLong(string key, long value)
    {
        Write(key, JsonValue.Create(value));
    }

    public void PutBoolean(string key, bool value)
    {
        Write(key, JsonValue.Create(value));
    }

    public void Remove(string key)
    {
        Write(key, null);
    }

    // A value of another type than the one asked for gives the default, so a damaged entry never stops the app.
    private T Read<T>(string key, Func<JsonNode, T> read, T defaultValue)
    {
        lock (_lock)
        {
            if (_values[key] is not { } node)
            {
                return defaultValue;
            }

            try
            {
                return read(node);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FormatException)
            {
                return defaultValue;
            }
        }
    }

    private void Write(string key, JsonNode? value)
    {
        lock (_lock)
        {
            if (value is null)
            {
                _values.Remove(key);
            }
            else
            {
                _values[key] = value;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);

            // Writing to a temporary file first means a crash during the write never leaves a half-written file.
            string temporaryPath = _path + ".tmp";
            File.WriteAllText(temporaryPath, _values.ToJsonString(WriteOptions));
            File.Move(temporaryPath, _path, overwrite: true);
        }
    }

    // A missing or unreadable file starts with no settings, so every setting takes its default.
    private static JsonObject Load(string path)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? new JsonObject();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new JsonObject();
        }
    }
}
