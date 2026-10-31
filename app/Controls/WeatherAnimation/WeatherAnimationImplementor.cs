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

using System.Windows.Media;

// Windows Forms, there for the tray icon, also brings System.Drawing into scope.
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.Controls.WeatherAnimation;

/// <summary>
/// One kind of weather animation. Sizes are the width and height of the drawing area, intervals are in
/// milliseconds, rotations are in degrees and always zero on Windows, which has no gravity sensor to tilt the scene,
/// and the scroll rate goes from 0 at the top of the screen to 1 once the scene has faded out.
/// </summary>
public abstract class WeatherAnimationImplementor
{
    public abstract void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D);

    public abstract void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D);

    /// <summary>
    /// A color with an opacity from 0 to 255, truncated as an integer alpha is.
    /// </summary>
    protected static Color WithAlpha(Color color, double alpha)
    {
        return Color.FromArgb((byte)Math.Clamp((int)alpha, 0, 255), color.R, color.G, color.B);
    }

    protected static Color Rgb(byte red, byte green, byte blue)
    {
        return Color.FromRgb(red, green, blue);
    }

    /// <summary>
    /// A color written as #RRGGBB.
    /// </summary>
    protected static Color Hex(string color)
    {
        return (Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
    }

    /// <summary>
    /// A whole number from 0 up to the bound, or 0 when the area is too small to have one.
    /// </summary>
    protected static int NextInt(int bound)
    {
        return bound > 0 ? Random.Shared.Next(bound) : 0;
    }

    protected static float NextFloat()
    {
        return Random.Shared.NextSingle();
    }

    protected static double NextDouble()
    {
        return Random.Shared.NextDouble();
    }

    protected static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
