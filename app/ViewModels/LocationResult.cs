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

using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// A search result as listed: the place name, with its administrative areas below.
/// </summary>
public sealed record LocationResult(Location Location)
{
    public string Place => Location.GetPlace();

    public string AdministrationLevels => Location.AdministrationLevels();
}
