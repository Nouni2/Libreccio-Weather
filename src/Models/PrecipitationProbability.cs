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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Models;

public sealed record PrecipitationProbability
{
    public Ratio? Total { get; init; }

    public Ratio? Thunderstorm { get; init; }

    public Ratio? Rain { get; init; }

    public Ratio? Snow { get; init; }

    public Ratio? Ice { get; init; }

    public bool IsValid => Total is { } total && total.Value > 0;
}
