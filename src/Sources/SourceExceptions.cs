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

namespace LibreccioWeather.Core.Sources;

/// <summary>
/// The source has no data for the location.
/// </summary>
public sealed class InvalidLocationException : Exception
{
    public InvalidLocationException()
        : base("The source has no data for this location.")
    {
    }
}

/// <summary>
/// The location search failed, as opposed to returning no results.
/// </summary>
public sealed class LocationSearchException : Exception
{
    public LocationSearchException()
        : base("The location search failed.")
    {
    }
}
