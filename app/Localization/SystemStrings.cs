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

using System.Runtime.InteropServices;

namespace LibreccioWeather.Localization;

/// <summary>
/// Button text that Breezy Weather takes from Android itself, taken here from Windows in the Windows display language.
/// </summary>
internal static class SystemStrings
{
    // MB_GetString takes a dialog button id minus one; IDCANCEL is 2.
    private const uint CancelButton = 1;

    public static string Cancel => GetButtonText(CancelButton);

    private static string GetButtonText(uint button)
    {
        IntPtr text = MB_GetString(button);
        return text == IntPtr.Zero ? "" : Marshal.PtrToStringUni(text) ?? "";
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MB_GetString(uint button);
}
