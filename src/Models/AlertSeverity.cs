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

// The numeric values are stored, so they must not change.
public enum AlertSeverity
{
    Unknown = 0,
    Minor = 1,
    Moderate = 2,
    Severe = 3,
    Extreme = 4,
}

public static class AlertSeverityExtensions
{
    extension(AlertSeverity)
    {
        public static AlertSeverity GetInstance(int? value)
        {
            return value is { } id && Enum.IsDefined(typeof(AlertSeverity), id) ? (AlertSeverity)id : AlertSeverity.Unknown;
        }
    }
}
