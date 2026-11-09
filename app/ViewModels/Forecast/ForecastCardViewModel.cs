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
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Appearance;
using LibreccioWeather.Controls.WeatherAnimation;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Display;
using LibreccioWeather.Icons;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Forecast;

public sealed partial class ForecastCardViewModel : ObservableObject
{
    // The width of a column at 100 % text size.
    private const double TrendItemWidth = 67;

    private readonly Action<string?>? _setSelectedTab;
    private readonly Func<int, int?> _getDailyIndex;
    private List<ForecastColumnSlot> _slots = [];

    [ObservableProperty]
    private ForecastTab? _selectedTrend;

    private ForecastCardViewModel(
        bool daily, string title, string? subtitle, IReadOnlyList<ForecastTab> trends, int initialIndex,
        string? selectedTab, Action<string?> setSelectedTab, Func<int, int?> getDailyIndex)
    {
        _getDailyIndex = getDailyIndex;
        IsDaily = daily;
        Title = title;
        Subtitle = subtitle;
        Trends = trends;
        InitialIndex = initialIndex;
        ForecastTab initialTrend = trends.FirstOrDefault(trend => trend.Name == selectedTab) ?? trends[0];
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

    /// <summary>
    /// The card's tabs. Each builds its columns the first time it is shown.
    /// </summary>
    public IReadOnlyList<ForecastTab> Trends { get; }

    public bool HasMultipleTrends => Trends.Count > 1;

    /// <summary>
    /// The column positions of the selected tab. Positions are kept across tabs with the same number of columns, so
    /// switching tabs updates the columns' elements instead of creating them again.
    /// </summary>
    public IReadOnlyList<ForecastColumnSlot> Columns => _slots;

    public int InitialIndex { get; }

    /// <summary>
    /// The width of a column when the columns do not all fit in the card; wider cards share their width among them.
    /// </summary>
    public double MinColumnWidth { get; } = TrendItemWidth * TextScale.FontScaleToApply;

    public double ColumnHeight => IsDaily ? 318 : 206;

    /// <summary>
    /// Whether hovering a column shows its description.
    /// </summary>
    public bool ShowsColumnTooltips { get; init; }

    partial void OnSelectedTrendChanged(ForecastTab? value)
    {
        IReadOnlyList<ForecastColumn> columns = value?.Trend.Columns ?? [];
        if (columns.Count == _slots.Count)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                _slots[i].Column = columns[i];
            }
        }
        else
        {
            _slots = [.. columns.Select(column => new ForecastColumnSlot(column))];
            OnPropertyChanged(nameof(Columns));
        }

        if (value is not null)
        {
            _setSelectedTab?.Invoke(value.Name);
        }
    }

    /// <summary>
    /// Takes over the column positions of the card this one replaces on screen, when both have as many columns, so
    /// the columns' views stay and show this card's data instead of being built again.
    /// </summary>
    public void AdoptColumns(ForecastCardViewModel previous)
    {
        if (previous.IsDaily != IsDaily || previous._slots.Count != _slots.Count || ReferenceEquals(previous._slots, _slots))
        {
            return;
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            previous._slots[i].Column = _slots[i].Column;
        }

        _slots = previous._slots;
    }

    /// <summary>
    /// Raised with the day and the detail page a clicked column opens.
    /// </summary>
    public event EventHandler<(int? DailyIndex, DetailScreen? Screen)>? DetailsRequested;

    [RelayCommand]
    private void OpenDetails(ForecastColumnSlot column)
    {
        int position = _slots.IndexOf(column);
        if (position >= 0)
        {
            DetailsRequested?.Invoke(this, (_getDailyIndex(position), SelectedTrend?.Screen));
        }
    }

    public static ForecastCardViewModel? CreateDaily(
        Core.Models.Weather weather, Location location, TemperatureUnit unit, PrecipitationUnit precipitationUnit,
        SpeedUnit speedUnit, SettingsManager settings,
        string? selectedTab, Action<string?> setSelectedTab)
    {
        Func<ForecastTrend, ForecastTrend> theme = CreateTheme(weather, location, settings, daily: true);
        List<ForecastTab> tabs = [];
        void Add(string nameKey, DetailScreen screen, Func<ForecastTrend> build)
        {
            tabs.Add(new ForecastTab(Strings.Get(nameKey), screen, () => theme(build() with { Screen = screen })));
        }

        foreach (DailyTrendDisplay display in settings.DailyTrendDisplayList)
        {
            if (display == DailyTrendDisplay.Temperature)
            {
                Add("tag_temperature", DetailScreen.Conditions,
                    () => CreateDailyTemperature(weather, location, unit, false, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == DailyTrendDisplay.Precipitation && weather.DailyForecast.Any(day =>
                day.Day?.Precipitation?.Total is not null || day.Night?.Precipitation?.Total is not null))
            {
                Add("tag_precipitation", DetailScreen.Precipitation, () => CreateDailyPrecipitation(weather, location, precipitationUnit));
            }
            else if (display == DailyTrendDisplay.FeelsLike && weather.DailyForecast.Any(day =>
                day.Day?.Temperature?.FeelsLikeTemperature is not null || day.Night?.Temperature?.FeelsLikeTemperature is not null))
            {
                Add("tag_feels_like", DetailScreen.FeelsLike, () => CreateDailyTemperature(weather, location, unit, true, false));
            }
            else if (display == DailyTrendDisplay.UVIndex && weather.DailyForecast.Any(day => day.UV?.Index is not null))
            {
                Add("tag_uv", DetailScreen.UVIndex, () => CreateDailyUV(weather, location, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == DailyTrendDisplay.AirQuality && weather.DailyForecast.Any(day =>
                day.AirQuality is { } airQuality && AirQualityIndex.GetIndex(airQuality) is not null))
            {
                Add("tag_aqi", DetailScreen.AirQuality,
                    () => CreateDailyAirQuality(weather, location, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == DailyTrendDisplay.Wind && weather.DailyForecast.Any(day =>
                day.Day?.Wind?.Speed is not null || day.Night?.Wind?.Speed is not null))
            {
                Add("tag_wind", DetailScreen.Wind, () => CreateDailyWind(weather, location, speedUnit));
            }
            else if (display == DailyTrendDisplay.Sunshine && weather.DailyForecast.Any(day =>
                day.SunshineDuration is { } duration && duration.Ticks / TimeSpan.TicksPerMinute > 0))
            {
                Add("tag_sunshine", DetailScreen.CloudCover, () => CreateDailySunshine(weather, location));
            }
        }

        return tabs.Count == 0 ? null : new ForecastCardViewModel(true, Strings.Get("daily_forecast"),
            weather.Current?.DailyForecast, tabs, weather.TodayIndex ?? 0, selectedTab, setSelectedTab, position => position)
        {
            ShowsColumnTooltips = settings.IsForecastColumnTooltipEnabled,
        };
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
        Core.Models.Weather weather, Location location, TemperatureUnit unit, PrecipitationUnit precipitationUnit,
        SpeedUnit speedUnit, PressureUnit pressureUnit, DistanceUnit distanceUnit, SettingsManager settings,
        string? selectedTab, Action<string?> setSelectedTab)
    {
        Func<ForecastTrend, ForecastTrend> theme = CreateTheme(weather, location, settings, daily: false);
        List<ForecastTab> tabs = [];
        void Add(string nameKey, DetailScreen screen, Func<ForecastTrend> build)
        {
            tabs.Add(new ForecastTab(Strings.Get(nameKey), screen, () => theme(build() with { Screen = screen })));
        }

        foreach (HourlyTrendDisplay display in settings.HourlyTrendDisplayList)
        {
            if (display == HourlyTrendDisplay.Temperature)
            {
                Add("tag_temperature", DetailScreen.Conditions,
                    () => CreateHourlyTemperature(weather, location, unit, false, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == HourlyTrendDisplay.Precipitation && weather.NextHourlyForecast.Any(hour =>
                hour.Precipitation?.Total is not null))
            {
                Add("tag_precipitation", DetailScreen.Precipitation, () => CreateHourlyPrecipitation(weather, location, precipitationUnit));
            }
            else if (display == HourlyTrendDisplay.FeelsLike && weather.NextHourlyForecast.Any(hour =>
                hour.Temperature?.FeelsLikeTemperature is not null))
            {
                Add("tag_feels_like", DetailScreen.FeelsLike, () => CreateHourlyTemperature(weather, location, unit, true, false));
            }
            else if (display == HourlyTrendDisplay.Humidity && weather.NextHourlyForecast.Any(hour => hour.DewPoint is not null))
            {
                Add("tag_humidity_dew_point", DetailScreen.Humidity, () => CreateHourlyHumidity(weather, location, unit));
            }
            else if (display == HourlyTrendDisplay.UVIndex && weather.NextHourlyForecast.Any(hour => hour.UV?.Index is not null))
            {
                Add("tag_uv", DetailScreen.UVIndex, () => CreateHourlyUV(weather, location, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == HourlyTrendDisplay.Pressure && HasHourlyPressure(weather))
            {
                Add("tag_pressure", DetailScreen.Pressure,
                    () => CreateHourlyPressure(weather, location, pressureUnit, settings.IsTrendHorizontalLinesEnabled)!);
            }
            else if (display == HourlyTrendDisplay.CloudCover && weather.NextHourlyForecast.Any(hour => hour.CloudCover is not null))
            {
                Add("tag_cloud_cover", DetailScreen.CloudCover, () => CreateHourlyCloudCover(weather, location));
            }
            else if (display == HourlyTrendDisplay.Visibility && weather.NextHourlyForecast.Any(hour => hour.Visibility is not null))
            {
                Add("tag_visibility", DetailScreen.Visibility, () => CreateHourlyVisibility(weather, location, distanceUnit));
            }
            else if (display == HourlyTrendDisplay.AirQuality && weather.NextHourlyForecast.Any(hour =>
                hour.AirQuality is { } airQuality && AirQualityIndex.GetIndex(airQuality) is not null))
            {
                Add("tag_aqi", DetailScreen.AirQuality,
                    () => CreateHourlyAirQuality(weather, location, settings.IsTrendHorizontalLinesEnabled));
            }
            else if (display == HourlyTrendDisplay.Wind && weather.NextHourlyForecast.Any(hour => hour.Wind?.Speed is not null))
            {
                Add("tag_wind", DetailScreen.Wind, () => CreateHourlyWind(weather, location, speedUnit));
            }
        }

        // An hour opens the first day that starts less than a day before it, which assumes days start at midnight.
        return tabs.Count == 0 ? null : new ForecastCardViewModel(false, Strings.Get("hourly_forecast"),
            weather.Current?.HourlyForecast, tabs, 0, selectedTab, setSelectedTab, position =>
            {
                DateTimeOffset hourlyDate = weather.NextHourlyForecast[position].Date;
                for (int index = 0; index < weather.DailyForecast.Count; index++)
                {
                    if (weather.DailyForecast[index].Date > hourlyDate.AddDays(-1))
                    {
                        return index;
                    }
                }

                return null;
            })
            {
                ShowsColumnTooltips = settings.IsForecastColumnTooltipEnabled,
            };
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

    // Curves take the colors of the current weather and the bars an opacity set by the location's light or dark
    // theme. The high line takes the first theme color, or on a dark hourly chart the second, half-opaque one; the
    // low line and the probability bars take the second. Charts colored by risk or amount keep their colors and
    // take only the bars' opacity, and the wind and two-bar charts keep a fixed opacity.
    // The colors and accents a tab takes, worked out once for the card and applied to each tab when it is built.
    private static Func<ForecastTrend, ForecastTrend> CreateTheme(
        Core.Models.Weather weather, Location location, SettingsManager settings, bool daily)
    {
        bool daylight = RiseProgress.IsDaylight(location, DateTimeOffset.UtcNow);
        Color[] themeColors = MaterialWeatherThemeDelegate.GetThemeColors(weather.Current?.WeatherCode, daylight);
        bool lightTheme = ThemeManager.IsLightTheme(settings, daylight);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset localNow = TimeZoneInfo.ConvertTime(now, location.TimeZone);
        bool UsesAccentColor(int position)
        {
            if (daily)
            {
                if (position >= weather.DailyForecast.Count)
                {
                    return true;
                }

                DateTimeOffset date = weather.DailyForecast[position].Date;
                DateTimeOffset localDate = TimeZoneInfo.ConvertTime(date, location.TimeZone);
                return (localDate.Year == localNow.Year && localDate.DayOfYear == localNow.DayOfYear) || date > now;
            }

            return position == 0 || (position < weather.NextHourlyForecast.Count
                && TimeZoneInfo.ConvertTime(weather.NextHourlyForecast[position].Date, location.TimeZone).Hour == 0);
        }

        return built =>
        {
            ForecastTrend trend = built with
            {
                Columns = [.. built.Columns.Select((column, position) => column with { IsAccented = UsesAccentColor(position) })],
            };
            if (trend.Screen == DetailScreen.Wind || trend.Columns.Any(column => column.Chart.IsDoubleHistogram))
            {
                return trend;
            }

            bool themed = trend.Screen == DetailScreen.Conditions || trend.Screen == DetailScreen.FeelsLike
                || trend.Screen == DetailScreen.Humidity || trend.Screen == DetailScreen.Pressure
                || trend.Screen == DetailScreen.Visibility;
            var histogramAlpha = (byte)(int)(255 * (lightTheme ? (themed ? 0.2f : 1f) : 0.5f));
            Color? high = themed ? themeColors[daily || lightTheme ? 1 : 2] : null;
            Color? low = themed ? themeColors[2] : null;
            return trend with
            {
                Columns = [.. trend.Columns.Select(column => column with
                {
                    Chart = column.Chart with { HighLineColor = high, LowLineColor = low, HistogramAlpha = histogramAlpha },
                })],
            };
        };
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
    string Name, IReadOnlyList<ForecastColumn> Columns, IReadOnlyList<TrendKeyLine> KeyLines)
{
    /// <summary>
    /// The page of the detail screen that a column of this chart opens.
    /// </summary>
    public DetailScreen? Screen { get; init; }
}

/// <summary>
/// A tab of a forecast card. Its chart and columns are built the first time it is shown, so a card only builds the tabs
/// that are looked at, as a list binds only the items it shows.
/// </summary>
public sealed class ForecastTab(string name, DetailScreen? screen, Func<ForecastTrend> build)
{
    private ForecastTrend? _trend;

    public string Name { get; } = name;

    /// <summary>
    /// The page of the detail screen that a column of this tab opens.
    /// </summary>
    public DetailScreen? Screen { get; } = screen;

    public ForecastTrend Trend => _trend ??= build();
}

public sealed record TrendKeyLine(float Value, string ContentLeft, string ContentRight, bool AboveLine);

/// <summary>
/// One column position of a forecast card, showing the column of the selected tab.
/// </summary>
public sealed partial class ForecastColumnSlot : ObservableObject
{
    // Set by the constructor through the property.
    [ObservableProperty]
    private ForecastColumn _column = null!;

    public ForecastColumnSlot(ForecastColumn column)
    {
        Column = column;
    }
}

public sealed record ForecastColumn(
    string Label, string? Date, string? DayIcon, string? NightIcon, string? Description, TrendChartData Chart,
    bool ShowDayIcon = true, System.Windows.Media.ImageSource? DayWindIcon = null, System.Windows.Media.ImageSource? NightWindIcon = null)
{
    /// <summary>
    /// Whether the labels take the stronger colors: today and later days, and the first hour and each midnight.
    /// </summary>
    public bool IsAccented { get; init; } = true;
}

public sealed record TrendChartData(
    float?[] HighValues, float?[]? LowValues, string? HighText, string? LowText,
    float? Highest, float? Lowest, float? Histogram, string? HistogramText, bool HasTopPolylineLabel,
    float HighestHistogram = 100, float LowestHistogram = 0, Color? HistogramColor = null,
    bool HistogramUsesTitleColor = false, DoubleHistogramData? DoubleHistogram = null,
    SecondaryHistogramData? SecondaryHistogram = null, byte HistogramAlpha = 127,
    Color? HighLineColor = null, Color? LowLineColor = null)
{
    public bool IsDoubleHistogram => DoubleHistogram is not null;
}

public sealed record DoubleHistogramData(
    float? HighValue, float? LowValue, string? HighText, string? LowText, float Highest,
    Color HighColor, Color LowColor, SecondaryHistogramData? HighSecondary = null, SecondaryHistogramData? LowSecondary = null);

public sealed record SecondaryHistogramData(float? Value, string? Text, Color Color);
