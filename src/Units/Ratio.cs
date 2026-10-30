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
/// A ratio such as humidity or cloud cover, stored as a whole number of per mille.
/// </summary>
public readonly record struct Ratio : IComparable<Ratio>
{
    private Ratio(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in per mille.
    /// </summary>
    public long Value { get; }

    public double InPermille => ToDouble(RatioUnit.Permille);

    public double InPercent => ToDouble(RatioUnit.Percent);

    public double InFraction => ToDouble(RatioUnit.Fraction);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static Ratio From(long value, RatioUnit unit)
    {
        return new Ratio(UnitRounding.ToLong(RatioUnit.Convert(value, unit, RatioUnit.Permille)));
    }

    public static Ratio From(double value, RatioUnit unit)
    {
        double valueInPermille = RatioUnit.Convert(value, unit, RatioUnit.Permille);
        if (double.IsNaN(valueInPermille))
        {
            throw new ArgumentException("Ratio value cannot be NaN.", nameof(value));
        }

        return new Ratio(UnitRounding.RoundToLong(valueInPermille));
    }

    public static Ratio FromPermille(long value) => From(value, RatioUnit.Permille);

    public static Ratio FromPermille(double value) => From(value, RatioUnit.Permille);

    public static Ratio FromPercent(long value) => From(value, RatioUnit.Percent);

    public static Ratio FromPercent(double value) => From(value, RatioUnit.Percent);

    public static Ratio FromFraction(long value) => From(value, RatioUnit.Fraction);

    public static Ratio FromFraction(double value) => From(value, RatioUnit.Fraction);

    public double ToDouble(RatioUnit unit)
    {
        return RatioUnit.Convert(Value, RatioUnit.Permille, unit);
    }

    /// <summary>
    /// Returns null when the stored per mille value is outside the given inclusive range, 0 to 1000 by default.
    /// </summary>
    public Ratio? ToValidRangeOrNull(int minPermille = 0, int maxPermille = 1000)
    {
        return Value >= minPermille && Value <= maxPermille ? this : null;
    }

    public int CompareTo(Ratio other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(RatioUnit.Permille);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "65.0percent".
    /// </summary>
    public string ToString(RatioUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(Ratio left, Ratio right) => left.Value < right.Value;

    public static bool operator >(Ratio left, Ratio right) => left.Value > right.Value;

    public static bool operator <=(Ratio left, Ratio right) => left.Value <= right.Value;

    public static bool operator >=(Ratio left, Ratio right) => left.Value >= right.Value;
}
