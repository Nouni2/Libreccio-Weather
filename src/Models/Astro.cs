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

namespace LibreccioWeather.Core.Models;

/// <summary>
/// Rise and set times of the sun or the moon, or the start and end of twilight.
/// </summary>
public sealed record Astro
{
    public DateTimeOffset? RiseDate { get; init; }

    public DateTimeOffset? SetDate { get; init; }

    /// <summary>
    /// Time between rise and set. Only meaningful for the sun: a missing rise or set means polar night, so zero.
    /// Null when the rise comes after the set.
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (RiseDate is not { } rise || SetDate is not { } set)
            {
                return TimeSpan.Zero;
            }

            return rise > set ? null : set - rise;
        }
    }

    public bool IsValid => RiseDate is not null && SetDate is not null;
}
