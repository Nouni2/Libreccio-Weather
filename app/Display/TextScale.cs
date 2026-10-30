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

using System.IO;
using System.Security;
using Microsoft.Win32;

namespace LibreccioWeather.Display;

internal static class TextScale
{
    public static float Factor
    {
        get
        {
            try
            {
                return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Accessibility", "TextScaleFactor", 100)
                    is int percent && percent > 0 ? percent / 100f : 1;
            }
            catch (Exception exception) when (exception is IOException or SecurityException or UnauthorizedAccessException)
            {
                return 1;
            }
        }
    }
}
