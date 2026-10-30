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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Cloud cover in the WMO categories used for aviation, measured in oktas.
/// </summary>
public static class CloudCoverFormatting
{
    public const double Skc = 12.5;
    public const double Few = 37.5;
    public const double Sct = 62.5;
    public const double Bkn = 87.5;
    public const double Ovc = 100.0;

    /// <summary>
    /// The lower bound of each category, then the top of the scale.
    /// </summary>
    public static IReadOnlyList<double> ScaleThresholds { get; } = [0.0, Skc, Few, Sct, Bkn, Ovc];

    public static string? GetDescription(Ratio cloudCover)
    {
        return cloudCover.InPercent switch
        {
            >= 0.0 and < Skc => Strings.Get("common_weather_text_clear_sky"),
            >= Skc and < Few => Strings.Get("common_weather_text_mostly_clear"),
            >= Few and < Sct => Strings.Get("common_weather_text_partly_cloudy"),
            >= Sct and < Bkn => Strings.Get("common_weather_text_mostly_cloudy"),
            >= Bkn and <= Ovc => Strings.Get("common_weather_text_cloudy"),
            _ => null,
        };
    }
}
