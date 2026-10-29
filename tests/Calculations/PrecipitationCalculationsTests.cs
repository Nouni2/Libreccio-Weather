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

using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Tests.Calculations;

public class PrecipitationCalculationsTests
{
    [Fact]
    public void ComputeTotalPrecipitation_NoInput_ReturnsNull()
    {
        Assert.Null(PrecipitationCalculations.ComputeTotalPrecipitation(null, null, null, null));
    }

    [Theory]
    [InlineData(2.0, 20.0, 1.0, null, 30.0)]
    [InlineData(0.0, null, 3.0, null, 10.0)]
    [InlineData(0.0, null, null, 2.0, 18.4)]
    [InlineData(0.0, 50.0, 6.0, 1.0, 79.2)]
    [InlineData(-1.0, null, 5.0, null, 10.0)]
    [InlineData(-6.0, null, 4.5, null, 5.0)]
    public void ComputeTotalPrecipitation_RainSnowIce_ReturnsLiquidEquivalent(
        double temperatureCelsius,
        double? rainMillimeters,
        double? snowCentimeters,
        double? iceCentimeters,
        double expectedMillimeters)
    {
        Precipitation? total = PrecipitationCalculations.ComputeTotalPrecipitation(
            Temperature.FromCelsius(temperatureCelsius),
            rainMillimeters is { } rain ? Precipitation.FromMillimeters(rain) : null,
            snowCentimeters is { } snow ? Precipitation.FromCentimeters(snow) : null,
            iceCentimeters is { } ice ? Precipitation.FromCentimeters(ice) : null);

        Assert.Equal(Precipitation.FromMillimeters(expectedMillimeters), total);
    }
}
