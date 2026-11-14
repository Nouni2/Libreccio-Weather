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

using System.Collections.Concurrent;
using System.Text.Json;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources.Geography;

namespace LibreccioWeather.Core.Sources.BreezyTimeZone;

public sealed class BreezyTimeZoneService
{
    private static readonly ConcurrentDictionary<string, Lazy<JsonElement[]>> Geometry = new(StringComparer.Ordinal);
    private static readonly HashSet<string> GeometryCountries = new(StringComparer.Ordinal)
    {
        "AR", "AU", "BR", "CA", "CD", "CL", "CN", "CY", "EC", "ES", "FM", "ID", "KI", "KZ", "MN", "MX", "MY", "NZ", "PF", "PG", "PS", "PT", "RU", "UA", "US",
    };

    public TimeZoneInfo RequestTimeZone(Location location)
    {
        string timezone = location.CountryCode?.ToUpperInvariant() switch
        {
            "AD" => "Europe/Andorra",
            "AE" => "Asia/Dubai",
            "AF" => "Asia/Kabul",
            "AG" => "America/Puerto_Rico",
            "AI" => "America/Puerto_Rico",
            "AL" => "Europe/Tirane",
            "AM" => "Asia/Yerevan",
            "AO" => "Africa/Lagos",
            "AQ" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "AR" => GetTimeZoneForMultiTimeZoneCountry(location, "America/Argentina/Buenos_Aires"),
            "AS" => "Pacific/Pago_Pago",
            "AT" => "Europe/Vienna",
            "AU" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "AW" => "America/Puerto_Rico",
            "AX" => "Europe/Helsinki",
            "AZ" => "Asia/Baku",
            "BA" => "Europe/Belgrade",
            "BB" => "America/Barbados",
            "BD" => "Asia/Dhaka",
            "BE" => "Europe/Brussels",
            "BF" => "Africa/Abidjan",
            "BG" => "Europe/Sofia",
            "BH" => "Asia/Qatar",
            "BI" => "Africa/Maputo",
            "BJ" => "Africa/Lagos",
            "BL" => "America/Puerto_Rico",
            "BM" => "Atlantic/Bermuda",
            "BN" => "Asia/Kuching",
            "BO" => "America/La_Paz",
            "BQ" => "America/Puerto_Rico",
            "BR" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "BS" => "America/Toronto",
            "BT" => "Asia/Thimphu",
            "BW" => "Africa/Maputo",
            "BY" => "Europe/Minsk",
            "BZ" => "America/Belize",
            "CA" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "CC" => "Asia/Yangon",
            "CD" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "CF" => "Africa/Lagos",
            "CG" => "Africa/Lagos",
            "CH" => "Europe/Zurich",
            "CI" => "Africa/Abidjan",
            "CK" => "Pacific/Rarotonga",
            "CL" => GetTimeZoneForMultiTimeZoneCountry(location, "America/Santiago"),
            "CM" => "Africa/Lagos",
            "CN" => GetTimeZoneForMultiTimeZoneCountry(location, "Asia/Shanghai"),
            "CO" => "America/Bogota",
            "CR" => "America/Costa_Rica",
            "CU" => "America/Havana",
            "CV" => "Atlantic/Cape_Verde",
            "CW" => "America/Puerto_Rico",
            "CX" => "Asia/Bangkok",
            "CY" => GetTimeZoneForMultiTimeZoneCountry(location, "Asia/Nicosia"),
            "CZ" => "Europe/Prague",
            "DE" => "Europe/Berlin",
            "DJ" => "Africa/Nairobi",
            "DK" => "Europe/Berlin",
            "DM" => "America/Puerto_Rico",
            "DO" => "America/Santo_Domingo",
            "DZ" => "Africa/Algiers",
            "EC" => GetTimeZoneForMultiTimeZoneCountry(location, "America/Guayaquil"),
            "EE" => "Europe/Tallinn",
            "EG" => "Africa/Cairo",
            "EH" => "Africa/El_Aaiun",
            "ER" => "Africa/Nairobi",
            "ES" => GetTimeZoneForMultiTimeZoneCountry(location, "Europe/Madrid"),
            "ET" => "Africa/Nairobi",
            "FI" => "Europe/Helsinki",
            "FJ" => "Pacific/Fiji",
            "FK" => "Atlantic/Stanley",
            "FM" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "FO" => "Atlantic/Faroe",
            "FR" => "Europe/Paris",
            "GA" => "Africa/Lagos",
            "GB" => "Europe/London",
            "GD" => "America/Puerto_Rico",
            "GE" => "Asia/Tbilisi",
            "GF" => "America/Cayenne",
            "GG" => "Europe/London",
            "GH" => "Africa/Abidjan",
            "GI" => "Europe/Gibraltar",
            "GL" => GetTimeZoneForMultiTimeZoneCountry(location, "America/Nuuk"),
            "GM" => "Africa/Abidjan",
            "GN" => "Africa/Abidjan",
            "GP" => "America/Puerto_Rico",
            "GQ" => "Africa/Lagos",
            "GR" => "Europe/Athens",
            "GS" => "Atlantic/South_Georgia",
            "GT" => "America/Guatemala",
            "GU" => "Pacific/Guam",
            "GW" => "Africa/Bissau",
            "GY" => "America/Guyana",
            "HK" => "Asia/Hong_Kong",
            "HN" => "America/Tegucigalpa",
            "HR" => "Europe/Belgrade",
            "HT" => "America/Port-au-Prince",
            "HU" => "Europe/Budapest",
            "ID" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "IE" => "Europe/Dublin",
            "IL" => "Asia/Jerusalem",
            "IM" => "Europe/London",
            "IN" => "Asia/Kolkata",
            "IO" => "Indian/Chagos",
            "IQ" => "Asia/Baghdad",
            "IR" => "Asia/Tehran",
            "IS" => "Africa/Abidjan",
            "IT" => "Europe/Rome",
            "JE" => "Europe/London",
            "JM" => "America/Jamaica",
            "JO" => "Asia/Amman",
            "JP" => "Asia/Tokyo",
            "KE" => "Africa/Nairobi",
            "KG" => "Asia/Bishkek",
            "KH" => "Asia/Bangkok",
            "KI" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "KM" => "Africa/Nairobi",
            "KN" => "America/Puerto_Rico",
            "KP" => "Asia/Pyongyang",
            "KR" => "Asia/Seoul",
            "KW" => "Asia/Riyadh",
            "KY" => "America/Panama",
            "KZ" => GetTimeZoneForMultiTimeZoneCountry(location, "Asia/Almaty"),
            "LA" => "Asia/Bangkok",
            "LB" => "Asia/Beirut",
            "LC" => "America/Puerto_Rico",
            "LI" => "Europe/Zurich",
            "LK" => "Asia/Colombo",
            "LR" => "Africa/Monrovia",
            "LS" => "Africa/Johannesburg",
            "LT" => "Europe/Vilnius",
            "LU" => "Europe/Brussels",
            "LV" => "Europe/Riga",
            "LY" => "Africa/Tripoli",
            "MA" => "Africa/Casablanca",
            "MC" => "Europe/Paris",
            "MD" => "Europe/Chisinau",
            "ME" => "Europe/Belgrade",
            "MF" => "America/Puerto_Rico",
            "MG" => "Africa/Nairobi",
            "MH" => "Pacific/Majuro",
            "MK" => "Europe/Belgrade",
            "ML" => "Africa/Abidjan",
            "MM" => "Asia/Yangon",
            "MN" => GetTimeZoneForMultiTimeZoneCountry(location, "Asia/Ulaanbaatar"),
            "MO" => "Asia/Macau",
            "MP" => "Pacific/Guam",
            "MQ" => "America/Martinique",
            "MR" => "Africa/Abidjan",
            "MS" => "America/Puerto_Rico",
            "MT" => "Europe/Malta",
            "MU" => "Indian/Mauritius",
            "MV" => "Indian/Maldives",
            "MW" => "Africa/Maputo",
            "MX" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "MY" => GetTimeZoneForMultiTimeZoneCountry(location, "Asia/Kuala_Lumpur"),
            "MZ" => "Africa/Maputo",
            "NA" => "Africa/Windhoek",
            "NC" => "Pacific/Noumea",
            "NE" => "Africa/Lagos",
            "NF" => "Pacific/Norfolk",
            "NG" => "Africa/Lagos",
            "NI" => "America/Managua",
            "NL" => "Europe/Brussels",
            "NO" => "Europe/Berlin",
            "NP" => "Asia/Kathmandu",
            "NR" => "Pacific/Nauru",
            "NU" => "Pacific/Niue",
            "NZ" => GetTimeZoneForMultiTimeZoneCountry(location, "Pacific/Auckland"),
            "OM" => "Asia/Dubai",
            "PA" => "America/Panama",
            "PE" => "America/Lima",
            "PF" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "PG" => GetTimeZoneForMultiTimeZoneCountry(location, "Pacific/Port_Moresby"),
            "PH" => "Asia/Manila",
            "PK" => "Asia/Karachi",
            "PL" => "Europe/Warsaw",
            "PM" => "America/Miquelon",
            "PN" => "Pacific/Pitcairn",
            "PR" => "America/Puerto_Rico",
            "PS" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "PT" => GetTimeZoneForMultiTimeZoneCountry(location, "Europe/Lisbon"),
            "PW" => "Pacific/Palau",
            "PY" => "America/Asuncion",
            "QA" => "Asia/Qatar",
            "RE" => "Asia/Dubai",
            "RO" => "Europe/Bucharest",
            "RS" => "Europe/Belgrade",
            "RU" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "RW" => "Africa/Maputo",
            "SA" => "Asia/Riyadh",
            "SB" => "Pacific/Guadalcanal",
            "SC" => "Asia/Dubai",
            "SD" => "Africa/Khartoum",
            "SE" => "Europe/Berlin",
            "SG" => "Asia/Singapore",
            "SH" => "Africa/Abidjan",
            "SI" => "Europe/Belgrade",
            "SJ" => "Europe/Berlin",
            "SK" => "Europe/Prague",
            "SL" => "Africa/Abidjan",
            "SM" => "Europe/Rome",
            "SN" => "Africa/Abidjan",
            "SO" => "Africa/Nairobi",
            "SR" => "America/Paramaribo",
            "SS" => "Africa/Juba",
            "ST" => "Africa/Sao_Tome",
            "SV" => "America/El_Salvador",
            "SX" => "America/Puerto_Rico",
            "SY" => "Asia/Damascus",
            "SZ" => "Africa/Johannesburg",
            "TC" => "America/Grand_Turk",
            "TD" => "Africa/Ndjamena",
            "TF" => "GMT",
            "TG" => "Africa/Abidjan",
            "TH" => "Asia/Bangkok",
            "TJ" => "Asia/Dushanbe",
            "TK" => "Pacific/Fakaofo",
            "TL" => "Asia/Dili",
            "TM" => "Asia/Ashgabat",
            "TN" => "Africa/Tunis",
            "TO" => "Pacific/Tongatapu",
            "TR" => "Europe/Istanbul",
            "TT" => "America/Puerto_Rico",
            "TV" => "Pacific/Tarawa",
            "TW" => "Asia/Taipei",
            "TZ" => "Africa/Nairobi",
            "UA" => GetTimeZoneForMultiTimeZoneCountry(location, "Europe/Kyiv"),
            "UG" => "Africa/Nairobi",
            "UM" => "GMT",
            "US" => GetTimeZoneForMultiTimeZoneCountry(location, "GMT"),
            "UY" => "America/Montevideo",
            "UZ" => "Asia/Tashkent",
            "VA" => "Europe/Rome",
            "VC" => "America/Puerto_Rico",
            "VE" => "America/Caracas",
            "VG" => "America/Puerto_Rico",
            "VI" => "America/Puerto_Rico",
            "VN" => "Asia/Ho_Chi_Minh",
            "VU" => "Pacific/Efate",
            "WF" => "Pacific/Tarawa",
            "WS" => "Pacific/Apia",
            "XK" => "Europe/Belgrade",
            "YE" => "Asia/Riyadh",
            "YT" => "Africa/Nairobi",
            "ZA" => "Africa/Johannesburg",
            "ZM" => "Africa/Maputo",
            "ZW" => "Africa/Maputo",
            _ => "GMT",
        };
        return Location.FindTimeZone(timezone);
    }

    private static string GetTimeZoneForMultiTimeZoneCountry(Location location, string defaultTimeZone)
    {
        string country = location.CountryCode?.ToUpperInvariant() ?? "";
        JsonElement[] features = GeometryCountries.Contains(country)
            ? Geometry.GetOrAdd(country, key => new Lazy<JsonElement[]>(
                () => GeoJsonFeatures.Load("breezytz_" + key.ToLowerInvariant() + ".json"))).Value : [];
        JsonElement[] matchingSubdivisions = features.Where(feature =>
        {
            string[] subdivisions = GeoJsonFeatures.Property(feature, "isoPart2")?.Split(',') ?? [];
            return subdivisions.Contains(location.Admin1Code) || subdivisions.Contains(location.Admin2Code);
        }).ToArray();
        if (matchingSubdivisions.Length == 1 && GeoJsonFeatures.Property(matchingSubdivisions[0], "timezone") is { } exactZone)
        {
            return exactZone;
        }

        if (matchingSubdivisions.Length > 0 && FirstMatchingTimeZone(matchingSubdivisions, location) is { } subdivisionZone)
        {
            return subdivisionZone;
        }

        return FirstMatchingTimeZone(features, location) ?? defaultTimeZone;
    }

    private static string? FirstMatchingTimeZone(IEnumerable<JsonElement> features, Location location)
    {
        foreach (JsonElement feature in features)
        {
            if (GeoJsonFeatures.Matches(feature, location.Latitude, location.Longitude))
            {
                return GeoJsonFeatures.Property(feature, "timezone");
            }
        }

        return null;
    }
}
