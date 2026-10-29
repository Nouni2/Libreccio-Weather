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
using System.Text;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Locations;

/// <summary>
/// A saved location, with its address, the source chosen for each feature, and its last weather.
/// </summary>
public sealed record Location
{
    public const string CurrentPositionId = "CURRENT_POSITION";

    /// <summary>
    /// 5 km, in meters.
    /// </summary>
    public const int CloseDistance = 5000;

    // An unknown time zone falls back to GMT, and a location on GMT is treated as having no valid time zone.
    private const string FallbackTimeZoneId = "GMT";

    private static readonly TimeZoneInfo FallbackTimeZone =
        TimeZoneInfo.CreateCustomTimeZone(FallbackTimeZoneId, TimeSpan.Zero, FallbackTimeZoneId, FallbackTimeZoneId);

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public TimeZoneInfo TimeZone { get; init; } = FallbackTimeZone;

    public string Country { get; init; } = "";

    public string? CountryCode { get; init; }

    public string? Admin1 { get; init; }

    public string? Admin1Code { get; init; }

    public string? Admin2 { get; init; }

    public string? Admin2Code { get; init; }

    public string? Admin3 { get; init; }

    public string? Admin3Code { get; init; }

    public string? Admin4 { get; init; }

    public string? Admin4Code { get; init; }

    public string City { get; init; } = "";

    public string? CityId { get; init; }

    public string? District { get; init; }

    public bool NeedsGeocodeRefresh { get; init; }

    public string? CustomName { get; init; }

    public Weather? Weather { get; init; }

    public string ForecastSource { get; init; } = "openmeteo";

    public string? CurrentSource { get; init; }

    public string? AirQualitySource { get; init; }

    public string? PollenSource { get; init; }

    public string? MinutelySource { get; init; }

    public string? AlertSource { get; init; }

    public string? NormalsSource { get; init; }

    public string? ReverseGeocodingSource { get; init; }

    public bool IsCurrentPosition { get; init; }

    /// <summary>
    /// Values a source needs for this location, by source identifier.
    /// For example "accu" maps to cityId 230, and "nws" maps to gridId 8, gridX 20 and gridY 30.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Parameters { get; init; } =
        new Dictionary<string, IReadOnlyDictionary<string, string>>();

    public string FormattedId => IsCurrentPosition
        ? CurrentPositionId
        : Latitude.ToString("F6", CultureInfo.InvariantCulture)
            + "&"
            + Longitude.ToString("F6", CultureInfo.InvariantCulture)
            + "&"
            + ForecastSource;

    // Coordinates of exactly 0, 0 mean no position was set.
    public bool IsUsable => Latitude != 0.0 || Longitude != 0.0;

    public bool IsTimeZoneInvalid => TimeZone.Id == FallbackTimeZoneId;

    public string CityAndDistrict
    {
        get
        {
            StringBuilder builder = new();
            if (City.Length > 0)
            {
                builder.Append(City);
            }

            if (!string.IsNullOrEmpty(District) && District != City)
            {
                AppendSeparated(builder, District);
            }

            return builder.ToString();
        }
    }

    public bool HasValidCountryCode =>
        CountryCode is { Length: 2 } code && char.IsAsciiLetter(code[0]) && char.IsAsciiLetter(code[1]);

    /// <summary>
    /// The time zone for an IANA identifier, or GMT when the identifier is missing or unknown.
    /// </summary>
    public static TimeZoneInfo FindTimeZone(string? timeZoneId)
    {
        if (timeZoneId is not null && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out TimeZoneInfo? timeZone))
        {
            return timeZone;
        }

        return FallbackTimeZone;
    }

    /// <summary>
    /// Whether two strings are equal, counting null and empty as the same.
    /// </summary>
    public static bool IsEquals(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
        {
            return true;
        }

        return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && a == b;
    }

    /// <summary>
    /// The administrative areas above the city, from the country down, without repeating the city.
    /// </summary>
    public string AdministrationLevels()
    {
        StringBuilder builder = new();
        if (Country.Length > 0)
        {
            builder.Append(Country);
        }

        if (!string.IsNullOrEmpty(Admin1))
        {
            AppendSeparated(builder, Admin1);
        }

        if (!string.IsNullOrEmpty(Admin2))
        {
            AppendSeparated(builder, Admin2);
        }

        if (!string.IsNullOrEmpty(Admin3) && (!string.IsNullOrEmpty(Admin4) || Admin3 != City))
        {
            AppendSeparated(builder, Admin3);
        }

        if (!string.IsNullOrEmpty(Admin4) && Admin4 != City)
        {
            AppendSeparated(builder, Admin4);
        }

        return builder.ToString();
    }

    /// <summary>
    /// This location with its address replaced by the given one.
    /// </summary>
    /// <param name="overwriteCoordinates">Also replaces the coordinates, when the address has both.</param>
    public Location ToLocationWithAddressInfo(LocationAddressInfo addressInfo, bool overwriteCoordinates)
    {
        bool useAddressCoordinates = overwriteCoordinates
            && addressInfo.Latitude is not null
            && addressInfo.Longitude is not null;

        return this with
        {
            Latitude = useAddressCoordinates ? addressInfo.Latitude!.Value : Latitude,
            Longitude = useAddressCoordinates ? addressInfo.Longitude!.Value : Longitude,
            TimeZone = FindTimeZone(addressInfo.TimeZoneId),
            Country = addressInfo.Country ?? "",
            CountryCode = addressInfo.CountryCode,
            Admin1 = addressInfo.Admin1 ?? "",
            Admin1Code = addressInfo.Admin1Code ?? "",
            Admin2 = addressInfo.Admin2 ?? "",
            Admin2Code = addressInfo.Admin2Code ?? "",
            Admin3 = addressInfo.Admin3 ?? "",
            Admin3Code = addressInfo.Admin3Code ?? "",
            Admin4 = addressInfo.Admin4 ?? "",
            Admin4Code = addressInfo.Admin4Code ?? "",
            City = addressInfo.City ?? "",
            CityId = addressInfo.CityCode ?? "",
            District = addressInfo.District ?? "",
            NeedsGeocodeRefresh = false,
        };
    }

    /// <summary>
    /// The address of this location. Reverse geocoding sources build their own address instead of calling this.
    /// </summary>
    public LocationAddressInfo ToAddressInfo()
    {
        return new LocationAddressInfo
        {
            Latitude = Latitude,
            Longitude = Longitude,
            TimeZoneId = TimeZone.Id,
            Country = Country,
            CountryCode = CountryCode ?? "",
            Admin1 = Admin1,
            Admin1Code = Admin1Code,
            Admin2 = Admin2,
            Admin2Code = Admin2Code,
            Admin3 = Admin3,
            Admin3Code = Admin3Code,
            Admin4 = Admin4,
            Admin4Code = Admin4Code,
            City = City,
            CityCode = CityId,
            District = District,
        };
    }

    // Two locations are equal when they are the same place with the same settings and the same refresh of the weather.
    public bool Equals(Location? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null
            || FormattedId != other.FormattedId
            || CustomName != other.CustomName
            || ForecastSource != other.ForecastSource
            || CurrentSource != other.CurrentSource
            || AirQualitySource != other.AirQualitySource
            || PollenSource != other.PollenSource
            || MinutelySource != other.MinutelySource
            || AlertSource != other.AlertSource
            || NormalsSource != other.NormalsSource
            || NeedsGeocodeRefresh != other.NeedsGeocodeRefresh
            || !ParametersEqual(Parameters, other.Parameters))
        {
            return false;
        }

        if (Weather is null && other.Weather is null)
        {
            return true;
        }

        return Weather is not null
            && other.Weather is not null
            && Weather.Base.RefreshTime == other.Weather.Base.RefreshTime;
    }

    public override int GetHashCode()
    {
        return FormattedId.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString()
    {
        StringBuilder builder = new($"{Country} {Admin1} {Admin2} {Admin3} {Admin4}");
        if (Admin4 != City && City.Length > 0)
        {
            builder.Append(' ').Append(City);
        }

        if (City != District && !string.IsNullOrEmpty(District))
        {
            builder.Append(' ').Append(District);
        }

        return builder.ToString();
    }

    private static void AppendSeparated(StringBuilder builder, string value)
    {
        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(value);
    }

    private static bool ParametersEqual(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> left,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach ((string source, IReadOnlyDictionary<string, string> leftValues) in left)
        {
            if (!right.TryGetValue(source, out IReadOnlyDictionary<string, string>? rightValues)
                || leftValues.Count != rightValues.Count)
            {
                return false;
            }

            foreach ((string key, string value) in leftValues)
            {
                if (!rightValues.TryGetValue(key, out string? otherValue) || value != otherValue)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
