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

using LibreccioWeather.Core.Charts;

namespace LibreccioWeather.Core.Tests.Charts;

public sealed class DetailsChartAxesTests
{
    private const long Hour = 3_600_000L;

    // With no room for a single label, the hours between labels are infinite, which Kotlin saturates to the largest
    // integer, so no label is shown. An unchecked cast would give a negative spacing and show every hour.
    [Fact]
    public void GetTopLabelValues_NoRoomForALabel_ShowsNone()
    {
        long[] hours = [.. Enumerable.Range(0, 25).Select(hour => hour * Hour)];

        Assert.Empty(DetailsChartAxes.GetTopLabelValues(hours, 0, 24 * Hour, 10f, 20f));
    }

    // 02:00 does not exist in Paris on 29 March 2026; Java's lenient calendar reads it with the winter offset, which is
    // 03:00 summer time.
    [Fact]
    public void GetHourOfDay_SkippedHour_UsesOffsetBeforeTheChange()
    {
        TimeZoneInfo paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var day = new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.FromHours(1));

        Assert.Equal(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), DetailsChartAxes.GetHourOfDay(day, paris, 2));
    }
}
