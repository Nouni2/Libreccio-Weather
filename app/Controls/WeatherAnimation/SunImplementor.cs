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
/// A clear day: three sets of turning squares, each set four squares at 22.5° from one another, centered at the
/// right edge, halfway down the width.
/// </summary>
public sealed class SunImplementor : WeatherAnimationImplementor
{
    // Lower than the top of the screen, so the sun does not sit behind the top bar's icons.
    private const double SunPosition = 0.5;
    private const double SmallSunAlpha = 0.16;
    private const double MediumSunAlpha = 0.08;
    private const double LargeSunAlpha = 0.04;

    private readonly Color _color;
    private readonly float[] _angles = new float[3];
    private readonly float[] _unitSizes;

    public SunImplementor(int[] canvasSizes, bool darkMode)
    {
        _color = darkMode ? Colors.Black : Colors.White;
        _unitSizes =
        [
            (float)(0.5 * 0.47 * canvasSizes[0]),
            (float)(1.7794 * 0.5 * 0.47 * canvasSizes[0]),
            (float)(3.0594 * 0.5 * 0.47 * canvasSizes[0]),
        ];
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        for (int i = 0; i < _angles.Length; i++)
        {
            _angles[i] = (float)((_angles[i] + (90.0 / (3000 + (1000 * i)) * interval)) % 90);
        }
    }

    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        float deltaX = (float)(Math.Sin(ToRadians(rotation2D)) * 0.3 * canvasSizes[0]);
        float deltaY = (float)(Math.Sin(ToRadians(rotation3D)) * -0.3 * canvasSizes[0]);
        canvas.Translate(canvasSizes[0] + deltaX, (float)((SunPosition * canvasSizes[0]) + deltaY));

        double[] alphas = [SmallSunAlpha, MediumSunAlpha, LargeSunAlpha];
        for (int index = 0; index < alphas.Length; index++)
        {
            Color color = WithAlpha(_color, (1 - scrollRate) * 255 * alphas[index]);
            canvas.Rotate(_angles[index]);
            for (int i = 0; i <= 3; i++)
            {
                canvas.DrawRect(-_unitSizes[index], -_unitSizes[index], _unitSizes[index], _unitSizes[index], color);
                canvas.Rotate(22.5);
            }

            canvas.Rotate(-90 - _angles[index]);
        }
    }
}
