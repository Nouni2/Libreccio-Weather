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

public sealed class PollutantConcentrationUnit : WeatherUnit
{
    public static readonly PollutantConcentrationUnit MicrogramPerCubicMeter = new(
        "microgpcum",
        fromReference: value => value,
        toReference: value => value,
        new UnitDecimals(0),
        chartStep: 15.0);

    public static readonly PollutantConcentrationUnit MilligramPerCubicMeter = new(
        "mgpcum",
        fromReference: value => value / 1000.0,
        toReference: value => value * 1000.0,
        new UnitDecimals(0, @long: 1),
        chartStep: 15.0);

    public static IReadOnlyList<PollutantConcentrationUnit> All { get; } =
        [MicrogramPerCubicMeter, MilligramPerCubicMeter];

    private readonly Func<double, double> _fromReference;
    private readonly Func<double, double> _toReference;

    private PollutantConcentrationUnit(
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

    public static PollutantConcentrationUnit? GetUnit(string id)
    {
        return All.FirstOrDefault(unit => unit.Id == id);
    }

    internal static double Convert(
        double value,
        PollutantConcentrationUnit sourceUnit,
        PollutantConcentrationUnit targetUnit)
    {
        if (sourceUnit == MicrogramPerCubicMeter)
        {
            return targetUnit._fromReference(value);
        }

        if (targetUnit == MicrogramPerCubicMeter)
        {
            return sourceUnit._toReference(value);
        }

        return targetUnit._fromReference(sourceUnit._toReference(value));
    }
}
