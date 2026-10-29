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

namespace LibreccioWeather.Core.Models;

public sealed record Wind
{
    /// <summary>
    /// Direction the wind comes from, between 0 and 360 degrees, or -1 when the direction is variable.
    /// </summary>
    public double? Degree { get; init; }

    public Speed? Speed { get; init; }

    public Speed? Gusts { get; init; }

    public bool IsValid => Speed is { } speed && speed.Value > 0;

    /// <summary>
    /// An arrow pointing where the wind blows to, which is opposite to where it comes from.
    /// </summary>
    public string? Arrow
    {
        get
        {
            // Ranges share their bounds, so the first matching range wins.
            return Degree switch
            {
                null => null,
                -1.0 => "⟳",
                >= 22.5 and <= 67.5 => "↙",
                >= 67.5 and <= 112.5 => "←",
                >= 112.5 and <= 157.5 => "↖",
                >= 157.5 and <= 202.5 => "↑",
                >= 202.5 and <= 247.5 => "↗",
                >= 247.5 and <= 292.5 => "→",
                >= 292.5 and <= 337.5 => "↘",
                _ => "↓",
            };
        }
    }

    public static double? ValidateDegree(double? degree)
    {
        return degree is { } value && (value == -1.0 || value is >= 0.0 and <= 360.0) ? value : null;
    }
}
