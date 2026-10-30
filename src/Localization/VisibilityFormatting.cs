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

public static class VisibilityFormatting
{
    public const double VeryPoor = 1000.0;
    public const double Poor = 4000.0;
    public const double Moderate = 10000.0;
    public const double Good = 20000.0;
    public const double Clear = 40000.0;

    /// <summary>
    /// The lower bound of each visibility category.
    /// </summary>
    public static IReadOnlyList<Distance> ScaleThresholds { get; } =
    [
        Distance.FromMeters(0L),
        Distance.FromMeters(VeryPoor),
        Distance.FromMeters(Poor),
        Distance.FromMeters(Moderate),
        Distance.FromMeters(Good),
        Distance.FromMeters(Clear),
    ];

    public static string? GetDescription(Distance visibility)
    {
        return visibility.InMeters switch
        {
            >= 0.0 and < VeryPoor => Strings.Get("visibility_very_poor"),
            >= VeryPoor and < Poor => Strings.Get("visibility_poor"),
            >= Poor and < Moderate => Strings.Get("visibility_moderate"),
            >= Moderate and < Good => Strings.Get("visibility_good"),
            >= Good and < Clear => Strings.Get("visibility_clear"),
            >= Clear and <= double.MaxValue => Strings.Get("visibility_perfectly_clear"),
            _ => null,
        };
    }
}
