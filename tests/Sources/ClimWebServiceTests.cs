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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.ClimWeb;

namespace LibreccioWeather.Core.Tests.Sources;

public sealed class ClimWebServiceTests
{
    [Fact]
    public async Task RequestWeatherAsync_UnicodeAlertText_UsesKotlinWhitespace()
    {
        using HttpClient client = new(new AlertResponseHandler(new Dictionary<string, string?>
        {
            ["headline"] = "\u001CWind\u0085",
        }));
        DccmsService source = new(client, CreateConfig());

        WeatherWrapper result = await source.RequestWeatherAsync(new Location(), [SourceFeature.Alert], TestContext.Current.CancellationToken);

        Assert.Equal("Wind\u0085", Assert.Single(result.AlertList!).Headline);
    }

    [Theory]
    [InlineData("2026-11-13T22:00:00+0100", 11, 13, 21)]
    [InlineData("2026-11-32T22:00:00+0000", 12, 2, 22)]
    public async Task RequestWeatherAsync_AlertDate_UsesLenientCalendarParsing(string onset, int month, int day, int hour)
    {
        using HttpClient client = new(new AlertResponseHandler(new Dictionary<string, string?> { ["onset"] = onset }));
        DccmsService source = new(client, CreateConfig());
        Task<WeatherWrapper> request = source.RequestWeatherAsync(new Location(), [SourceFeature.Alert], TestContext.Current.CancellationToken);
        if (!Icu.SupportsDateParsing)
        {
            await Assert.ThrowsAsync<JsonException>(() => request);
            return;
        }

        Alert alert = Assert.Single((await request).AlertList!);
        Assert.Equal(new DateTimeOffset(2026, month, day, hour, 0, 0, TimeSpan.Zero), alert.StartDate);
    }

    private static ConfigStore CreateConfig() => new(Path.Combine(Path.GetTempPath(), "Libreccio-Weather.Tests",
        Guid.NewGuid().ToString("N"), "source_dccms.json"));

    private sealed class AlertResponseHandler(Dictionary<string, string?> properties) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string payload = JsonSerializer.Serialize(new
            {
                features = new[]
                {
                    new
                    {
                        type = "Feature",
                        geometry = new
                        {
                            type = "Polygon",
                            coordinates = new[] { new[] { new[] { -1.0, -1.0 }, new[] { 1.0, -1.0 }, new[] { 1.0, 1.0 }, new[] { -1.0, 1.0 } } },
                        },
                        properties,
                    },
                },
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            });
        }
    }
}
