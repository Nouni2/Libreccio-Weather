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
/// How often the weather is refreshed in the background.
/// </summary>
public sealed class UpdateInterval
{
    public static readonly UpdateInterval Never = new("never", null);
    public static readonly UpdateInterval ThirtyMinutes = new("0:30", TimeSpan.FromMinutes(30));
    public static readonly UpdateInterval OneHour = new("1:00", TimeSpan.FromHours(1));
    public static readonly UpdateInterval OneAndAHalfHours = new("1:30", TimeSpan.FromHours(1.5));
    public static readonly UpdateInterval TwoHours = new("2:00", TimeSpan.FromHours(2));
    public static readonly UpdateInterval ThreeHours = new("3:00", TimeSpan.FromHours(3));
    public static readonly UpdateInterval SixHours = new("6:00", TimeSpan.FromHours(6));
    public static readonly UpdateInterval TwelveHours = new("12:00", TimeSpan.FromHours(12));
    public static readonly UpdateInterval TwentyFourHours = new("24:00", TimeSpan.FromHours(24));

    public static IReadOnlyList<UpdateInterval> Entries { get; } =
    [
        Never,
        ThirtyMinutes,
        OneHour,
        OneAndAHalfHours,
        TwoHours,
        ThreeHours,
        SixHours,
        TwelveHours,
        TwentyFourHours,
    ];

    private UpdateInterval(string id, TimeSpan? interval)
    {
        Id = id;
        Interval = interval;
    }

    public string Id { get; }

    public string Name => OptionNames.GetNameByValue(Id, "automatic_refresh_rates", "automatic_refresh_rate_values")!;

    /// <summary>
    /// The time between two refreshes, or null when background refresh is disabled.
    /// </summary>
    public TimeSpan? Interval { get; }

    /// <summary>
    /// How long a location's weather stays valid. It is 1.5 hours when background refresh is disabled.
    /// </summary>
    public TimeSpan Validity => Interval ?? TimeSpan.FromHours(1.5);

    public static UpdateInterval GetInstance(string value)
    {
        return Entries.FirstOrDefault(interval => interval.Id == value) ?? OneAndAHalfHours;
    }

    public override string ToString()
    {
        return Id;
    }
}
