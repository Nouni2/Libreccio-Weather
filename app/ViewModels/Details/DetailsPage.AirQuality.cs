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

using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    private static readonly PollutantIndex[] PrimaryDetailPollutants =
        [PollutantIndex.PM25, PollutantIndex.PM10, PollutantIndex.O3, PollutantIndex.NO2];

    private static readonly PollutantIndex[] SecondaryDetailPollutants = [PollutantIndex.SO2, PollutantIndex.CO];

    private static void BuildAirQuality(DetailsContext context, List<object> blocks)
    {
        Weather weather = context.Weather;
        Daily daily = context.Daily;
        PollutantIndex? selectedPollutant = context.SelectedPollutant;
        List<PollutantIndex> supportedPollutants = [.. PollutantIndex.Entries.Where(pollutant =>
            weather.DailyForecast.Any(day => day.AirQuality is { } airQuality && pollutant.GetConcentration(airQuality) is not null)
            || weather.HourlyForecast.Any(hour => hour.AirQuality is { } airQuality && pollutant.GetConcentration(airQuality) is not null)
            || (weather.Current?.AirQuality is { } current && pollutant.GetConcentration(current) is not null))];

        HourlyValues<AirQuality> mappedValues = HourlyValues.OfObjects(context.HourlyList, hourly =>
            hourly.AirQuality is { } airQuality
            && (selectedPollutant is not null ? selectedPollutant.GetConcentration(airQuality) is not null : airQuality.IsIndexValid)
                ? airQuality : null);
        (DateTimeOffset Time, AirQuality Value)? defaultValue = context.Current?.AirQuality is { IsValid: true } currentAirQuality
            && context.CurrentUpdateTime is { } time ? (time, currentAirQuality) : null;
        AirQuality? selectedAirQuality = daily.AirQuality is { IsValid: true } dailyAirQuality ? dailyAirQuality : defaultValue?.Value;
        List<DetailsPollutantItem> detailedPollutants = GetDetailedPollutants(selectedAirQuality, out PollutantIndex? primaryPollutant);

        object summary = daily.AirQuality is { IsValid: true } average
            ? AirQualityItem(Strings.Get("air_quality_average"), selectedPollutant, average)
            : AirQualityItem(FormatHour(context, defaultValue?.Time), selectedPollutant, defaultValue?.Value);
        var header = new DetailsHeader(summary);
        blocks.Add(header);
        blocks.Add(new DetailsSpacer(NormalMargin));
        if (mappedValues.Count >= DetailScreen.ChartMinCount)
        {
            blocks.Add(AirQualityChart(context, selectedPollutant, mappedValues, x => header.Content =
                x is { } value && mappedValues.TryGet(value, out AirQuality airQuality)
                    ? AirQualityItem(FormatHour(context, value), selectedPollutant, airQuality)
                    : summary));
        }
        else
        {
            AddUnavailableChart(blocks, mappedValues.Count);
        }

        if (supportedPollutants.Count > 0)
        {
            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsToggleGroup(
            [
                new DetailsToggle(Text(Strings.Get("air_quality_index_short")), selectedPollutant is null,
                    new RelayCommand(() => context.SetPollutant(null))),
                .. supportedPollutants.Select(pollutant => new DetailsToggle(FormatPollutantName(pollutant.ShortName),
                    selectedPollutant == pollutant, new RelayCommand(() => context.SetPollutant(pollutant)))),
            ], FillWidth: false));
        }

        if (selectedPollutant is { } pollutant)
        {
            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsSectionHeader(FormatPollutantName(Strings.Format(pollutant.AboutPollutantKey, pollutant.ShortName))));
            blocks.Add(new DetailsCardText(Strings.Get(pollutant.SourcesKey)));
        }
        else if (selectedAirQuality is { } airQuality)
        {
            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsSectionHeader(Text(Strings.Get("air_quality_health_information"))));
            blocks.Add(new DetailsCardText(AirQualityIndex.GetDescription(airQuality) ?? ""));
            if (primaryPollutant is { } primary)
            {
                blocks.Add(new DetailsSpacer(NormalMargin));
                blocks.Add(new DetailsSectionHeader(Text(Strings.Get("air_quality_pollutant_primary")),
                    primary != PollutantIndex.PM10 && primary != PollutantIndex.PM25
                        ? FormatPollutantName(primary.GetFullName())
                        : Text(primary.GetFullName())));
                blocks.Add(new DetailsCardText(Strings.Get(primary.SourcesKey)));
            }

            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsSectionHeader(Text(Strings.Get("air_quality_pollutant_details"))));
            blocks.Add(new DetailsPollutantGrid(detailedPollutants));
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "air_quality_index_about",
            Strings.Get("air_quality_index_about_description_1"), Strings.Get("air_quality_index_about_description_2"));
        blocks.Add(new DetailsSpacer(NormalMargin));
        blocks.Add(new DetailsSectionHeader(selectedPollutant is not null
            ? FormatPollutantName(Strings.Format(selectedPollutant.AboutIndexKey, selectedPollutant.ShortName))
            : Text(Strings.Get("air_quality_index_scale"))));
        blocks.Add(AirQualityScale(selectedPollutant));
    }

    // The bars use each pollutant's index rather than its concentration, which gives a more realistic length.
    private static List<DetailsPollutantItem> GetDetailedPollutants(AirQuality? airQuality, out PollutantIndex? primaryPollutant)
    {
        List<(PollutantIndex Pollutant, DetailsPollutantItem Item, int Progress)> items = [];
        if (airQuality is not null)
        {
            foreach (PollutantIndex pollutant in PrimaryDetailPollutants)
            {
                if (pollutant.GetConcentration(airQuality) is { } concentration)
                {
                    items.Add(PollutantItem(airQuality, pollutant, concentration));
                }
            }

            foreach (PollutantIndex pollutant in SecondaryDetailPollutants)
            {
                double concentration = pollutant.GetConcentration(airQuality) ?? 0.0;
                if (concentration > 0)
                {
                    items.Add(PollutantItem(airQuality, pollutant, concentration));
                }
            }
        }

        primaryPollutant = items.Count > 0 ? items.MaxBy(item => item.Progress).Pollutant : null;
        return [.. items.Select(item => item.Item)];
    }

    private static (PollutantIndex, DetailsPollutantItem, int) PollutantItem(AirQuality airQuality, PollutantIndex pollutant, double concentration)
    {
        int index = AirQualityIndex.GetIndex(airQuality, pollutant)!.Value;
        double progress = Math.Min((float)index / AirQualityIndex.IndexExcessivePollution, 1.0f);
        return (pollutant, new DetailsPollutantItem(
            FormatPollutantName(pollutant.ShortName),
            ForecastColors.GetAirQualityColor(index),
            progress,
            UnitFormatting.Format(concentration, pollutant.Unit),
            pollutant.VoicedName + Strings.Get("colon_separator") + UnitFormatting.Format(concentration, pollutant.Unit, unitWidth: UnitWidth.Long)),
            index);
    }

    private static DetailsHeaderItem AirQualityItem(string? header, PollutantIndex? selectedPollutant, AirQuality? airQuality)
    {
        List<DetailsTextRun> value = [];
        if (selectedPollutant is null)
        {
            if (airQuality is not null && AirQualityIndex.GetIndex(airQuality) is { } index)
            {
                value.Add(new DetailsTextRun(NumberFormatting.Format(index, 0, Strings.Culture) + " "));
            }
        }
        else if (airQuality is not null && selectedPollutant.GetConcentration(airQuality) is { } concentration)
        {
            value.AddRange(UnitsDifferentFontSize(UnitFormatting.Format(concentration, selectedPollutant.Unit)));
            value.Add(new DetailsTextRun(" "));
        }

        if (airQuality is not null && AirQualityIndex.GetName(airQuality, selectedPollutant) is { } name)
        {
            value.Add(new DetailsTextRun(name, DetailsRunStyle.SmallerVariant));
        }

        return new DetailsHeaderItem(new DetailsLabel(header ?? ""), value,
            Dot: airQuality is not null
                ? ForecastColors.GetAirQualityColor(AirQualityIndex.GetIndex(airQuality, selectedPollutant))
                : System.Windows.Media.Colors.Transparent);
    }

    private static DetailsChartData AirQualityChart(
        DetailsContext context, PollutantIndex? selectedPollutant, HourlyValues<AirQuality> mappedValues, Action<long?> setActiveX)
    {
        List<AirQuality> values = [.. mappedValues.Values];
        int maxY = Math.Max(selectedPollutant?.MaxY ?? AirQualityIndex.AqiThresholds[4], values.Max(airQuality =>
            selectedPollutant is null
                ? AirQualityIndex.GetIndex(airQuality)!.Value
                : selectedPollutant.GetConcentration(airQuality) is { } concentration
                    ? UnitFormatting.RoundToInt(DetailsChartAxes.RoundUpToNearestMultiplier(concentration, 1.0))
                    : 0));
        IReadOnlyList<int> thresholds = selectedPollutant?.Thresholds ?? AirQualityIndex.AqiThresholds;

        // Each level starts with the color of its index.
        List<ChartColorStop> colors = [.. thresholds.Select((threshold, level) =>
            new ChartColorStop(threshold, ForecastColors.GetAirQualityColor(AirQualityIndex.AqiThresholds[level])))];

        List<double> endAxisValues = [.. thresholds.Select(threshold => (double)threshold)];
        if (maxY > endAxisValues[^1])
        {
            endAxisValues.Add(maxY);
        }

        return MakeChart(context, DetailsChartKind.Line, mappedValues.Keys,
            [[.. values.Select(airQuality => selectedPollutant is null
                ? AirQualityIndex.GetIndex(airQuality)!.Value
                : selectedPollutant.GetConcentration(airQuality)!.Value)]],
            [colors],
            null, maxY, endAxisValues,
            value => selectedPollutant is null
                ? NumberFormatting.Format(UnitFormatting.RoundToInt(value), 0, Strings.Culture)
                : UnitFormatting.Format(value, selectedPollutant.Unit),
            [new ChartAxisLabel(thresholds[3], Strings.GetArray("air_quality_levels")[3])], setActiveX) with
        {
            TopValues = mappedValues.Keys,
            TopLabel = x => mappedValues.Get(x) is { } airQuality
                ? NumberFormatting.Format(selectedPollutant is null
                    ? AirQualityIndex.GetIndex(airQuality)!.Value
                    : UnitFormatting.RoundToInt(selectedPollutant.GetConcentration(airQuality)!.Value), 0, Strings.Culture)
                : "-",
        };
    }

    private static DetailsScale AirQualityScale(PollutantIndex? selectedPollutant)
    {
        IReadOnlyList<int> thresholds = selectedPollutant?.Thresholds ?? AirQualityIndex.AqiThresholds;
        List<IReadOnlyList<DetailsScaleCell>> rows = [];
        for (int index = 0; index < thresholds.Count; index++)
        {
            int startingValue = thresholds[index];
            int? aqi = selectedPollutant is null ? startingValue : selectedPollutant.GetIndex(startingValue);
            string ending = index + 1 < thresholds.Count
                ? " – " + NumberFormatting.Format(thresholds[index + 1] - 1, 0, Strings.Culture)
                : "+";
            Color color = ForecastColors.GetAirQualityColor(aqi);
            rows.Add(
            [
                new DetailsScaleCell(AirQualityIndex.GetAqiToName(aqi)!, color),
                new DetailsScaleCell(AirQualityIndex.GetAqiToHarmlessExposure(aqi)!),
                new DetailsScaleCell(NumberFormatting.Format(startingValue, 0, Strings.Culture) + ending),
            ]);
        }

        return new DetailsScale([2, 1.5, 1.5],
        [
            Strings.Get("air_quality_index_description"),
            Strings.Get("air_quality_index_harmless_exposure"),
            selectedPollutant is not null ? UnitFormatting.GetDisplayName(selectedPollutant.Unit) : Strings.Get("air_quality_index_short"),
        ], rows);
    }
}
