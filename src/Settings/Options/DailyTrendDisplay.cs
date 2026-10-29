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
/// A chart of the daily forecast card. The charts shown, in their order, are stored as their identifiers joined by "&amp;".
/// </summary>
public sealed class DailyTrendDisplay
{
    public static readonly DailyTrendDisplay Temperature = new("temperature", "conditions");
    public static readonly DailyTrendDisplay AirQuality = new("air_quality", "air_quality");
    public static readonly DailyTrendDisplay Wind = new("wind", "wind");
    public static readonly DailyTrendDisplay UVIndex = new("uv_index", "uv_index");
    public static readonly DailyTrendDisplay Precipitation = new("precipitation", "precipitation");
    public static readonly DailyTrendDisplay Sunshine = new("sunshine", "sunshine");
    public static readonly DailyTrendDisplay FeelsLike = new("feels_like", "temperature_feels_like");

    public static IReadOnlyList<DailyTrendDisplay> Entries { get; } =
        [Temperature, AirQuality, Wind, UVIndex, Precipitation, Sunshine, FeelsLike];

    private readonly string _nameKey;

    private DailyTrendDisplay(string id, string nameKey)
    {
        Id = id;
        _nameKey = nameKey;
    }

    public string Id { get; }

    public string Name => Strings.Get(_nameKey);

    /// <summary>
    /// The charts in a stored value. Unknown identifiers are skipped, and an empty value shows every chart.
    /// </summary>
    public static List<DailyTrendDisplay> ToDailyTrendDisplayList(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [.. Entries];
        }

        return value.Split('&')
            .Select(id => Entries.FirstOrDefault(chart => chart.Id == id))
            .OfType<DailyTrendDisplay>()
            .ToList();
    }

    public static string ToValue(IEnumerable<DailyTrendDisplay> list)
    {
        return string.Join("&", list.Select(chart => chart.Id));
    }

    /// <summary>
    /// The names of the charts, joined with the separator of the selected language.
    /// </summary>
    public static string GetSummary(IEnumerable<DailyTrendDisplay> list)
    {
        return string.Join(Strings.GetUnit("locale_separator"), list.Select(chart => chart.Name));
    }

    public override string ToString()
    {
        return Id;
    }
}
