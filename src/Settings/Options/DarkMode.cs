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

public sealed class DarkMode
{
    public static readonly DarkMode System = new("system");
    public static readonly DarkMode Light = new("light");
    public static readonly DarkMode Dark = new("dark");

    public static IReadOnlyList<DarkMode> Entries { get; } = [System, Light, Dark];

    private DarkMode(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Name => OptionNames.GetNameByValue(Id, "dark_modes", "dark_mode_values")!;

    public static DarkMode GetInstance(string value)
    {
        return Entries.FirstOrDefault(mode => mode.Id == value) ?? System;
    }

    public override string ToString()
    {
        return Id;
    }
}
