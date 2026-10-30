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

using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// The lowest and highest value of a day, such as "40–85 %", or the single value when both are equal. A range only
/// carries the unit after its highest value.
/// </summary>
public static class DailyRangeFormatting
{
    public static string? GetRangeSummary(DailyRelativeHumidity humidity)
    {
        if (humidity.Min is not { } min || humidity.Max is not { } max)
        {
            return null;
        }

        return min == max
            ? UnitFormatting.FormatPercent(max)
            : Strings.Format("humidity_from_to_number", UnitFormatting.FormatValue(min), UnitFormatting.FormatPercent(max));
    }

    public static string? GetRangeSummary(DailyCloudCover cloudCover)
    {
        if (cloudCover.Min is not { } min || cloudCover.Max is not { } max)
        {
            return null;
        }

        return min == max
            ? UnitFormatting.FormatPercent(max)
            : Strings.Format("cloud_cover_from_to_number", UnitFormatting.FormatValue(min), UnitFormatting.FormatPercent(max));
    }

    public static string? GetRangeDescriptionSummary(DailyCloudCover cloudCover)
    {
        if (cloudCover.Min is not { } min || cloudCover.Max is not { } max)
        {
            return null;
        }

        string? minDescription = CloudCoverFormatting.GetDescription(min);
        string? maxDescription = CloudCoverFormatting.GetDescription(max);
        return minDescription == maxDescription
            ? maxDescription
            : Strings.Format("cloud_cover_from_to_description", minDescription, maxDescription);
    }

    public static string? GetRangeSummary(DailyDewPoint dewPoint, TemperatureUnit unit, UnitWidth unitWidth = UnitWidth.Narrow)
    {
        if (dewPoint.Min is not { } min || dewPoint.Max is not { } max)
        {
            return null;
        }

        return min == max
            ? UnitFormatting.Format(max, unit, unitWidth: unitWidth)
            : Strings.Format("dew_point_from_to_number",
                UnitFormatting.FormatValue(min, unit), UnitFormatting.Format(max, unit, unitWidth: unitWidth));
    }

    public static string? GetRangeSummary(DailyVisibility visibility, DistanceUnit unit, UnitWidth unitWidth = UnitWidth.Short)
    {
        if (visibility.Min is not { } min || visibility.Max is not { } max)
        {
            return null;
        }

        return min == max
            ? UnitFormatting.Format(max, unit, unitWidth: unitWidth)
            : Strings.Format("visibility_from_to_number",
                UnitFormatting.FormatValue(min, unit), UnitFormatting.Format(max, unit, unitWidth: unitWidth));
    }

    public static string? GetRangeDescriptionSummary(DailyVisibility visibility)
    {
        if (visibility.Min is not { } min || visibility.Max is not { } max)
        {
            return null;
        }

        string? minDescription = VisibilityFormatting.GetDescription(min);
        string? maxDescription = VisibilityFormatting.GetDescription(max);
        return minDescription == maxDescription
            ? maxDescription
            : Strings.Format("visibility_from_to_description", minDescription, maxDescription);
    }
}
