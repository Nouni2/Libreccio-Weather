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
    // Layout reads the factor several times per pass, and on every frame while a pane slides, so the registry is read
    // at most once a second; a change of text size still shows at the next layout after that.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(1);
    private static float _factor = 1;
    private static long _readAt = long.MinValue;

    public static float Factor
    {
        get
        {
            long now = Environment.TickCount64;
            if (_readAt != long.MinValue && now - _readAt < CacheDuration.TotalMilliseconds)
            {
                return _factor;
            }

            _factor = Read();
            _readAt = now;
            return _factor;
        }
    }

    private static float Read()
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

    /// <summary>
    /// The scale of widths that hold text: half of the text size's change from 100 %, so 150 % text widens them by
    /// 25 %.
    /// </summary>
    public static float FontScaleToApply
    {
        get
        {
            float scale = Factor;
            return scale != 1f ? 1f + (Math.Abs(scale - 1f) / 2f * (scale > 1f ? 1f : -1f)) : scale;
        }
    }
}
