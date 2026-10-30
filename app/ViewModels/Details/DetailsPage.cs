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

using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Details;

/// <summary>
/// What a page of the detail screen is built from: the selected day, its hours and the current conditions when the day
/// is today.
/// </summary>
internal sealed class DetailsContext
{
    public DetailsContext(
        Location location, Weather weather, Daily daily, SettingsManager settings, string regionCode,
        DetailScreen screen, PollutantIndex? selectedPollutant,
        Action<DetailScreen> setScreen, Action<PollutantIndex?> setPollutant)
    {
        Location = location;
        Weather = weather;
        Daily = daily;
        Settings = settings;
        RegionCode = regionCode;
        Screen = screen;
        SelectedPollutant = selectedPollutant;
        SetScreen = setScreen;
        SetPollutant = setPollutant;
        HourlyList = GetHourlyList(location, weather, daily);
        CurrentUpdateTime = weather.Base.CurrentUpdateTime ?? weather.Base.ForecastUpdateTime ?? weather.Base.RefreshTime;
        Current = IsToday(location, daily) && CurrentUpdateTime > daily.Date ? weather.Current : null;
        DayStart = DetailsChartAxes.GetHourOfDay(daily.Date, location.TimeZone, 0);
        DayEnd = DetailsChartAxes.GetHourOfDay(daily.Date, location.TimeZone, 0, dayOffset: 1);
    }

    public Location Location { get; }

    public Weather Weather { get; }

    public Daily Daily { get; }

    public SettingsManager Settings { get; }

    public string RegionCode { get; }

    public DetailScreen Screen { get; }

    public PollutantIndex? SelectedPollutant { get; }

    public Action<DetailScreen> SetScreen { get; }

    public Action<PollutantIndex?> SetPollutant { get; }

    public IReadOnlyList<Hourly> HourlyList { get; }

    public DateTimeOffset? CurrentUpdateTime { get; }

    public Current? Current { get; }

    public DateTimeOffset DayStart { get; }

    public DateTimeOffset DayEnd { get; }

    public TimeZoneInfo TimeZone => Location.TimeZone;

    // The hours of the day, plus the hour before when the day does not start on an hour, and the first hour of the next
    // day, so a forecast every three or six hours still spans the whole day.
    private static IReadOnlyList<Hourly> GetHourlyList(Location location, Weather weather, Daily daily)
    {
        DateTimeOffset startingDate = DetailsChartAxes.GetHourOfDay(daily.Date, location.TimeZone, 0);
        DateTimeOffset endingDate = DetailsChartAxes.GetHourOfDay(daily.Date, location.TimeZone, 0, dayOffset: 1);
        IReadOnlyList<Hourly> hours = weather.HourlyForecast;

        int firstHourlyIndex = IndexOfFirst(hours, hourly => hourly.Date >= startingDate);
        if (firstHourlyIndex > 0 && hours[firstHourlyIndex].Date > startingDate)
        {
            firstHourlyIndex--;
        }

        if (firstHourlyIndex == -1)
        {
            return [];
        }

        int lastHourlyIndex = IndexOfFirst(hours, hourly => hourly.Date >= endingDate);
        if (lastHourlyIndex == -1)
        {
            lastHourlyIndex = hours.Count - 1;
        }

        return [.. hours.Skip(firstHourlyIndex).Take(lastHourlyIndex - firstHourlyIndex + 1)];
    }

    private static int IndexOfFirst(IReadOnlyList<Hourly> hours, Func<Hourly, bool> predicate)
    {
        for (int i = 0; i < hours.Count; i++)
        {
            if (predicate(hours[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsToday(Location location, Daily daily)
    {
        DateTime current = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).DateTime;
        DateTime thisDay = TimeZoneInfo.ConvertTime(daily.Date, location.TimeZone).DateTime;
        return current.Year == thisDay.Year && current.DayOfYear == thisDay.DayOfYear;
    }
}

/// <summary>
/// The values of a chart in hour order, one for each hour. A later value for the same hour replaces an earlier one in
/// its place.
/// </summary>
internal static class HourlyValues
{
    public static HourlyValues<T> Of<T>(IEnumerable<Hourly> hours, Func<Hourly, T?> selector) where T : struct
    {
        return new HourlyValues<T>(hours.Select(hourly => (hourly, selector(hourly)))
            .Where(item => item.Item2.HasValue)
            .Select(item => (item.hourly.Date.ToUnixTimeMilliseconds(), item.Item2!.Value)));
    }

    public static HourlyValues<T> OfObjects<T>(IEnumerable<Hourly> hours, Func<Hourly, T?> selector) where T : class
    {
        return new HourlyValues<T>(hours.Select(hourly => (hourly, selector(hourly)))
            .Where(item => item.Item2 is not null)
            .Select(item => (item.hourly.Date.ToUnixTimeMilliseconds(), item.Item2!)));
    }
}

internal sealed class HourlyValues<T> where T : notnull
{
    private readonly List<long> _keys = [];
    private readonly Dictionary<long, T> _values = [];

    public HourlyValues(IEnumerable<(long X, T Value)> values)
    {
        foreach ((long x, T value) in values)
        {
            if (!_values.ContainsKey(x))
            {
                _keys.Add(x);
            }

            _values[x] = value;
        }
    }

    public int Count => _keys.Count;

    public IReadOnlyList<long> Keys => _keys;

    public IEnumerable<T> Values => _keys.Select(key => _values[key]);

    public T? Get(long x) => _values.TryGetValue(x, out T? value) ? value : default;

    public bool TryGet(long x, out T value) => _values.TryGetValue(x, out value!);
}

internal static partial class DetailsPage
{
    private const double NormalMargin = 16;
    private const double SmallMargin = 8;

    // The space kept below the content for the page button: twice its margin, its padding and its 56 high button.
    private const double BottomInset = 16 * 3 + 56;

    // The digits of the number systems the app can format, which separate a value from its unit.
    private const string Digits = "0123456789٠١٢٣٤٥٦٧٨٩০১২৩৪৫৬৭৮৯०१२३४५६७८९۰۱۲۳۴۵۶۷۸۹௦௧௨௩௪௫௬௭௮௯";

    public static IReadOnlyList<object> Build(DetailsContext context)
    {
        List<object> blocks = [];
        DetailScreen screen = context.Screen;
        if (screen == DetailScreen.Conditions || screen == DetailScreen.FeelsLike)
        {
            BuildConditions(context, blocks);
        }
        else if (screen == DetailScreen.Precipitation)
        {
            BuildPrecipitation(context, blocks);
        }
        else if (screen == DetailScreen.Wind)
        {
            BuildWind(context, blocks);
        }
        else if (screen == DetailScreen.AirQuality)
        {
            BuildAirQuality(context, blocks);
        }
        else if (screen == DetailScreen.Pollen)
        {
            BuildPollen(context, blocks);
        }
        else if (screen == DetailScreen.UVIndex)
        {
            BuildUV(context, blocks);
        }
        else if (screen == DetailScreen.Humidity)
        {
            BuildHumidity(context, blocks);
        }
        else if (screen == DetailScreen.Pressure)
        {
            BuildPressure(context, blocks);
        }
        else if (screen == DetailScreen.CloudCover)
        {
            BuildCloudCover(context, blocks);
        }
        else if (screen == DetailScreen.Visibility)
        {
            BuildVisibility(context, blocks);
        }
        else if (screen == DetailScreen.SunMoon)
        {
            BuildSunMoon(context, blocks);
        }

        blocks.Add(new DetailsSpacer(BottomInset));
        return blocks;
    }

    private static string FormatHour(DetailsContext context, long x)
    {
        return DateFormatting.GetFormattedTime(DateTimeOffset.FromUnixTimeMilliseconds(x), context.TimeZone);
    }

    private static string? FormatHour(DetailsContext context, DateTimeOffset? date)
    {
        return date is { } value ? DateFormatting.GetFormattedTime(value, context.TimeZone) : null;
    }

    private static DetailsLabel DaytimeLabel() => new(Strings.Get("daytime"));

    private static DetailsLabel NighttimeLabelWithInfo() => new(Strings.Get("nighttime"), Strings.Get("nighttime_details"));

    private static IReadOnlyList<DetailsTextRun> Text(string? text) => text is null ? [] : [new DetailsTextRun(text)];

    /// <summary>
    /// A value with its unit at a smaller size: the text before the first digit and after the last one is the unit. The
    /// whole text keeps one size when it has no digit.
    /// </summary>
    private static IReadOnlyList<DetailsTextRun> UnitsDifferentFontSize(string formattedMeasure)
    {
        int first = formattedMeasure.IndexOfAny(Digits.ToCharArray());
        int last = formattedMeasure.LastIndexOfAny(Digits.ToCharArray());
        if (first < 0 || last < 0)
        {
            return [new DetailsTextRun(formattedMeasure)];
        }

        List<DetailsTextRun> runs = [];
        if (first > 0)
        {
            runs.Add(new DetailsTextRun(formattedMeasure[..first], DetailsRunStyle.Smaller));
        }

        runs.Add(new DetailsTextRun(formattedMeasure[first..(last + 1)]));
        if (last + 1 < formattedMeasure.Length)
        {
            runs.Add(new DetailsTextRun(formattedMeasure[(last + 1)..], DetailsRunStyle.Smaller));
        }

        return runs;
    }

    /// <summary>
    /// A pollutant name with the digits of its formula as subscripts, such as NO₂.
    /// </summary>
    private static IReadOnlyList<DetailsTextRun> FormatPollutantName(string name)
    {
        int first = name.IndexOfAny(Digits.ToCharArray());
        int last = name.LastIndexOfAny(Digits.ToCharArray());
        if (first < 0 || last < 0)
        {
            return [new DetailsTextRun(name)];
        }

        List<DetailsTextRun> runs = [];
        if (first > 0)
        {
            runs.Add(new DetailsTextRun(name[..first]));
        }

        runs.Add(new DetailsTextRun(name[first..(last + 1)], DetailsRunStyle.Subscript));
        if (last + 1 < name.Length)
        {
            runs.Add(new DetailsTextRun(name[(last + 1)..]));
        }

        return runs;
    }

    /// <summary>
    /// Labels at every multiple of the step between the bottom and the top of the chart.
    /// </summary>
    private static IReadOnlyList<double> StepValues(double minY, double maxY, double step)
    {
        List<double> values = [];
        for (long k = (long)Math.Ceiling(minY / step - 1e-9); k * step <= maxY + 1e-9; k++)
        {
            values.Add(k * step);
        }

        return values;
    }

    /// <summary>
    /// A chart with the hours of the day along the bottom. Pressing an hour shows it in the header.
    /// </summary>
    private static DetailsChartData MakeChart(
        DetailsContext context, DetailsChartKind kind, IReadOnlyList<long> keys, IReadOnlyList<IReadOnlyList<double>> series,
        IReadOnlyList<IReadOnlyList<ChartColorStop>> colors, double? minY, double maxY,
        IReadOnlyList<double> endAxisValues, Func<double, string> endAxisFormatter,
        IReadOnlyList<ChartAxisLabel> horizontalLines, Action<long?> setActiveX)
    {
        double bottom = minY ?? Math.Min(0, series.SelectMany(values => values).DefaultIfEmpty(0).Min());
        List<DetailsChartSeries> chartSeries = [];
        for (int i = 0; i < series.Count; i++)
        {
            chartSeries.Add(new DetailsChartSeries(keys, series[i], colors.Count > i ? colors[i] : []));
        }

        return new DetailsChartData(
            kind,
            context.DayStart.ToUnixTimeMilliseconds(),
            context.DayEnd.ToUnixTimeMilliseconds(),
            bottom,
            maxY,
            chartSeries,
            [.. endAxisValues.Select(value => new ChartAxisLabel(value, endAxisFormatter(value)))],
            [.. DetailsChartAxes.GetBottomLabelValues(context.DayStart, context.TimeZone)
                .Select(value => new ChartAxisLabel(value, FormatHour(context, value)))],
            context.Settings.IsTrendHorizontalLinesEnabled ? horizontalLines : [],
            setActiveX);
    }

    private static IReadOnlyList<ChartColorStop> Stops(params (double Value, Color Color)[] stops)
    {
        return [.. stops.Select(stop => new ChartColorStop(stop.Value, stop.Color))];
    }

    private static Color Rgb(byte r, byte g, byte b, byte a = 255) => Color.FromArgb(a, r, g, b);

    private static void AddUnavailableChart(List<object> blocks, int size)
    {
        blocks.Add(new DetailsUnavailableChart(Strings.Get(size == 0 ? "chart_no_hourly_data" : "chart_not_enough_hourly_data")));
    }

    private static void AddAbout(List<object> blocks, string titleKey, string text1, string? text2 = null)
    {
        blocks.Add(new DetailsSectionHeader(Text(Strings.Get(titleKey))));
        blocks.Add(new DetailsCardText(text1, text2));
    }
}
