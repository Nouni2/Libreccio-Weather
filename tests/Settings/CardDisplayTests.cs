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

public class CardDisplayTests
{
    private const string Value =
        "nowcast&daily_forecast&hourly_forecast&precipitation&wind&air_quality&pollen&humidity&uv&visibility&pressure&sun&moon";

    private static readonly CardDisplay[] List =
    [
        CardDisplay.Nowcast,
        CardDisplay.DailyForecast,
        CardDisplay.HourlyForecast,
        CardDisplay.Precipitation,
        CardDisplay.Wind,
        CardDisplay.AirQuality,
        CardDisplay.Pollen,
        CardDisplay.Humidity,
        CardDisplay.UV,
        CardDisplay.Visibility,
        CardDisplay.Pressure,
        CardDisplay.Sun,
        CardDisplay.Moon,
    ];

    [Fact]
    public void ToCardDisplayList_Value_ReturnsCardsInOrder()
    {
        Assert.Equal(List, CardDisplay.ToCardDisplayList(Value));
    }

    [Fact]
    public void ToValue_Cards_JoinsIdentifiers()
    {
        Assert.Equal(Value, CardDisplay.ToValue(List));
    }

    // The test runs with the default English text, where the separator is ", ".
    [Fact]
    public void GetSummary_Cards_JoinsNamesWithSeparator()
    {
        Assert.Equal(
            "Precipitation nowcasting, Daily forecast, Hourly forecast, Precipitation, Wind, Air quality, Pollen, "
            + "Humidity, UV index, Visibility, Pressure, Sun, Moon",
            CardDisplay.GetSummary(List));
    }
}
