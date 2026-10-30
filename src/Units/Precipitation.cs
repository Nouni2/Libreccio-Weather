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
/// A precipitation amount, stored as a whole number of micrometers.
/// </summary>
public readonly record struct Precipitation : IComparable<Precipitation>
{
    private Precipitation(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in micrometers.
    /// </summary>
    public long Value { get; }

    public double InMicrometers => ToDouble(PrecipitationUnit.Micrometer);

    public double InMillimeters => ToDouble(PrecipitationUnit.Millimeter);

    public double InCentimeters => ToDouble(PrecipitationUnit.Centimeter);

    public double InInches => ToDouble(PrecipitationUnit.Inch);

    public double InLitersPerSquareMeter => ToDouble(PrecipitationUnit.LiterPerSquareMeter);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Precipitation From(long value, PrecipitationUnit unit)
    {
        return new Precipitation(UnitRounding.ToLong(PrecipitationUnit.Convert(value, unit, PrecipitationUnit.Micrometer)));
    }

    public static Precipitation From(double value, PrecipitationUnit unit)
    {
        double valueInMicrometers = PrecipitationUnit.Convert(value, unit, PrecipitationUnit.Micrometer);
        if (double.IsNaN(valueInMicrometers))
        {
            throw new ArgumentException("Precipitation value cannot be NaN.", nameof(value));
        }

        return new Precipitation(UnitRounding.RoundToLong(valueInMicrometers));
    }

    public static Precipitation FromMicrometers(long value) => From(value, PrecipitationUnit.Micrometer);

    public static Precipitation FromMicrometers(double value) => From(value, PrecipitationUnit.Micrometer);

    public static Precipitation FromMillimeters(long value) => From(value, PrecipitationUnit.Millimeter);

    public static Precipitation FromMillimeters(double value) => From(value, PrecipitationUnit.Millimeter);

    public static Precipitation FromCentimeters(long value) => From(value, PrecipitationUnit.Centimeter);

    public static Precipitation FromCentimeters(double value) => From(value, PrecipitationUnit.Centimeter);

    public static Precipitation FromInches(long value) => From(value, PrecipitationUnit.Inch);

    public static Precipitation FromInches(double value) => From(value, PrecipitationUnit.Inch);

    public static Precipitation FromLitersPerSquareMeter(long value) => From(value, PrecipitationUnit.LiterPerSquareMeter);

    public static Precipitation FromLitersPerSquareMeter(double value) => From(value, PrecipitationUnit.LiterPerSquareMeter);

    public double ToDouble(PrecipitationUnit unit)
    {
        return PrecipitationUnit.Convert(Value, PrecipitationUnit.Micrometer, unit);
    }

    /// <summary>
    /// Returns null outside 0 to 1000 mm, the plausible range for one hour.
    /// </summary>
    public Precipitation? ToValidHourlyOrNull()
    {
        return Value is >= 0 and <= 1000000 ? this : null;
    }

    /// <summary>
    /// Returns null outside 0 to 10000 mm, the plausible range for half a day.
    /// </summary>
    public Precipitation? ToValidHalfDayOrNull()
    {
        return Value is >= 0 and <= 10000000 ? this : null;
    }

    public int CompareTo(Precipitation other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(PrecipitationUnit.Micrometer);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "2.4mm".
    /// </summary>
    public string ToString(PrecipitationUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Precipitation left, Precipitation right) => left.Value < right.Value;

    public static bool operator >(Precipitation left, Precipitation right) => left.Value > right.Value;

    public static bool operator <=(Precipitation left, Precipitation right) => left.Value <= right.Value;

    public static bool operator >=(Precipitation left, Precipitation right) => left.Value >= right.Value;
}
