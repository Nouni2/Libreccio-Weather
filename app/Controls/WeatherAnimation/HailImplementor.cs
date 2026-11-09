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
/// Hail: 51 spinning squares in three sizes and colors.
/// </summary>
public sealed class HailImplementor : WeatherAnimationImplementor
{
    /// <summary>
    /// The weather's own color, as packed ARGB, which tints the pull-to-refresh indicator.
    /// </summary>
    public static uint GetThemeColor(bool daylight) => daylight ? 0xFF68BAFF : 0xFF1A5B92;

    // Marks that no rotation was seen yet, so the first frame does not move hailstones by a rotation change.
    private const float InitialRotation3D = 1000f;
    private const int HailCount = 51;

    private readonly Hail[] _hails;
    private float _lastRotation3D;

    public HailImplementor(int[] canvasSizes, bool daylight)
    {
        Color[] colors = daylight
            ? [Rgb(128, 197, 255), Rgb(185, 222, 255), Rgb(255, 255, 255)]
            : [Rgb(40, 102, 155), Rgb(99, 144, 182), Rgb(255, 255, 255)];
        float[] scales = [0.6f, 0.8f, 1f];
        _hails = new Hail[HailCount];
        for (int i = 0; i < HailCount; i++)
        {
            _hails[i] = new Hail(canvasSizes[0], canvasSizes[1], colors[i * 3 / HailCount], scales[i * 3 / HailCount]);
        }

        _lastRotation3D = InitialRotation3D;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        foreach (Hail hail in _hails)
        {
            hail.Move(interval, _lastRotation3D == InitialRotation3D ? 0f : rotation3D - _lastRotation3D);
        }

        _lastRotation3D = rotation3D;
    }

    // Each square turns about its own position before the canvas's offset is applied, which places it differently
    // from its computed center; the drawing keeps that placement.
    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        canvas.Rotate(rotation2D, canvasSizes[0] * 0.5f, canvasSizes[1] * 0.5f);
        foreach (Hail hail in _hails)
        {
            canvas.Rotate(hail.Rotation, hail.Cx, hail.Cy);
            canvas.DrawRect(hail.Left, hail.Top, hail.Right, hail.Bottom, WithAlpha(hail.Color, (1 - scrollRate) * 255));
            canvas.Rotate(-hail.Rotation, hail.Cx, hail.Cy);
        }
    }

    private sealed class Hail
    {
        private readonly int _viewWidth;
        private readonly int _viewHeight;
        private readonly int _canvasSize;
        private readonly float _size;
        private readonly float _speedY;
        private float _speedX;
        private float _speedRotation;

        public Hail(int viewWidth, int viewHeight, Color color, float scale)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            Color = color;
            Scale = scale;
            _canvasSize = (int)Math.Pow((viewWidth * viewWidth) + (viewHeight * viewHeight), 0.5);
            _size = (float)(0.0324 * Math.Min(viewWidth, viewHeight)) * 0.8f;
            _speedY = Math.Min(viewWidth, viewHeight) / 150f;
            Init(true);
        }

        public Color Color { get; }

        public float Scale { get; }

        public float Cx { get; private set; }

        public float Cy { get; private set; }

        public float CenterX { get; private set; }

        public float CenterY { get; private set; }

        public float Rotation { get; private set; }

        // Empty until the first move.
        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Right { get; private set; }

        public float Bottom { get; private set; }

        public void Move(long interval, float deltaRotation3D)
        {
            Cx += (float)(_speedX * interval * Math.Pow(Scale, 1.5));
            Cy += (float)(_speedY * interval * (Math.Pow(Scale, 1.5) - (5 * Math.Sin(ToRadians(deltaRotation3D)))));
            Rotation = (Rotation + (_speedRotation * interval)) % 360;
            if (Cy - _size >= _canvasSize)
            {
                Init(false);
            }
            else
            {
                ComputeCenterPosition();
            }

            Left = Cx - (_size * Scale);
            Top = Cy - (_size * Scale);
            Right = Cx + (_size * Scale);
            Bottom = Cy + (_size * Scale);
        }

        private void Init(bool firstTime)
        {
            Cx = NextInt(_canvasSize);
            Cy = firstTime ? NextInt((int)(_canvasSize - _size)) - _canvasSize : -_size;
            Rotation = 360 * NextFloat();
            _speedRotation = 360f / 500f * NextFloat();
            _speedX = 0.75f * (NextFloat() * _speedY * (Random.Shared.Next(2) == 0 ? 1 : -1));
            ComputeCenterPosition();
        }

        private void ComputeCenterPosition()
        {
            CenterX = (float)(Cx - ((_canvasSize - _viewWidth) * 0.5));
            CenterY = (float)(Cy - ((_canvasSize - _viewHeight) * 0.5));
        }
    }
}
