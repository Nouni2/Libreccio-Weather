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

public sealed class PrecipitationUnit : WeatherUnit
{
    // Micrometers are only displayed for PM2.5 particle sizes, hence the decimals.
    public static readonly PrecipitationUnit Micrometer = new(
        "microm",
        fromReference: value => value,
        toReference: value => value,
        new UnitDecimals(0, @long: 1),
        chartStep: 5000.0);

    public static readonly PrecipitationUnit Millimeter = new(
        "mm",
        fromReference: value => value / 1000.0,
        toReference: value => value * 1000.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 5.0);

    public static readonly PrecipitationUnit Centimeter = new(
        "cm",
        fromReference: value => value / 10000.0,
        toReference: value => value * 10000.0,
        new UnitDecimals(2, narrow: 1, @long: 3),
        chartStep: 0.5);

    public static readonly PrecipitationUnit Inch = new(
        "in",
        fromReference: value => value / 25400.0,
        toReference: value => value * 25400.0,
        new UnitDecimals(2, narrow: 1, @long: 3),
        chartStep: 0.2);

    /// <summary>
    /// The same quantity as millimeters, displayed as liters per square meter.
    /// </summary>
    public static readonly PrecipitationUnit LiterPerSquareMeter = new(
        "lpsqm",
        fromReference: value => value / 1000.0,
        toReference: value => value * 1000.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 5.0);

    public static IReadOnlyList<PrecipitationUnit> All { get; } =
        [Micrometer, Millimeter, Centimeter, Inch, LiterPerSquareMeter];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;

    private PrecipitationUnit(
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

    public static PrecipitationUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    /// <summary>
    /// Default unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// </summary>
    public static PrecipitationUnit GetDefaultUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "BR" => Centimeter,
            "US" => Inch,
            _ => Millimeter,
        };
    }

    /// <summary>
    /// Default snowfall unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// </summary>
    public static PrecipitationUnit GetDefaultSnowfallUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "US" => Inch,
            _ => Centimeter,
        };
    }

    internal static double Convert(double value, PrecipitationUnit sourceUnit, PrecipitationUnit targetUnit)
    {
        if (sourceUnit == Micrometer)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == Micrometer)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }
}
