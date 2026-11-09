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

using LibreccioWeather.Core.Colors;

namespace LibreccioWeather.Core.Tests.Colors;

public sealed class HsvColorTests
{
    // The night weather colours are brightened by lowering saturation and raising value by a quarter. Transparent
    // black becomes an opaque grey, and a saturation pushed below zero is clamped to a grey.
    [Theory]
    [InlineData(0x00000000u, 0xFF404040u)]
    [InlineData(0xFF4F5D68u, 0xFFA8A8A8u)]
    [InlineData(0xFF141C2Cu, 0xFF4C576Cu)]
    [InlineData(0xFF264E8Fu, 0xFF6B91CFu)]
    public void HsvToColor_ShiftedPastLimits_ClampsAndRoundsHalfUp(uint color, uint expected)
    {
        float[] hsv = HsvColor.ColorToHsv(color);
        hsv[1] -= 0.25f;
        hsv[2] += 0.25f;

        Assert.Equal(expected, HsvColor.HsvToColor(hsv));
    }
}
