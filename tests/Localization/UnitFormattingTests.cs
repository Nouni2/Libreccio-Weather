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

using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Core.Tests.Localization;

public sealed class UnitFormattingTests
{
    [Theory]
    [InlineData(-2.5, -2)]
    [InlineData(2.5, 3)]
    [InlineData(2147483648.0, int.MaxValue)]
    [InlineData(-2147483649.0, int.MinValue)]
    [InlineData(double.PositiveInfinity, int.MaxValue)]
    [InlineData(double.NegativeInfinity, int.MinValue)]
    public void RoundToInt_HalvesAndOutOfRangeValues_UsesKotlinRounding(double value, int expected)
    {
        Assert.Equal(expected, UnitFormatting.RoundToInt(value));
    }

    [Fact]
    public void RoundToInt_NaN_Throws()
    {
        Assert.Throws<ArgumentException>(() => UnitFormatting.RoundToInt(double.NaN));
    }
}
