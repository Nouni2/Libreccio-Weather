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

using System.Net;
using System.Net.Http;
using System.Text.Json;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Sources;

namespace LibreccioWeather.Core.Refresh;

public enum RefreshErrorType
{
    NetworkUnavailable,
    ServerTimeout,
    ApiKeyRequiredMissing,
    ApiLimitReached,
    ApiUnauthorized,
    ServerUnavailable,
    ServerInsecure,
    ParsingError,
    SourceNotInstalled,
    NonFreeNetworkSource,
    LocationFailed,
    AccessLocationPermissionMissing,
    AccessBackgroundLocationPermissionMissing,
    LocationAccessOff,
    ReverseGeocodingFailed,
    LocationSearchFailed,
    InvalidLocation,
    UnsupportedFeature,
    InvalidIncompleteData,
    OutdatedServerData,
    IncompatibleForecastTimes,
    DataRefreshFailed,
}

/// <summary>
/// A problem met while refreshing, with the source and feature it concerns when known.
/// </summary>
public sealed record RefreshError(RefreshErrorType Error, string? Source = null, SourceFeature? Feature = null)
{
    /// <summary>
    /// The error type matching an exception, or the given default when none matches.
    /// </summary>
    public static RefreshErrorType GetTypeFromException(Exception exception, RefreshErrorType defaultType)
    {
        return exception switch
        {
            InvalidLocationException => RefreshErrorType.InvalidLocation,
            HttpRequestException { HttpRequestError: HttpRequestError.NameResolutionError } =>
                RefreshErrorType.NetworkUnavailable,
            HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError } =>
                RefreshErrorType.ServerInsecure,
            HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } =>
                RefreshErrorType.ApiUnauthorized,
            HttpRequestException { StatusCode: HttpStatusCode.Conflict or HttpStatusCode.TooManyRequests } =>
                RefreshErrorType.ApiLimitReached,
            HttpRequestException { StatusCode: >= HttpStatusCode.InternalServerError and <= (HttpStatusCode)599 } =>
                RefreshErrorType.ServerUnavailable,
            HttpRequestException { HttpRequestError: HttpRequestError.ConnectionError } =>
                RefreshErrorType.ServerUnavailable,
            TaskCanceledException => RefreshErrorType.ServerTimeout,
            JsonException => RefreshErrorType.ParsingError,
            _ => defaultType,
        };
    }
}

/// <summary>
/// The weather after a refresh, which may be the previous weather when the refresh failed, and what went wrong.
/// </summary>
public sealed record WeatherResult(Weather? Weather, IReadOnlyList<RefreshError> Errors);
