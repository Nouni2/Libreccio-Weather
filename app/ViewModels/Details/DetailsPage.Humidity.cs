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
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using Temperature = LibreccioWeather.Core.Units.Temperature;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildHumidity(DetailsContext context, List<object> blocks)
    {
        TemperatureUnit unit = context.Settings.GetTemperatureUnit(context.RegionCode);
        Daily daily = context.Daily;
        HourlyValues<Ratio> mappedHumidityValues = HourlyValues.Of(context.HourlyList, hourly => hourly.RelativeHumidity);
        HourlyValues<Temperature> mappedDewPointValues = HourlyValues.Of(context.HourlyList, hourly => hourly.DewPoint);
        (DateTimeOffset Time, Ratio Value)? defaultHumidity = context.Current?.RelativeHumidity is { } humidity
            && context.CurrentUpdateTime is { } time ? (time, humidity) : null;
        (DateTimeOffset Time, Temperature Value)? defaultDewPoint = context.Current?.DewPoint is { } dewPoint
            && context.CurrentUpdateTime is { } dewTime ? (dewTime, dewPoint) : null;

        object humiditySummary = daily.RelativeHumidity is { Min: not null, Max: not null } dailyHumidity
            ? new DetailsHeaderItem(new DetailsLabel(DateFormatting.GetFullLabel(daily.Date, context.TimeZone)),
                Text(DailyRangeFormatting.GetRangeSummary(dailyHumidity) ?? ""))
            : HumidityItem(FormatHour(context, defaultHumidity?.Time) ?? "", defaultHumidity?.Value);
        var humidityHeader = new DetailsHeader(humiditySummary);
        blocks.Add(humidityHeader);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedHumidityValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(MakeChart(context, DetailsChartKind.Line, mappedHumidityValues.Keys,
                [[.. mappedHumidityValues.Values.Select(ratio => ratio.InPercent)]],
                [Stops(
                    (100, Rgb(56, 70, 114)),
                    (97, Rgb(56, 98, 157)),
                    (93, Rgb(56, 123, 173)),
                    (90, Rgb(56, 132, 173)),
                    (87, Rgb(56, 135, 173)),
                    (83, Rgb(56, 148, 173)),
                    (80, Rgb(56, 157, 173)),
                    (75, Rgb(56, 160, 173)),
                    (70, Rgb(56, 174, 173)),
                    (60, Rgb(56, 173, 121)),
                    (50, Rgb(105, 173, 56)),
                    (40, Rgb(173, 146, 56)),
                    (30, Rgb(173, 110, 56)),
                    (0, Rgb(173, 85, 56)))],
                null, 100.0, StepValues(0, 100, 20),
                value => UnitFormatting.FormatPercent(Ratio.FromPercent(value)),
                [], x => humidityHeader.Content = x is { } value && mappedHumidityValues.TryGet(value, out Ratio ratio)
                    ? HumidityItem(FormatHour(context, value), ratio)
                    : humiditySummary) with
            {
                TopValues = mappedHumidityValues.Keys,
                TopLabel = x => mappedHumidityValues.TryGet(x, out Ratio ratio) ? UnitFormatting.FormatPercent(ratio, UnitWidth.Narrow) : "-",
            });
        }
        else
        {
            AddUnavailableChart(blocks, mappedHumidityValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "humidity_about", Strings.Get("humidity_about_description"));
        blocks.Add(new DetailsDivider());
        blocks.Add(new DetailsHeadline(Strings.Get("dew_point")));
        blocks.Add(new DetailsSpacer(NormalMargin));

        object dewPointSummary = daily.DewPoint is { Min: not null, Max: not null } dailyDewPoint
            ? new DetailsHeaderItem(new DetailsLabel(DateFormatting.GetFullLabel(daily.Date, context.TimeZone)),
                Text(DailyRangeFormatting.GetRangeSummary(dailyDewPoint, unit) ?? ""),
                DailyRangeFormatting.GetRangeSummary(dailyDewPoint, unit, UnitWidth.Long))
            : DewPointItem(FormatHour(context, defaultDewPoint?.Time) ?? "", defaultDewPoint?.Value, unit);
        var dewPointHeader = new DetailsHeader(dewPointSummary);
        blocks.Add(dewPointHeader);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedDewPointValues.Count >= DetailScreen.ChartMinCount)
        {
            double step = unit.ChartStep;
            double maxY = DetailsChartAxes.RoundUpToNearestMultiplier(mappedDewPointValues.Values.Max().ToDouble(unit), step);
            double minY = DetailsChartAxes.RoundDownToNearestMultiplier(mappedDewPointValues.Values.Min().ToDouble(unit), step);
            blocks.Add(MakeChart(context, DetailsChartKind.Line, mappedDewPointValues.Keys,
                [[.. mappedDewPointValues.Values.Select(value => value.ToDouble(unit))]],
                [TemperatureColors(unit)],
                minY, maxY, StepValues(minY, maxY, step),
                value => UnitFormatting.Format(Temperature.From(value, unit), unit, UnitWidth.Narrow, UnitWidth.Narrow),
                [], x => dewPointHeader.Content = x is { } value && mappedDewPointValues.TryGet(value, out Temperature dew)
                    ? DewPointItem(FormatHour(context, value), dew, unit)
                    : dewPointSummary) with
            {
                TopValues = mappedDewPointValues.Keys,
                TopLabel = x => mappedDewPointValues.TryGet(x, out Temperature dew)
                    ? UnitFormatting.Format(dew, unit, UnitWidth.Narrow, UnitWidth.Narrow) : "-",
            });
        }
        else
        {
            AddUnavailableChart(blocks, mappedDewPointValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "dew_point_about",
            Strings.Format("dew_point_about_description", UnitFormatting.FormatPercent(Ratio.FromPercent(100L), UnitWidth.Narrow)));
    }

    private static DetailsHeaderItem HumidityItem(string label, Ratio? humidity)
    {
        return new DetailsHeaderItem(new DetailsLabel(label),
            Text(humidity is { } value ? UnitFormatting.FormatPercent(value) : ""));
    }

    private static DetailsHeaderItem DewPointItem(string label, Temperature? dewPoint, TemperatureUnit unit)
    {
        return new DetailsHeaderItem(new DetailsLabel(label),
            Text(dewPoint is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Short, UnitWidth.Narrow) : ""),
            dewPoint is { } spoken ? UnitFormatting.Format(spoken, unit, unitWidth: UnitWidth.Long) : null);
    }
}
