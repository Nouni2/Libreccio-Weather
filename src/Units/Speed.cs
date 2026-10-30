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
/// A speed, stored as a whole number of centimeters per second.
/// </summary>
public readonly record struct Speed : IComparable<Speed>
{
    private Speed(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in centimeters per second.
    /// </summary>
    public long Value { get; }

    public double InCentimetersPerSecond => ToDouble(SpeedUnit.CentimeterPerSecond);

    public double InMetersPerSecond => ToDouble(SpeedUnit.MeterPerSecond);

    public double InKilometersPerHour => ToDouble(SpeedUnit.KilometerPerHour);

    public double InMilesPerHour => ToDouble(SpeedUnit.MilePerHour);

    public double InKnots => ToDouble(SpeedUnit.Knot);

    public double InFeetPerSecond => ToDouble(SpeedUnit.FootPerSecond);

    public int InBeaufort => (int)UnitRounding.RoundToLong(ToDouble(SpeedUnit.BeaufortScale));

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Speed From(long value, SpeedUnit unit)
    {
        return new Speed(UnitRounding.ToLong(SpeedUnit.Convert(value, unit, SpeedUnit.CentimeterPerSecond)));
    }

    public static Speed From(double value, SpeedUnit unit)
    {
        double valueInCentimetersPerSecond = SpeedUnit.Convert(value, unit, SpeedUnit.CentimeterPerSecond);
        if (double.IsNaN(valueInCentimetersPerSecond))
        {
            throw new ArgumentException("Speed value cannot be NaN.", nameof(value));
        }

        return new Speed(UnitRounding.RoundToLong(valueInCentimetersPerSecond));
    }

    public static Speed FromCentimetersPerSecond(long value) => From(value, SpeedUnit.CentimeterPerSecond);

    public static Speed FromCentimetersPerSecond(double value) => From(value, SpeedUnit.CentimeterPerSecond);

    public static Speed FromMetersPerSecond(long value) => From(value, SpeedUnit.MeterPerSecond);

    public static Speed FromMetersPerSecond(double value) => From(value, SpeedUnit.MeterPerSecond);

    public static Speed FromKilometersPerHour(long value) => From(value, SpeedUnit.KilometerPerHour);

    public static Speed FromKilometersPerHour(double value) => From(value, SpeedUnit.KilometerPerHour);

    public static Speed FromMilesPerHour(long value) => From(value, SpeedUnit.MilePerHour);

    public static Speed FromMilesPerHour(double value) => From(value, SpeedUnit.MilePerHour);

    public static Speed FromKnots(long value) => From(value, SpeedUnit.Knot);

    public static Speed FromKnots(double value) => From(value, SpeedUnit.Knot);

    public static Speed FromFeetPerSecond(long value) => From(value, SpeedUnit.FootPerSecond);

    public static Speed FromFeetPerSecond(double value) => From(value, SpeedUnit.FootPerSecond);

    public static Speed FromBeaufort(long value) => From(value, SpeedUnit.BeaufortScale);

    public static Speed FromBeaufort(double value) => From(value, SpeedUnit.BeaufortScale);

    public double ToDouble(SpeedUnit unit)
    {
        return SpeedUnit.Convert(Value, SpeedUnit.CentimeterPerSecond, unit);
    }

    /// <summary>
    /// Returns null outside 0 to 150 m/s.
    /// </summary>
    public Speed? ToValidOrNull()
    {
        return Value is >= 0 and <= 15000 ? this : null;
    }

    public int CompareTo(Speed other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(SpeedUnit.CentimeterPerSecond);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "15.2kph".
    /// </summary>
    public string ToString(SpeedUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Speed left, Speed right) => left.Value < right.Value;

    public static bool operator >(Speed left, Speed right) => left.Value > right.Value;

    public static bool operator <=(Speed left, Speed right) => left.Value <= right.Value;

    public static bool operator >=(Speed left, Speed right) => left.Value >= right.Value;
}
