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
using Precipitation = LibreccioWeather.Core.Units.Precipitation;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildPrecipitation(DetailsContext context, List<object> blocks)
    {
        Daily daily = context.Daily;
        PrecipitationUnit unit = context.Settings.GetPrecipitationUnit(context.RegionCode);
        HourlyValues<Precipitation> mappedQuantityValues = HourlyValues.Of(context.HourlyList, hourly => hourly.Precipitation?.Total);

        object summary = new DetailsHeaderRow(
            daily.Day?.Precipitation?.Total is { } day ? PrecipitationItem(DaytimeLabel(), day, unit) : null,
            daily.Night?.Precipitation?.Total is { } night ? PrecipitationItem(NighttimeLabelWithInfo(), night, unit) : null);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedQuantityValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(PrecipitationChart(context, mappedQuantityValues, unit, x =>
                header.Content = x is { } value && mappedQuantityValues.TryGet(value, out Precipitation precipitation)
                    ? PrecipitationItem(new DetailsLabel(FormatHour(context, value)), precipitation, unit)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedQuantityValues.Count);
        }

        AddPrecipitationDetails(context, blocks, daily.Day?.Precipitation, daily.Night?.Precipitation, unit);
        AddPrecipitationProbability(context, blocks, beforeDivider: 0);

        PrecipitationDuration? dayDuration = daily.Day?.PrecipitationDuration;
        PrecipitationDuration? nightDuration = daily.Night?.PrecipitationDuration;
        if (WholeMinutes(dayDuration?.Total) > 0 || WholeMinutes(nightDuration?.Total) > 0)
        {
            blocks.Add(new DetailsDivider());
            blocks.Add(new DetailsHeadline(Strings.Get("precipitation_duration")));
            blocks.Add(new DetailsSpacer(SmallMargin));
            AddPrecipitationDurationSummary(blocks, dayDuration, nightDuration);
        }
    }

    private static long WholeMinutes(TimeSpan? duration) => duration is { } value ? value.Ticks / TimeSpan.TicksPerMinute : 0;

    private static DetailsHeaderItem PrecipitationItem(DetailsLabel label, Precipitation precipitation, PrecipitationUnit unit)
    {
        return new DetailsHeaderItem(label, UnitsDifferentFontSize(UnitFormatting.Format(precipitation, unit)),
            UnitFormatting.Format(precipitation, unit, unitWidth: UnitWidth.Long));
    }

    private static DetailsChartData PrecipitationChart(
        DetailsContext context, HourlyValues<Precipitation> mappedValues, PrecipitationUnit unit, Action<long?> setActiveX)
    {
        double step = unit.ChartStep;
        double heavy = Core.Models.Precipitation.PrecipitationHourlyHeavy;
        double maxY = DetailsChartAxes.RoundUpToNearestMultiplier(
            Precipitation.FromMillimeters(Math.Max(heavy, mappedValues.Values.Max(value => value.InMillimeters))).ToDouble(unit), step);
        List<ChartAxisLabel> lines =
        [
            new(Precipitation.FromMillimeters(heavy).ToDouble(unit), Strings.Get("precipitation_intensity_heavy")),
        ];
        if (maxY < Precipitation.FromMillimeters(heavy * 2.0).ToDouble(unit))
        {
            lines.Add(new ChartAxisLabel(Precipitation.FromMillimeters(Core.Models.Precipitation.PrecipitationHourlyMedium).ToDouble(unit),
                Strings.Get("precipitation_intensity_medium")));
            lines.Add(new ChartAxisLabel(Precipitation.FromMillimeters(Core.Models.Precipitation.PrecipitationHourlyLight).ToDouble(unit),
                Strings.Get("precipitation_intensity_light")));
        }

        return MakeChart(context, DetailsChartKind.Bar, mappedValues.Keys,
            [[.. mappedValues.Values.Select(value => value.ToDouble(unit))]], [],
            null, maxY, StepValues(0, maxY, step),
            value => UnitFormatting.Format(Precipitation.From(value, unit), unit),
            lines, setActiveX) with
        {
            BarColor = Rgb(60, 116, 160),
        };
    }

    private static void AddPrecipitationDetails(
        DetailsContext context, List<object> blocks, Core.Models.Precipitation? day, Core.Models.Precipitation? night,
        PrecipitationUnit unit)
    {
        List<(string Key, Precipitation? Day, Precipitation? Night)> items = [];
        if ((day?.Rain?.Value ?? 0) > 0 || (night?.Rain?.Value ?? 0) > 0)
        {
            items.Add(("precipitation_rain", day?.Rain, night?.Rain));
        }

        if ((day?.Snow?.Value ?? 0) > 0 || (night?.Snow?.Value ?? 0) > 0)
        {
            items.Add(("precipitation_snow", day?.Snow, night?.Snow));
        }

        if ((day?.Ice?.Value ?? 0) > 0 || (night?.Ice?.Value ?? 0) > 0)
        {
            items.Add(("precipitation_ice", day?.Ice, night?.Ice));
        }

        if ((day?.Thunderstorm?.Value ?? 0) > 0 || (night?.Thunderstorm?.Value ?? 0) > 0)
        {
            items.Add(("precipitation_thunderstorm", day?.Thunderstorm, night?.Thunderstorm));
        }

        if (items.Count == 0)
        {
            return;
        }

        PrecipitationUnit snowfallUnit = context.Settings.GetSnowfallUnit(context.RegionCode);
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsHalfDayColumns(
            [DaytimeLabel(), .. items.Select(item => PrecipitationDetail(item.Key, item.Day, item.Key == "precipitation_snow" ? snowfallUnit : unit))],
            [NighttimeLabelWithInfo(), .. items.Select(item => PrecipitationDetail(item.Key, item.Night, item.Key == "precipitation_snow" ? snowfallUnit : unit))]));
    }

    private static DetailsItem PrecipitationDetail(string key, Precipitation? precipitation, PrecipitationUnit unit)
    {
        return new DetailsItem(Strings.Get(key),
            precipitation is { } value ? UnitFormatting.Format(value, unit) : Strings.Get("null_data_text"),
            AutomationName: precipitation is { } spoken
                ? Strings.Get(key) + Strings.Get("colon_separator") + UnitFormatting.Format(spoken, unit, unitWidth: UnitWidth.Long)
                : null);
    }

    private static void AddPrecipitationDurationSummary(List<object> blocks, PrecipitationDuration? day, PrecipitationDuration? night)
    {
        List<(string Key, TimeSpan? Day, TimeSpan? Night)> items = [];
        if (WholeMinutes(day?.Rain) > 0 || WholeMinutes(night?.Rain) > 0)
        {
            items.Add(("precipitation_rain", day?.Rain, night?.Rain));
        }

        if (WholeMinutes(day?.Snow) > 0 || WholeMinutes(night?.Snow) > 0)
        {
            items.Add(("precipitation_snow", day?.Snow, night?.Snow));
        }

        if (WholeMinutes(day?.Ice) > 0 || WholeMinutes(night?.Ice) > 0)
        {
            items.Add(("precipitation_ice", day?.Ice, night?.Ice));
        }

        if (WholeMinutes(day?.Thunderstorm) > 0 || WholeMinutes(night?.Thunderstorm) > 0)
        {
            items.Add(("precipitation_thunderstorm", day?.Thunderstorm, night?.Thunderstorm));
        }

        blocks.Add(new DetailsHalfDayColumns(
            [DaytimeLabel(), DurationTotal(day?.Total), .. items.Select(item => DurationDetail(item.Key, item.Day))],
            [NighttimeLabelWithInfo(), DurationTotal(night?.Total), .. items.Select(item => DurationDetail(item.Key, item.Night))]));
    }

    private static DetailsLargeValue DurationTotal(TimeSpan? total)
    {
        return total is { } value
            ? new DetailsLargeValue(UnitsDifferentFontSize(DurationFormatting.FormatHours(value)),
                DurationFormatting.FormatTime(value, UnitWidth.Long))
            : new DetailsLargeValue(Text(Strings.Get("null_data_text")));
    }

    private static DetailsItem DurationDetail(string key, TimeSpan? duration)
    {
        return new DetailsItem(Strings.Get(key), duration is { } value ? DurationFormatting.FormatHours(value) : null,
            AutomationName: duration is { } spoken
                ? Strings.Get(key) + Strings.Get("colon_separator") + DurationFormatting.FormatTime(spoken, UnitWidth.Long)
                : null);
    }
}
