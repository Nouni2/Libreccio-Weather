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

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Which formatters the unit, percent and duration formatting may use, as chosen in the debug settings. The app keeps
/// these in step with the settings, so formatting does not read the settings file for every value.
/// </summary>
public static class FormatterSettings
{
    /// <summary>
    /// Whether ICU's number formatter, which formats a value with its unit and sign in one call, may be used.
    /// </summary>
    public static bool UseNumberFormatter { get; set; } = true;

    /// <summary>
    /// Whether ICU's measure formatting may be used: unit names, values with their unit when the number formatter is
    /// off, and plain numbers with the locale's grouping.
    /// </summary>
    public static bool UseMeasureFormat { get; set; } = true;

    /// <summary>
    /// Whether Windows' ICU has the number formatter, without which the first switch has no effect.
    /// </summary>
    public static bool IsNumberFormatterSupported => Icu.SupportsUnitNames;
}
