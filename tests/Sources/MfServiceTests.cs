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
using System.Net;
using System.Text;
using System.Text.Json;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.MeteoFrance;

namespace LibreccioWeather.Core.Tests.Sources;

public sealed class MfServiceTests
{
    [Theory]
    [InlineData("2026-11-13T22:30:60.123999Z", "2026-11-13T22:30:59.123Z")]
    [InlineData("20261113T2230+01", "2026-11-13T21:30:00Z")]
    [InlineData("2026-11-13T22:30:00.123999+01:00", "2026-11-13T21:30:00.123Z")]
    public async Task RequestWeatherAsync_DateSerializerEdgeCases_PreservesMillisecondInstants(string input, string expected)
    {
        string payload = JsonSerializer.Serialize(new
        {
            properties = new
            {
                country = "FR",
                name = "Paris",
                timezone = "Europe/Paris",
                forecast = new[] { new { time = input, T = 1.2 } },
            },
        });
        using HttpClient client = new(new ResponseHandler(_ => payload));
        MfService source = new(client, CreateConfig());

        WeatherWrapper result = await source.RequestWeatherAsync(new Location(), [SourceFeature.Forecast], TestContext.Current.CancellationToken);

        HourlyWrapper hour = Assert.Single(result.HourlyForecast!);
        Assert.Equal(DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture), hour.Date);
    }

    [Fact]
    public async Task RequestWeatherAsync_AlertIdentity_UsesJavaObjectsHash()
    {
        string warning = """
            {"timelaps":[{"phenomenon_id":"1","timelaps_items":[{"begin_time":"1970-01-01T00:00:00Z","color_id":2}]}]}
            """;
        using HttpClient client = new(new ResponseHandler(request => request.RequestUri!.Query.Contains("echeance=J0", StringComparison.Ordinal)
            ? warning : "{}"));
        MfService source = new(client, CreateConfig());
        Location location = new()
        {
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [MfService.Id] = new Dictionary<string, string> { ["domain"] = "75" },
            },
        };

        WeatherWrapper result = await source.RequestWeatherAsync(location, [SourceFeature.Alert], TestContext.Current.CancellationToken);

        Alert alert = Assert.Single(result.AlertList!);
        Assert.Equal("76942", alert.AlertId);
        Assert.Equal(AlertSeverity.Moderate, alert.Severity);
    }

    [Fact]
    public async Task RequestWeatherAsync_OverseasMixedTextArray_UsesPrimitiveContents()
    {
        string warning = """
            {"text":{"text_bloc_item":[{"text":["alpha",2,true,null,{}]}]}}
            """;
        using HttpClient client = new(new ResponseHandler(request => request.RequestUri!.AbsolutePath.EndsWith("dictionary", StringComparison.Ordinal)
            ? "{}" : warning));
        MfService source = new(client, CreateConfig());
        Location location = new()
        {
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [MfService.Id] = new Dictionary<string, string> { ["domain"] = "VIGI971" },
            },
        };

        WeatherWrapper result = await source.RequestWeatherAsync(location, [SourceFeature.Alert], TestContext.Current.CancellationToken);

        Assert.Equal("alpha\n2\ntrue\nnull\n\n", Assert.Single(result.AlertList!).Description);
    }

    [Theory]
    [InlineData("[]", null)]
    [InlineData("[2]", 2.0)]
    public async Task RequestNearestLocationAsync_MissingCoordinates_LeavesMissingValuesNull(string coordinates, double? longitude)
    {
        string payload = $$$"""
            {"geometry":{"coordinates":{{{coordinates}}}},"properties":{"country":"FR","name":"Paris","timezone":"Europe/Paris","french_department":"75"}}
            """;
        using HttpClient client = new(new ResponseHandler(_ => payload));
        MfService source = new(client, CreateConfig());

        IReadOnlyList<LocationAddressInfo> result = await source.RequestNearestLocationAsync(48.86, 2.35, TestContext.Current.CancellationToken);

        LocationAddressInfo location = Assert.Single(result);
        Assert.Null(location.Latitude);
        Assert.Equal(longitude, location.Longitude);
    }

    private static ConfigStore CreateConfig()
    {
        return new ConfigStore(Path.Combine(Path.GetTempPath(), "Libreccio-Weather.Tests", Guid.NewGuid().ToString("N"), "source_mf.json"));
    }

    private sealed class ResponseHandler(Func<HttpRequestMessage, string> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response(request), Encoding.UTF8, "application/json"),
            });
        }
    }
}
