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

namespace LibreccioWeather.Core.Localization;

public static class CountryNames
{
    /// <summary>
    /// A country's name in the selected language, such as "Allemagne" for DE in French. Without ICU, .NET's name for
    /// the region is used, which follows Windows' display language rather than the app's.
    /// </summary>
    public static string GetCountryName(string countryCode)
    {
        if (Icu.GetDisplayCountry(countryCode, Strings.Culture) is { Length: > 0 } name)
        {
            return name;
        }

        try
        {
            return new RegionInfo(countryCode).DisplayName;
        }
        catch (ArgumentException)
        {
            return countryCode;
        }
    }
}
