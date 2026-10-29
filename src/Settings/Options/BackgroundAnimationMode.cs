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

public sealed class BackgroundAnimationMode
{
    public static readonly BackgroundAnimationMode System = new("system");
    public static readonly BackgroundAnimationMode Enabled = new("enabled");
    public static readonly BackgroundAnimationMode Disabled = new("disabled");

    public static IReadOnlyList<BackgroundAnimationMode> Entries { get; } = [System, Enabled, Disabled];

    private BackgroundAnimationMode(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public string Name => OptionNames.GetNameByValue(Id, "background_animation", "background_animation_values")!;

    public static BackgroundAnimationMode GetInstance(string value)
    {
        return Entries.FirstOrDefault(mode => mode.Id == value) ?? System;
    }

    public override string ToString()
    {
        return Id;
    }
}
