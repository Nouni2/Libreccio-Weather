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

public sealed class TrendCoordinatesTests
{
    [Theory]
    [InlineData(5f, 0)]
    [InlineData(6f, int.MinValue)]
    [InlineData(4f, int.MaxValue)]
    public void ComputeSingleCoordinate_EqualBounds_UsesSaturatingFloatConversion(float value, int expected)
    {
        Assert.Equal(expected, TrendCoordinates.ComputeSingleCoordinate(100, 24, 36, value, 5f, 5f));
    }
}
