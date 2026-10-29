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
/// A pollen or mold concentration, stored as a whole number per cubic meter.
/// </summary>
public readonly record struct PollenConcentration : IComparable<PollenConcentration>
{
    private PollenConcentration(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, per cubic meter.
    /// </summary>
    public long Value { get; }

    public double InPerCubicMeter => Value;

    public static PollenConcentration FromPerCubicMeter(long value)
    {
        return new PollenConcentration(value);
    }

    public static PollenConcentration FromPerCubicMeter(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Pollen concentration value cannot be NaN.", nameof(value));
        }

        return new PollenConcentration(UnitRounding.RoundToLong(value));
    }

    // Per cubic meter is the only pollen unit, so no conversion is needed.
    public double ToDouble(PollenConcentrationUnit unit)
    {
        return Value;
    }

    /// <summary>
    /// Returns null for a negative concentration.
    /// </summary>
    public PollenConcentration? ToValidOrNull()
    {
        return Value >= 0 ? this : null;
    }

    public int CompareTo(PollenConcentration other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(PollenConcentrationUnit.PerCubicMeter);
    }

    /// <summary>
    /// The value followed by the unit identifier, such as "42pcum".
    /// </summary>
    public string ToString(PollenConcentrationUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(PollenConcentration left, PollenConcentration right) => left.Value < right.Value;

    public static bool operator >(PollenConcentration left, PollenConcentration right) => left.Value > right.Value;

    public static bool operator <=(PollenConcentration left, PollenConcentration right) => left.Value <= right.Value;

    public static bool operator >=(PollenConcentration left, PollenConcentration right) => left.Value >= right.Value;
}
