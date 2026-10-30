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
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings.Options;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// The sun or moon card of the main screen: an arc from rise to set with the body at the current time, the rise and
/// set times, and a description such as polar day or the moon phase.
/// </summary>
public sealed record AstroCardViewModel(
    bool IsSun,
    string Title,
    long StartTime,
    long EndTime,
    long CurrentTime,
    string RiseText,
    string? RiseDescription,
    string SetText,
    string? SetDescription,
    string? Description,
    int? MoonPhaseAngle,
    Color LineColor,
    bool AnimateElements)
{
    // The clear daytime color and the brightened night-sky color stay independent of the current weather.
    private static readonly Color SunLine = Color.FromRgb(253, 188, 76);
    private static readonly Color MoonLine = Color.FromRgb(76, 87, 108);

    public string AutomationName => string.Join(Strings.GetUnit("locale_separator"),
        new[] { Title, RiseDescription, SetDescription, Description }.Where(text => !string.IsNullOrEmpty(text)));

    public static AstroCardViewModel? Create(CardDisplay card, Weather weather, Location location, bool animateElements)
    {
        if (weather.DailyForecast.Count == 0 || weather.Today is not { } today)
        {
            return null;
        }

        return card == CardDisplay.Sun ? CreateSun(today, location, animateElements)
            : card == CardDisplay.Moon ? CreateMoon(weather, today, location, animateElements)
            : null;
    }

    private static AstroCardViewModel? CreateSun(Daily today, Location location, bool animateElements)
    {
        if (today.Sun is not { } sun)
        {
            return null;
        }

        string riseText = "";
        string setText = "";
        string? riseDescription = null;
        string? setDescription = null;
        string? description = null;
        if (sun.IsValid)
        {
            // Rising at midnight and setting a millisecond before the next is the sign of a polar day.
            if (sun.SetDate == sun.RiseDate!.Value.AddDays(1).AddMilliseconds(-1))
            {
                description = Strings.Get("ephemeris_polar_day");
            }
            else
            {
                riseText = DateFormatting.GetFormattedTime(sun.RiseDate!.Value, location.TimeZone);
                setText = DateFormatting.GetFormattedTime(sun.SetDate!.Value, location.TimeZone);
                riseDescription = Strings.Format("ephemeris_sunrise_at", riseText);
                setDescription = Strings.Format("ephemeris_sunset_at", setText);
            }
        }
        else if (sun.RiseDate is null && sun.SetDate is null)
        {
            description = Strings.Get("ephemeris_polar_night");
        }

        (long start, long end, long current) = GetTimes(sun);
        return new AstroCardViewModel(true, Strings.Get("ephemeris_sun"), start, end, current,
            riseText, riseDescription, setText, setDescription, description, null, SunLine, animateElements);
    }

    private static AstroCardViewModel? CreateMoon(Weather weather, Daily today, Location location, bool animateElements)
    {
        if (today.Moon is not { IsValid: true } moon)
        {
            return null;
        }

        // Before tonight's moonrise, the moon of last night is shown while it is still up.
        Astro validatedMoon = moon;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now < moon.RiseDate!.Value && weather.TodayIndex is { } todayIndex && todayIndex > 0
            && weather.DailyForecast[todayIndex - 1].Moon is { IsValid: true } yesterdayMoon && yesterdayMoon.SetDate!.Value > now)
        {
            validatedMoon = yesterdayMoon;
        }

        string riseText = DateFormatting.GetFormattedTime(validatedMoon.RiseDate!.Value, location.TimeZone);
        string setText = DateFormatting.GetFormattedTime(validatedMoon.SetDate!.Value, location.TimeZone);
        MoonPhase? moonPhase = today.MoonPhase is { IsValid: true } phase ? phase : null;
        (long start, long end, long current) = GetTimes(validatedMoon);
        return new AstroCardViewModel(false, Strings.Get("ephemeris_moon"), start, end, current,
            riseText, Strings.Format("ephemeris_moonrise_at", riseText),
            setText, Strings.Format("ephemeris_moonset_at", setText),
            moonPhase is not null ? MoonPhaseFormatting.GetDescription(moonPhase) : null,
            moonPhase?.Angle, MoonLine, animateElements);
    }

    // Without both times the arc is empty: its start and end are just after now.
    private static (long Start, long End, long Current) GetTimes(Astro astro)
    {
        long current = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return astro.RiseDate is { } rise && astro.SetDate is { } set
            ? (rise.ToUnixTimeMilliseconds(), set.ToUnixTimeMilliseconds(), current)
            : (current + 1, current + 1, current);
    }
}
