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

namespace LibreccioWeather.Background;

/// <summary>
/// Whether Windows' Battery saver is on. Windows or the user turns it on; this only reads its state.
/// </summary>
internal static class BatterySaver
{
    // SystemStatusFlag in SYSTEM_POWER_STATUS is 1 while Battery saver is on.
    private const byte BatterySaverOn = 1;

    public static bool IsOn()
    {
        // When the state cannot be read, updates are not held back.
        return GetSystemPowerStatus(out SystemPowerStatus status) && status.SystemStatusFlag == BatterySaverOn;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
