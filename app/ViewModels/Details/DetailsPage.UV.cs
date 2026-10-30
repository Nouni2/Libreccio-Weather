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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static void BuildUV(DetailsContext context, List<object> blocks)
    {
        HourlyValues<UV> mappedValues = HourlyValues.OfObjects(context.HourlyList, hourly => hourly.UV is { IsValid: true } uv ? uv : null);
        (DateTimeOffset Time, UV UV)? defaultValue = context.Current?.UV is { IsValid: true } currentUV
            && context.CurrentUpdateTime is { } time ? (time, currentUV) : null;

        object summary = context.Daily.UV is { IsValid: true } dailyUV
            ? UVItem(Strings.Get("uv_index_maximum_value"), dailyUV)
            : UVItem(FormatHour(context, defaultValue?.Time), defaultValue?.UV);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(UVChart(context, mappedValues, x => header.Content = x is { } value && mappedValues.TryGet(value, out UV uv)
                ? UVItem(FormatHour(context, value), uv) : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "uv_index_about", Strings.Get("uv_index_about_description"));
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsSectionHeader(Text(Strings.Get("uv_index_scale"))));
        blocks.Add(UVScale());
    }

    private static DetailsHeaderItem UVItem(string? header, UV? uv)
    {
        List<DetailsTextRun> value = [];
        if (uv?.Index is { } index)
        {
            value.Add(new DetailsTextRun(NumberFormatting.Format(index, 1, Strings.Culture) + " "));
        }

        if (uv is not null && UVFormatting.GetLevel(uv) is { } level)
        {
            value.Add(new DetailsTextRun(level, DetailsRunStyle.SmallerVariant));
        }

        return new DetailsHeaderItem(new DetailsLabel(header ?? ""), value,
            Dot: uv is not null ? ForecastColors.GetUVColor(uv) : System.Windows.Media.Colors.Transparent);
    }

    private static DetailsChartData UVChart(DetailsContext context, HourlyValues<UV> mappedValues, Action<long?> setActiveX)
    {
        double maxY = Math.Max(UV.UVIndexHigh,
            UnitFormatting.RoundToInt(mappedValues.Values.Select(uv => uv.Index ?? 0.0).DefaultIfEmpty(0.0).Max()) + 1.0);
        return MakeChart(context, DetailsChartKind.Line, mappedValues.Keys,
            [[.. mappedValues.Values.Select(uv => uv.Index ?? 0)]],
            [Stops(
                (19, Rgb(255, 255, 255)),
                (11, Rgb(0x99, 0x00, 0x4C)),
                (10, Rgb(0xE5, 0x2F, 0x35)),
                (7, Rgb(0xFF, 0xA7, 0x26)),
                (5, Rgb(0xFF, 0xCA, 0x28)),
                (2, Rgb(0x72, 0xD5, 0x72)),
                (0, Rgb(110, 110, 110)))],
            null, maxY, StepValues(0, maxY, 1.0),
            value => NumberFormatting.Format(UnitFormatting.RoundToInt(value), 0, Strings.Culture),
            [new ChartAxisLabel(UV.UVIndexMiddle, Strings.Get("uv_alert_level"))], setActiveX) with
        {
            TopValues = mappedValues.Keys,
            TopLabel = x => mappedValues.Get(x)?.Index is { } index
                ? NumberFormatting.Format(UnitFormatting.RoundToInt(index), 0, Strings.Culture) : "-",
        };
    }

    private static DetailsScale UVScale()
    {
        List<IReadOnlyList<DetailsScaleCell>> rows = [];
        IReadOnlyList<int> thresholds = UV.UVThresholds;
        for (int index = 0; index < thresholds.Count; index++)
        {
            int startingValue = thresholds[index];
            string ending = index + 1 < thresholds.Count
                ? " – " + NumberFormatting.Format(thresholds[index + 1] - 1, 0, Strings.Culture)
                : "+";
            var uv = new UV { Index = startingValue };
            rows.Add(
            [
                new DetailsScaleCell(UVFormatting.GetLevel(uv)!, ForecastColors.GetUVColor(uv)),
                new DetailsScaleCell(NumberFormatting.Format(startingValue, 0, Strings.Culture) + ending),
            ]);
        }

        return new DetailsScale([2, 1], [Strings.Get("uv_index_description"), Strings.Get("uv_index")], rows);
    }
}
