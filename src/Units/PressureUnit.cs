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

public sealed class PressureUnit : WeatherUnit
{
    /// <summary>
    /// Standard atmospheric pressure at sea level, in pascals.
    /// </summary>
    public const int Standard = 101325;

    public static readonly PressureUnit Pascal = new(
        "pa",
        fromReference: value => value,
        toReference: value => value,
        new UnitDecimals(0),
        chartStep: 15.0);

    public static readonly PressureUnit Hectopascal = new(
        "hpa",
        fromReference: value => value / 100.0,
        toReference: value => value * 100.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 15.0);

    public static readonly PressureUnit Kilopascal = new(
        "kpa",
        fromReference: value => value / 1000.0,
        toReference: value => value * 1000.0,
        new UnitDecimals(2, narrow: 1, @long: 3),
        chartStep: 1.5);

    public static readonly PressureUnit Millibar = new(
        "mb",
        fromReference: value => value / 100.0,
        toReference: value => value * 100.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 15.0);

    public static readonly PressureUnit Atmosphere = new(
        "atm",
        fromReference: value => value / 101325.0,
        toReference: value => value * 101325.0,
        new UnitDecimals(2, narrow: 1, @long: 3),
        chartStep: 0.015);

    public static readonly PressureUnit MillimeterOfMercury = new(
        "mmhg",
        fromReference: value => value * (760.0 / 101325.0),
        toReference: value => value * (101325.0 / 760.0),
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 10.0);

    public static readonly PressureUnit InchOfMercury = new(
        "inhg",
        fromReference: value => value * (760.0 / 101325.0) / 25.4,
        toReference: value => value * 25.4 * (101325.0 / 760.0),
        new UnitDecimals(2, narrow: 1, @long: 3),
        chartStep: 0.5);

    public static IReadOnlyList<PressureUnit> All { get; } =
        [Pascal, Hectopascal, Kilopascal, Millibar, Atmosphere, MillimeterOfMercury, InchOfMercury];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;

    private PressureUnit(
        string id,
        Func<double, double> fromReference,
        Func<double, double> toReference,
        UnitDecimals decimals,
        double chartStep)
        : base(id, decimals)
    {
        _fromReference = fromReference;
        _toReference = toReference;
        ChartStep = chartStep;
    }

    public double ChartStep { get; }

    public static PressureUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    /// <summary>
    /// Default unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// Canada uses kilopascals rather than CLDR's hectopascals.
    /// </summary>
    public static PressureUnit GetDefaultUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "BR" or "EG" or "GB" or "IL" or "TH" => Millibar,
            "MX" or "RU" => MillimeterOfMercury,
            "US" => InchOfMercury,
            "CA" => Kilopascal,
            _ => Hectopascal,
        };
    }

    internal static double Convert(double value, PressureUnit sourceUnit, PressureUnit targetUnit)
    {
        if (sourceUnit == Pascal)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == Pascal)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }
}
