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

using LibreccioWeather.Core.Settings;

namespace LibreccioWeather.Core.Tests.Settings;

// The JSON file replaces Android's shared preferences, which have no equivalent in .NET.
public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"libreccio-settings-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        File.Delete(_path);
    }

    [Fact]
    public void Put_Reopened_KeepsValues()
    {
        ConfigStore store = new(_path);
        store.PutString("refresh_rate", "3:00");
        store.PutBoolean("alert_notification_switch", false);
        store.PutLong("language_update_last_timestamp", 1_700_000_000_000);

        ConfigStore reopened = new(_path);
        Assert.Equal("3:00", reopened.GetString("refresh_rate", null));
        Assert.False(reopened.GetBoolean("alert_notification_switch", true));
        Assert.Equal(1_700_000_000_000, reopened.GetLong("language_update_last_timestamp", 0));
    }

    [Fact]
    public void Get_DamagedFile_ReturnsDefaults()
    {
        File.WriteAllText(_path, "{ not json");

        SettingsManager settings = new(new ConfigStore(_path));
        Assert.True(settings.IsAlertPushEnabled);
        Assert.Equal(SettingsManager.DefaultTodayForecastTime, settings.TodayForecastTime);
    }
}
