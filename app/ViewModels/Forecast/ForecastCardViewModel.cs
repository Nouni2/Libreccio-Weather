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

using CommunityToolkit.Mvvm.ComponentModel;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel : ObservableObject
{
    private readonly Action<string?>? _setSelectedTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Columns))]
    private ForecastTrend? _selectedTrend;

    private ForecastCardViewModel(
        bool daily, string title, string? subtitle, IReadOnlyList<ForecastTrend> trends, int initialIndex,
        string? selectedTab, Action<string?> setSelectedTab)
    {
        IsDaily = daily;
        Title = title;
        Subtitle = subtitle;
        Trends = trends;
        InitialIndex = initialIndex;
        ForecastTrend initialTrend = trends.FirstOrDefault(trend => trend.Name == selectedTab) ?? trends[0];
        SelectedTrend = initialTrend;
        _setSelectedTab = setSelectedTab;
        if (selectedTab is not null && initialTrend.Name != selectedTab)
        {
            setSelectedTab(null);
        }
    }

    public bool IsDaily { get; }

    public string Title { get; }

    public string? Subtitle { get; }

    public IReadOnlyList<ForecastTrend> Trends { get; }

    public bool HasMultipleTrends => Trends.Count > 1;

    public IReadOnlyList<ForecastColumn> Columns => SelectedTrend?.Columns ?? [];

    public int InitialIndex { get; }

    public double ColumnHeight => IsDaily ? 318 : 206;

    partial void OnSelectedTrendChanged(ForecastTrend? value)
    {
        if (value is not null)
        {
            _setSelectedTab?.Invoke(value.Name);
        }
    }

    public static ForecastCardViewModel? CreateDaily(
        Core.Models.Weather weather, Location location, TemperatureUnit unit, PrecipitationUnit precipitationUnit, SettingsManager settings,
        string? selectedTab, Action<string?> setSelectedTab)
    {
        List<ForecastTrend> trends = [];
        foreach (DailyTrendDisplay display in settings.DailyTrendDisplayList)
        {
            if (display == DailyTrendDisplay.Temperature)
            {
                trends.Add(CreateDailyTemperature(weather, location, unit, false, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == DailyTrendDisplay.Precipitation && weather.DailyForecast.Any(day =>
                day.Day?.Precipitation?.Total is not null || day.Night?.Precipitation?.Total is not null))
            {
                trends.Add(CreateDailyPrecipitation(weather, location, precipitationUnit));
            }
            else if (display == DailyTrendDisplay.FeelsLike && weather.DailyForecast.Any(day =>
                day.Day?.Temperature?.FeelsLikeTemperature is not null || day.Night?.Temperature?.FeelsLikeTemperature is not null))
            {
                trends.Add(CreateDailyTemperature(weather, location, unit, true, false));
            }
        }

        return trends.Count == 0 ? null : new ForecastCardViewModel(true, Strings.Get("daily_forecast"),
            weather.Current?.DailyForecast, trends, weather.TodayIndex ?? 0, selectedTab, setSelectedTab);
    }

    private static ForecastTrend CreateDailyTemperature(
        Core.Models.Weather weather, Location location, TemperatureUnit unit, bool feelsLike, bool showReferenceLines)
    {
        float?[] daytimeTemperatures = BuildTemperatureArray(
            weather.DailyForecast.Select(day => GetTemperature(day.Day?.Temperature, feelsLike)?.Value));
        float?[] nighttimeTemperatures = BuildTemperatureArray(
            weather.DailyForecast.Select(day => GetTemperature(day.Night?.Temperature, feelsLike)?.Value));
        Normals? normals = feelsLike ? null : GetNormals(weather, location);
        (float? highest, float? lowest) = GetBounds(normals, daytimeTemperatures, nighttimeTemperatures);
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            Ratio? dayProbability = daily.Day?.PrecipitationProbability?.Total;
            Ratio? nightProbability = daily.Night?.PrecipitationProbability?.Total;
            Ratio? probability = dayProbability is null ? nightProbability
                : nightProbability is null || dayProbability.Value.Value >= nightProbability.Value.Value
                    ? dayProbability : nightProbability;
            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : DateFormatting.GetWeek(daily.Date, location.TimeZone),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                WeatherIcons.GetPath(daily.Day?.WeatherCode, true),
                WeatherIcons.GetPath(daily.Night?.WeatherCode, false),
                ForecastDescriptions.DailyTemperature(weather, location, position, unit, feelsLike),
                new TrendChartData(
                    BuildTemperatureArrayForItem(daytimeTemperatures, position),
                    BuildTemperatureArrayForItem(nighttimeTemperatures, position),
                    FormatTemperature(GetTemperature(daily.Day?.Temperature, feelsLike), unit),
                    FormatTemperature(GetTemperature(daily.Night?.Temperature, feelsLike), unit),
                    highest, lowest, probability is { Value: > 0 } p ? (float)p.InPercent : null,
                    FormatProbability(probability), true)));
        }

        return new ForecastTrend(Strings.Get(feelsLike ? "tag_feels_like" : "tag_temperature"),
            columns, showReferenceLines ? BuildReferenceLines(normals, unit) : []);
    }

    public static ForecastCardViewModel? CreateHourly(
        Core.Models.Weather weather, Location location, TemperatureUnit unit, PrecipitationUnit precipitationUnit, SettingsManager settings,
        string? selectedTab, Action<string?> setSelectedTab)
    {
        List<ForecastTrend> trends = [];
        foreach (HourlyTrendDisplay display in settings.HourlyTrendDisplayList)
        {
            if (display == HourlyTrendDisplay.Temperature)
            {
                trends.Add(CreateHourlyTemperature(weather, location, unit, false, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == HourlyTrendDisplay.Precipitation && weather.NextHourlyForecast.Any(hour =>
                hour.Precipitation?.Total is not null))
            {
                trends.Add(CreateHourlyPrecipitation(weather, location, precipitationUnit));
            }
            else if (display == HourlyTrendDisplay.FeelsLike && weather.NextHourlyForecast.Any(hour =>
                hour.Temperature?.FeelsLikeTemperature is not null))
            {
                trends.Add(CreateHourlyTemperature(weather, location, unit, true, false));
            }
            else if (display == HourlyTrendDisplay.Humidity && weather.NextHourlyForecast.Any(hour => hour.DewPoint is not null))
            {
                trends.Add(CreateHourlyHumidity(weather, location, unit));
            }
        }

        return trends.Count == 0 ? null : new ForecastCardViewModel(false, Strings.Get("hourly_forecast"),
            weather.Current?.HourlyForecast, trends, 0, selectedTab, setSelectedTab);
    }

    private static ForecastTrend CreateHourlyTemperature(
        Core.Models.Weather weather, Location location, TemperatureUnit unit, bool feelsLike, bool showReferenceLines)
    {
        IReadOnlyList<Hourly> hours = weather.NextHourlyForecast;
        float?[] temperatures = BuildTemperatureArray(hours.Select(hour => GetTemperature(hour.Temperature, feelsLike)?.Value));
        Normals? normals = feelsLike ? null : GetNormals(weather, location);
        (float? highest, float? lowest) = GetBounds(normals, temperatures, []);
        List<ForecastColumn> columns = [];
        for (int position = 0; position < hours.Count; position++)
        {
            Hourly hourly = hours[position];
            Ratio? probability = hourly.PrecipitationProbability?.Total;
            columns.Add(new ForecastColumn(
                DateFormatting.GetHour(hourly.Date, location.TimeZone),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null,
                ForecastDescriptions.HourlyTemperature(hourly, location, unit, feelsLike),
                new TrendChartData(
                    BuildTemperatureArrayForItem(temperatures, position), null,
                    FormatTemperature(GetTemperature(hourly.Temperature, feelsLike), unit), null,
                    highest, lowest, probability is { Value: > 0 } p ? (float)p.InPercent : null,
                    FormatProbability(probability), false)));
        }

        return new ForecastTrend(Strings.Get(feelsLike ? "tag_feels_like" : "tag_temperature"),
            columns, showReferenceLines ? BuildReferenceLines(normals, unit) : []);
    }

    private static float?[] BuildTemperatureArray(IEnumerable<long?> temperatures)
    {
        List<long?> values = temperatures.ToList();
        float?[] result = new float?[Math.Max(0, values.Count * 2 - 1)];
        for (int i = 0; i < result.Length; i += 2)
        {
            result[i] = values[i / 2] is { } value ? (float)value : null;
        }

        for (int i = 1; i < result.Length; i += 2)
        {
            result[i] = result[i - 1] is { } before && result[i + 1] is { } after
                ? (before + after) * 0.5f : null;
        }

        return result;
    }

    private static float?[] BuildTemperatureArrayForItem(float?[] temperatures, int position)
    {
        int index = position * 2;
        return [index > 0 ? temperatures[index - 1] : null, temperatures[index],
            index + 1 < temperatures.Length ? temperatures[index + 1] : null];
    }

    private static Normals? GetNormals(Core.Models.Weather weather, Location location)
    {
        Month month = (Month)TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Month;
        weather.Normals.TryGetValue(month, out Normals? normals);
        return normals;
    }

    private static (float? Highest, float? Lowest) GetBounds(Normals? normals, float?[] daytime, float?[] nighttime)
    {
        float? highest = normals?.DaytimeTemperature is { } day ? (float)day.Value : null;
        float? lowest = normals?.NighttimeTemperature is { } night ? (float)night.Value : null;
        foreach (float value in daytime.Concat(nighttime).OfType<float>())
        {
            if (highest is null || value > highest)
            {
                highest = value;
            }

            if (lowest is null || value < lowest)
            {
                lowest = value;
            }
        }

        return (highest, lowest);
    }

    private static Core.Units.Temperature? GetTemperature(Core.Models.Temperature? temperature, bool feelsLike)
    {
        return feelsLike ? temperature?.FeelsLikeTemperature ?? temperature?.Value : temperature?.Value;
    }

    private static IReadOnlyList<TrendKeyLine> BuildReferenceLines(Normals? normals, TemperatureUnit unit)
    {
        if (normals?.DaytimeTemperature is not { } day || normals.NighttimeTemperature is not { } night)
        {
            return [];
        }

        string label = Strings.Get("temperature_normal_short");
        return
        [
            new TrendKeyLine((float)day.Value, FormatTemperature(day, unit)!, label, true),
            new TrendKeyLine((float)night.Value, FormatTemperature(night, unit)!, label, false),
        ];
    }

    private static string? FormatTemperature(Core.Units.Temperature? temperature, TemperatureUnit unit)
    {
        return temperature is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Narrow, UnitWidth.Narrow) : null;
    }

    private static string? FormatProbability(Ratio? probability)
    {
        return probability is { Value: > 0 } value
            ? UnitFormatting.FormatPercent(value, UnitWidth.Narrow)
            : null;
    }
}

public sealed record ForecastTrend(
    string Name, IReadOnlyList<ForecastColumn> Columns, IReadOnlyList<TrendKeyLine> KeyLines);

public sealed record TrendKeyLine(float Value, string ContentLeft, string ContentRight, bool AboveLine);

public sealed record ForecastColumn(
    string Label, string? Date, string? DayIcon, string? NightIcon, string? Description, TrendChartData Chart);

public sealed record TrendChartData(
    float?[] HighValues, float?[]? LowValues, string? HighText, string? LowText,
    float? Highest, float? Lowest, float? Histogram, string? HistogramText, bool HasTopPolylineLabel,
    float HighestHistogram = 100, float LowestHistogram = 0, Color? HistogramColor = null,
    bool HistogramUsesTitleColor = false, DoubleHistogramData? DoubleHistogram = null)
{
    public bool IsDoubleHistogram => DoubleHistogram is not null;
}

public sealed record DoubleHistogramData(
    float? HighValue, float? LowValue, string? HighText, string? LowText, float Highest,
    Color HighColor, Color LowColor);
