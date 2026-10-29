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

using System.Net.NetworkInformation;

namespace LibreccioWeather.Core.Refresh;

public static class NetworkStatus
{
    // True whenever any network adapter is up, including virtual and VPN adapters, so most offline cases pass.
    public static bool IsOnline()
    {
        return NetworkInterface.GetIsNetworkAvailable();
    }
}
