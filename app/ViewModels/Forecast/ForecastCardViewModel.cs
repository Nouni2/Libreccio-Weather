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
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed class ForecastCardViewModel
{
    private ForecastCardViewModel(bool daily, string title, string? subtitle, IReadOnlyList<ForecastColumn> columns, int initialIndex)
    {
        IsDaily = daily;
        Title = title;
        Subtitle = subtitle;
        Columns = columns;
        InitialIndex = initialIndex;
    }

    public bool IsDaily { get; }

    public string Title { get; }

    public string? Subtitle { get; }

    public IReadOnlyList<ForecastColumn> Columns { get; }

    public int InitialIndex { get; }

    public double ColumnHeight => IsDaily ? 318 : 206;

    public static ForecastCardViewModel CreateDaily(Core.Models.Weather weather, Location location, TemperatureUnit unit)
    {
        float?[] daytimeTemperatures = BuildTemperatureArray(
            weather.DailyForecast.Select(day => day.Day?.Temperature?.Value?.Value));
        float?[] nighttimeTemperatures = BuildTemperatureArray(
            weather.DailyForecast.Select(day => day.Night?.Temperature?.Value?.Value));
        (float? highest, float? lowest) = GetBounds(weather, location, daytimeTemperatures, nighttimeTemperatures);
        List<ForecastColumn> columns = [];
        for (int position = 0; position < weather.DailyForecast.Count; position++)
        {
            Daily daily = weather.DailyForecast[position];
            DateTimeOffset date = TimeZoneInfo.ConvertTime(daily.Date, location.TimeZone);
            Ratio? dayProbability = daily.Day?.PrecipitationProbability?.Total;
            Ratio? nightProbability = daily.Night?.PrecipitationProbability?.Total;
            Ratio? probability = dayProbability is null ? nightProbability
                : nightProbability is null || dayProbability.Value.Value >= nightProbability.Value.Value
                    ? dayProbability : nightProbability;
            columns.Add(new ForecastColumn(
                position == weather.TodayIndex ? Strings.Get("daily_today_short") : date.ToString("ddd", Strings.Culture),
                DateFormatting.GetFormattedShortDayAndMonth(daily.Date, location.TimeZone),
                WeatherIcons.GetPath(daily.Day?.WeatherCode, true),
                WeatherIcons.GetPath(daily.Night?.WeatherCode, false),
                string.Join(Strings.Get("dot_separator"),
                    new[] { daily.Day?.WeatherText, daily.Night?.WeatherText }.OfType<string>()),
                new TrendChartData(
                    BuildTemperatureArrayForItem(daytimeTemperatures, position),
                    BuildTemperatureArrayForItem(nighttimeTemperatures, position),
                    FormatTemperature(daily.Day?.Temperature?.Value, unit),
                    FormatTemperature(daily.Night?.Temperature?.Value, unit),
                    highest, lowest, probability is { Value: > 0 } p ? (float)p.InPercent : null,
                    FormatProbability(probability), true)));
        }

        return new ForecastCardViewModel(true, Strings.Get("daily_forecast"),
            weather.Current?.DailyForecast, columns, weather.TodayIndex ?? 0);
    }

    public static ForecastCardViewModel CreateHourly(Core.Models.Weather weather, Location location, TemperatureUnit unit)
    {
        IReadOnlyList<Hourly> hours = weather.NextHourlyForecast;
        float?[] temperatures = BuildTemperatureArray(hours.Select(hour => hour.Temperature?.Value?.Value));
        (float? highest, float? lowest) = GetBounds(weather, location, temperatures, []);
        List<ForecastColumn> columns = [];
        for (int position = 0; position < hours.Count; position++)
        {
            Hourly hourly = hours[position];
            DateTimeOffset date = TimeZoneInfo.ConvertTime(hourly.Date, location.TimeZone);
            Ratio? probability = hourly.PrecipitationProbability?.Total;
            columns.Add(new ForecastColumn(
                date.ToString(Strings.Culture.DateTimeFormat.ShortTimePattern.Contains('H') ? "H:mm" : "h tt", Strings.Culture),
                null, WeatherIcons.GetPath(hourly.WeatherCode, hourly.IsDaylight), null, hourly.WeatherText,
                new TrendChartData(
                    BuildTemperatureArrayForItem(temperatures, position), null,
                    FormatTemperature(hourly.Temperature?.Value, unit), null,
                    highest, lowest, probability is { Value: > 0 } p ? (float)p.InPercent : null,
                    FormatProbability(probability), false)));
        }

        return new ForecastCardViewModel(false, Strings.Get("hourly_forecast"),
            weather.Current?.HourlyForecast, columns, 0);
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

    private static (float? Highest, float? Lowest) GetBounds(
        Core.Models.Weather weather, Location location, float?[] daytime, float?[] nighttime)
    {
        Month month = (Month)TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Month;
        weather.Normals.TryGetValue(month, out Normals? normals);
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

    private static string? FormatTemperature(Core.Units.Temperature? temperature, TemperatureUnit unit)
    {
        return temperature is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Narrow, UnitWidth.Narrow) : null;
    }

    private static string? FormatProbability(Ratio? probability)
    {
        return probability is { Value: > 0 } value
            ? string.Format(Strings.Culture, Strings.GetUnit("ratio_percent_nominative_short"),
                NumberFormatting.Format(value.InPercent, 0, Strings.Culture))
            : null;
    }
}

public sealed record ForecastColumn(
    string Label, string? Date, string? DayIcon, string? NightIcon, string? Description, TrendChartData Chart);

public sealed record TrendChartData(
    float?[] HighValues, float?[]? LowValues, string? HighText, string? LowText,
    float? Highest, float? Lowest, float? Histogram, string? HistogramText, bool HasTopPolylineLabel);
