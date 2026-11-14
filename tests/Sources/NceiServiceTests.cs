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
using System.Text;
using System.Text.Json;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.Ncei;
using UnitTemperature = LibreccioWeather.Core.Units.Temperature;

namespace LibreccioWeather.Core.Tests.Sources;

public sealed class NceiServiceTests
{
    [Theory]
    [InlineData("0x1.8p3D", 12.0)]
    [InlineData("12.5f", 12.5)]
    [InlineData("\u001f12\u001f", 12.0)]
    [InlineData("\u00a012\u00a0", null)]
    public async Task RequestWeatherAsync_TemperatureString_UsesNumericSuffixHexAndWhitespaceRules(string input, double? expected)
    {
        using HttpClient client = new(new NormalsResponseHandler(input));
        NceiService source = new(client);
        Location location = new()
        {
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [NceiService.Id] = new Dictionary<string, string> { ["stations"] = "{\"station\":0}" },
            },
        };

        WeatherWrapper result = await source.RequestWeatherAsync(location, [SourceFeature.Normals], TestContext.Current.CancellationToken);

        Assert.Equal(expected is { } value ? UnitTemperature.FromCelsius(value) : (UnitTemperature?)null,
            result.Normals![Month.January].DaytimeTemperature);
    }

    private sealed class NormalsResponseHandler(string temperature) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string content = JsonSerializer.Serialize(new[] { new { DATE = "2020-01", STATION = "station", TMAX = temperature } });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
        }
    }
}
