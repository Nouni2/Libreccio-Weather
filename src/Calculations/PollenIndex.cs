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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Calculations;

public sealed class PollenIndex
{
    private static readonly int[] IndexThresholds = [0, 25, 50, 75, 100];

    public static readonly PollenIndex Alder = new("pollen_alnus", [0, 10, 60, 100, 500], pollen => pollen.Alder);
    public static readonly PollenIndex Ash = new("pollen_fraxinus", [0, 30, 100, 200, 400], pollen => pollen.Ash);
    public static readonly PollenIndex Birch = new("pollen_betula", [0, 10, 60, 100, 500], pollen => pollen.Birch);
    public static readonly PollenIndex Chestnut = new("pollen_castanea", [0, 1, 2, 3, 4], pollen => pollen.Chestnut);
    public static readonly PollenIndex Cypress = new("pollen_cupressaceae_taxaceae", [0, 1, 2, 3, 4], pollen => pollen.Cypress);
    public static readonly PollenIndex Grass = new("pollen_poaeceae", [0, 3, 30, 50, 250], pollen => pollen.Grass);
    public static readonly PollenIndex Hazel = new("pollen_corylus", [0, 1, 2, 3, 4], pollen => pollen.Hazel);
    public static readonly PollenIndex Hornbeam = new("pollen_carpinus", [0, 1, 2, 3, 4], pollen => pollen.Hornbeam);
    public static readonly PollenIndex Linden = new("pollen_tilia", [0, 1, 2, 3, 4], pollen => pollen.Linden);
    public static readonly PollenIndex Mold = new("pollen_mold", [0, 6500, 13000, 50000, 65000], pollen => pollen.Mold);
    public static readonly PollenIndex Mugwort = new("pollen_artemisia", [0, 3, 30, 50, 250], pollen => pollen.Mugwort);
    public static readonly PollenIndex Oak = new("pollen_quercus", [0, 50, 100, 200, 400], pollen => pollen.Oak);
    public static readonly PollenIndex Olive = new("pollen_olea", [0, 20, 100, 200, 500], pollen => pollen.Olive);
    public static readonly PollenIndex Plane = new("pollen_platanus", [0, 1, 2, 3, 4], pollen => pollen.Plane);
    public static readonly PollenIndex Plantain = new("pollen_plantaginaceae", [0, 1, 2, 3, 4], pollen => pollen.Plantain);
    public static readonly PollenIndex Poplar = new("pollen_populus", [0, 1, 2, 3, 4], pollen => pollen.Poplar);
    public static readonly PollenIndex Ragweed = new("pollen_ambrosia", [0, 3, 30, 50, 250], pollen => pollen.Ragweed);
    public static readonly PollenIndex Sorrel = new("pollen_rumex", [0, 1, 2, 3, 4], pollen => pollen.Sorrel);
    public static readonly PollenIndex Tree = new("pollen_tree", [0, 10, 50, 100, 300], pollen => pollen.Tree);
    public static readonly PollenIndex Urticaceae = new("pollen_urticaceae", [0, 1, 2, 3, 4], pollen => pollen.Urticaceae);
    public static readonly PollenIndex Willow = new("pollen_salix", [0, 1, 2, 3, 4], pollen => pollen.Willow);

    public static IReadOnlyList<PollenIndex> Entries { get; } =
        [Alder, Ash, Birch, Chestnut, Cypress, Grass, Hazel, Hornbeam, Linden, Mold, Mugwort, Oak, Olive, Plane, Plantain, Poplar, Ragweed, Sorrel, Tree, Urticaceae, Willow];

    private readonly int[] _thresholds;
    private readonly Func<Pollen, PollenConcentration?> _getConcentration;

    private PollenIndex(string nameKey, int[] thresholds, Func<Pollen, PollenConcentration?> getConcentration)
    {
        NameKey = nameKey;
        _thresholds = thresholds;
        _getConcentration = getConcentration;
    }

    public string NameKey { get; }

    public PollenConcentration? GetConcentration(Pollen pollen)
    {
        return _getConcentration(pollen);
    }

    public int? GetIndex(double? concentration)
    {
        if (concentration is not { } value)
        {
            return null;
        }

        int level = Array.FindLastIndex(_thresholds, threshold => value >= threshold);
        if (level < 0)
        {
            return 0;
        }

        double index = level < _thresholds.Length - 1
            ? ((double)IndexThresholds[level + 1] - IndexThresholds[level])
                / ((double)_thresholds[level + 1] - _thresholds[level]) * (value - _thresholds[level]) + IndexThresholds[level]
            : value * IndexThresholds[^1] / _thresholds[^1];
        return (int)Math.Clamp(UnitRounding.RoundToLong(index), int.MinValue, int.MaxValue);
    }

    /// <summary>
    /// The name of a pollen index level, with its own name for a negligible index of zero.
    /// </summary>
    public static string? GetPollenIndexToName(int? index)
    {
        if (index is null)
        {
            return null;
        }

        if (index == 0)
        {
            return Strings.Get("pollen_level_0");
        }

        return GetPollenIndexToLevel(index) is { } level ? Strings.GetArray("pollen_levels").ElementAtOrDefault(level) : null;
    }

    public static int? GetPollenIndexToLevel(int? index)
    {
        if (index is not { } value)
        {
            return null;
        }

        int level = Array.FindLastIndex(IndexThresholds, threshold => value >= threshold);
        return level >= 0 ? level : null;
    }
}
