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

namespace LibreccioWeather.Core.Colors;

/// <summary>
/// Converts packed ARGB colors to hue, saturation and value and back. Every step is computed in single precision
/// and each component rounds half up, so a color brightened or desaturated past its limits lands on the same bytes
/// as on Android.
/// </summary>
public static class HsvColor
{
    // A saturation this close to zero is drawn as a grey.
    private const float NearlyZero = 1f / (1 << 12);

    /// <summary>
    /// Returns the hue in degrees from 0 to 360, then the saturation and the value from 0 to 1. Alpha is ignored.
    /// </summary>
    public static float[] ColorToHsv(uint color)
    {
        int red = (int)((color >> 16) & 0xFF);
        int green = (int)((color >> 8) & 0xFF);
        int blue = (int)(color & 0xFF);
        int min = Math.Min(red, Math.Min(green, blue));
        int max = Math.Max(red, Math.Max(green, blue));
        int delta = max - min;
        float value = max / 255f;
        if (delta == 0)
        {
            return [0f, 0f, value];
        }

        float saturation = (float)delta / max;
        float hue;
        if (red == max)
        {
            hue = (float)(green - blue) / delta;
        }
        else if (green == max)
        {
            hue = 2f + (float)(blue - red) / delta;
        }
        else
        {
            hue = 4f + (float)(red - green) / delta;
        }

        hue *= 60f;
        if (hue < 0)
        {
            hue += 360f;
        }

        return [hue, saturation, value];
    }

    /// <summary>
    /// Builds a color from a hue in degrees, a saturation and a value. Saturation and value outside 0 to 1 are
    /// clamped, and a hue outside 0 to 360 counts as 0.
    /// </summary>
    public static uint HsvToColor(float[] hsv, byte alpha = 0xFF)
    {
        float saturation = Pin(hsv[1]);
        float value = Pin(hsv[2]);
        uint valueByte = RoundToByte(value * 255);
        if (Math.Abs(saturation) <= NearlyZero)
        {
            return Pack(alpha, valueByte, valueByte, valueByte);
        }

        float sector = hsv[0] < 0 || hsv[0] >= 360 ? 0 : hsv[0] / 60;
        float whole = MathF.Floor(sector);
        float fraction = sector - whole;
        uint p = RoundToByte((1f - saturation) * value * 255);
        uint q = RoundToByte((1f - (saturation * fraction)) * value * 255);
        uint t = RoundToByte((1f - (saturation * (1f - fraction))) * value * 255);
        return (uint)whole switch
        {
            0 => Pack(alpha, valueByte, t, p),
            1 => Pack(alpha, q, valueByte, p),
            2 => Pack(alpha, p, valueByte, t),
            3 => Pack(alpha, p, q, valueByte),
            4 => Pack(alpha, t, p, valueByte),
            _ => Pack(alpha, valueByte, p, q),
        };
    }

    // NaN pins to 0, where Math.Clamp would keep it.
    private static float Pin(float component)
    {
        float upper = 1f < component ? 1f : component;
        return 0f < upper ? upper : 0f;
    }

    private static uint RoundToByte(float component) => (uint)(int)MathF.Floor(component + 0.5f);

    private static uint Pack(byte alpha, uint red, uint green, uint blue)
    {
        return ((uint)alpha << 24) | (red << 16) | (green << 8) | blue;
    }
}
