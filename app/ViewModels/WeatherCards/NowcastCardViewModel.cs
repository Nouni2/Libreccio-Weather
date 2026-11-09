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

using LibreccioWeather.Controls.WeatherAnimation;
using LibreccioWeather.Controls.WeatherCards;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.ViewModels.Details;
using PrecipitationUnit = LibreccioWeather.Core.Units.PrecipitationUnit;
using UnitPrecipitation = LibreccioWeather.Core.Units.Precipitation;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// The precipitation of the next hour: whether it starts, continues or stops, and a column every five minutes.
/// </summary>
public sealed class NowcastCardViewModel
{
    private const long FiveMinutes = 5 * 60_000L;

    private NowcastCardViewModel(string title, string subtitle, string description, NowcastChartData chart)
    {
        Title = title;
        Subtitle = subtitle;
        Description = description;
        Chart = chart;
    }

    public string Title { get; }

    public string Subtitle { get; }

    public string Description { get; }

    public NowcastChartData Chart { get; }

    /// <summary>
    /// The card, when the next hour has precipitation over at least three intervals.
    /// </summary>
    /// <param name="lightRainColors">Whether the rain colors are the daylight ones: the location is in daylight and
    /// its screen is light.</param>
    public static NowcastCardViewModel? Create(
        Weather weather, Location location, PrecipitationUnit unit, bool horizontalLines, bool lightRainColors)
    {
        if (!MinutelyFormatting.HasMinutelyPrecipitation(weather) || weather.MinutelyForecast.Count < 3)
        {
            return null;
        }

        IReadOnlyList<Minutely> minutelyList = weather.MinutelyForecast;
        long minX = minutelyList[0].Date.ToUnixTimeMilliseconds();
        long maxX = minutelyList[^1].EndingDate.ToUnixTimeMilliseconds();

        // A missing intensity is drawn as none.
        Dictionary<long, UnitPrecipitation> minutely = [];
        foreach (Minutely interval in weather.MinutelyForecastBy5Minutes)
        {
            minutely[interval.Date.ToUnixTimeMilliseconds()] = interval.PrecipitationIntensity ?? UnitPrecipitation.FromMicrometers(0L);
        }

        double heavy = UnitPrecipitation.FromMillimeters(Precipitation.PrecipitationHourlyHeavy).InMicrometers;
        double maxY = Math.Max(heavy, minutely.Values.Max(value => value.InMicrometers));

        // Very heavy precipitation leaves out the lower thresholds: below the heavy level all three are kept, up to
        // twice the heavy level the light and heavy ones, and above that only the heavy one.
        List<ChartAxisLabel> lines = [];
        if (horizontalLines)
        {
            double maxMillimeters = UnitPrecipitation.FromMicrometers(maxY).InMillimeters;
            if (maxMillimeters < Precipitation.PrecipitationHourlyHeavy * 2.0f)
            {
                lines.Add(new ChartAxisLabel(Micrometers(Precipitation.PrecipitationHourlyLight), Strings.Get("precipitation_intensity_light")));
            }

            if (maxMillimeters <= Precipitation.PrecipitationHourlyHeavy)
            {
                lines.Add(new ChartAxisLabel(Micrometers(Precipitation.PrecipitationHourlyMedium), Strings.Get("precipitation_intensity_medium")));
            }

            lines.Add(new ChartAxisLabel(heavy, Strings.Get("precipitation_intensity_heavy")));
        }

        bool hasOnlyThresholdValues = !minutely.Values.Any(value => value.InMillimeters is not (0.0
            or Precipitation.PrecipitationHourlyLight or Precipitation.PrecipitationHourlyMedium
            or Precipitation.PrecipitationHourlyHeavy));

        long middle = minX + (maxX - minX) / 2;
        List<ChartAxisLabel> labels = [.. new[] { minX, middle, maxX }
            .Select(x => new ChartAxisLabel(x, DateFormatting.GetFormattedTime(DateTimeOffset.FromUnixTimeMilliseconds(x), location.TimeZone)))];

        string MarkerText(long x)
        {
            string start = DateFormatting.GetFormattedTime(DateTimeOffset.FromUnixTimeMilliseconds(x), location.TimeZone);
            string end = DateFormatting.GetFormattedTime(DateTimeOffset.FromUnixTimeMilliseconds(x + FiveMinutes), location.TimeZone);
            UnitPrecipitation? value = minutely.TryGetValue(x, out UnitPrecipitation found) ? found : null;
            string quantity = hasOnlyThresholdValues
                ? (value?.InMillimeters ?? 0.0) switch
                {
                    0.0 => Strings.Get("precipitation_none"),
                    Precipitation.PrecipitationHourlyLight => Strings.Get("precipitation_intensity_light"),
                    Precipitation.PrecipitationHourlyMedium => Strings.Get("precipitation_intensity_medium"),
                    Precipitation.PrecipitationHourlyHeavy => Strings.Get("precipitation_intensity_heavy"),
                    _ => value is { } other ? UnitFormatting.Format(other, unit) : "",
                }
                : value is { } amount ? UnitFormatting.Format(amount, unit) : "";
            return start + "-" + end + Strings.Get("colon_separator") + quantity;
        }

        var chart = new NowcastChartData(
            minX, maxX, FiveMinutes,
            [.. minutely.Keys], [.. minutely.Values.Select(value => value.InMicrometers)], maxY,
            MaterialWeatherThemeDelegate.GetThemeColors(WeatherCode.Rain, lightRainColors)[0],
            labels, lines, MarkerText);
        return new NowcastCardViewModel(
            MinutelyFormatting.GetMinutelyTitle(weather),
            MinutelyFormatting.GetMinutelyDescription(weather, location),
            MinutelyFormatting.GetContentDescription(minutelyList, location),
            chart);
    }

    private static double Micrometers(double millimeters) => UnitPrecipitation.FromMillimeters(millimeters).InMicrometers;
}
