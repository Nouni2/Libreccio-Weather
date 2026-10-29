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

namespace LibreccioWeather.Core.Seasons;

/// <summary>
/// Meteorological seasons, which follow whole months: winter is December to February, spring March to May,
/// summer June to August and autumn September to November in the northern hemisphere.
/// </summary>
public static class MeteorologicalSeasons
{
    /// <summary>
    /// The season on a date at a latitude. Seasons are reversed below the equator; the equator itself counts as northern.
    /// </summary>
    public static Season Get(DateOnly date, double latitude)
    {
        Season northern = date.Month switch
        {
            12 or 1 or 2 => Season.Winter,
            3 or 4 or 5 => Season.Spring,
            6 or 7 or 8 => Season.Summer,
            _ => Season.Autumn,
        };

        if (latitude >= 0.0)
        {
            return northern;
        }

        return northern switch
        {
            Season.Winter => Season.Summer,
            Season.Spring => Season.Autumn,
            Season.Summer => Season.Winter,
            _ => Season.Spring,
        };
    }
}
