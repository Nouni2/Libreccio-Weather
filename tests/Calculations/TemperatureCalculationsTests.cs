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

public class TemperatureCalculationsTests
{
    [Fact]
    public void ComputeHumidex_MissingInput_ReturnsNull()
    {
        Assert.Null(TemperatureCalculations.ComputeHumidex(null, null));
        Assert.Null(TemperatureCalculations.ComputeHumidex(null, Temperature.FromCelsius(13.0)));
        Assert.Null(TemperatureCalculations.ComputeHumidex(Temperature.FromCelsius(20.0), null));
    }

    [Fact]
    public void ComputeHumidex_TemperatureAndDewPoint_ReturnsHumidex()
    {
        Assert.Equal(
            22.8,
            TemperatureCalculations.ComputeHumidex(Temperature.FromCelsius(20.0), Temperature.FromCelsius(13.0))?.InCelsius);
        Assert.Equal(
            52.5,
            TemperatureCalculations.ComputeHumidex(Temperature.FromCelsius(39.0), Temperature.FromCelsius(26.0))?.InCelsius);
    }
}
