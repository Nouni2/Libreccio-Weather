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
/// A pressure, stored as a whole number of pascals.
/// </summary>
public readonly record struct Pressure : IComparable<Pressure>
{
    private Pressure(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in pascals.
    /// </summary>
    public long Value { get; }

    public double InPascals => ToDouble(PressureUnit.Pascal);

    public double InHectopascals => ToDouble(PressureUnit.Hectopascal);

    public double InKilopascals => ToDouble(PressureUnit.Kilopascal);

    public double InMillibars => ToDouble(PressureUnit.Millibar);

    public double InAtmospheres => ToDouble(PressureUnit.Atmosphere);

    public double InMillimetersOfMercury => ToDouble(PressureUnit.MillimeterOfMercury);

    public double InInchesOfMercury => ToDouble(PressureUnit.InchOfMercury);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Pressure From(long value, PressureUnit unit)
    {
        return new Pressure(UnitRounding.ToLong(PressureUnit.Convert(value, unit, PressureUnit.Pascal)));
    }

    public static Pressure From(double value, PressureUnit unit)
    {
        double valueInPascals = PressureUnit.Convert(value, unit, PressureUnit.Pascal);
        if (double.IsNaN(valueInPascals))
        {
            throw new ArgumentException("Pressure value cannot be NaN.", nameof(value));
        }

        return new Pressure(UnitRounding.RoundToLong(valueInPascals));
    }

    public static Pressure FromPascals(long value) => From(value, PressureUnit.Pascal);

    public static Pressure FromPascals(double value) => From(value, PressureUnit.Pascal);

    public static Pressure FromHectopascals(long value) => From(value, PressureUnit.Hectopascal);

    public static Pressure FromHectopascals(double value) => From(value, PressureUnit.Hectopascal);

    public static Pressure FromKilopascals(long value) => From(value, PressureUnit.Kilopascal);

    public static Pressure FromKilopascals(double value) => From(value, PressureUnit.Kilopascal);

    public static Pressure FromMillibars(long value) => From(value, PressureUnit.Millibar);

    public static Pressure FromMillibars(double value) => From(value, PressureUnit.Millibar);

    public static Pressure FromAtmospheres(long value) => From(value, PressureUnit.Atmosphere);

    public static Pressure FromAtmospheres(double value) => From(value, PressureUnit.Atmosphere);

    public static Pressure FromMillimetersOfMercury(long value) => From(value, PressureUnit.MillimeterOfMercury);

    public static Pressure FromMillimetersOfMercury(double value) => From(value, PressureUnit.MillimeterOfMercury);

    public static Pressure FromInchesOfMercury(long value) => From(value, PressureUnit.InchOfMercury);

    public static Pressure FromInchesOfMercury(double value) => From(value, PressureUnit.InchOfMercury);

    public double ToDouble(PressureUnit unit)
    {
        return PressureUnit.Convert(Value, PressureUnit.Pascal, unit);
    }

    /// <summary>
    /// Returns null outside 800 hPa to 1200 hPa.
    /// </summary>
    public Pressure? ToValidOrNull()
    {
        return Value is >= 80000 and <= 120000 ? this : null;
    }

    public int CompareTo(Pressure other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(PressureUnit.Pascal);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "1013.3hpa".
    /// </summary>
    public string ToString(PressureUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Pressure left, Pressure right) => left.Value < right.Value;

    public static bool operator >(Pressure left, Pressure right) => left.Value > right.Value;

    public static bool operator <=(Pressure left, Pressure right) => left.Value <= right.Value;

    public static bool operator >=(Pressure left, Pressure right) => left.Value >= right.Value;
}
