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
/// Snow: 50 flakes in three sizes and colors, drifting sideways as they fall.
/// </summary>
public sealed class SnowImplementor : WeatherAnimationImplementor
{
    // Marks that no rotation was seen yet, so the first frame does not move flakes by a rotation change.
    private const float InitialRotation3D = 1000f;
    private const int SnowCount = 50;
    private const double SnowSpeed = 1.5;

    private readonly Snow[] _snows;
    private float _lastRotation3D;

    public SnowImplementor(int[] canvasSizes, bool daylight)
    {
        Color[] colors = daylight
            ? [Rgb(190, 225, 255), Rgb(211, 233, 255), Rgb(255, 255, 255)]
            : [Rgb(111, 133, 155), Rgb(140, 161, 182), Rgb(255, 255, 255)];
        float[] scales = [0.6f, 0.8f, 1f];
        _snows = new Snow[SnowCount];
        for (int i = 0; i < SnowCount; i++)
        {
            _snows[i] = new Snow(canvasSizes[0], canvasSizes[1], colors[i * 3 / SnowCount], scales[i * 3 / SnowCount]);
        }

        _lastRotation3D = InitialRotation3D;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        foreach (Snow snow in _snows)
        {
            snow.Move(interval, _lastRotation3D == InitialRotation3D ? 0f : rotation3D - _lastRotation3D);
        }

        _lastRotation3D = rotation3D;
    }

    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        canvas.Rotate(rotation2D, canvasSizes[0] * 0.5f, canvasSizes[1] * 0.5f);
        foreach (Snow snow in _snows)
        {
            canvas.DrawCircle(snow.CenterX, snow.CenterY, snow.Radius, WithAlpha(snow.Color, (1 - scrollRate) * 255));
        }
    }

    private sealed class Snow
    {
        private readonly int _viewWidth;
        private readonly int _viewHeight;
        private readonly int _canvasSize;
        private readonly float _speedY;
        private float _cx;
        private float _cy;
        private float _speedX;

        public Snow(int viewWidth, int viewHeight, Color color, float scale)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            Color = color;
            Scale = scale;
            _canvasSize = (int)Math.Pow((viewWidth * viewWidth) + (viewHeight * viewHeight), 0.5);
            Radius = (float)(_canvasSize * (0.005 + (NextDouble() * 0.007)) * scale);
            _speedY = (float)(_canvasSize / (1000.0 * (2.5 + NextDouble())) * SnowSpeed);
            Init(true);
        }

        public Color Color { get; }

        public float Scale { get; }

        public float Radius { get; }

        public float CenterX { get; private set; }

        public float CenterY { get; private set; }

        public void Move(long interval, float deltaRotation3D)
        {
            _cx += (float)(_speedX * interval * Math.Pow(Scale, 1.5));
            _cy += (float)(_speedY * interval * (Math.Pow(Scale, 1.5) - (5 * Math.Sin(ToRadians(deltaRotation3D)))));
            if (CenterY >= _canvasSize)
            {
                Init(false);
            }
            else
            {
                ComputeCenterPosition();
            }
        }

        private void Init(bool firstTime)
        {
            _cx = NextInt(_canvasSize);
            _cy = firstTime ? NextInt((int)(_canvasSize - Radius)) - _canvasSize : -Radius;

            // At least 2, so a small window still gets some sideways drift.
            _speedX = NextInt(Math.Max((int)(2 * _speedY), 2)) - _speedY;
            ComputeCenterPosition();
        }

        private void ComputeCenterPosition()
        {
            CenterX = (int)(_cx - ((_canvasSize - _viewWidth) * 0.5));
            CenterY = (int)(_cy - ((_canvasSize - _viewHeight) * 0.5));
        }
    }
}
