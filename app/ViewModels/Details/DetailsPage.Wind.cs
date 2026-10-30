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

using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildWind(DetailsContext context, List<object> blocks)
    {
        SpeedUnit unit = context.Settings.GetSpeedUnit(context.RegionCode);
        HourlyValues<Wind> mappedValues = HourlyValues.OfObjects(context.HourlyList, hourly =>
            hourly.Wind is { Speed: not null } wind ? wind : null);
        (DateTimeOffset Time, Wind Wind)? defaultValue = context.Current?.Wind is { IsValid: true } currentWind
            && context.CurrentUpdateTime is { } time
            ? (time, currentWind) : null;

        var header = new DetailsHeader(WindHeader(context, null, defaultValue, unit));
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            // After a press, the header keeps the current wind rather than going back to the day's summary.
            blocks.Add(WindChart(context, mappedValues, unit, x => header.Content = WindHeader(context,
                x is { } value && mappedValues.TryGet(value, out Wind wind) ? (DateTimeOffset.FromUnixTimeMilliseconds(value), wind)
                    : x is null ? defaultValue : null,
                defaultValue, unit)));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "wind_speed_about", Strings.Get("wind_speed_about_description"));
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsSectionHeader(Text(Strings.Get("wind_strength_scale"))));
        blocks.Add(WindScale(context, unit));
    }

    private static object WindHeader(
        DetailsContext context, (DateTimeOffset Time, Wind Wind)? activeItem, (DateTimeOffset Time, Wind Wind)? defaultValue,
        SpeedUnit unit)
    {
        Daily daily = context.Daily;
        if (activeItem is { } active)
        {
            return WindItem(new DetailsLabel(FormatHour(context, active.Time)!), active.Wind, unit);
        }

        if (daily.Day?.Wind?.IsValid == true || daily.Night?.Wind?.IsValid == true)
        {
            return new DetailsHeaderRow(
                daily.Day?.Wind is { } day ? WindItem(DaytimeLabel(), day, unit) : null,
                daily.Night?.Wind is { } night ? WindItem(NighttimeLabelWithInfo(), night, unit) : null);
        }

        return WindItem(new DetailsLabel(FormatHour(context, defaultValue?.Time) ?? ""), null, unit);
    }

    private static DetailsHeaderItem WindItem(DetailsLabel label, Wind? wind, SpeedUnit unit)
    {
        List<DetailsTextRun> value = [];
        string? valueName = null;
        if (wind?.Speed is { } speed)
        {
            value.AddRange(UnitsDifferentFontSize(UnitFormatting.Format(speed, unit)));
            valueName = UnitFormatting.Format(speed, unit, unitWidth: UnitWidth.Long);
            if (wind.Arrow is { } arrow)
            {
                value.Add(new DetailsTextRun(" " + arrow));
                valueName += Strings.GetUnit("locale_separator") + WindFormatting.GetDirection(wind, shortName: false);
            }
        }

        DetailsLabel subtext = wind?.Gusts is { } gusts && wind.Speed is { } windSpeed && gusts > windSpeed
            ? new DetailsLabel(Strings.Get("wind_gusts_short") + Strings.Get("colon_separator") + UnitFormatting.Format(gusts, unit))
            : new DetailsLabel("");
        return new DetailsHeaderItem(label, value, valueName, subtext, SubtextIsVariant: true);
    }

    private static DetailsChartData WindChart(
        DetailsContext context, HourlyValues<Wind> mappedValues, SpeedUnit unit, Action<long?> setActiveX)
    {
        double step = unit.ChartStep;
        List<Wind> winds = [.. mappedValues.Values];
        double maxY = DetailsChartAxes.RoundUpToNearestMultiplier(Speed.FromMetersPerSecond(Math.Max(15.0, Math.Max(
            winds.Select(wind => wind.Gusts?.InMetersPerSecond ?? 0.0).DefaultIfEmpty(0.0).Max(),
            winds.Max(wind => wind.Speed!.Value.InMetersPerSecond)))).ToDouble(unit), step);

        List<IReadOnlyList<double>> series = [[.. winds.Select(wind => wind.Speed!.Value.ToDouble(unit))]];
        if (winds.Any(wind => wind.Gusts is not null))
        {
            series.Add([.. winds.Select(wind =>
                (wind.Gusts is { } gusts && gusts > wind.Speed!.Value ? gusts : wind.Speed!.Value).ToDouble(unit))]);
        }

        List<ChartAxisLabel> lines = [];
        if (maxY > Speed.FromBeaufort(7L).ToDouble(unit))
        {
            lines.Add(new ChartAxisLabel(Speed.FromBeaufort(7L).ToDouble(unit), WindFormatting.GetBeaufortScaleStrength(Speed.FromBeaufort(7L))!));
        }

        if (maxY < Speed.FromMetersPerSecond(Speed.FromBeaufort(7L).InMetersPerSecond + 5.0).ToDouble(unit))
        {
            lines.Add(new ChartAxisLabel(Speed.FromBeaufort(3L).ToDouble(unit), WindFormatting.GetBeaufortScaleStrength(Speed.FromBeaufort(3L))!));
        }

        System.Windows.Media.Color iconColor = Rgb(0xE6, 0xE0, 0xE9);
        return MakeChart(context, DetailsChartKind.Line, mappedValues.Keys, series,
            [WindColors(unit, 255), WindColors(unit, 160)],
            null, maxY, StepValues(0, maxY, step),
            value => UnitFormatting.Format(Speed.From(value, unit), unit),
            lines, setActiveX) with
        {
            TopIconValues = mappedValues.Keys,
            TopIcon = x => mappedValues.Get(x) is { } wind ? WindIcons.GetDrawableArrow(wind, iconColor) : null,
        };
    }

    private static IReadOnlyList<ChartColorStop> WindColors(SpeedUnit unit, byte alpha)
    {
        double M(double metersPerSecond) => Speed.FromMetersPerSecond(metersPerSecond).ToDouble(unit);
        System.Windows.Media.Color B(int force)
        {
            System.Windows.Media.Color color = ForecastColors.GetBeaufortScaleColor(force);
            return Rgb(color.R, color.G, color.B, alpha);
        }

        return Stops(
            (M(104.0), Rgb(128, 128, 128, alpha)),
            (M(77.0), Rgb(205, 202, 112, alpha)),
            (M(51.0), Rgb(219, 212, 135, alpha)),
            (M(46.0), Rgb(231, 215, 215, alpha)),
            (M(36.0), B(12)),
            (M(30.5), B(11)),
            (M(26.4), B(10)),
            (M(24.475), Rgb(109, 97, 163, alpha)),
            (M(22.55), B(9)),
            (M(18.9), B(8)),
            (M(17.175), Rgb(129, 58, 78, alpha)),
            (M(15.45), B(7)),
            (M(13.85), Rgb(159, 127, 58, alpha)),
            (M(12.25), B(6)),
            (M(9.3), B(5)),
            (M(6.7), B(4)),
            (M(4.4), B(3)),
            (M(2.4), B(2)),
            (M(1.0), B(1)),
            (M(0.0), B(0)));
    }

    private static DetailsScale WindScale(DetailsContext context, SpeedUnit settingsUnit)
    {
        SpeedUnit unit = settingsUnit == SpeedUnit.BeaufortScale ? SpeedUnit.GetDefaultUnit(context.RegionCode) : settingsUnit;
        List<IReadOnlyList<DetailsScaleCell>> rows = [];
        for (int index = 0; index <= 12; index++)
        {
            Speed beaufort = Speed.FromBeaufort((long)index);
            string start = UnitFormatting.FormatValue(beaufort, unit);
            string end = index < 12
                ? " – " + UnitFormatting.FormatValue(Speed.From(Speed.FromBeaufort((long)(index + 1)).ToDouble(unit) - 0.1, unit), unit)
                : "+";
            rows.Add(
            [
                new DetailsScaleCell(NumberFormatting.Format(index, 0, Strings.Culture), ForecastColors.GetBeaufortScaleColor(index)),
                new DetailsScaleCell(WindFormatting.GetBeaufortScaleStrength(beaufort)!),
                new DetailsScaleCell(start + end),
            ]);
        }

        return new DetailsScale([1, 1.5, 1.5],
            [UnitFormatting.GetDisplayName(SpeedUnit.BeaufortScale), Strings.Get("wind_strength_scale_description"),
                UnitFormatting.GetDisplayName(unit)],
            rows);
    }
}
