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

using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.Core.Tests.Settings;

public class DailyTrendDisplayTests
{
    private const string Value = "temperature&air_quality&wind&uv_index&precipitation&sunshine&feels_like";

    private static readonly DailyTrendDisplay[] List =
    [
        DailyTrendDisplay.Temperature,
        DailyTrendDisplay.AirQuality,
        DailyTrendDisplay.Wind,
        DailyTrendDisplay.UVIndex,
        DailyTrendDisplay.Precipitation,
        DailyTrendDisplay.Sunshine,
        DailyTrendDisplay.FeelsLike,
    ];

    [Fact]
    public void ToDailyTrendDisplayList_Value_ReturnsChartsInOrder()
    {
        Assert.Equal(List, DailyTrendDisplay.ToDailyTrendDisplayList(Value));
    }

    [Fact]
    public void ToValue_Charts_JoinsIdentifiers()
    {
        Assert.Equal(Value, DailyTrendDisplay.ToValue(List));
    }

    // The test runs with the default English text, where the separator is ", ".
    [Fact]
    public void GetSummary_Charts_JoinsNamesWithSeparator()
    {
        Assert.Equal(
            "Conditions, Air quality, Wind, UV index, Precipitation, Sunshine, Feels like",
            DailyTrendDisplay.GetSummary(List));
    }
}
