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

using System.Globalization;
using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Core.Tests.Localization;

public sealed class PluralRulesTests
{
    // Without ICU, Select is the table itself, and the comparison holds trivially.
    [Fact]
    public void SelectWithoutIcu_EveryShippedLanguage_MatchesIcu()
    {
        long[] counts = [.. Enumerable.Range(0, 1200).Select(count => (long)count), 1_000_000, 1_000_001, 2_000_000, 101, -1];
        List<string> mismatches = [];
        foreach (string code in AppLanguages.Codes)
        {
            if (AppLanguages.GetCulture(code) is not CultureInfo culture)
            {
                continue;
            }

            foreach (long count in counts)
            {
                string expected = PluralRules.Select(culture, count);
                string actual = PluralRules.SelectWithoutIcu(culture, count);
                if (expected != actual)
                {
                    mismatches.Add($"{code} {count}: ICU {expected}, table {actual}");
                }
            }
        }

        Assert.Empty(mismatches);
    }
}
