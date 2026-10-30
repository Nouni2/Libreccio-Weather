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

using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildPressure(DetailsContext context, List<object> blocks)
    {
        PressureUnit unit = context.Settings.GetPressureUnit(context.RegionCode);
        HourlyValues<Pressure> mappedValues = HourlyValues.Of(context.HourlyList, hourly => hourly.Pressure);
        (DateTimeOffset Time, Pressure Value)? defaultValue = context.Current?.Pressure is { } pressure
            && context.CurrentUpdateTime is { } time ? (time, pressure) : null;

        object summary = context.Daily.Pressure?.Average is { } average
            ? PressureItem(Strings.Get("pressure_average"), average, unit)
            : PressureItem(FormatHour(context, defaultValue?.Time), defaultValue?.Value, unit);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(PressureChart(context, mappedValues, unit, x => header.Content =
                x is { } value && mappedValues.TryGet(value, out Pressure hourlyPressure)
                    ? PressureItem(FormatHour(context, value), hourlyPressure, unit)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "pressure_about", Strings.Get("pressure_about_description1"), Strings.Get("pressure_about_description2"));
    }

    private static DetailsHeaderItem PressureItem(string? header, Pressure? pressure, PressureUnit unit)
    {
        return new DetailsHeaderItem(new DetailsLabel(header ?? ""),
            pressure is { } value ? UnitsDifferentFontSize(UnitFormatting.Format(value, unit)) : [],
            pressure is { } spoken ? UnitFormatting.Format(spoken, unit, unitWidth: UnitWidth.Long) : null);
    }

    private static DetailsChartData PressureChart(
        DetailsContext context, HourlyValues<Pressure> mappedValues, PressureUnit unit, Action<long?> setActiveX)
    {
        double chartStep = unit.ChartStep;
        double standard = Pressure.FromPascals((long)PressureUnit.Standard).ToDouble(unit);
        List<Pressure> values = [.. mappedValues.Values];
        double maxY = DetailsChartAxes.RoundUpToNearestMultiplier(
            Math.Max(standard + chartStep * 1.6, values.Max(value => value.ToDouble(unit))), chartStep);
        double minY = DetailsChartAxes.RoundDownToNearestMultiplier(
            Math.Min(standard - chartStep * 1.6, values.Min(value => value.ToDouble(unit))), chartStep);
        System.Windows.Media.Color iconColor = Rgb(0xE6, 0xE0, 0xE9);

        double H(double hectopascals) => Pressure.FromHectopascals(hectopascals).ToDouble(unit);
        return MakeChart(context, DetailsChartKind.Line, mappedValues.Keys,
            [[.. values.Select(value => value.ToDouble(unit))]],
            [Stops(
                (H(1080.0), Rgb(48, 8, 24)),
                (H(1046.0), Rgb(111, 24, 64)),
                (H(1038.0), Rgb(142, 47, 57)),
                (H(1030.0), Rgb(159, 81, 44)),
                (H(1024.0), Rgb(163, 116, 67)),
                (H(1019.0), Rgb(167, 147, 107)),
                (H(1015.25), Rgb(176, 174, 152)),
                (H(1013.25), Rgb(182, 182, 182)),
                (H(1011.25), Rgb(155, 183, 172)),
                (H(1007.0), Rgb(103, 162, 155)),
                (H(1002.0), Rgb(26, 140, 147)),
                (H(995.0), Rgb(0, 117, 146)),
                (H(986.0), Rgb(0, 90, 148)),
                (H(976.0), Rgb(0, 52, 146)),
                (H(950.0), Rgb(0, 32, 96)),
                (H(900.0), Rgb(8, 16, 48)))],
            minY, maxY, StepValues(minY, maxY, chartStep),
            value => UnitFormatting.Format(Pressure.From(value, unit), unit),
            [new ChartAxisLabel(standard, Strings.Get("pressure_standard"))], setActiveX) with
        {
            TopIconValues = mappedValues.Keys,
            TopIcon = x =>
            {
                // The first hour takes the trend of the second, which compares it with the first.
                int currentIndex = mappedValues.Keys.ToList().IndexOf(x);
                if (currentIndex == 0)
                {
                    currentIndex = 1;
                }

                if (currentIndex <= 0 || currentIndex >= values.Count)
                {
                    return null;
                }

                long difference = values[currentIndex].Value - values[currentIndex - 1].Value;
                string trend = difference >= 0.5 ? "IconArrowUpwardAlt" : difference <= -0.5 ? "IconArrowDownwardAlt" : "IconEqual";
                return DetailsIcons.CreateImage(trend, iconColor);
            },
        };
    }
}
