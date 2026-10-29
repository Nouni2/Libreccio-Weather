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

public class TemperatureTests
{
    [Fact]
    public void InFahrenheit_FromReferenceUnit_Converts()
    {
        Assert.Equal(32.0, Temperature.FromDeciCelsius(0).InFahrenheit);
        Assert.Equal(50.0, Temperature.FromDeciCelsius(100).InFahrenheit);
    }

    [Fact]
    public void FromFahrenheit_ToReferenceUnit_Converts()
    {
        Assert.Equal(0, Temperature.FromFahrenheit(32.0).Value);
        Assert.Equal(100, Temperature.FromFahrenheit(50.0).Value);
    }

    [Fact]
    public void InFahrenheit_FromCelsius_Converts()
    {
        Assert.Equal(32.0, Temperature.FromCelsius(0).InFahrenheit);
        Assert.Equal(50.0, Temperature.FromCelsius(10).InFahrenheit);
    }

    [Fact]
    public void InKelvins_FromCelsius_Converts()
    {
        Assert.Equal(273.15, Temperature.FromCelsius(0).InKelvins);
        Assert.Equal(283.15, Temperature.FromCelsius(10).InKelvins);
    }

    [Fact]
    public void GetDefaultUnit_UnitedStates_ReturnsFahrenheit()
    {
        Assert.Same(TemperatureUnit.Fahrenheit, TemperatureUnit.GetDefaultUnit("US"));
    }

    [Fact]
    public void GetUnit_Id_ReturnsUnit()
    {
        Assert.Same(TemperatureUnit.Celsius, TemperatureUnit.GetUnit("c"));
        Assert.Same(TemperatureUnit.Fahrenheit, TemperatureUnit.GetUnit("f"));
        Assert.Same(TemperatureUnit.Kelvin, TemperatureUnit.GetUnit("k"));
    }

    // Stored values round half towards positive infinity. Math.Round would round half to even instead.
    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -2)]
    [InlineData(-2.6, -3)]
    public void FromDeciCelsius_Half_RoundsTowardsPositiveInfinity(double value, long expected)
    {
        Assert.Equal(expected, Temperature.FromDeciCelsius(value).Value);
    }
}
