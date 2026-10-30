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

namespace LibreccioWeather.Core.Units;

/// <summary>
/// A temperature, stored as a whole number of tenths of a degree Celsius.
/// </summary>
public readonly record struct Temperature : IComparable<Temperature>
{
    private Temperature(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in tenths of a degree Celsius.
    /// </summary>
    public long Value { get; }

    public double InDeciCelsius => ToDouble(TemperatureUnit.DeciCelsius);

    public double InCelsius => ToDouble(TemperatureUnit.Celsius);

    public double InFahrenheit => ToDouble(TemperatureUnit.Fahrenheit);

    public double InKelvins => ToDouble(TemperatureUnit.Kelvin);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Temperature From(long value, TemperatureUnit unit)
    {
        return new Temperature(UnitRounding.ToLong(TemperatureUnit.Convert(value, unit, TemperatureUnit.DeciCelsius)));
    }

    public static Temperature From(double value, TemperatureUnit unit)
    {
        double valueInDeciCelsius = TemperatureUnit.Convert(value, unit, TemperatureUnit.DeciCelsius);
        if (double.IsNaN(valueInDeciCelsius))
        {
            throw new ArgumentException("Temperature value cannot be NaN.", nameof(value));
        }

        return new Temperature(UnitRounding.RoundToLong(valueInDeciCelsius));
    }

    public static Temperature FromDeciCelsius(long value) => From(value, TemperatureUnit.DeciCelsius);

    public static Temperature FromDeciCelsius(double value) => From(value, TemperatureUnit.DeciCelsius);

    public static Temperature FromCelsius(long value) => From(value, TemperatureUnit.Celsius);

    public static Temperature FromCelsius(double value) => From(value, TemperatureUnit.Celsius);

    public static Temperature FromFahrenheit(long value) => From(value, TemperatureUnit.Fahrenheit);

    public static Temperature FromFahrenheit(double value) => From(value, TemperatureUnit.Fahrenheit);

    public static Temperature FromKelvins(long value) => From(value, TemperatureUnit.Kelvin);

    public static Temperature FromKelvins(double value) => From(value, TemperatureUnit.Kelvin);

    public double ToDouble(TemperatureUnit unit)
    {
        return TemperatureUnit.Convert(Value, TemperatureUnit.DeciCelsius, unit);
    }

    /// <summary>
    /// Converts this value as a temperature difference rather than an absolute temperature.
    /// </summary>
    public double ToDoubleDeviation(TemperatureUnit unit)
    {
        return TemperatureUnit.ConvertDeviation(Value, TemperatureUnit.DeciCelsius, unit);
    }

    /// <summary>
    /// Returns null outside -100 °C to 100 °C.
    /// </summary>
    public Temperature? ToValidOrNull()
    {
        return Value is >= -1000 and <= 1000 ? this : null;
    }

    public int CompareTo(Temperature other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(TemperatureUnit.DeciCelsius);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "21.5c".
    /// </summary>
    public string ToString(TemperatureUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Temperature left, Temperature right) => left.Value < right.Value;

    public static bool operator >(Temperature left, Temperature right) => left.Value > right.Value;

    public static bool operator <=(Temperature left, Temperature right) => left.Value <= right.Value;

    public static bool operator >=(Temperature left, Temperature right) => left.Value >= right.Value;
}
