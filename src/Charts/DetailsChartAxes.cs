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

namespace LibreccioWeather.Core.Charts;

/// <summary>
/// The axes of the day charts on the detail screen, which span a day from midnight to midnight in the location's time
/// zone.
/// </summary>
public static class DetailsChartAxes
{
    private const long MillisecondsPerHour = 3_600_000L;

    public static double RoundUpToNearestMultiplier(double value, double multiplier)
    {
        return Math.Ceiling(value / multiplier) * multiplier;
    }

    public static double RoundDownToNearestMultiplier(double value, double multiplier)
    {
        return Math.Floor(value / multiplier) * multiplier;
    }

    /// <summary>
    /// The start of an hour on the day of a date, in a time zone. An hour skipped by a daylight saving change is read
    /// with the offset in force before the change, which moves it forward by the skipped time. An hour that occurs twice
    /// is taken at its first occurrence.
    /// </summary>
    public static DateTimeOffset GetHourOfDay(DateTimeOffset day, TimeZoneInfo timeZone, int hour, int dayOffset = 0)
    {
        DateTime local = TimeZoneInfo.ConvertTime(day, timeZone).DateTime.Date.AddDays(dayOffset).AddHours(hour);
        TimeSpan offset;
        if (timeZone.IsInvalidTime(local))
        {
            offset = timeZone.GetUtcOffset(local.AddDays(-1));
        }
        else if (timeZone.IsAmbiguousTime(local))
        {
            offset = timeZone.GetAmbiguousTimeOffsets(local).Max();
        }
        else
        {
            offset = timeZone.GetUtcOffset(local);
        }

        return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset);
    }

    /// <summary>
    /// The bottom axis labels: 06:00, 12:00 and 18:00 on the day.
    /// </summary>
    public static IReadOnlyList<long> GetBottomLabelValues(DateTimeOffset dayStart, TimeZoneInfo timeZone)
    {
        return [.. new[] { 6, 12, 18 }.Select(hour => GetHourOfDay(dayStart, timeZone, hour).ToUnixTimeMilliseconds())];
    }

    /// <summary>
    /// The values that get a top label or icon. Labels are spaced by as many hours as it takes for the widest one to fit
    /// in the chart, and the first and last hours of the day stay free.
    /// </summary>
    public static IReadOnlyList<long> GetTopLabelValues(
        IReadOnlyList<long> possibleValues, long minX, long maxX, float layerWidth, float maxLabelWidth)
    {
        // A day can last 23, 24 or 25 hours.
        long hoursDisplayed = (maxX - minX) / MillisecondsPerHour;
        float maxLabelsThatCanFit = MathF.Floor(layerWidth / maxLabelWidth);
        int hoursToSkip = TrendCoordinates.ToInt(Math.Max(MathF.Ceiling(hoursDisplayed / maxLabelsThatCanFit), 0f));

        long margin = Math.Max(hoursToSkip - 1, 1) * MillisecondsPerHour;
        long firstDisplayableLabel = minX + margin;
        long lastDisplayableLabel = maxX - margin;
        long spacing = hoursToSkip * MillisecondsPerHour;

        List<long> labels = [];
        long previousValue = 0L;
        foreach (long value in possibleValues)
        {
            if (value < firstDisplayableLabel || value > lastDisplayableLabel)
            {
                continue;
            }

            if (value >= previousValue + spacing)
            {
                labels.Add(value);
                previousValue = value;
            }
        }

        return labels;
    }
}
