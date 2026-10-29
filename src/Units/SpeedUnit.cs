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

public sealed class SpeedUnit : WeatherUnit
{
    // Lower bound of each Beaufort force, in centimeters per second.
    public const int WindSpeed1 = 30;
    public const int WindSpeed2 = 160;
    public const int WindSpeed3 = 340;
    public const int WindSpeed4 = 550;
    public const int WindSpeed5 = 800;
    public const int WindSpeed6 = 1080;
    public const int WindSpeed7 = 1390;
    public const int WindSpeed8 = 1720;
    public const int WindSpeed9 = 2080;
    public const int WindSpeed10 = 2450;
    public const int WindSpeed11 = 2850;
    public const int WindSpeed12 = 3270;

    // Extended forces beyond 12 are not part of the official scale.
    public const int WindSpeed13 = 3700;
    public const int WindSpeed14 = 4150;
    public const int WindSpeed15 = 4620;
    public const int WindSpeed16 = 5100;
    public const int WindSpeed17 = 5610;

    private static readonly int[] BeaufortThresholds =
    [
        WindSpeed1, WindSpeed2, WindSpeed3, WindSpeed4, WindSpeed5, WindSpeed6,
        WindSpeed7, WindSpeed8, WindSpeed9, WindSpeed10, WindSpeed11, WindSpeed12,
    ];

    public static readonly SpeedUnit CentimeterPerSecond = new(
        "cmps",
        fromReference: value => value,
        toReference: value => value,
        new UnitDecimals(0),
        chartStep: 50.0);

    public static readonly SpeedUnit MeterPerSecond = new(
        "mps",
        fromReference: value => value / 100.0,
        toReference: value => value * 100.0,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 0.5);

    public static readonly SpeedUnit KilometerPerHour = new(
        "kph",
        fromReference: value => value * 0.036,
        toReference: value => value / 0.036,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 15.0);

    public static readonly SpeedUnit MilePerHour = new(
        "mph",
        fromReference: value => value / 44.704,
        toReference: value => value * 44.704,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 10.0);

    public static readonly SpeedUnit Knot = new(
        "kn",
        fromReference: value => value * 0.036 / 1.852,
        toReference: value => value * 1.852 / 0.036,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 10.0);

    public static readonly SpeedUnit FootPerSecond = new(
        "ftps",
        fromReference: value => value / 30.48,
        toReference: value => value * 30.48,
        new UnitDecimals(1, narrow: 0, @long: 2),
        chartStep: 15.0);

    /// <summary>
    /// A scale rather than a unit: converting to it and back loses precision.
    /// </summary>
    public static readonly SpeedUnit BeaufortScale = new(
        "bf",
        fromReference: ToBeaufort,
        toReference: FromBeaufort,
        new UnitDecimals(0),
        chartStep: 2.0);

    public static IReadOnlyList<SpeedUnit> All { get; } =
        [CentimeterPerSecond, MeterPerSecond, KilometerPerHour, MilePerHour, Knot, FootPerSecond, BeaufortScale];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;

    private SpeedUnit(
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

    public static SpeedUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    /// <summary>
    /// Default unit for an ISO 3166-1 alpha-2 country code, from the Unicode CLDR unit preferences.
    /// </summary>
    public static SpeedUnit GetDefaultUnit(string? countryCode)
    {
        return countryCode?.ToUpperInvariant() switch
        {
            "CN" or "DK" or "FI" or "JP" or "KR" or "NO" or "NL" or "PL" or "RU" or "SE" => MeterPerSecond,
            "GB" or "US" => MilePerHour,
            _ => KilometerPerHour,
        };
    }

    internal static double Convert(double value, SpeedUnit sourceUnit, SpeedUnit targetUnit)
    {
        if (sourceUnit == CentimeterPerSecond)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == CentimeterPerSecond)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }

    // Negative speeds map to force 0.
    private static double ToBeaufort(double centimetersPerSecond)
    {
        long speed = (long)centimetersPerSecond;
        if (speed < 0)
        {
            return 0.0;
        }

        int force = 0;
        while (force < BeaufortThresholds.Length && speed >= BeaufortThresholds[force])
        {
            force++;
        }

        return force;
    }

    // Each force maps back to the lowest speed of its range. A force that is not a finite number maps to 0.
    private static double FromBeaufort(double force)
    {
        if (!double.IsFinite(force) || force < 1.0)
        {
            return 0.0;
        }

        int index = (int)Math.Min(Math.Floor(force), BeaufortThresholds.Length) - 1;
        return BeaufortThresholds[index];
    }
}
