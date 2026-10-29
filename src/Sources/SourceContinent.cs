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
/// The area a weather source covers.
/// </summary>
public enum SourceContinent
{
    Worldwide,
    Africa,
    Asia,
    Europe,
    NorthAmerica,
    Oceania,
    SouthAmerica,
}

public static class SourceContinentExtensions
{
    extension(SourceContinent continent)
    {
        /// <summary>
        /// Identifier used when the continent is stored.
        /// </summary>
        public string Id => continent switch
        {
            SourceContinent.Worldwide => "worldwide",
            SourceContinent.Africa => "africa",
            SourceContinent.Asia => "asia",
            SourceContinent.Europe => "europe",
            SourceContinent.NorthAmerica => "north_america",
            SourceContinent.Oceania => "oceania",
            SourceContinent.SouthAmerica => "south_america",
            _ => throw new ArgumentOutOfRangeException(nameof(continent), continent, null),
        };
    }

    extension(SourceContinent)
    {
        public static SourceContinent? GetInstance(string value)
        {
            foreach (SourceContinent continent in Enum.GetValues<SourceContinent>())
            {
                if (continent.Id == value)
                {
                    return continent;
                }
            }

            return null;
        }
    }
}
