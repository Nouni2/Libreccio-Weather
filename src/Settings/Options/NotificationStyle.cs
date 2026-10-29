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

namespace LibreccioWeather.Core.Settings.Options;

/// <summary>
/// The style of the Mistray popup: compact, several locations, days or hours.
/// </summary>
public sealed class NotificationStyle
{
    public static readonly NotificationStyle Native = new("native");
    public static readonly NotificationStyle Cities = new("cities");
    public static readonly NotificationStyle Daily = new("daily");
    public static readonly NotificationStyle Hourly = new("hourly");

    public static IReadOnlyList<NotificationStyle> Entries { get; } = [Native, Cities, Daily, Hourly];

    private NotificationStyle(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Name => OptionNames.GetNameByValue(Id, "notification_styles", "notification_style_values")!;

    public static NotificationStyle GetInstance(string value)
    {
        return Entries.FirstOrDefault(style => style.Id == value) ?? Daily;
    }

    public override string ToString()
    {
        return Id;
    }
}
