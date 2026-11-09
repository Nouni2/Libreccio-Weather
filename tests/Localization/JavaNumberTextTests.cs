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

public sealed class JavaNumberTextTests
{
    // .NET writes 48 and 0.0001 where a Kotlin string template writes "48.0" and "1.0E-4".
    [Theory]
    [InlineData(48.0, "48.0")]
    [InlineData(2.3522, "2.3522")]
    [InlineData(-0.0, "-0.0")]
    [InlineData(0.0001, "1.0E-4")]
    [InlineData(-0.00012, "-1.2E-4")]
    [InlineData(12345678.0, "1.2345678E7")]
    public void Format_WholeAndSmallNumbers_MatchesJavaDoubleToString(double value, string expected)
    {
        Assert.Equal(expected, JavaNumberText.Format(value));
    }
}
