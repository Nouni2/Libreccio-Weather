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
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Tests.Calculations;

public sealed class AirQualityIndexTests
{
    [Fact]
    public void GetIndex_ConcentrationBeyondIntRange_Saturates()
    {
        AirQuality airQuality = new() { PM25 = PollutantConcentration.FromMicrogramsPerCubicMeter(10_000_000_000L) };
        Assert.Equal(int.MaxValue, AirQualityIndex.GetIndex(airQuality));
    }
}
