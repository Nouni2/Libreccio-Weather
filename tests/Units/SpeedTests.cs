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

namespace LibreccioWeather.Core.Tests.Units;

// The Beaufort conversion walks a threshold table instead of listing every range, so its bounds are checked here.
public class SpeedTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(29, 0)]
    [InlineData(30, 1)]
    [InlineData(339, 2)]
    [InlineData(340, 3)]
    [InlineData(3269, 11)]
    [InlineData(3270, 12)]
    [InlineData(10000, 12)]
    public void InBeaufort_CentimetersPerSecond_ReturnsForce(long centimetersPerSecond, int expected)
    {
        Assert.Equal(expected, Speed.FromCentimetersPerSecond(centimetersPerSecond).InBeaufort);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 30)]
    [InlineData(3, 340)]
    [InlineData(12, 3270)]
    [InlineData(15, 3270)]
    public void FromBeaufort_Force_ReturnsLowestSpeedOfForce(long force, long expected)
    {
        Assert.Equal(expected, Speed.FromBeaufort(force).Value);
    }
}
