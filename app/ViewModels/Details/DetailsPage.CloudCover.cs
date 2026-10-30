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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildCloudCover(DetailsContext context, List<object> blocks)
    {
        Daily daily = context.Daily;
        HourlyValues<Ratio> mappedValues = HourlyValues.Of(context.HourlyList, hourly => hourly.CloudCover);
        (DateTimeOffset Time, Ratio Value)? defaultValue = context.Current?.CloudCover is { } cloudCover
            && context.CurrentUpdateTime is { } time ? (time, cloudCover) : null;

        object summary = daily.CloudCover is { Min: not null, Max: not null } dailyCloudCover
            ? new DetailsHeaderItem(new DetailsLabel(DateFormatting.GetFullLabel(daily.Date, context.TimeZone)),
                Text(DailyRangeFormatting.GetRangeSummary(dailyCloudCover) ?? ""),
                Description: new DetailsLabel(DailyRangeFormatting.GetRangeDescriptionSummary(dailyCloudCover) ?? ""))
            : CloudCoverItem(FormatHour(context, defaultValue?.Time), defaultValue?.Value);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(MakeChart(context, DetailsChartKind.Line, mappedValues.Keys,
                [[.. mappedValues.Values.Select(ratio => ratio.InPercent)]],
                [Stops(
                    (100, Rgb(213, 213, 205)),
                    (98, Rgb(198, 201, 201)),
                    (95, Rgb(171, 180, 179)),
                    (50, Rgb(116, 116, 116)),
                    (10, Rgb(132, 119, 70)),
                    (0, Rgb(146, 130, 70)))],
                null, 100.0, StepValues(0, 100, 20),
                value => UnitFormatting.FormatPercent(Ratio.FromPercent(value)),
                [], x => header.Content = x is { } value && mappedValues.TryGet(value, out Ratio ratio)
                    ? CloudCoverItem(FormatHour(context, value), ratio)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        if (daily.SunshineDuration is { } sunshineDuration)
        {
            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsItem(Strings.Get("sunshine_duration"), DurationFormatting.FormatTime(sunshineDuration),
                "IconSunshineDuration",
                AutomationName: Strings.Get("sunshine_duration") + Strings.Get("colon_separator")
                    + DurationFormatting.FormatTime(sunshineDuration, UnitWidth.Long)));
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        if (daily.CloudCover?.Summary is { Length: > 0 } dailySummary)
        {
            blocks.Add(new DetailsSectionHeader(Text(Strings.Get("daily_summary"))));
            blocks.Add(new DetailsCardText(dailySummary));
        }

        AddAbout(blocks, "cloud_cover_about", Strings.Get("cloud_cover_about_description"));
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsSectionHeader(Text(Strings.Get("cloud_cover_scale"))));
        blocks.Add(CloudCoverScale());
    }

    private static DetailsHeaderItem CloudCoverItem(string? header, Ratio? cloudCover)
    {
        return new DetailsHeaderItem(new DetailsLabel(header ?? ""),
            Text(cloudCover is { } value ? UnitFormatting.FormatPercent(value) : ""),
            Description: new DetailsLabel(cloudCover is { } described ? CloudCoverFormatting.GetDescription(described) ?? "" : ""));
    }

    private static DetailsScale CloudCoverScale()
    {
        IReadOnlyList<double> thresholds = CloudCoverFormatting.ScaleThresholds;
        List<IReadOnlyList<DetailsScaleCell>> rows = [];
        for (int index = 0; index < thresholds.Count - 1; index++)
        {
            Ratio startingValue = Ratio.FromPercent(thresholds[index]);
            string ending = " – " + UnitFormatting.FormatValue(Ratio.FromPercent(thresholds[index + 1]));
            rows.Add(
            [
                new DetailsScaleCell(CloudCoverFormatting.GetDescription(startingValue)!),
                new DetailsScaleCell(UnitFormatting.FormatValue(startingValue) + ending),
            ]);
        }

        return new DetailsScale([1.5, 1], [Strings.Get("wind_strength_scale_description"), "%"], rows);
    }
}
