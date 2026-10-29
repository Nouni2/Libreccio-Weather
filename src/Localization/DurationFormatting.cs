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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Durations written as hours and minutes, such as "1 hour, 30 minutes".
/// </summary>
public static class DurationFormatting
{
    /// <summary>
    /// A duration as hours, then minutes, then seconds when <paramref name="withSeconds"/> is set, leaving out the parts
    /// that are zero. A zero duration is written as zero hours.
    /// </summary>
    public static string FormatTime(TimeSpan duration, UnitWidth unitWidth = UnitWidth.Short, bool withSeconds = false)
    {
        List<(long Value, DurationPart Part)> parts = [];
        long hours = (long)Math.Floor(duration.TotalHours);
        if (hours != 0)
        {
            parts.Add((hours, DurationPart.Hour));
        }

        if (duration.Minutes != 0)
        {
            parts.Add((duration.Minutes, DurationPart.Minute));
        }

        if (withSeconds && duration.Seconds != 0)
        {
            parts.Add((duration.Seconds, DurationPart.Second));
        }

        if (parts.Count == 0)
        {
            parts.Add((0, DurationPart.Hour));
        }

        if (Icu.SupportsDurations && FormatWithIcu(parts, unitWidth) is { } formatted)
        {
            return formatted;
        }

        return string.Join(Strings.GetUnit("locale_separator"), parts.Select(part => FormatWithStrings(part.Value, part.Part, unitWidth)));
    }

    private enum DurationPart
    {
        Hour,
        Minute,
        Second,
    }

    // Each part as a measure in CLDR's words, joined as a list of units.
    private static string? FormatWithIcu(List<(long Value, DurationPart Part)> parts, UnitWidth unitWidth)
    {
        string width = unitWidth switch
        {
            UnitWidth.Long => "unit-width-full-name",
            UnitWidth.Narrow => "unit-width-narrow",
            _ => "unit-width-short",
        };

        List<string> measures = [];
        foreach ((long value, DurationPart part) in parts)
        {
            string unit = part switch
            {
                DurationPart.Hour => "duration-hour",
                DurationPart.Minute => "duration-minute",
                _ => "duration-second",
            };
            if (Icu.FormatNumber($"measure-unit/{unit} {width}", value, Strings.Culture)?.Text is not { } measure)
            {
                return null;
            }

            measures.Add(measure);
        }

        return Icu.FormatUnitList(measures, Strings.Culture);
    }

    // The translated nominative pattern, such as "{0} h", with the value as a whole number.
    private static string FormatWithStrings(long value, DurationPart part, UnitWidth unitWidth)
    {
        string unit = part switch
        {
            DurationPart.Hour => "hr",
            DurationPart.Minute => "min",
            _ => "sec",
        };
        string suffix = unitWidth == UnitWidth.Long ? "long" : "short";
        return string.Format(
            Strings.Culture,
            Strings.GetUnit($"duration_{unit}_nominative_{suffix}"),
            NumberFormatting.Format(value, 0, Strings.Culture));
    }
}
