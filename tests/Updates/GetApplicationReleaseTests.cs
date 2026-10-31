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

using LibreccioWeather.Core.Updates;

namespace LibreccioWeather.Core.Tests.Updates;

public class GetApplicationReleaseTests
{
    // Pre-releases keep their order, so that "0.1.0-alpha1" is not read as "0.1.01".
    [Theory]
    [InlineData("0.1.0-alpha1", "v0.1.0-alpha2", true)]
    [InlineData("0.1.0-alpha1", "v0.1.0", true)]
    [InlineData("0.1.0", "v0.1.0-alpha2", false)]
    [InlineData("0.1.0-alpha2", "v0.1.0-alpha1", false)]
    [InlineData("0.1.0-alpha1", "v0.1.0-alpha1", false)]
    [InlineData("0.1.0-alpha1+abcdef12", "v0.1.0-alpha1", false)]
    [InlineData("0.1.0", "v0.2.0-alpha1", true)]
    [InlineData("0.9.0", "v0.10.0", true)]
    [InlineData("0.1.0-alpha.2", "v0.1.0-alpha.10", true)]
    [InlineData("0.1.0", "nightly", false)]
    public void IsNewVersion(string versionName, string versionTag, bool expected)
    {
        Assert.Equal(expected, GetApplicationRelease.IsNewVersion(versionName, versionTag));
    }
}
