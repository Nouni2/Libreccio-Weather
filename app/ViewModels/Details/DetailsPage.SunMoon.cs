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

using System.Text;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels.Details;

internal static partial class DetailsPage
{
    // The sun and moon altitude chart and the moon phase need sun and moon positions, which are not computed, so the
    // page shows the rise and set times it has, and its explanation.
    private static void BuildSunMoon(DetailsContext context, List<object> blocks)
    {
        Daily today = context.Daily;
        if (today.Sun?.IsValid == true || today.Moon?.IsValid == true)
        {
            List<DetailsAstroItem> items = [];
            if (today.Sun is { IsValid: true } sun)
            {
                items.Add(SunItem(context, sun));
            }

            if (today.Twilight is { IsValid: true } twilight)
            {
                items.Add(RiseSetItem(context, "IconTwilight", twilight, "ephemeris_dawn_at", "ephemeris_dusk_at"));
            }

            if (today.Moon is { IsValid: true } moon)
            {
                items.Add(RiseSetItem(context, "IconClearNight", moon, "ephemeris_moonrise_at", "ephemeris_moonset_at"));
            }

            blocks.Add(new DetailsSpacer(NormalMargin));
            blocks.Add(new DetailsAstroList(items));
        }
        else
        {
            blocks.Add(new DetailsMessage(Strings.Get("chart_no_daily_data")));
        }

        blocks.Add(new DetailsSpacer(NormalMargin));
        AddAbout(blocks, "ephemeris_about",
            Strings.Get("ephemeris_about_rise") + " " + Strings.Get("ephemeris_about_set") + "\n\n"
                + Strings.Get("ephemeris_about_dawn") + " " + Strings.Get("ephemeris_about_dusk"));
    }

    private static DetailsAstroItem SunItem(DetailsContext context, Astro sun)
    {
        string? sunriseTime = FormatHour(context, sun.RiseDate);
        string? sunsetTime = FormatHour(context, sun.SetDate);
        string text = FormatAstroTime(context, sun.RiseDate) + "↑ / " + FormatAstroTime(context, sun.SetDate) + "↓"
            + (sun.Duration is { } duration ? " / " + DurationFormatting.FormatHours(duration) : "");

        var talkBack = new StringBuilder();
        if (sunriseTime is not null)
        {
            talkBack.Append(Strings.Format("ephemeris_sunrise_at", sunriseTime));
        }

        if (sunsetTime is not null)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            talkBack.Append(Strings.Format("ephemeris_sunset_at", sunsetTime));
        }

        if (sun.Duration is { } spokenDuration)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            // The duration follows its label without a separator, as on Android.
            talkBack.Append(Strings.Get("sunshine_duration"));
            talkBack.Append(DurationFormatting.FormatTime(spokenDuration, UnitWidth.Long));
        }

        return new DetailsAstroItem("IconClearDay", text, talkBack.ToString());
    }

    private static DetailsAstroItem RiseSetItem(DetailsContext context, string iconKey, Astro astro, string riseKey, string setKey)
    {
        string? rise = FormatHour(context, astro.RiseDate);
        string? set = FormatHour(context, astro.SetDate);
        string text = FormatAstroTime(context, astro.RiseDate) + "↑ / " + FormatAstroTime(context, astro.SetDate) + "↓";

        var talkBack = new StringBuilder();
        if (rise is not null)
        {
            talkBack.Append(Strings.Format(riseKey, rise));
        }

        if (set is not null)
        {
            if (talkBack.Length > 0)
            {
                talkBack.Append(Strings.GetUnit("locale_separator"));
            }

            talkBack.Append(Strings.Format(setKey, set));
        }

        return new DetailsAstroItem(iconKey, text, talkBack.ToString());
    }

    // Debug builds show the full date and time, as debuggable builds do on Android.
    private static string FormatAstroTime(DetailsContext context, DateTimeOffset? date)
    {
        if (date is not { } value)
        {
            return Strings.Get("null_data_text");
        }
#if DEBUG
        return TimeZoneInfo.ConvertTime(value, context.TimeZone).ToString("yyyy-MM-dd HH:mm", Strings.Culture);
#else
        return DateFormatting.GetFormattedTime(value, context.TimeZone);
#endif
    }
}
