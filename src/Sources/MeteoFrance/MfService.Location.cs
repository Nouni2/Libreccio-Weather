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
using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.Core.Sources.MeteoFrance;

public sealed partial class MfService
{
    private static readonly HashSet<string> FrenchDepartments = new(StringComparer.Ordinal)
    {
        "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "30", "31", "32", "33", "34", "35", "36", "37",
        "38", "39", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "50", "51", "52", "53", "54", "55", "56",
        "57", "58", "59", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "70", "71", "72", "73", "74", "75",
        "76", "77", "78", "79", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "90", "91", "92", "93", "94",
        "95", "99",
    };

    public async Task<IReadOnlyList<LocationAddressInfo>> RequestNearestLocationAsync(double latitude, double longitude,
        CancellationToken cancellationToken = default)
    {
        string query = "v2/forecast?lat=" + latitude.ToString(CultureInfo.InvariantCulture)
            + "&lon=" + longitude.ToString(CultureInfo.InvariantCulture) + "&formatDate=iso";
        MfForecastResult result = await GetAsync<MfForecastResult>(query, GetToken(), cancellationToken).ConfigureAwait(false);
        LocationAddressInfo? location = ConvertLocation(result);
        return location is null ? [] : [location];
    }

    private static LocationAddressInfo? ConvertLocation(MfForecastResult result)
    {
        if (result.Properties is not { } properties)
        {
            return null;
        }

        string countryCode = properties.Country[..2];
        if (countryCode == "FR")
        {
            countryCode = properties.FrenchDepartment switch
            {
                "971" => "GP",
                "972" => "MQ",
                "973" => "GF",
                "974" => "RE",
                "975" => "PM",
                "976" => "YT",
                "977" => "BL",
                "978" => "MF",
                "984" => properties.Timezone == "Antarctica/DumontDUrville" ? "AQ" : "TF",
                "986" => "WF",
                "987" => "PF",
                "988" => "NC",
                // The nearest town can cross a territory boundary; its reported zone belongs to the requested point.
                _ => properties.Timezone switch
                {
                    "Europe/Jersey" => "JE",
                    "America/Lower_Princes" => "SX",
                    _ => countryCode,
                },
            };
        }

        double[] coordinates = result.Geometry?.Coordinates ?? [];
        return new LocationAddressInfo
        {
            Latitude = coordinates.Length > 1 ? coordinates[1] : null,
            Longitude = coordinates.Length > 0 ? coordinates[0] : null,
            TimeZoneId = properties.Timezone,
            CountryCode = countryCode,
            Admin2 = properties.FrenchDepartment is { Length: > 0 } department && FrenchDepartments.Contains(department)
                ? Strings.Get("mf_department_" + department) : null,
            Admin2Code = properties.FrenchDepartment,
            City = properties.Name,
        };
    }
}
