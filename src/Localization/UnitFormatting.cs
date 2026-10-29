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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Values with their unit, formatted from the translated unit patterns in UnitStrings, such as "{0} °C".
/// </summary>
public static class UnitFormatting
{
    public static string Format(
        Temperature temperature,
        TemperatureUnit unit,
        UnitWidth valueWidth = UnitWidth.Short,
        UnitWidth unitWidth = UnitWidth.Short)
    {
        string value = NumberFormatting.Format(temperature.ToDouble(unit), unit.GetPrecision(valueWidth), Strings.Culture);
        return string.Format(Strings.Culture, Nominative("temperature", unit, unitWidth), value);
    }

    /// <summary>
    /// The unit symbol alone, such as "°C".
    /// </summary>
    public static string GetNominativeUnit(TemperatureUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return string.Format(Strings.Culture, Nominative("temperature", unit, width), "").Trim();
    }

    /// <summary>
    /// The value rounded to a whole number, half towards positive infinity, without its unit.
    /// </summary>
    public static string FormatRounded(Temperature temperature, TemperatureUnit unit)
    {
        return NumberFormatting.Format(temperature.ToDouble(unit), 0, Strings.Culture);
    }

    public static int RoundToInt(double value)
    {
        return (int)Math.Floor(value + 0.5);
    }

    // Keys follow the upstream names, such as temperature_c_nominative_short.
    private static string Nominative(string quantity, WeatherUnit unit, UnitWidth width)
    {
        string suffix = width switch
        {
            UnitWidth.Narrow => "narrow",
            UnitWidth.Long => "long",
            _ => "short",
        };
        return Strings.GetUnit($"{quantity}_{unit.Id}_nominative_{suffix}");
    }
}
