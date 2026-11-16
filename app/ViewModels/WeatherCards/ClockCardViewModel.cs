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

using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.MaterialColors;
using LibreccioWeather.Core.Settings.Options;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// The clock card: the location's time in digits under analog hands.
/// </summary>
public sealed class ClockCardViewModel
{
    private ClockCardViewModel(TimeZoneInfo timeZone, string automationName, Color? hourHandColor, Color? minuteHandColor)
    {
        TimeZone = timeZone;
        AutomationName = automationName;
        HourHandColor = hourHandColor;
        MinuteHandColor = minuteHandColor;
    }

    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// The time when the card was bound, which the spoken text keeps until the card is bound again.
    /// </summary>
    public string AutomationName { get; }

    public Color? HourHandColor { get; }

    public Color? MinuteHandColor { get; }

    public static ClockCardViewModel? Create(CardDisplay card, Location location)
    {
        if (card != CardDisplay.Clock)
        {
            return null;
        }

        string automationName = Strings.Get("clock") + Strings.Get("colon_separator")
            + DateFormatting.GetFormattedTime(DateTimeOffset.UtcNow, location.TimeZone);

        // The hands are Android's own drawables, colored by the system palettes system_accent1_700 and
        // system_accent2_500, which follow the wallpaper whatever the app's colors are. Windows' accent color stands for
        // the wallpaper here even with the app's accent colors off; without it, the hands are not drawn.
        Color? hourHandColor = null;
        Color? minuteHandColor = null;
        if (ThemeManager.ReadSystemAccentColor() is { } accent)
        {
            var scheme = new SchemeTonalSpot(Hct.FromInt(accent), false, 0.0);
            hourHandColor = ToColor(scheme.PrimaryPalette.Tone(100 - 700 / 10));
            minuteHandColor = ToColor(scheme.SecondaryPalette.Tone(100 - 500 / 10));
        }

        return new ClockCardViewModel(location.TimeZone, automationName, hourHandColor, minuteHandColor);
    }

    private static Color ToColor(int argb)
    {
        return Color.FromArgb((byte)((uint)argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }
}
