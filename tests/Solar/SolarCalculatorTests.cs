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

using LibreccioWeather.Core.Solar;

namespace LibreccioWeather.Core.Tests.Solar;

/// <summary>
/// Reference solar days, read through the managed layer: polar days and nights, events on the neighbouring UTC days,
/// both poles and a far date. .NET dates start in year 1, so no earlier day can be checked.
/// </summary>
public sealed class SolarCalculatorTests
{
    // Another platform's math library can move a result across a millisecond boundary.
    private const long ToleranceMs = 1;

    public static IEnumerable<object?[]> ReferenceDays =>
    [
        // Paris, June solstice
        [48.8566, 2.3522, 35.0, 2026, 6, 21, SolarDayType.Normal, 1782042744423L, 1782013616697L, 1782071871872L, 1782011056756L, 1782074431729L, 16.181993061],
        // Paris, March equinox
        [48.8566, 2.3522, 35.0, 2026, 3, 20, SolarDayType.Normal, 1774007881624L, 1773986021150L, 1774029797110L, 1773984136459L, 1774031687296L, 12.159988871],
        // Reykjavík, with its sunset on the next UTC day and no civil twilight
        [64.1466, -21.9426, 20.0, 2024, 6, 15, SolarDayType.Normal, 1718458104453L, 1718420218474L, 1718496040832L, null, null, 21.061766193],
        // Tromsø, polar day
        [69.6492, 18.9553, 10.0, 2026, 6, 21, SolarDayType.PolarDay, 1782038759075L, null, null, null, null, 24.0],
        // Tromsø, polar night with civil twilight
        [69.6492, 18.9553, 10.0, 2026, 12, 21, SolarDayType.PolarNight, 1797849733000L, null, null, 1797841875300L, 1797857589881L, 0.0],
        // 70° N next to polar day, with a sunset but no sunrise
        [70.0, 0.0, 0.0, 2026, 7, 27, SolarDayType.Normal, 1785153993337L, null, 1785195507988L, null, null, 23.531847541],
        // Sydney, with its sunrise on the previous UTC day
        [-33.8688, 151.2093, 58.0, 2026, 12, 21, SolarDayType.Normal, 1797817981123L, 1797792036662L, 1797843926345L, 1797790289086L, 1797845674060L, 14.413800903],
        // Kiritimati, UTC+14
        [1.8721, -157.3630, 0.0, 2026, 3, 20, SolarDayType.Normal, 1774046205426L, 1774024408164L, 1774068004236L, 1774023169767L, 1774069242745L, 12.110020056],
        // Apia, UTC+13
        [-13.8333, -171.7667, 2.0, 2026, 6, 21, SolarDayType.Normal, 1782084539289L, 1782064181228L, 1782104897485L, 1782062791111L, 1782106287586L, 11.310071619],
        // Quito, at 2850 m
        [-0.1807, -78.4678, 2850.0, 2026, 9, 23, SolarDayType.Normal, 1790183170115L, 1790161420369L, 1790204920028L, 1790160136173L, 1790206204245L, 12.083238578],
        // North Pole, setting before transit
        [90.0, 0.0, 0.0, 2026, 9, 25, SolarDayType.Normal, 1790337100511L, null, 1790307787675L, null, null, 3.857545683],
        // South Pole, rising after transit
        [-90.0, 0.0, 0.0, 2026, 9, 20, SolarDayType.Normal, 1789905206168L, 1789935907166L, null, null, null, 3.471945106],
        // Paris, 2999
        [48.8566, 2.3522, 35.0, 2999, 12, 21, SolarDayType.Normal, 32502772009633L, 32502757115100L, 32502786903746L, 32502754885503L, 32502789133326L, 8.274624053],
    ];

    // Every reference transit falls on its UTC date, so reading the dates in UTC selects the same days.
    [Theory]
    [MemberData(nameof(ReferenceDays))]
    public void GetSolarDay_ReferenceDays_MatchSpp(
        double latitude, double longitude, double elevation, int year, int month, int day, SolarDayType type,
        long transit, long? rise, long? set, long? dawn, long? dusk, double durationHours)
    {
        Assert.True(SolarCalculator.IsAvailable, "spp.dll is not loaded.");

        SolarDay? result = SolarCalculator.GetSolarDay(
            latitude, longitude, new DateOnly(year, month, day), TimeZoneInfo.Utc, elevation);

        Assert.NotNull(result);
        Assert.Equal(type, result.Type);
        AssertInstant(transit, result.Transit);
        AssertInstant(rise, result.Rise);
        AssertInstant(set, result.Set);
        AssertInstant(dawn, result.CivilDawn);
        AssertInstant(dusk, result.CivilDusk);
        Assert.Equal(durationHours, result.DurationHours, 1e-6);
    }

    // Kiritimati is 14 hours ahead of UTC: its local 21 March is the solar day of the UTC date 20 March.
    [Fact]
    public void GetSolarDay_LocalDateAheadOfUtc_SelectsTheDayWhoseTransitFallsOnIt()
    {
        TimeZoneInfo kiritimati = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Kiritimati");

        SolarDay? result = SolarCalculator.GetSolarDay(1.8721, -157.3630, new DateOnly(2026, 3, 21), kiritimati);

        Assert.NotNull(result);
        AssertInstant(1774046205426, result.Transit);
    }

    // The batch call and the definition of sunrise agree: the upper limb is at the horizon at sunrise and sunset.
    [Fact]
    public void GetUpperLimbAltitudes_AtSunriseAndSunset_IsZero()
    {
        DateTimeOffset[] instants =
        [
            DateTimeOffset.FromUnixTimeMilliseconds(1782013616697),
            DateTimeOffset.FromUnixTimeMilliseconds(1782071871872),
        ];

        double[]? altitudes = SolarCalculator.GetUpperLimbAltitudes(48.8566, 2.3522, instants, 35);

        Assert.NotNull(altitudes);
        Assert.All(altitudes, altitude => Assert.InRange(altitude, -1e-5, 1e-5));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(3001, 1, 1)]
    [InlineData(9999, 12, 31)]
    public void GetSolarDay_UnrepresentableOrUnsupportedDate_ReturnsNull(int year, int month, int day)
    {
        SolarDay? result = SolarCalculator.GetSolarDay(0, 0, new DateOnly(year, month, day), TimeZoneInfo.Utc);

        Assert.Null(result);
    }

    private static void AssertInstant(long? expected, DateTimeOffset? actual)
    {
        if (expected is not { } value)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.InRange(actual.Value.ToUnixTimeMilliseconds(), value - ToleranceMs, value + ToleranceMs);
    }
}
