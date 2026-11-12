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

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Localization;

public static partial class AlertFormatting
{
    /// <summary>
    /// When an alert starts and ends, such as "12 Nov, 14:00 — 13 Nov, 08:00", with the end day left out when it is the
    /// start day. Empty without a start date. The full form spells the month out for screen readers; its end day is
    /// the start day's, which also leaves the end day out.
    /// </summary>
    public static string GetFormattedDates(Alert alert, TimeZoneInfo timeZone, bool full = false)
    {
        StringBuilder builder = new();
        if (alert.StartDate is not { } startDate)
        {
            return "";
        }

        string separator = Strings.GetUnit("locale_separator");
        string startDateDay = full
            ? DateFormatting.GetFormattedFullDayAndMonth(startDate, timeZone)
            : DateFormatting.GetFormattedMediumDayAndMonth(startDate, timeZone);
        builder.Append(startDateDay).Append(separator).Append(DateFormatting.GetFormattedTime(startDate, timeZone));
        if (alert.EndDate is { } endDate)
        {
            builder.Append(" — ");
            string endDateDay = full
                ? DateFormatting.GetFormattedFullDayAndMonth(startDate, timeZone)
                : DateFormatting.GetFormattedMediumDayAndMonth(endDate, timeZone);
            if (startDateDay != endDateDay)
            {
                builder.Append(endDateDay).Append(separator);
            }

            builder.Append(DateFormatting.GetFormattedTime(endDate, timeZone));
        }

        return builder.ToString();
    }

    /// <summary>
    /// An alert's description as plain text: line breaks become new lines, the other tags are dropped and character
    /// references are decoded, as the text shows without the styling the tags give.
    /// </summary>
    public static string GetPlainDescription(string description)
    {
        string withBreaks = LineBreakTags().Replace(description.Replace("\r\n", "\n"), "\n");
        return WebUtility.HtmlDecode(Tags().Replace(withBreaks, ""));
    }

    [GeneratedRegex(@"<br\s*/?>|</p>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTags();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}
