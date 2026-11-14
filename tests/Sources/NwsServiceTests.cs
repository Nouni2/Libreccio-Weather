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
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.Nws;

namespace LibreccioWeather.Core.Tests.Sources;

public sealed class NwsServiceTests
{
    [Theory]
    [InlineData("PT1H30M", 1)]
    [InlineData("PT4294967297H", 1)]
    [InlineData("PT2H", 2)]
    public async Task RequestWeatherAsync_DurationAcrossClockChange_TruncatesNarrowsAndAdvancesElapsedHours(string duration, int expectedHours)
    {
        using HttpClient client = new(new ForecastResponseHandler(duration));
        NwsService source = new(client);
        Location location = new()
        {
            TimeZone = Location.FindTimeZone("America/New_York"),
            Parameters = new Dictionary<string, IReadOnlyDictionary<string, string>>
            {
                [NwsService.Id] = new Dictionary<string, string> { ["gridId"] = "OKX", ["gridX"] = "1", ["gridY"] = "1" },
            },
        };

        WeatherWrapper result = await source.RequestWeatherAsync(location, [SourceFeature.Forecast], TestContext.Current.CancellationToken);

        Assert.Equal(expectedHours, result.HourlyForecast!.Count);
        DateTimeOffset first = new(2026, 11, 1, 5, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < expectedHours; i++)
        {
            Assert.Equal(first.AddHours(i), result.HourlyForecast[i].Date);
        }

        if (expectedHours == 2)
        {
            Assert.Equal(1, TimeZoneInfo.ConvertTime(result.HourlyForecast[0].Date, location.TimeZone).Hour);
            Assert.Equal(1, TimeZoneInfo.ConvertTime(result.HourlyForecast[1].Date, location.TimeZone).Hour);
        }
    }

    private sealed class ForecastResponseHandler(string duration) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string content = request.RequestUri!.AbsolutePath.EndsWith("/forecast", StringComparison.Ordinal)
                ? """
                    {"properties":{"periods":[
                    {"startTime":"2026-11-01T06:00:00-05:00","isDaytime":true,"temperature":{"value":10}},
                    {"startTime":"2026-11-01T18:00:00-05:00","isDaytime":false,"temperature":{"value":2}}
                    ]}}
                    """
                : $$$$"""
                    {"properties":{"temperature":{"values":[{"validTime":"2026-11-01T01:00:00-04:00/{{{{duration}}}}","value":5}]}}}
                    """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            });
        }
    }
}
