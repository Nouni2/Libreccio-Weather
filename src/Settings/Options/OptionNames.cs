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

using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Core.Settings.Options;

/// <summary>
/// Names of option values, from a pair of string arrays: one of names and one of the stored values, in the same order.
/// </summary>
public static class OptionNames
{
    public static string? GetNameByValue(string value, string nameArrayKey, string valueArrayKey)
    {
        return GetNameByValue(value, Strings.GetArray(nameArrayKey), Strings.GetArray(valueArrayKey));
    }

    // Pairs stop at the shorter array, as Kotlin's zip does.
    public static string? GetNameByValue(string value, IReadOnlyList<string> names, IReadOnlyList<string> values)
    {
        return values.Zip(names).FirstOrDefault(pair => pair.First == value).Second;
    }
}
