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

public sealed class TemperatureUnit : WeatherUnit
{
    public static readonly TemperatureUnit DeciCelsius = new(
        "dc",
        fromReference: value => value,
        toReference: value => value,
        deviationFromReference: value => value,
        deviationToReference: value => value,
        new UnitDecimals(0),
        chartStep: 50.0);

    public static readonly TemperatureUnit Celsius = new(
        "c",
        fromReference: value => value / 10.0,
        toReference: value => value * 10.0,
        deviationFromReference: value => value / 10.0,
        deviationToReference: value => value * 10.0,
        new UnitDecimals(1, narrow: 0, @long: 1),
        chartStep: 5.0);

    public static readonly TemperatureUnit Fahrenheit = new(
        "f",
        fromReference: value => 9.0 / 5.0 * (value / 10.0) + 32,
        toReference: value => 5.0 / 9.0 * (value - 32.0) * 10.0,
        deviationFromReference: value => 9.0 / 5.0 * (value / 10.0),
        deviationToReference: value => 5.0 / 9.0 * value * 10.0,
        new UnitDecimals(0, narrow: 0, @long: 1),
        chartStep: 10.0);

    public static readonly TemperatureUnit Kelvin = new(
        "k",
        fromReference: value => 273.15 + value / 10.0,
        toReference: value => (value - 273.15) * 10.0,
        deviationFromReference: value => value / 10.0,
        deviationToReference: value => value * 10.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 5.0);

    public static IReadOnlyList<TemperatureUnit> All { get; } = [DeciCelsius, Celsius, Fahrenheit, Kelvin];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;
    private readonly Func<double, double> _deviationFromReference;
    private readonly Func<double, double> _deviationToReference;

    private TemperatureUnit(
        string id,
        Func<double, double> fromReference,
        Func<double, double> toReference,
        Func<double, double> deviationFromReference,
        Func<double, double> deviationToReference,
        UnitDecimals decimals,
        double chartStep)
        : base(id, decimals)
    {
        _fromReference = fromReference;
        _toReference = toReference;
        _deviationFromReference = deviationFromReference;
        _deviationToReference = deviationToReference;
        ChartStep = chartStep;
    }

    public double ChartStep { get; }

    public static TemperatureUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    /// <summary>
    /// Default unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// </summary>
    public static TemperatureUnit GetDefaultUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "BS" or "BZ" or "KY" or "PR" or "PW" or "US" => Fahrenheit,
            _ => Celsius,
        };
    }

    internal static double Convert(double value, TemperatureUnit sourceUnit, TemperatureUnit targetUnit)
    {
        if (sourceUnit == DeciCelsius)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == DeciCelsius)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }

    // A deviation has no offset: a difference of 1 °C is 1.8 °F, not 33.8 °F.
    internal static double ConvertDeviation(double value, TemperatureUnit sourceUnit, TemperatureUnit targetUnit)
    {
        if (sourceUnit == DeciCelsius)
        {
            return targetUnit._deviationFromReference(value);
        }

        if (targetUnit == DeciCelsius)
        {
            return sourceUnit._deviationToReference(value);
        }

        return targetUnit._deviationFromReference(sourceUnit._deviationToReference(value));
    }
}
