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
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.MetNorway;

namespace LibreccioWeather.Core.Tests.Sources;

public sealed class MetNoServiceTests
{
    [Fact]
    public async Task RequestWeatherAsync_EmptyAlertInterval_KeepsStartNullAndUsesEventEndingTime()
    {
        using HttpClient client = new(new AlertResponseHandler());
        MetNoService source = new(client);

        WeatherWrapper result = await source.RequestWeatherAsync(new Location(), [SourceFeature.Alert], TestContext.Current.CancellationToken);

        Alert alert = Assert.Single(result.AlertList!);
        Assert.Null(alert.StartDate);
        Assert.Equal(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero), alert.EndDate);
    }

    private sealed class AlertResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"features":[{"properties":{"id":"wind-alert","eventEndingTime":"2026-11-14T12:00:00Z"},"when":{"interval":[]}}]}
                    """, Encoding.UTF8, "application/json"),
            });
        }
    }
}
