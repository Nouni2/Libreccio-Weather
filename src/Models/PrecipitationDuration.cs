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

public sealed record PrecipitationDuration
{
    public TimeSpan? Total { get; init; }

    public TimeSpan? Thunderstorm { get; init; }

    public TimeSpan? Rain { get; init; }

    public TimeSpan? Snow { get; init; }

    public TimeSpan? Ice { get; init; }
}
