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

using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.Core.Tests.Settings;

public class OptionNamesTests
{
    [Fact]
    public void GetNameByValue_Value_ReturnsNameAtSamePosition()
    {
        string[] names = ["Automatic", "Follow system", "Always light", "Always dark"];
        string[] values = ["auto", "system", "light", "dark"];

        Assert.Equal("Automatic", OptionNames.GetNameByValue("auto", names, values));
    }
}
