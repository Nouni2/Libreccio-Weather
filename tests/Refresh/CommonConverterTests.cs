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

using LibreccioWeather.Core.Refresh;

namespace LibreccioWeather.Core.Tests.Refresh;

public sealed class CommonConverterTests
{
    [Fact]
    public void GetWindDegree_CardinalAndVariableDirections_ReturnsDegrees()
    {
        Assert.Null(WeatherCompletion.GetWindDegree(null));
        Assert.Equal(90.0, WeatherCompletion.GetWindDegree("E"));
        Assert.Equal(202.5, WeatherCompletion.GetWindDegree("SSO"));
        Assert.Equal(-1.0, WeatherCompletion.GetWindDegree("VR"));
    }
}
