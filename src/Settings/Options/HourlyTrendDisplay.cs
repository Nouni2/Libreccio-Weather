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
/// A chart of the hourly forecast card. The charts shown, in their order, are stored as their identifiers joined by "&amp;".
/// </summary>
public sealed class HourlyTrendDisplay
{
    public static readonly HourlyTrendDisplay Temperature = new("temperature", "conditions");
    public static readonly HourlyTrendDisplay AirQuality = new("air_quality", "air_quality");
    public static readonly HourlyTrendDisplay Wind = new("wind", "wind");
    public static readonly HourlyTrendDisplay UVIndex = new("uv_index", "uv_index");
    public static readonly HourlyTrendDisplay Precipitation = new("precipitation", "precipitation");
    public static readonly HourlyTrendDisplay FeelsLike = new("feels_like", "temperature_feels_like");
    public static readonly HourlyTrendDisplay Humidity = new("humidity", "humidity_dew_point");
    public static readonly HourlyTrendDisplay Pressure = new("pressure", "pressure");
    public static readonly HourlyTrendDisplay CloudCover = new("cloud_cover", "cloud_cover");
    public static readonly HourlyTrendDisplay Visibility = new("visibility", "visibility");

    public static IReadOnlyList<HourlyTrendDisplay> Entries { get; } =
        [Temperature, AirQuality, Wind, UVIndex, Precipitation, FeelsLike, Humidity, Pressure, CloudCover, Visibility];

    private readonly string _nameKey;

    private HourlyTrendDisplay(string id, string nameKey)
    {
        Id = id;
        _nameKey = nameKey;
    }

    public string Id { get; }

    public string Name => Strings.Get(_nameKey);

    /// <summary>
    /// The charts in a stored value. Unknown identifiers are skipped, and an empty value shows every chart.
    /// </summary>
    public static List<HourlyTrendDisplay> ToHourlyTrendDisplayList(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [.. Entries];
        }

        return value.Split('&')
            .Select(id => Entries.FirstOrDefault(chart => chart.Id == id))
            .OfType<HourlyTrendDisplay>()
            .ToList();
    }

    public static string ToValue(IEnumerable<HourlyTrendDisplay> list)
    {
        return string.Join("&", list.Select(chart => chart.Id));
    }

    /// <summary>
    /// The names of the charts, joined with the separator of the selected language.
    /// </summary>
    public static string GetSummary(IEnumerable<HourlyTrendDisplay> list)
    {
        return string.Join(Strings.GetUnit("locale_separator"), list.Select(chart => chart.Name));
    }

    public override string ToString()
    {
        return Id;
    }
}
