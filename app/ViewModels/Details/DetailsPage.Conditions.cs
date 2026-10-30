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

using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;
using Temperature = LibreccioWeather.Core.Units.Temperature;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static readonly Dictionary<string, ImageSource> WeatherIconCache = [];

    private static void BuildConditions(DetailsContext context, List<object> blocks)
    {
        TemperatureUnit unit = context.Settings.GetTemperatureUnit(context.RegionCode);
        bool showRealTemp = context.Screen != DetailScreen.FeelsLike;
        Daily daily = context.Daily;
        Normals? normals = context.Weather.Normals.TryGetValue(
            (Month)TimeZoneInfo.ConvertTime(daily.Date, context.TimeZone).Month, out Normals? found) ? found : null;
        HourlyValues<Hourly> mappedValues = HourlyValues.OfObjects(context.HourlyList, hourly =>
            hourly.Temperature?.Value is not null && (showRealTemp || hourly.Temperature.FeelsLikeTemperature is not null)
                ? hourly : null);
        bool keepSpaceForSubtext = normals?.DaytimeTemperature is not null || normals?.NighttimeTemperature is not null;
        string monthFormatted = Strings.Culture.DateTimeFormat.MonthGenitiveNames[
            TimeZoneInfo.ConvertTime(daily.Date, context.TimeZone).Month - 1];

        object summary = WeatherConditionSummary(daily, showRealTemp, normals, monthFormatted, unit, keepSpaceForSubtext);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(TemperatureChart(context, mappedValues, showRealTemp, normals, unit, x =>
                header.Content = x is { } value && mappedValues.TryGet(value, out Hourly hourly)
                    ? WeatherConditionItem(new DetailsLabel(FormatHour(context, value)), showRealTemp, hourly.Temperature,
                        hourly.WeatherCode, hourly.WeatherText, hourly.IsDaylight, null, "", keepSpaceForSubtext, unit)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        blocks.Add(new DetailsToggleGroup(
        [
            new DetailsToggle(Text(Strings.Get("temperature_real")), showRealTemp,
                new RelayCommand(() => context.SetScreen(DetailScreen.Conditions))),
            new DetailsToggle(Text(Strings.Get("temperature_feels_like")), !showRealTemp,
                new RelayCommand(() => context.SetScreen(DetailScreen.FeelsLike))),
        ], FillWidth: true));
        blocks.Add(new DetailsBodySmall(Strings.Get(showRealTemp ? "temperature_real_details" : "temperature_feels_like_details")));

        if ((daily.Day?.WeatherSummary is not null && daily.Day.WeatherText != daily.Day.WeatherSummary)
            || (daily.Night?.WeatherSummary is not null && daily.Night.WeatherText != daily.Night.WeatherSummary))
        {
            blocks.Add(new DetailsSpacer(SmallMargin));
            blocks.Add(new DetailsSectionHeader(Text(Strings.Get("daily_summary"))));
            blocks.Add(new DetailsCardText(GetDailySummary(daily)));
        }

        if (daily.DegreeDay is { IsValid: true } degreeDay)
        {
            blocks.Add(new DetailsSpacer(SmallMargin));
            if ((degreeDay.Heating?.Value ?? 0) > 0)
            {
                blocks.Add(DegreeDayItem("temperature_degree_day_heating", "IconModeHeat", degreeDay.Heating!.Value, degreeDay.Heating, unit));
            }
            else if ((degreeDay.Cooling?.Value ?? 0) > 0)
            {
                // The spoken value is the heating degree day, as on Android.
                blocks.Add(DegreeDayItem("temperature_degree_day_cooling", "IconModeCool", degreeDay.Cooling!.Value, degreeDay.Heating, unit));
            }
        }

        AddPrecipitationProbability(context, blocks, beforeDivider: SmallMargin);
    }

    private static DetailsItem DegreeDayItem(
        string key, string iconKey, Temperature degreeDay, Temperature? spoken, TemperatureUnit unit)
    {
        string text = UnitFormatting.Format(Temperature.From(degreeDay.ToDoubleDeviation(unit), unit), unit);
        string? automationName = spoken is { } spokenValue
            ? Strings.Get(key) + Strings.Get("colon_separator")
                + UnitFormatting.Format(Temperature.From(spokenValue.ToDoubleDeviation(unit), unit), unit, unitWidth: UnitWidth.Long)
            : null;
        return new DetailsItem(Strings.Get(key), text, iconKey, Strings.Get(key + "_explanation"), automationName);
    }

    // The same summary for both halves is written once; otherwise each half is labelled.
    private static string GetDailySummary(Daily daily)
    {
        var builder = new StringBuilder();
        if (daily.Day?.WeatherSummary == daily.Night?.WeatherSummary)
        {
            builder.Append(daily.Day?.WeatherSummary);
        }
        else
        {
            if (daily.Day?.WeatherSummary is { } day)
            {
                builder.Append(Strings.Get("daytime")).Append(Strings.Get("colon_separator")).Append(day);
            }

            if (daily.Night?.WeatherSummary is { } night)
            {
                // The line break depends on the night summary, so it also starts a lone night summary.
                if (night.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(Strings.Get("nighttime")).Append(Strings.Get("colon_separator")).Append(night);
            }
        }

        return builder.ToString();
    }

    private static DetailsHeaderRow WeatherConditionSummary(
        Daily daily, bool showRealTemp, Normals? normals, string monthFormatted, TemperatureUnit unit, bool keepSpaceForSubtext)
    {
        return new DetailsHeaderRow(
            daily.Day is { } day
                ? WeatherConditionItem(DaytimeLabel(), showRealTemp, day.Temperature, day.WeatherCode, day.WeatherText, true,
                    normals, monthFormatted, keepSpaceForSubtext, unit)
                : null,
            daily.Night is { } night
                ? WeatherConditionItem(NighttimeLabelWithInfo(), showRealTemp, night.Temperature, night.WeatherCode, night.WeatherText,
                    false, normals, monthFormatted, keepSpaceForSubtext, unit)
                : null);
    }

    private static DetailsHeaderItem WeatherConditionItem(
        DetailsLabel label, bool showRealTemp, Core.Models.Temperature? temperature, WeatherCode? weatherCode, string? weatherText,
        bool isDaytime, Normals? normals, string monthFormatted, bool keepSpaceForSubtext, TemperatureUnit unit)
    {
        Temperature? shown = showRealTemp ? temperature?.Value : temperature?.FeelsLikeTemperature;
        DetailsLabel? subtext = null;
        if (!showRealTemp)
        {
            subtext = new DetailsLabel(temperature?.Value is { } real
                ? Strings.Get("temperature_real") + Strings.Get("colon_separator") + UnitFormatting.Format(real, unit)
                : "");
        }
        else
        {
            Temperature? normal = isDaytime ? normals?.DaytimeTemperature : normals?.NighttimeTemperature;
            if (temperature?.Value is { } halfDay && normal is { } normalValue)
            {
                double departure = halfDay.ToDouble(unit) - normalValue.ToDouble(unit);
                subtext = new DetailsLabel(
                    Strings.Get("temperature_normals_deviation") + Strings.Get("colon_separator")
                        + UnitFormatting.Format(Temperature.From(departure, unit), unit, UnitWidth.Short, UnitWidth.Narrow, showSign: true),
                    Strings.Format(isDaytime
                        ? "temperature_normals_deviation_explanation_maximum"
                        : "temperature_normals_deviation_explanation_minimum", monthFormatted));
            }
            else if (keepSpaceForSubtext)
            {
                // An empty line keeps the day and night temperatures level when only one has a normal.
                subtext = new DetailsLabel("");
            }
        }

        return new DetailsHeaderItem(
            label,
            Text(shown is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Short, UnitWidth.Narrow) : ""),
            shown is { } spoken ? UnitFormatting.Format(spoken, unit, unitWidth: UnitWidth.Long) : null,
            subtext,
            SubtextIsVariant: !showRealTemp,
            WeatherIcon: WeatherIcons.GetPath(weatherCode, isDaytime),
            WeatherText: weatherText ?? "",
            ShowWeather: true);
    }

    private static DetailsChartData TemperatureChart(
        DetailsContext context, HourlyValues<Hourly> mappedValues, bool showRealTemp, Normals? normals, TemperatureUnit unit,
        Action<long?> setActiveX)
    {
        double step = unit.ChartStep;
        List<Hourly> values = [.. mappedValues.Values];
        double minDeci = showRealTemp
            ? values.Min(hourly => hourly.Temperature!.Value!.Value.InDeciCelsius)
            : Math.Min(values.Min(hourly => hourly.Temperature!.Value!.Value.InDeciCelsius),
                values.Min(hourly => hourly.Temperature!.FeelsLikeTemperature!.Value.InDeciCelsius));
        if (normals?.NighttimeTemperature is { } night && night.InDeciCelsius < minDeci)
        {
            minDeci = night.InDeciCelsius;
        }

        double maxDeci = showRealTemp
            ? values.Max(hourly => hourly.Temperature!.Value!.Value.InDeciCelsius)
            : Math.Max(values.Max(hourly => hourly.Temperature!.Value!.Value.InDeciCelsius),
                values.Max(hourly => hourly.Temperature!.FeelsLikeTemperature!.Value.InDeciCelsius));
        if (normals?.DaytimeTemperature is { } day && day.InDeciCelsius > maxDeci)
        {
            maxDeci = day.InDeciCelsius;
        }

        double minY = DetailsChartAxes.RoundDownToNearestMultiplier(Temperature.FromDeciCelsius(minDeci).ToDouble(unit), step);
        double maxY = DetailsChartAxes.RoundUpToNearestMultiplier(Temperature.FromDeciCelsius(maxDeci).ToDouble(unit), step);

        List<IReadOnlyList<double>> series =
        [
            [.. values.Select(hourly => (showRealTemp ? hourly.Temperature!.Value!.Value : hourly.Temperature!.FeelsLikeTemperature!.Value).ToDouble(unit))],
        ];
        if (!showRealTemp)
        {
            series.Add([.. values.Select(hourly => hourly.Temperature!.Value!.Value.ToDouble(unit))]);
        }

        List<ChartAxisLabel> lines = [];
        if (normals?.DaytimeTemperature is { } normalDay)
        {
            lines.Add(new ChartAxisLabel(normalDay.ToDouble(unit), Strings.Get("temperature_normal_short")));
        }

        if (normals?.NighttimeTemperature is { } normalNight)
        {
            lines.Add(new ChartAxisLabel(normalNight.ToDouble(unit), Strings.Get("temperature_normal_short")));
        }

        return MakeChart(context, DetailsChartKind.Line, mappedValues.Keys, series,
            [TemperatureColors(unit), Stops((50, Rgb(128, 128, 128, 160)), (0, Rgb(128, 128, 128, 160)))],
            minY, maxY, StepValues(minY, maxY, step),
            value => UnitFormatting.Format(Temperature.From(value, unit), unit, UnitWidth.Narrow, UnitWidth.Narrow),
            lines, setActiveX) with
        {
            TopIconValues = mappedValues.Keys,
            TopIcon = x => mappedValues.Get(x) is { WeatherCode: { } code } hourly
                ? GetWeatherIcon(WeatherIcons.GetPath(code, hourly.IsDaylight)) : null,
        };
    }

    private static IReadOnlyList<ChartColorStop> TemperatureColors(TemperatureUnit unit)
    {
        double C(int celsius) => Temperature.FromCelsius((long)celsius).ToDouble(unit);
        return Stops(
            (C(47), Rgb(71, 14, 0)),
            (C(30), Rgb(232, 83, 25)),
            (C(21), Rgb(243, 183, 4)),
            (C(10), Rgb(128, 147, 24)),
            (C(1), Rgb(68, 125, 99)),
            (C(0), Rgb(93, 133, 198)),
            (C(-4), Rgb(100, 166, 189)),
            (C(-8), Rgb(106, 191, 181)),
            (C(-15), Rgb(157, 219, 217)),
            (C(-25), Rgb(143, 89, 169)),
            (C(-40), Rgb(162, 70, 145)),
            (C(-55), Rgb(202, 172, 195)),
            (C(-70), Rgb(115, 70, 105)));
    }

    private static ImageSource? GetWeatherIcon(string? path)
    {
        if (path is null)
        {
            return null;
        }

        if (!WeatherIconCache.TryGetValue(path, out ImageSource? image))
        {
            var bitmap = new BitmapImage(new Uri(path));
            bitmap.Freeze();
            image = bitmap;
            WeatherIconCache[path] = image;
        }

        return image;
    }

    /// <summary>
    /// The precipitation probability section, shared by the conditions and precipitation pages.
    /// </summary>
    private static void AddPrecipitationProbability(DetailsContext context, List<object> blocks, double beforeDivider)
    {
        Daily daily = context.Daily;
        HourlyValues<Ratio> mappedProbabilityValues = HourlyValues.Of(context.HourlyList, hourly => hourly.PrecipitationProbability?.Total);
        if (daily.Day?.PrecipitationProbability is null && daily.Night?.PrecipitationProbability is null
            && mappedProbabilityValues.Count == 0)
        {
            return;
        }

        if (beforeDivider > 0)
        {
            blocks.Add(new DetailsSpacer(beforeDivider));
        }

        blocks.Add(new DetailsDivider());
        blocks.Add(new DetailsHeadline(Strings.Get("precipitation_probability")));
        blocks.Add(new DetailsSpacer(SmallMargin));
        object summary = new DetailsHeaderRow(
            PrecipitationProbabilityItem(DaytimeLabel(), daily.Day?.PrecipitationProbability?.Total),
            PrecipitationProbabilityItem(NighttimeLabelWithInfo(), daily.Night?.PrecipitationProbability?.Total));
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedProbabilityValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(MakeChart(context, DetailsChartKind.Line, mappedProbabilityValues.Keys,
                [[.. mappedProbabilityValues.Values.Select(ratio => ratio.InPercent)]],
                [Stops((100, Rgb(60, 116, 160)), (0, Rgb(60, 116, 160)))],
                null, 100.0, StepValues(0, 100, 20),
                value => UnitFormatting.FormatPercent(Ratio.FromPercent(value)),
                [], x => header.Content = x is { } value && mappedProbabilityValues.TryGet(value, out Ratio ratio)
                    ? PrecipitationProbabilityItem(new DetailsLabel(FormatHour(context, value)), ratio)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedProbabilityValues.Count);
        }

        PrecipitationProbability? day = daily.Day?.PrecipitationProbability;
        PrecipitationProbability? night = daily.Night?.PrecipitationProbability;
        List<(string Key, Ratio? Day, Ratio? Night)> items = [];
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

        if (items.Count > 0)
        {
            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsHalfDayColumns(
                [DaytimeLabel(), .. items.Select(item => ProbabilityDetail(item.Key, item.Day))],
                [NighttimeLabelWithInfo(), .. items.Select(item => ProbabilityDetail(item.Key, item.Night))]));
        }
    }

    private static DetailsItem ProbabilityDetail(string key, Ratio? probability)
    {
        return new DetailsItem(Strings.Get(key),
            probability is { } value ? UnitFormatting.FormatPercent(value) : Strings.Get("null_data_text"),
            AutomationName: probability is { } spoken
                ? Strings.Get(key) + Strings.Get("colon_separator") + UnitFormatting.FormatPercent(spoken)
                : null);
    }

    // A missing probability leaves an empty label, so both columns stay aligned.
    private static DetailsHeaderItem PrecipitationProbabilityItem(DetailsLabel label, Ratio? probability)
    {
        return new DetailsHeaderItem(
            probability is not null ? label : new DetailsLabel(""),
            Text(probability is { } value ? UnitFormatting.FormatPercent(value) : ""));
    }
}
