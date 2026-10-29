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

public sealed class DistanceUnit : WeatherUnit
{
    public static readonly DistanceUnit Meter = new(
        "m",
        fromReference: value => value,
        toReference: value => value,
        new UnitDecimals(0),
        chartStep: maxY => maxY < 40000 ? 5000.0 : 10000.0);

    public static readonly DistanceUnit Kilometer = new(
        "km",
        fromReference: value => value / 1000.0,
        toReference: value => value * 1000.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: maxY => maxY < 4 ? 5.0 : 10.0);

    public static readonly DistanceUnit Mile = new(
        "mi",
        fromReference: value => value / 1609.344,
        toReference: value => value * 1609.344,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: MileChartStep);

    public static readonly DistanceUnit NauticalMile = new(
        "nmi",
        fromReference: value => value / 1852.0,
        toReference: value => value * 1852.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: MileChartStep);

    public static readonly DistanceUnit Foot = new(
        "ft",
        fromReference: value => value * 3.28084,
        toReference: value => value / 3.28084,
        new UnitDecimals(0),
        chartStep: maxY => maxY < 150000 ? 25000.0 : 50000.0);

    public static IReadOnlyList<DistanceUnit> All { get; } = [Meter, Kilometer, Mile, NauticalMile, Foot];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;
    private readonly Func<double, double> _chartStep;

    private DistanceUnit(
        string id,
        Func<double, double> fromReference,
        Func<double, double> toReference,
        UnitDecimals decimals,
        Func<double, double> chartStep)
        : base(id, decimals)
    {
        _fromReference = fromReference;
        _toReference = toReference;
        _chartStep = chartStep;
    }

    /// <summary>
    /// Step between chart grid lines, which depends on the highest value shown.
    /// </summary>
    public double GetChartStep(double maxY)
    {
        return _chartStep(maxY);
    }

    public static DistanceUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    /// <summary>
    /// Default unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// Germany uses kilometers rather than CLDR's meters, as its national weather service does.
    /// </summary>
    public static DistanceUnit GetDefaultUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "NL" => Meter,
            "GB" or "US" => Mile,
            _ => Kilometer,
        };
    }

    internal static double Convert(double value, DistanceUnit sourceUnit, DistanceUnit targetUnit)
    {
        if (sourceUnit == Meter)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == Meter)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }

    private static double MileChartStep(double maxY)
    {
        if (maxY <= 15.0)
        {
            return 3.0;
        }

        return maxY <= 30.0 ? 5.0 : 30.0;
    }
}
