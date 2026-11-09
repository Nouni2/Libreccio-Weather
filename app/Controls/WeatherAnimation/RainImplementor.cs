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
/// Rain, sleet and thunderstorms: rounded streaks falling at 8° in three sizes and colors, with flashes of light for
/// thunderstorms. Nothing is drawn when animations are off.
/// </summary>
public sealed class RainImplementor : WeatherAnimationImplementor
{
    public const int TypeRain = 1;
    public const int TypeThunderstorm = 3;
    public const int TypeSleet = 4;

    /// <summary>
    /// The weather's own color, as packed ARGB, which tints the pull-to-refresh indicator.
    /// </summary>
    public static uint GetThemeColor(int type, bool daylight) => type switch
    {
        TypeSleet => daylight ? 0xFF68BAFF : 0xFF1A5B92,
        TypeThunderstorm => daylight ? 0xFFB296BD : 0xFF231739,
        _ => daylight ? 0xFF4297E7 : 0xFF264E8F,
    };

    // Marks that no rotation was seen yet, so the first frame does not move drops by a rotation change.
    private const float InitialRotation3D = 1000f;
    private const int RainCount = 75;
    private const int SleetCount = 45;

    private readonly bool _animate;
    private readonly Rain[] _rains;
    private readonly CloudImplementor.Thunder? _thunder;
    private float _lastRotation3D;

    public RainImplementor(int[] canvasSizes, bool animate, int type, bool daylight)
    {
        _animate = animate;
        if (animate)
        {
            Color[] colors;
            int rainCount;
            switch (type)
            {
                case TypeThunderstorm:
                    rainCount = RainCount;
                    _thunder = new CloudImplementor.Thunder(minimumDelay: 3000);
                    colors = daylight
                        ? [Rgb(182, 142, 82), Color.FromRgb(0x6C, 0x55, 0x92), Rgb(255, 255, 255)]
                        : [Rgb(182, 142, 82), Rgb(88, 92, 113), Rgb(255, 255, 255)];
                    break;
                case TypeSleet:
                    rainCount = SleetCount;
                    colors = daylight
                        ? [Rgb(128, 197, 255), Rgb(185, 222, 255), Rgb(255, 255, 255)]
                        : [Rgb(40, 102, 155), Rgb(99, 144, 182), Rgb(255, 255, 255)];
                    break;
                default:
                    rainCount = RainCount;
                    colors = daylight
                        ? [Rgb(223, 179, 114), Rgb(152, 175, 222), Rgb(255, 255, 255)]
                        : [Rgb(182, 142, 82), Rgb(88, 92, 113), Rgb(255, 255, 255)];
                    break;
            }

            float[] scales = [0.6f, 0.8f, 1f];
            _rains = new Rain[rainCount];
            for (int i = 0; i < rainCount; i++)
            {
                _rains[i] = new Rain(canvasSizes[0], canvasSizes[1], colors[i * 3 / rainCount], scales[i * 3 / rainCount], type);
            }
        }
        else
        {
            _rains = [];
        }

        _lastRotation3D = InitialRotation3D;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        if (!_animate)
        {
            return;
        }

        foreach (Rain rain in _rains)
        {
            rain.Move(interval, _lastRotation3D == InitialRotation3D ? 0f : rotation3D - _lastRotation3D);
        }

        _thunder?.Shine(interval);
        _lastRotation3D = rotation3D;
    }

    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        canvas.Rotate(rotation2D + 8f, canvasSizes[0] * 0.5f, canvasSizes[1] * 0.5f);
        foreach (Rain rain in _rains)
        {
            canvas.DrawRoundRect(
                rain.Left, rain.Top, rain.Right, rain.Bottom, rain.Width / 2f, WithAlpha(rain.Color, (1 - scrollRate) * 255));
        }

        if (_thunder is { } thunder)
        {
            canvas.DrawColor(Color.FromArgb(
                (byte)Math.Clamp((int)((1 - scrollRate) * thunder.Alpha * 255 * 0.66), 0, 255),
                CloudImplementor.Thunder.Red, CloudImplementor.Thunder.Green, CloudImplementor.Thunder.Blue));
        }
    }

    private sealed class Rain
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
        private float _height;

        public Rain(int viewWidth, int viewHeight, Color color, float scale, int type)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            Color = color;
            Scale = scale;
            _canvasSize = (int)Math.Pow((viewWidth * viewWidth) + (viewHeight * viewHeight), 0.5);
            double velocity = type == TypeSleet ? 3.0 : 5.0;
            _speed = (float)(_canvasSize / (1000.0 * (1.75 + NextDouble())) * velocity);
            _maxWidth = (float)((type == TypeSleet ? 0.006 : 0.003) * _canvasSize);
            _minWidth = (float)((type == TypeSleet ? 0.004 : 0.002) * _canvasSize);
            _maxHeight = _maxWidth * 10;
            _minHeight = _minWidth * 6;
            Init(true);
        }

        public Color Color { get; }

        public float Scale { get; }

        public float Width { get; private set; }

        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Right { get; private set; }

        public float Bottom { get; private set; }

        public void Move(long interval, float deltaRotation3D)
        {
            _y += (float)(_speed * interval
                * (Math.Pow(Scale, 1.5) - (5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Cos(ToRadians(8)))));
            _x -= (float)(_speed * interval * 5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Sin(ToRadians(8)));
            if (_y >= _canvasSize)
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
            _x = NextInt(_canvasSize);
            _y = firstTime ? NextInt((int)(_canvasSize - _maxHeight)) - _canvasSize : -_maxHeight * (1 + (2 * NextFloat()));
            Width = _minWidth + (NextFloat() * (_maxWidth - _minWidth));
            _height = _minHeight + (NextFloat() * (_maxHeight - _minHeight));
            BuildRect();
        }

        private void BuildRect()
        {
            float x = (float)(_x - ((_canvasSize - _viewWidth) * 0.5));
            float y = (float)(_y - ((_canvasSize - _viewHeight) * 0.5));
            Left = x;
            Top = y;
            Right = x + (Width * Scale);
            Bottom = y + (_height * Scale);
        }
    }
}
