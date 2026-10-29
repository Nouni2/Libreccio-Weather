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

using LibreccioWeather.Core.Models.Wrappers;

namespace LibreccioWeather.Core.Models;

public sealed record Daily
{
    /// <summary>
    /// Midnight at the start of the day, in the time zone of the location.
    /// </summary>
    public required DateTimeOffset Date { get; init; }

    public HalfDay? Day { get; init; }

    public HalfDay? Night { get; init; }

    public DegreeDay? DegreeDay { get; init; }

    public Astro? Sun { get; init; }

    public Astro? Twilight { get; init; }

    public Astro? Moon { get; init; }

    public MoonPhase? MoonPhase { get; init; }

    public AirQuality? AirQuality { get; init; }

    public Pollen? Pollen { get; init; }

    public UV? UV { get; init; }

    public TimeSpan? SunshineDuration { get; init; }

    public DailyRelativeHumidity? RelativeHumidity { get; init; }

    public DailyDewPoint? DewPoint { get; init; }

    public DailyPressure? Pressure { get; init; }

    public DailyCloudCover? CloudCover { get; init; }

    public DailyVisibility? Visibility { get; init; }

    public DailyWrapper ToDailyWrapper()
    {
        return new DailyWrapper
        {
            Date = Date,
            Day = Day?.ToHalfDayWrapper(),
            Night = Night?.ToHalfDayWrapper(),
            DegreeDay = DegreeDay,
            UV = UV,
            SunshineDuration = SunshineDuration,
        };
    }
}
