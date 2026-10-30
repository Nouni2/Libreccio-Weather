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
using LibreccioWeather.Core.Astronomy;
using LibreccioWeather.Core.Charts;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Solar;
using LibreccioWeather.Core.Units;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    // The sun's color, and the night sky's color brightened by a quarter in saturation and value, for the moon.
    private static readonly Color SunColor = Color.FromRgb(253, 188, 76);
    private static readonly Color MoonColor = Color.FromRgb(76, 87, 108);

    // A gentle curve between the highest and lowest points of a body.
    private const double EphemerisCurvature = (Math.PI - 2.0) / Math.PI;

    private static void BuildSunMoon(DetailsContext context, List<object> blocks)
    {
        Daily today = context.Daily;
        if (today.Sun?.IsValid == true || today.Moon?.IsValid == true)
        {
            blocks.Add(EphemerisChart(context));
        }

        if (today.Sun?.IsValid == true || today.Moon?.IsValid == true || today.MoonPhase?.IsValid == true)
        {
            List<DetailsAstroItem> items = [];
            if (today.Sun is { IsValid: true } sun)
            {
                items.Add(SunItem(context, sun));
            }

            if (today.Twilight is { IsValid: true } twilight)
            {
                items.Add(RiseSetItem(context, "IconTwilight", twilight, "ephemeris_dawn_at", "ephemeris_dusk_at"));
            }

            if (today.Moon is { IsValid: true } moon)
            {
                items.Add(RiseSetItem(context, "IconClearNight", moon, "ephemeris_moonrise_at", "ephemeris_moonset_at"));
            }

            if (today.MoonPhase is { IsValid: true } moonPhase)
            {
                string description = MoonPhaseFormatting.GetDescription(moonPhase) ?? "";
                items.Add(new DetailsAstroItem(null, description, description, moonPhase.Angle));
            }

            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsAstroList(items));
        }
        else
        {
            blocks.Add(new DetailsMessage(Strings.Get("chart_no_daily_data")));
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "ephemeris_about",
            Strings.Get("ephemeris_about_rise") + " " + Strings.Get("ephemeris_about_set") + "\n\n"
                + Strings.Get("ephemeris_about_dawn") + " " + Strings.Get("ephemeris_about_dusk"));
    }

    // The highest and lowest points of the sun and the moon on the day, joined by curves.
    private static DetailsChartData EphemerisChart(DetailsContext context)
    {
        Location location = context.Location;
        DateTimeOffset startingDate = context.DayStart;
        DateTimeOffset endingDate = context.DayEnd;
        List<Astro> sunTimes = [.. context.Weather.DailyForecast.Select(daily => daily.Sun).OfType<Astro>()];
        List<Astro> moonTimes = [.. context.Weather.DailyForecast.Select(daily => daily.Moon).OfType<Astro>()];
        List<(long X, double Y)> sunValues = GetMappedAstroValues(location, sunTimes, isSun: true, startingDate, endingDate);
        List<(long X, double Y)> moonValues = GetMappedAstroValues(location, moonTimes, isSun: false, startingDate, endingDate);

        List<DetailsChartSeries> series = [];
        if (moonValues.Count > 0)
        {
            series.Add(EphemerisSeries(moonValues, MoonColor));
        }

        if (sunValues.Count > 0)
        {
            series.Add(EphemerisSeries(sunValues, SunColor));
        }

        return new DetailsChartData(
            DetailsChartKind.Line,
            startingDate.ToUnixTimeMilliseconds(),
            endingDate.ToUnixTimeMilliseconds(),
            -90.0,
            90.0,
            series,
            // The altitudes are written as degrees with the temperature's degree sign.
            [.. new[] { 0.0, 90.0 }.Select(value => new ChartAxisLabel(value,
                UnitFormatting.Format(Core.Units.Temperature.FromCelsius(value), TemperatureUnit.Celsius, UnitWidth.Narrow, UnitWidth.Narrow)))],
            [.. DetailsChartAxes.GetBottomLabelValues(startingDate, context.TimeZone)
                .Select(value => new ChartAxisLabel(value, FormatHour(context, value)))],
            [],
            _ => { })
        {
            Curvature = EphemerisCurvature,
        };
    }

    // Above the horizon the line has its color, and below it the color at half opacity; the area under it fades out.
    private static DetailsChartSeries EphemerisSeries(List<(long X, double Y)> values, Color color)
    {
        return new DetailsChartSeries([.. values.Select(value => value.X)], [.. values.Select(value => value.Y)],
            Stops((0.1, color), (-0.1, WithAlpha(color, 128))))
        {
            AreaColors = Stops((0.1, WithAlpha(color, 128)), (-0.1, Color.FromArgb(0, 0, 0, 0))),
        };
    }

    // The altitude at each noon and nadir, halfway between a rise and the next set or a set and the next rise, with a
    // cycle added before the first and after the last so the first and last days look complete.
    private static List<(long X, double Y)> GetMappedAstroValues(
        Location location, List<Astro> astro, bool isSun, DateTimeOffset startingDate, DateTimeOffset endingDate)
    {
        List<long> riseTimes = [];
        List<long> setTimes = [];
        foreach (Astro item in astro)
        {
            if (item.RiseDate is { } rise)
            {
                riseTimes.Add(rise.ToUnixTimeMilliseconds());
            }

            if (item.SetDate is { } set)
            {
                setTimes.Add(set.ToUnixTimeMilliseconds());
            }
        }

        if (riseTimes.Count > 1 && setTimes.Count > 1)
        {
            riseTimes.Insert(0, riseTimes[0] - (riseTimes[1] - riseTimes[0]));
            setTimes.Insert(0, setTimes[0] - (setTimes[1] - setTimes[0]));
            riseTimes.Add(riseTimes[^1] + (riseTimes[^1] - riseTimes[^2]));
            setTimes.Add(setTimes[^1] + (setTimes[^1] - setTimes[^2]));
        }

        long start = startingDate.ToUnixTimeMilliseconds();
        long end = endingDate.ToUnixTimeMilliseconds();

        // From the last rise and set before the day, so no time is spent on points the chart does not show.
        int riseCounter = 0;
        while (riseCounter < riseTimes.Count && riseTimes[riseCounter] < start)
        {
            riseCounter++;
        }

        if (riseCounter > 0)
        {
            riseCounter--;
        }

        int setCounter = 0;
        while (setCounter < setTimes.Count && setTimes[setCounter] < start)
        {
            setCounter++;
        }

        if (setCounter > 0)
        {
            setCounter--;
        }

        // Up to the first rise and set after the day.
        int riseCounterLimit = riseTimes.Count - 1;
        int setCounterLimit = setTimes.Count - 1;
        while (riseCounterLimit >= 0 && riseTimes[riseCounterLimit] > end)
        {
            riseCounterLimit--;
        }

        if (riseCounterLimit < riseTimes.Count - 1)
        {
            riseCounterLimit++;
        }

        while (setCounterLimit >= 0 && setTimes[setCounterLimit] > end)
        {
            setCounterLimit--;
        }

        if (setCounterLimit < setTimes.Count - 1)
        {
            setCounterLimit++;
        }

        List<long> keys = [];
        Dictionary<long, double> values = [];
        void Put(long x, double y)
        {
            if (!values.ContainsKey(x))
            {
                keys.Add(x);
            }

            values[x] = y;
        }

        while (riseCounter <= riseCounterLimit && setCounter <= setCounterLimit)
        {
            if (riseTimes[riseCounter] < setTimes[setCounter])
            {
                long noon = riseTimes[riseCounter] + (setTimes[setCounter] - riseTimes[riseCounter]) / 2;
                if (GetAltitude(location, noon, isSun) is { } altitude)
                {
                    Put(noon, altitude);
                }

                riseCounter++;
            }
            else
            {
                long nadir = setTimes[setCounter] + (riseTimes[riseCounter] - setTimes[setCounter]) / 2;
                if (GetAltitude(location, nadir, isSun) is { } altitude)
                {
                    Put(nadir, altitude);
                }

                setCounter++;
            }
        }

        return [.. keys.Select(key => (key, values[key]))];
    }

    private static double? GetAltitude(Location location, long instant, bool isSun)
    {
        DateTimeOffset date = DateTimeOffset.FromUnixTimeMilliseconds(instant);
        return isSun
            ? SolarCalculator.GetUpperLimbAltitude(location.Latitude, location.Longitude, date)
            : MoonCalculator.GetAltitude(date, location.Latitude, location.Longitude);
    }

    private static DetailsAstroItem SunItem(DetailsContext context, Astro sun)
    {
        string? sunriseTime = FormatHour(context, sun.RiseDate);
        string? sunsetTime = FormatHour(context, sun.SetDate);
        string text = FormatAstroTime(context, sun.RiseDate) + "↑ / " + FormatAstroTime(context, sun.SetDate) + "↓"
            + (sun.Duration is { } duration ? " / " + DurationFormatting.FormatHours(duration) : "");

        var talkBack = new StringBuilder();
        if (sunriseTime is not null)
        {
            talkBack.Append(Strings.Format("ephemeris_sunrise_at", sunriseTime));
        }

        if (sunsetTime is not null)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            talkBack.Append(Strings.Format("ephemeris_sunset_at", sunsetTime));
        }

        if (sun.Duration is { } spokenDuration)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            talkBack.Append(Strings.Get("sunshine_duration"));
            talkBack.Append(DurationFormatting.FormatTime(spokenDuration, UnitWidth.Long));
        }

        return new DetailsAstroItem("IconClearDay", text, talkBack.ToString());
    }

    private static DetailsAstroItem RiseSetItem(DetailsContext context, string iconKey, Astro astro, string riseKey, string setKey)
    {
        string? rise = FormatHour(context, astro.RiseDate);
        string? set = FormatHour(context, astro.SetDate);
        string text = FormatAstroTime(context, astro.RiseDate) + "↑ / " + FormatAstroTime(context, astro.SetDate) + "↓";

        var talkBack = new StringBuilder();
        if (rise is not null)
        {
            talkBack.Append(Strings.Format(riseKey, rise));
        }

        if (set is not null)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            talkBack.Append(Strings.Format(setKey, set));
        }

        return new DetailsAstroItem(iconKey, text, talkBack.ToString());
    }

    // Full dates distinguish events that cross midnight while inspecting calculations.
    private static string FormatAstroTime(DetailsContext context, DateTimeOffset? date)
    {
        if (date is not { } value)
        {
            return Strings.Get("null_data_text");
        }
#if DEBUG
        return TimeZoneInfo.ConvertTime(value, context.TimeZone).ToString("yyyy-MM-dd HH:mm", Strings.Culture);
#else
        return DateFormatting.GetFormattedTime(value, context.TimeZone);
#endif
    }
}
