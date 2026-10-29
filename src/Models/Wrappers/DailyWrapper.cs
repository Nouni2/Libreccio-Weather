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

namespace LibreccioWeather.Core.Models.Wrappers;

public sealed record DailyWrapper
{
    /// <summary>
    /// Midnight at the start of the day, in the time zone of the location.
    /// </summary>
    public required DateTimeOffset Date { get; init; }

    public HalfDayWrapper? Day { get; init; }

    public HalfDayWrapper? Night { get; init; }

    public DegreeDay? DegreeDay { get; init; }

    public UV? UV { get; init; }

    public TimeSpan? SunshineDuration { get; init; }

    public DailyRelativeHumidity? RelativeHumidity { get; init; }

    public DailyDewPoint? DewPoint { get; init; }

    public DailyPressure? Pressure { get; init; }

    public DailyCloudCover? CloudCover { get; init; }

    public DailyVisibility? Visibility { get; init; }

    /// <param name="airQuality">Air quality for this day, which sources report separately.</param>
    /// <param name="pollen">Pollen for this day, which sources report separately.</param>
    public Daily ToDaily(AirQuality? airQuality = null, Pollen? pollen = null)
    {
        return new Daily
        {
            Date = Date,
            Day = Day?.ToHalfDay(),
            Night = Night?.ToHalfDay(),
            DegreeDay = DegreeDay,
            AirQuality = airQuality,
            Pollen = pollen,
            UV = UV,
            SunshineDuration = SunshineDuration,
            RelativeHumidity = RelativeHumidity,
            DewPoint = DewPoint,
            Pressure = Pressure,
            CloudCover = CloudCover,
            Visibility = Visibility,
        };
    }
}
