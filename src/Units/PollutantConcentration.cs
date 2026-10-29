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
/// An air pollutant concentration, stored as a whole number of micrograms per cubic meter.
/// </summary>
public readonly record struct PollutantConcentration : IComparable<PollutantConcentration>
{
    private PollutantConcentration(long value)
    {
        Value = value;
    }

    /// <summary>
    /// The stored value, in micrograms per cubic meter.
    /// </summary>
    public long Value { get; }

    public double InMicrogramsPerCubicMeter => ToDouble(PollutantConcentrationUnit.MicrogramPerCubicMeter);

    public double InMilligramsPerCubicMeter => ToDouble(PollutantConcentrationUnit.MilligramPerCubicMeter);

    // A whole number is converted then truncated, while a fractional number is converted then rounded.
    public static PollutantConcentration From(long value, PollutantConcentrationUnit unit)
    {
        return new PollutantConcentration(
            (long)PollutantConcentrationUnit.Convert(value, unit, PollutantConcentrationUnit.MicrogramPerCubicMeter));
    }

    public static PollutantConcentration From(double value, PollutantConcentrationUnit unit)
    {
        double valueInMicrograms =
            PollutantConcentrationUnit.Convert(value, unit, PollutantConcentrationUnit.MicrogramPerCubicMeter);
        if (double.IsNaN(valueInMicrograms))
        {
            throw new ArgumentException("Pollutant concentration value cannot be NaN.", nameof(value));
        }

        return new PollutantConcentration(UnitRounding.RoundToLong(valueInMicrograms));
    }

    public static PollutantConcentration FromMicrogramsPerCubicMeter(long value) =>
        From(value, PollutantConcentrationUnit.MicrogramPerCubicMeter);

    public static PollutantConcentration FromMicrogramsPerCubicMeter(double value) =>
        From(value, PollutantConcentrationUnit.MicrogramPerCubicMeter);

    public static PollutantConcentration FromMilligramsPerCubicMeter(long value) =>
        From(value, PollutantConcentrationUnit.MilligramPerCubicMeter);

    public static PollutantConcentration FromMilligramsPerCubicMeter(double value) =>
        From(value, PollutantConcentrationUnit.MilligramPerCubicMeter);

    public double ToDouble(PollutantConcentrationUnit unit)
    {
        return PollutantConcentrationUnit.Convert(Value, PollutantConcentrationUnit.MicrogramPerCubicMeter, unit);
    }

    /// <summary>
    /// Returns null for a negative concentration.
    /// </summary>
    public PollutantConcentration? ToValidOrNull()
    {
        return Value >= 0 ? this : null;
    }

    public int CompareTo(PollutantConcentration other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return ToString(PollutantConcentrationUnit.MicrogramPerCubicMeter);
    }

    /// <summary>
    /// The value in the given unit followed by the unit identifier, such as "12microgpcum".
    /// </summary>
    public string ToString(PollutantConcentrationUnit unit, int? decimals = null)
    {
        int requested = decimals ?? unit.Decimals.Short;
        ArgumentOutOfRangeException.ThrowIfNegative(requested, nameof(decimals));
        return UnitDecimals.FormatToExactDecimals(ToDouble(unit), Math.Min(requested, unit.Decimals.Long)) + unit.Id;
    }

    public static bool operator <(PollutantConcentration left, PollutantConcentration right) =>
        left.Value < right.Value;

    public static bool operator >(PollutantConcentration left, PollutantConcentration right) =>
        left.Value > right.Value;

    public static bool operator <=(PollutantConcentration left, PollutantConcentration right) =>
        left.Value <= right.Value;

    public static bool operator >=(PollutantConcentration left, PollutantConcentration right) =>
        left.Value >= right.Value;
}
