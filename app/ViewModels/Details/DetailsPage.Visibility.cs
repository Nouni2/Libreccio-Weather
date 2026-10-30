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

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    // Once rounded, the chart reaches at least 75,000 ft.
    private const double VisibilityMinChartMaximum = 22850.0;

    private static void BuildVisibility(DetailsContext context, List<object> blocks)
    {
        Daily daily = context.Daily;
        DistanceUnit unit = context.Settings.GetDistanceUnit(context.RegionCode);
        HourlyValues<Distance> mappedValues = HourlyValues.Of(context.HourlyList, hourly => hourly.Visibility);
        (DateTimeOffset Time, Distance Value)? defaultValue = context.Current?.Visibility is { } visibility
            && context.CurrentUpdateTime is { } time ? (time, visibility) : null;

        object summary = daily.Visibility is { Min: not null, Max: not null } dailyVisibility
            ? new DetailsHeaderItem(new DetailsLabel(DateFormatting.GetFullLabel(daily.Date, context.TimeZone)),
                DailyRangeFormatting.GetRangeSummary(dailyVisibility, unit) is { } range ? UnitsDifferentFontSize(range) : [],
                DailyRangeFormatting.GetRangeSummary(dailyVisibility, unit, UnitWidth.Long),
                Description: new DetailsLabel(DailyRangeFormatting.GetRangeDescriptionSummary(dailyVisibility) ?? ""))
            : VisibilityItem(FormatHour(context, defaultValue?.Time) ?? "", defaultValue?.Value, unit);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            double maxY = Distance.FromMeters(Math.Max(VisibilityMinChartMaximum, mappedValues.Values.Max(value => value.InMeters)))
                .ToDouble(unit);
            double step = unit.GetChartStep(maxY);
            double maxYRounded = DetailsChartAxes.RoundUpToNearestMultiplier(maxY, step);
            double M(double meters) => Distance.FromMeters(meters).ToDouble(unit);
            blocks.Add(MakeChart(context, DetailsChartKind.Line, mappedValues.Keys,
                [[.. mappedValues.Values.Select(value => value.ToDouble(unit))]],
                [Stops(
                    (M(20000), Rgb(119, 141, 120)),
                    (M(15000), Rgb(91, 167, 99)),
                    (M(9000), Rgb(90, 169, 90)),
                    (M(8000), Rgb(98, 122, 160)),
                    (M(6000), Rgb(98, 122, 160)),
                    (M(5000), Rgb(167, 91, 91)),
                    (M(2200), Rgb(167, 91, 91)),
                    (M(1600), Rgb(162, 97, 160)),
                    (M(0), Rgb(166, 93, 165)))],
                null, maxYRounded, StepValues(0, maxYRounded, step),
                value => UnitFormatting.Format(Distance.From(value, unit), unit),
                [], x => header.Content = x is { } value && mappedValues.TryGet(value, out Distance distance)
                    ? VisibilityItem(FormatHour(context, value), distance, unit)
                    : summary) with
            {
                TopValues = mappedValues.Keys,
                TopLabel = x => mappedValues.TryGet(x, out Distance distance) ? UnitFormatting.FormatValue(distance, unit) : "-",
            });
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "visibility_about", Strings.Get("visibility_about_description"));
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsSectionHeader(Text(Strings.Get("visibility_scale"))));
        blocks.Add(VisibilityScale(unit));
    }

    private static DetailsHeaderItem VisibilityItem(string label, Distance? visibility, DistanceUnit unit)
    {
        return new DetailsHeaderItem(new DetailsLabel(label),
            visibility is { } value ? UnitsDifferentFontSize(UnitFormatting.Format(value, unit)) : [],
            visibility is { } spoken ? UnitFormatting.Format(spoken, unit, unitWidth: UnitWidth.Long) : null,
            Description: new DetailsLabel(visibility is { } described ? VisibilityFormatting.GetDescription(described) ?? "" : ""));
    }

    private static DetailsScale VisibilityScale(DistanceUnit unit)
    {
        IReadOnlyList<Distance> thresholds = VisibilityFormatting.ScaleThresholds;
        int decimals = unit.Decimals.Short;
        List<IReadOnlyList<DetailsScaleCell>> rows = [];
        for (int index = 0; index < thresholds.Count; index++)
        {
            Distance startingValue = thresholds[index];
            string starting = NumberFormatting.Format(startingValue.ToDouble(unit), decimals, Strings.Culture);
            string ending = index + 1 < thresholds.Count
                ? " – " + NumberFormatting.Format(
                    thresholds[index + 1].ToDouble(unit) - (decimals == 0 ? 1.0 : 0.1), decimals, Strings.Culture)
                : "+";
            // Just above the threshold, so the description is the category that starts there.
            Distance described = Distance.From(startingValue.ToDouble(unit) + 0.1, unit);
            rows.Add(
            [
                new DetailsScaleCell(VisibilityFormatting.GetDescription(described)!),
                new DetailsScaleCell(starting + ending),
            ]);
        }

        return new DetailsScale([1.5, 1],
            [Strings.Get("wind_strength_scale_description"), UnitFormatting.GetDisplayName(unit)], rows);
    }
}
