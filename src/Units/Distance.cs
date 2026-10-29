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
/// A distance, stored as a whole number of meters.
/// </summary>
public readonly record struct Distance : IComparable<Distance>
{
    private Distance(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in meters.
    /// </summary>
    public long Value { get; }

    public double InMeters => ToDouble(DistanceUnit.Meter);

    public double InKilometers => ToDouble(DistanceUnit.Kilometer);

    public double InMiles => ToDouble(DistanceUnit.Mile);

    public double InNauticalMiles => ToDouble(DistanceUnit.NauticalMile);

    public double InFeet => ToDouble(DistanceUnit.Foot);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Distance From(long value, DistanceUnit unit)
    {
        return new Distance((long)DistanceUnit.Convert(value, unit, DistanceUnit.Meter));
    }

    public static Distance From(double value, DistanceUnit unit)
    {
        double valueInMeters = DistanceUnit.Convert(value, unit, DistanceUnit.Meter);
        if (double.IsNaN(valueInMeters))
        {
            throw new ArgumentException("Distance value cannot be NaN.", nameof(value));
        }

        return new Distance(UnitRounding.RoundToLong(valueInMeters));
    }

    public static Distance FromMeters(long value) => From(value, DistanceUnit.Meter);

    public static Distance FromMeters(double value) => From(value, DistanceUnit.Meter);

    public static Distance FromKilometers(long value) => From(value, DistanceUnit.Kilometer);

    public static Distance FromKilometers(double value) => From(value, DistanceUnit.Kilometer);

    public static Distance FromMiles(long value) => From(value, DistanceUnit.Mile);

    public static Distance FromMiles(double value) => From(value, DistanceUnit.Mile);

    public static Distance FromNauticalMiles(long value) => From(value, DistanceUnit.NauticalMile);

    public static Distance FromNauticalMiles(double value) => From(value, DistanceUnit.NauticalMile);

    public static Distance FromFeet(long value) => From(value, DistanceUnit.Foot);

    public static Distance FromFeet(double value) => From(value, DistanceUnit.Foot);

    public double ToDouble(DistanceUnit unit)
    {
        return DistanceUnit.Convert(Value, DistanceUnit.Meter, unit);
    }

    /// <summary>
    /// Returns null for a negative distance.
    /// </summary>
    public Distance? ToValidOrNull()
    {
        return Value >= 0 ? this : null;
    }

    public int CompareTo(Distance other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(DistanceUnit.Meter);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "12.5km".
    /// </summary>
    public string ToString(DistanceUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Distance left, Distance right) => left.Value < right.Value;

    public static bool operator >(Distance left, Distance right) => left.Value > right.Value;

    public static bool operator <=(Distance left, Distance right) => left.Value <= right.Value;

    public static bool operator >=(Distance left, Distance right) => left.Value >= right.Value;
}
