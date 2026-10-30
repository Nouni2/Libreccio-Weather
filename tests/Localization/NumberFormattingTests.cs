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

using System.Globalization;
using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Core.Tests.Localization;

public class NumberFormattingTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void Format_Decimals_UsesFrenchNumberFormatting()
    {
        Assert.Equal("7,01", NumberFormatting.Format(7.00646, 2, French));
        Assert.Equal("14,34", NumberFormatting.Format(14.34234, 2, French));
        Assert.Equal("14,35", NumberFormatting.Format(14.34834, 2, French));
        Assert.Equal("14,348", NumberFormatting.Format(14.34834, 3, French));
        Assert.Equal("14,349", NumberFormatting.Format(14.34864, 3, French));
    }

    [Fact]
    public void Format_Decimals_DropsTrailingZeros()
    {
        Assert.Equal("7", NumberFormatting.Format(7.00246, 2, English));
    }

    [Fact]
    public void Format_Int_HasNoDecimals()
    {
        Assert.Equal("14", NumberFormatting.Format(14, 0, English));
    }

    [Fact]
    public void Format_ShowSign_AddsLeadingSign()
    {
        Assert.Equal("+7.3", NumberFormatting.Format(7.3, 1, English, showSign: true));
        Assert.Equal("-7.3", NumberFormatting.Format(-7.3, 1, English, showSign: true));
    }

    // Kotlin's roundToInt() rounds halves towards positive infinity, where Math.Round would give -3.
    [Theory]
    [InlineData(-2.5, "-2")]
    [InlineData(2.5, "3")]
    [InlineData(0.49999999999999994, "0")]
    [InlineData(double.PositiveInfinity, "2,147,483,647")]
    [InlineData(double.NegativeInfinity, "-2,147,483,648")]
    public void Format_NoDecimalsHalf_RoundsTowardsPositiveInfinity(double value, string expected)
    {
        Assert.Equal(expected, NumberFormatting.Format(value, 0, English));
    }

    [Fact]
    public void Format_NoDecimalsNaN_Throws()
    {
        Assert.Throws<ArgumentException>(() => NumberFormatting.Format(double.NaN, 0, English));
    }

    [Fact]
    public void Format_AboveDecimalRange_PreservesNumber()
    {
        Assert.Equal("1,000,000,000,000,000,000,000,000,000,000", NumberFormatting.Format(1e30, 1, English));
    }
}
