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

public sealed class QuantityConversionTests
{
    [Theory]
    [InlineData(double.PositiveInfinity, long.MaxValue)]
    [InlineData(double.NegativeInfinity, long.MinValue)]
    [InlineData(9223372036854775808.0, long.MaxValue)]
    [InlineData(-9223372036854775808.0, long.MinValue)]
    [InlineData(4503599627370497.0, 4503599627370497L)]
    [InlineData(0.49999999999999994, 0L)]
    [InlineData(2.5, 3L)]
    [InlineData(-2.5, -2L)]
    public void FromDouble_ConversionBoundaries_RoundsAndSaturates(double value, long expected)
    {
        Assert.Equal(expected, Temperature.FromDeciCelsius(value).Value);
        Assert.Equal(expected, Distance.FromMeters(value).Value);
        Assert.Equal(expected, Precipitation.FromMicrometers(value).Value);
        Assert.Equal(expected, Pressure.FromPascals(value).Value);
        Assert.Equal(expected, Ratio.FromPermille(value).Value);
        Assert.Equal(expected, Speed.FromCentimetersPerSecond(value).Value);
        Assert.Equal(expected, PollutantConcentration.FromMicrogramsPerCubicMeter(value).Value);
        Assert.Equal(expected, PollenConcentration.FromPerCubicMeter(value).Value);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void FromLong_ConversionExceedsStorageRange_Saturates(long value)
    {
        Assert.Equal(value, Temperature.FromCelsius(value).Value);
        Assert.Equal(value, Distance.FromKilometers(value).Value);
        Assert.Equal(value, Precipitation.FromMillimeters(value).Value);
        Assert.Equal(value, Pressure.FromHectopascals(value).Value);
        Assert.Equal(value, Ratio.FromPercent(value).Value);
        Assert.Equal(value, Speed.FromMetersPerSecond(value).Value);
        Assert.Equal(value, PollutantConcentration.FromMilligramsPerCubicMeter(value).Value);
    }

    [Fact]
    public void FromDouble_NaN_RejectsValue()
    {
        Assert.Throws<ArgumentException>(() => Temperature.FromDeciCelsius(double.NaN));
        Assert.Throws<ArgumentException>(() => Distance.FromMeters(double.NaN));
        Assert.Throws<ArgumentException>(() => Precipitation.FromMicrometers(double.NaN));
        Assert.Throws<ArgumentException>(() => Pressure.FromPascals(double.NaN));
        Assert.Throws<ArgumentException>(() => Ratio.FromPermille(double.NaN));
        Assert.Throws<ArgumentException>(() => Speed.FromCentimetersPerSecond(double.NaN));
        Assert.Throws<ArgumentException>(() => PollutantConcentration.FromMicrogramsPerCubicMeter(double.NaN));
        Assert.Throws<ArgumentException>(() => PollenConcentration.FromPerCubicMeter(double.NaN));
    }

    [Fact]
    public void InBeaufort_MaximumStoredSpeed_ReturnsHighestForce()
    {
        Assert.Equal(12, Speed.FromCentimetersPerSecond(long.MaxValue).InBeaufort);
    }
}
