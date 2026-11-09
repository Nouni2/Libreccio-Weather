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
/// Wind: 160 green streaks blowing across the screen in three sizes and colors.
/// </summary>
public sealed class WindImplementor : WeatherAnimationImplementor
{
    /// <summary>
    /// The weather's own color, as packed ARGB, which tints the pull-to-refresh indicator.
    /// </summary>
    public static uint GetThemeColor(bool daylight) => daylight ? 0xFFEACDA3 : 0xFF958675;

    // Marks that no rotation was seen yet, so the first frame does not move streaks by a rotation change.
    private const float InitialRotation3D = 1000f;
    private const int WindCount = 160;

    private readonly Wind[] _winds;
    private float _lastRotation3D;

    public WindImplementor(int[] canvasSizes, bool daylight)
    {
        Color[] colors = daylight
            ? [Hex("#C2E4CA"), Hex("#B2E0BA"), Hex("#D2F0DA")]
            : [Hex("#313E3A"), Hex("#529B73"), Hex("#638170")];
        float[] scales = [0.6f, 0.8f, 1f];
        _winds = new Wind[WindCount];
        for (int i = 0; i < WindCount; i++)
        {
            _winds[i] = new Wind(canvasSizes[0], canvasSizes[1], colors[i * 3 / WindCount], scales[i * 3 / WindCount]);
        }

        _lastRotation3D = InitialRotation3D;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        foreach (Wind wind in _winds)
        {
            wind.Move(interval, _lastRotation3D == InitialRotation3D ? 0f : rotation3D - _lastRotation3D);
        }

        _lastRotation3D = rotation3D;
    }

    // The streaks blow horizontally: the scene turns only by the device's tilt, without a slant of its own.
    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        canvas.Rotate(rotation2D, canvasSizes[0] * 0.5f, canvasSizes[1] * 0.5f);
        foreach (Wind wind in _winds)
        {
            canvas.DrawRect(wind.Left, wind.Top, wind.Right, wind.Bottom, WithAlpha(wind.Color, (1 - scrollRate) * 255));
        }
    }

    private sealed class Wind
    {
        private readonly int _viewWidth;
        private readonly int _viewHeight;
        private readonly int _canvasSize;
        private readonly float _speed;
        private readonly float _maxWidth;
        private readonly float _minWidth;
        private readonly float _maxHeight;
        private readonly float _minHeight;
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        public Wind(int viewWidth, int viewHeight, Color color, float scale)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            Color = color;
            Scale = scale;
            _canvasSize = (int)Math.Pow((viewWidth * viewWidth) + (viewHeight * viewHeight), 0.5);
            _speed = (float)(_canvasSize / (1000.0 * (0.5 + NextDouble())) * 6.0);
            _maxHeight = 0.007f * _canvasSize;
            _minHeight = 0.005f * _canvasSize;
            _maxWidth = _maxHeight * 10;
            _minWidth = _minHeight * 6;
            Init(true);
        }

        public Color Color { get; }

        public float Scale { get; }

        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Right { get; private set; }

        public float Bottom { get; private set; }

        public void Move(long interval, float deltaRotation3D)
        {
            _x += (float)(_speed * interval
                * (Math.Pow(Scale, 1.5) + (5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Cos(ToRadians(16)))));
            _y -= (float)(_speed * interval * 5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Sin(ToRadians(16)));
            if (_x >= _canvasSize)
            {
                Init(false);
            }
            else
            {
                BuildRect();
            }
        }

        private void Init(bool firstTime)
        {
            _y = NextInt(_canvasSize);
            _x = firstTime ? NextInt((int)(_canvasSize - _maxHeight)) - _canvasSize : -_maxHeight;
            _width = _minWidth + (NextFloat() * (_maxWidth - _minWidth));
            _height = _minHeight + (NextFloat() * (_maxHeight - _minHeight));
            BuildRect();
        }

        private void BuildRect()
        {
            float x = (float)(_x - ((_canvasSize - _viewWidth) * 0.5));
            float y = (float)(_y - ((_canvasSize - _viewHeight) * 0.5));
            Left = x;
            Top = y;
            Right = x + (_width * Scale);
            Bottom = y + (_height * Scale);
        }
    }
}
