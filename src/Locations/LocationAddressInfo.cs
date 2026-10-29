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

namespace LibreccioWeather.Core.Locations;

/// <summary>
/// The address of a place, as returned by location search or reverse geocoding.
/// </summary>
public sealed record LocationAddressInfo
{
    /// <summary>
    /// Required by location search. In reverse geocoding, a result too far from these coordinates is rejected.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Required by location search. In reverse geocoding, a result too far from these coordinates is rejected.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// IANA time zone identifier, such as America/New_York or Europe/Paris.
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Country name. Leave empty to have it named from the country code.
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 code, which also covers dependent territories and special areas.
    /// Never empty: locations in the middle of the ocean are not supported.
    /// </summary>
    public required string CountryCode { get; init; }

    public string? Admin1 { get; init; }

    /// <summary>
    /// An ISO 3166-2 code, or the code the country uses internally.
    /// </summary>
    public string? Admin1Code { get; init; }

    public string? Admin2 { get; init; }

    /// <summary>
    /// An ISO 3166-2 code, or the code the country uses internally.
    /// </summary>
    public string? Admin2Code { get; init; }

    public string? Admin3 { get; init; }

    /// <summary>
    /// An ISO 3166-2 code, or the code the country uses internally.
    /// </summary>
    public string? Admin3Code { get; init; }

    public string? Admin4 { get; init; }

    /// <summary>
    /// An ISO 3166-2 code, or the code the country uses internally.
    /// </summary>
    public string? Admin4Code { get; init; }

    public string? City { get; init; }

    public string? CityCode { get; init; }

    public string? District { get; init; }
}
