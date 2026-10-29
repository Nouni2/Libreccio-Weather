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

/// <summary>
/// Tree, grass and weed pollen concentrations, and mold.
/// </summary>
public sealed record Pollen
{
    public PollenConcentration? Alder { get; init; }

    public PollenConcentration? Ash { get; init; }

    public PollenConcentration? Birch { get; init; }

    public PollenConcentration? Chestnut { get; init; }

    public PollenConcentration? Cypress { get; init; }

    public PollenConcentration? Grass { get; init; }

    public PollenConcentration? Hazel { get; init; }

    public PollenConcentration? Hornbeam { get; init; }

    public PollenConcentration? Linden { get; init; }

    // Mold is not a pollen, but is reported alongside them.
    public PollenConcentration? Mold { get; init; }

    public PollenConcentration? Mugwort { get; init; }

    public PollenConcentration? Oak { get; init; }

    public PollenConcentration? Olive { get; init; }

    public PollenConcentration? Plane { get; init; }

    public PollenConcentration? Plantain { get; init; }

    public PollenConcentration? Poplar { get; init; }

    public PollenConcentration? Ragweed { get; init; }

    public PollenConcentration? Sorrel { get; init; }

    public PollenConcentration? Tree { get; init; }

    public PollenConcentration? Urticaceae { get; init; }

    public PollenConcentration? Willow { get; init; }

    public bool IsMoldValid => Mold is not null;

    public bool IsValid =>
        Alder is not null ||
        Ash is not null ||
        Birch is not null ||
        Chestnut is not null ||
        Cypress is not null ||
        Grass is not null ||
        Hazel is not null ||
        Hornbeam is not null ||
        Linden is not null ||
        Mold is not null ||
        Mugwort is not null ||
        Oak is not null ||
        Olive is not null ||
        Plane is not null ||
        Plantain is not null ||
        Poplar is not null ||
        Ragweed is not null ||
        Sorrel is not null ||
        Tree is not null ||
        Urticaceae is not null ||
        Willow is not null;

    public Pollen ToValid()
    {
        return this with
        {
            Alder = Alder?.ToValidOrNull(),
            Ash = Ash?.ToValidOrNull(),
            Birch = Birch?.ToValidOrNull(),
            Chestnut = Chestnut?.ToValidOrNull(),
            Cypress = Cypress?.ToValidOrNull(),
            Grass = Grass?.ToValidOrNull(),
            Hazel = Hazel?.ToValidOrNull(),
            Hornbeam = Hornbeam?.ToValidOrNull(),
            Linden = Linden?.ToValidOrNull(),
            Mold = Mold?.ToValidOrNull(),
            Mugwort = Mugwort?.ToValidOrNull(),
            Oak = Oak?.ToValidOrNull(),
            Olive = Olive?.ToValidOrNull(),
            Plane = Plane?.ToValidOrNull(),
            Plantain = Plantain?.ToValidOrNull(),
            Poplar = Poplar?.ToValidOrNull(),
            Ragweed = Ragweed?.ToValidOrNull(),
            Sorrel = Sorrel?.ToValidOrNull(),
            Tree = Tree?.ToValidOrNull(),
            Urticaceae = Urticaceae?.ToValidOrNull(),
            Willow = Willow?.ToValidOrNull(),
        };
    }
}
