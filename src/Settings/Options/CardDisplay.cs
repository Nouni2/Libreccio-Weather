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

using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Core.Settings.Options;

/// <summary>
/// A card of the main screen. The cards shown, in their order, are stored as their identifiers joined by "&amp;".
/// </summary>
public sealed class CardDisplay
{
    public static readonly CardDisplay Nowcast = new("nowcast", "precipitation_nowcasting");
    public static readonly CardDisplay DailyForecast = new("daily_forecast", "daily_forecast");
    public static readonly CardDisplay HourlyForecast = new("hourly_forecast", "hourly_forecast");
    public static readonly CardDisplay Precipitation = new("precipitation", "precipitation");
    public static readonly CardDisplay Wind = new("wind", "wind");
    public static readonly CardDisplay AirQuality = new("air_quality", "air_quality");
    public static readonly CardDisplay Pollen = new("pollen", "pollen");
    public static readonly CardDisplay Humidity = new("humidity", "humidity");
    public static readonly CardDisplay UV = new("uv", "uv_index");
    public static readonly CardDisplay Visibility = new("visibility", "visibility");
    public static readonly CardDisplay Pressure = new("pressure", "pressure");
    public static readonly CardDisplay Sun = new("sun", "ephemeris_sun");
    public static readonly CardDisplay Moon = new("moon", "ephemeris_moon");
    public static readonly CardDisplay Clock = new("clock", "clock");

    public static IReadOnlyList<CardDisplay> Entries { get; } =
    [
        Nowcast,
        DailyForecast,
        HourlyForecast,
        Precipitation,
        Wind,
        AirQuality,
        Pollen,
        Humidity,
        UV,
        Visibility,
        Pressure,
        Sun,
        Moon,
        Clock,
    ];

    private readonly string _nameKey;

    private CardDisplay(string id, string nameKey)
    {
        Id = id;
        _nameKey = nameKey;
    }

    public string Id { get; }

    public string Name => Strings.Get(_nameKey);

    /// <summary>
    /// The cards in a stored value. Unknown identifiers are skipped, and an empty value shows no card.
    /// </summary>
    public static List<CardDisplay> ToCardDisplayList(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return value.Split('&')
            .Select(id => Entries.FirstOrDefault(card => card.Id == id))
            .OfType<CardDisplay>()
            .ToList();
    }

    public static string ToValue(IEnumerable<CardDisplay> list)
    {
        return string.Join("&", list.Select(card => card.Id));
    }

    /// <summary>
    /// The names of the cards, joined with the separator of the selected language.
    /// </summary>
    public static string GetSummary(IEnumerable<CardDisplay> list)
    {
        return string.Join(Strings.GetUnit("locale_separator"), list.Select(card => card.Name));
    }

    public override string ToString()
    {
        return Id;
    }
}
