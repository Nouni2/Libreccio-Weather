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

using System.Globalization;
using System.Resources;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Translated text, looked up by the same keys as Breezy Weather's Android strings.
/// A key missing from the selected language falls back to English.
/// </summary>
public static class Strings
{
    // The names follow the embedded resource names of translations/Strings.resx and translations/UnitStrings.resx.
    private static readonly ResourceManager AppText =
        new("LibreccioWeather.Core.translations.Strings", typeof(Strings).Assembly);

    private static readonly ResourceManager UnitText =
        new("LibreccioWeather.Core.translations.UnitStrings", typeof(Strings).Assembly);

    /// <summary>
    /// The selected language. Also used to format numbers and dates.
    /// </summary>
    public static CultureInfo Culture { get; set; } = CultureInfo.GetCultureInfo(AppLanguages.Default);

    public static string Get(string key)
    {
        return AppText.GetString(key, Culture) ?? throw MissingKey(key);
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(Culture, Get(key), args);
    }

    /// <summary>
    /// The plural form for the count, from keys ending in the CLDR category such as "_one" and "_other".
    /// </summary>
    public static string GetPlural(string key, long count, params object?[] args)
    {
        string text = AppText.GetString(key + "_" + PluralRules.Select(Culture, count), Culture)
            ?? AppText.GetString(key + "_other", Culture)
            ?? throw MissingKey(key);
        return string.Format(Culture, text, args);
    }

    public static string GetUnit(string key)
    {
        return UnitText.GetString(key, Culture) ?? throw MissingKey(key);
    }

    private static KeyNotFoundException MissingKey(string key)
    {
        return new KeyNotFoundException($"No translation found for '{key}'.");
    }
}
