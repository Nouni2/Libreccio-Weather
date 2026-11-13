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
/// Button text taken from Windows itself, in the Windows display language, so it matches the system's own dialogs and
/// window buttons.
/// </summary>
internal static class SystemStrings
{
    // MB_GetString takes a dialog button id minus one; IDCANCEL is 2.
    private const uint CancelButton = 1;

    private const uint ScMinimize = 0xF020;
    private const uint ScMaximize = 0xF030;
    private const uint ScRestore = 0xF120;
    private const uint MfByCommand = 0x00000000;

    public static string Cancel => GetButtonText(CancelButton);

    public static string Minimize(IntPtr window) => GetSystemMenuText(window, ScMinimize);

    public static string Maximize(IntPtr window) => GetSystemMenuText(window, ScMaximize);

    public static string Restore(IntPtr window) => GetSystemMenuText(window, ScRestore);

    private static string GetButtonText(uint button)
    {
        IntPtr text = MB_GetString(button);
        return text == IntPtr.Zero ? "" : Marshal.PtrToStringUni(text) ?? "";
    }

    // The window's system menu holds Windows' names for its commands, such as "Mi&nimize", with an access key marked by
    // "&" ("&&" for an ampersand) and sometimes a shortcut after a tab, such as "&Close\tAlt+F4".
    private static string GetSystemMenuText(IntPtr window, uint command)
    {
        IntPtr menu = GetSystemMenu(window, false);
        if (menu == IntPtr.Zero)
        {
            return "";
        }

        char[] buffer = new char[256];
        int length = GetMenuString(menu, command, buffer, buffer.Length, MfByCommand);
        if (length <= 0)
        {
            return "";
        }

        string text = new(buffer, 0, length);
        int tab = text.IndexOf('\t');
        if (tab >= 0)
        {
            text = text[..tab];
        }

        return text.Replace("&&", "\0").Replace("&", "").Replace('\0', '&');
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr MB_GetString(uint button);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool revert);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetMenuString(IntPtr menu, uint item, [Out] char[] text, int maxCount, uint flags);
}
