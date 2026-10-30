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

public sealed class PercentFormattingTests
{
    [Theory]
    [InlineData("en-US", "80%")]
    [InlineData("fr-FR", "80\u00A0%")]
    public void Format_HalfWithNoDecimals_UsesPercentRoundingAndLocaleSpacing(string culture, string expected)
    {
        Assert.Equal(expected, PercentFormatting.Format(80.5, 0, CultureInfo.GetCultureInfo(culture)));
    }

    [Theory]
    [InlineData(12.0, "12%")]
    [InlineData(12.3, "12.3%")]
    [InlineData(-1.0, "0%")]
    [InlineData(double.NaN, "0%")]
    [InlineData(double.NegativeInfinity, "0%")]
    [InlineData(double.PositiveInfinity, "∞%")]
    public void Format_MaximumFractionDigits_DropsPaddingAndClampsNegativeValues(double value, string expected)
    {
        Assert.Equal(expected, PercentFormatting.Format(value, 1, CultureInfo.GetCultureInfo("en-US")));
    }
}
