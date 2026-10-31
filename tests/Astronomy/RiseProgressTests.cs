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

using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Tests.Astronomy;

public class RiseProgressTests
{
    private static readonly Location Paris = new() { TimeZone = Location.FindTimeZone("Europe/Paris") };

    // Without a rise or a set, the day runs from 06:00 to 18:00 local time, so local noon is halfway.
    [Fact]
    public void DefaultDayWithoutTimes()
    {
        DateTimeOffset noon = new(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(0.5, RiseProgress.GetRiseProgress(null, Paris, noon));
    }

    // Clocks go from 02:00 to 03:00 on 29 March 2026 in Paris. A rise at 01:30 and a set at 03:30 are one real hour
    // apart but two hours apart on the wall clock, which is what counts: 03:00 is three quarters of the way.
    [Fact]
    public void WallClockAcrossDaylightSavingChange()
    {
        Astro astro = new()
        {
            RiseDate = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero),
            SetDate = new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero),
        };
        DateTimeOffset now = new(2026, 3, 29, 1, 0, 0, TimeSpan.Zero);
        Assert.Equal(0.75, RiseProgress.GetRiseProgress(astro, Paris, now));
    }
}
