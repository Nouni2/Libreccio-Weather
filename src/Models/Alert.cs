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

namespace LibreccioWeather.Core.Models;

public sealed record Alert
{
    /// <summary>
    /// Stable identity of the alert, so a refresh does not notify it again.
    /// When the source has none, build it from a hash of the alert type or title, its level and its start time.
    /// </summary>
    public required string AlertId { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    public string? Headline { get; init; }

    /// <summary>
    /// May contain HTML. Line breaks in plain text are shown as HTML line breaks.
    /// </summary>
    public string? Description { get; init; }

    public string? Instruction { get; init; }

    public string? Source { get; init; }

    public AlertSeverity Severity { get; init; } = AlertSeverity.Unknown;

    /// <summary>
    /// Color as 0xAARRGGBB.
    /// </summary>
    public required int Color { get; init; }

    public static int ColorFromSeverity(AlertSeverity severity)
    {
        return severity switch
        {
            AlertSeverity.Extreme => Rgb(212, 45, 65),
            AlertSeverity.Severe => Rgb(240, 140, 17),
            AlertSeverity.Moderate => Rgb(244, 207, 0),
            AlertSeverity.Minor => Rgb(57, 156, 199),
            _ => Rgb(130, 168, 223),
        };
    }

    private static int Rgb(int red, int green, int blue)
    {
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }
}
