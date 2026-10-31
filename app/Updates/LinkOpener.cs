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

using System.ComponentModel;
using System.Diagnostics;

namespace LibreccioWeather.Updates;

/// <summary>
/// Opens a web or mail link with the program Windows associates with it.
/// </summary>
public static class LinkOpener
{
    public static void Open(string link)
    {
        // Only web and mail links are opened, so a link can never start a program.
        if (!link.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !link.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            Trace.TraceWarning("Could not open {0}: {1}", link, exception.Message);
        }
    }
}
