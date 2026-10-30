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

using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    // Open-Meteo gives concentrations without an index of its own, so every pollen uses the standard thresholds.
    private static void BuildPollen(DetailsContext context, List<object> blocks)
    {
        Pollen? pollen = context.Daily.Pollen;
        bool isIndexValid = pollen is not null
            && PollenIndex.Entries.Any(type => type.GetIndex(type.GetConcentration(pollen)?.InPerCubicMeter) is not null);
        if (pollen is not null && isIndexValid)
        {
            PollenIndex? primary = null;
            int primaryIndex = 0;
            foreach (PollenIndex type in PollenIndex.Entries.Where(type => type != PollenIndex.Mold))
            {
                if (type.GetIndex(type.GetConcentration(pollen)?.InPerCubicMeter) is { } index && index > 0 && index > primaryIndex)
                {
                    primary = type;
                    primaryIndex = index;
                }
            }

            if (primary is not null)
            {
                blocks.Add(new DetailsSectionHeader(Text(Strings.Get("pollen_primary")), Text(Strings.Get(primary.NameKey))));
                blocks.Add(new DetailsSpacer(NormalMargin));
            }

            blocks.Add(new DetailsSectionHeader(Text(Strings.Get("pollen_details"))));
            StringComparer collator = StringComparer.Create(Strings.Culture, ignoreCase: false);
            blocks.Add(new DetailsPollenGrid([.. PollenIndex.Entries
                .Where(type => type.GetConcentration(pollen) is not null)
                .OrderBy(type => Strings.Get(type.NameKey), collator)
                .Select(type =>
                {
                    Core.Units.PollenConcentration concentration = type.GetConcentration(pollen)!.Value;
                    int? index = type.GetIndex(concentration.InPerCubicMeter);
                    return new DetailsPollenItem(Strings.Get(type.NameKey),
                        UnitFormatting.Format(concentration) + " – " + PollenIndex.GetPollenIndexToName(index),
                        ForecastColors.GetPollenColor(index));
                })]));
        }
        else
        {
            blocks.Add(new DetailsMessage(Strings.Get("chart_no_daily_data")));
        }
    }
}
