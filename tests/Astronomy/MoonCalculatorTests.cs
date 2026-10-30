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

using LibreccioWeather.Core.Astronomy;

namespace LibreccioWeather.Core.Tests.Astronomy;

/// <summary>
/// Rise and set times are compared at minute resolution.
/// </summary>
public sealed class MoonCalculatorTests
{
    private const double Error = 0.1;

    [Theory]
    [InlineData(50.938056, 6.956944, 2017, 7, 12, "2017-07-12T21:26:00Z", "2017-07-12T06:54:00Z")]
    [InlineData(-41.2875, 174.776111, 2017, 7, 12, "2017-07-12T08:06:00Z", "2017-07-12T21:58:00Z")]
    [InlineData(-54.933333, -67.616667, 2017, 7, 13, "2017-07-13T00:31:00Z", "2017-07-13T14:49:00Z")]
    [InlineData(1.283333, 103.833333, 2017, 7, 13, "2017-07-13T14:35:00Z", "2017-07-13T02:09:00Z")]
    public void GetTimes_FromUtcMidnight_FindsRiseAndSet(
        double latitude, double longitude, int year, int month, int day, string rise, string set)
    {
        MoonTimes times = MoonCalculator.GetTimes(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero), latitude, longitude);

        Assert.Equal(DateTimeOffset.Parse(rise), times.Rise);
        Assert.Equal(DateTimeOffset.Parse(set), times.Set);
        Assert.False(times.IsAlwaysUp);
        Assert.False(times.IsAlwaysDown);
    }

    // At Alert the Moon stays below the horizon on 12 July 2017, and a full cycle finds its next rise and set.
    [Fact]
    public void GetTimes_Alert_AlwaysDownThenFullCycle()
    {
        var start = new DateTimeOffset(2017, 7, 12, 0, 0, 0, TimeSpan.Zero);

        MoonTimes oneDay = MoonCalculator.GetTimes(start, 82.5, -62.316667);
        MoonTimes fullCycle = MoonCalculator.GetTimes(start, 82.5, -62.316667, fullCycle: true);

        Assert.True(oneDay.IsAlwaysDown);
        Assert.False(oneDay.IsAlwaysUp);
        Assert.Equal(DateTimeOffset.Parse("2017-07-14T05:45:00Z"), fullCycle.Rise);
        Assert.Equal(DateTimeOffset.Parse("2017-07-14T11:27:00Z"), fullCycle.Set);
        Assert.True(fullCycle.IsAlwaysDown);
    }

    [Fact]
    public void GetTimes_Alert_AlwaysUp()
    {
        MoonTimes times = MoonCalculator.GetTimes(new DateTimeOffset(2017, 7, 18, 0, 0, 0, TimeSpan.Zero), 82.5, -62.316667);

        Assert.True(times.IsAlwaysUp);
        Assert.False(times.IsAlwaysDown);
    }

    // Times are in Cologne, two hours ahead of UTC in summer.
    [Theory]
    [InlineData("2017-06-24T04:30:00+02:00", 0.0, 176.0)]
    [InlineData("2017-07-01T02:51:00+02:00", 0.5, -90.0)]
    [InlineData("2017-07-09T06:06:00+02:00", 1.0, -3.2)]
    [InlineData("2017-07-16T21:25:00+02:00", 0.5, 90.0)]
    public void GetIllumination_Phases_MatchReference(string instant, double fraction, double phase)
    {
        MoonIllumination illumination = MoonCalculator.GetIllumination(DateTimeOffset.Parse(instant));

        Assert.InRange(illumination.Fraction, fraction - Error, fraction + Error);
        Assert.InRange(illumination.Phase, phase - Error, phase + Error);
    }

    [Theory]
    [InlineData(50.938056, 6.956944, "2017-07-12T13:28:00+02:00", -39.6)]
    [InlineData(50.938056, 6.956944, "2017-07-12T03:51:00+02:00", 25.3)]
    [InlineData(82.5, -62.316667, "2017-07-12T08:04:00-04:00", -10.9)]
    [InlineData(82.5, -62.316667, "2017-07-12T02:37:00-04:00", -5.7)]
    public void GetAltitude_MatchesReference(double latitude, double longitude, string instant, double altitude)
    {
        Assert.InRange(MoonCalculator.GetAltitude(DateTimeOffset.Parse(instant), latitude, longitude), altitude - Error, altitude + Error);
    }
}
