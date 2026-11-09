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
/// A clear night: 70 twinkling stars and, when animated, 10 meteors falling at 60°. Meteors start off screen and
/// come back 5 to 25 seconds after leaving it.
/// </summary>
public sealed class MeteorShowerImplementor : WeatherAnimationImplementor
{
    /// <summary>
    /// The weather's own color, as packed ARGB, which tints the pull-to-refresh indicator.
    /// </summary>
    public const uint ThemeColor = 0xFF141C2C;

    // Marks that no rotation was seen yet, so the first frame does not move meteors by a rotation change.
    private const float InitialRotation3D = 1000f;
    private const int MeteorReviveSecMin = 5;
    private const int MeteorReviveSecMax = 25;

    internal static readonly Color[] StarColors =
    [
        Rgb(210, 247, 255),
        Rgb(208, 233, 255),
        Rgb(175, 201, 228),
        Rgb(164, 194, 220),
        Rgb(97, 171, 220),
        Rgb(74, 141, 193),
        Rgb(54, 66, 119),
        Rgb(34, 48, 74),
        Rgb(236, 234, 213),
        Rgb(240, 220, 151),
    ];

    private readonly Meteor[] _meteors;
    private readonly Star[] _stars;
    private float _lastRotation3D;

    public MeteorShowerImplementor(int[] canvasSizes, bool animate)
    {
        int viewWidth = canvasSizes[0];
        int viewHeight = canvasSizes[1];
        _meteors = new Meteor[animate ? 10 : 0];
        for (int i = 0; i < _meteors.Length; i++)
        {
            _meteors[i] = new Meteor(viewWidth, viewHeight, StarColors[NextInt(StarColors.Length)], NextFloat());
        }

        int canvasSize = (int)Math.Pow(Math.Pow(viewWidth, 2.0) + Math.Pow(viewHeight, 2.0), 0.5);
        int width = canvasSize;
        int height = (int)(((canvasSize - viewHeight) * 0.5) + (viewWidth * 1.1111));
        float radius = (float)(0.00125 * canvasSize * (0.5 + NextFloat()));
        _stars = new Star[70];
        for (int i = 0; i < _stars.Length; i++)
        {
            int x = (int)(NextInt(width) - (0.5 * (canvasSize - viewWidth)));
            int y = (int)(NextInt(height) - (0.5 * (canvasSize - viewHeight)));
            long duration = (long)(2500 + (NextFloat() * 2500));
            _stars[i] = new Star(x, y, radius, StarColors[i % StarColors.Length], duration);
        }

        _lastRotation3D = InitialRotation3D;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        foreach (Meteor meteor in _meteors)
        {
            meteor.Update(interval, _lastRotation3D == InitialRotation3D ? 0f : rotation3D - _lastRotation3D);
        }

        foreach (Star star in _stars)
        {
            star.Shine(interval);
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
        foreach (Star star in _stars)
        {
            canvas.DrawPoint(star.CenterX, star.CenterY, star.Radius * 2, WithAlpha(star.Color, (1 - scrollRate) * star.Alpha * 255));
        }

        canvas.Rotate(60f, canvasSizes[0] * 0.5f, canvasSizes[1] * 0.5f);
        foreach (Meteor meteor in _meteors)
        {
            double centerX = (meteor.Left + meteor.Right) / 2;
            canvas.DrawLine(
                centerX, meteor.Top, centerX, meteor.Bottom, meteor.Right - meteor.Left,
                WithAlpha(meteor.Color, (1 - scrollRate) * 255));
        }
    }

    private sealed class Meteor
    {
        private readonly int _viewWidth;
        private readonly int _viewHeight;
        private readonly int _canvasSize;
        private readonly float _maxHeight;
        private readonly float _minHeight;
        private readonly float _width;
        private readonly float _speed;
        private float _x;
        private float _y;
        private float _height;
        private long _progress;
        private long _delay;

        public Meteor(int viewWidth, int viewHeight, Color color, float scale)
        {
            _viewWidth = viewWidth;
            _viewHeight = viewHeight;
            Color = color;
            Scale = scale;
            _canvasSize = (int)Math.Pow((viewWidth * viewWidth) + (viewHeight * viewHeight), 0.5);
            _width = (float)(viewWidth * 0.0088 * scale);
            _speed = viewWidth / 200f;
            _maxHeight = (float)(1.1 * viewWidth / Math.Cos(ToRadians(60.0)));
            _minHeight = (float)(_maxHeight * 0.7);
            Init(true);
        }

        public Color Color { get; }

        public float Scale { get; }

        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Right { get; private set; }

        public float Bottom { get; private set; }

        public void Update(long interval, float deltaRotation3D)
        {
            if (_y > _canvasSize)
            {
                _progress += interval;
                if (_progress > _delay)
                {
                    Init(false);
                }

                return;
            }

            Move(interval, deltaRotation3D);
            BuildRect();
        }

        private void Init(bool firstTime)
        {
            _progress = 0;
            _delay = (NextInt(MeteorReviveSecMax - MeteorReviveSecMin) + MeteorReviveSecMin) * 1000L;
            _x = NextInt(_canvasSize);

            // The first meteors start below the screen, so they come in one by one after their delay.
            _y = !firstTime ? NextInt(_canvasSize) - _maxHeight - _canvasSize : _canvasSize * 2f;
            _height = _minHeight + (NextFloat() * (_maxHeight - _minHeight));
            BuildRect();
        }

        private void BuildRect()
        {
            float x = (float)(_x - ((_canvasSize - _viewWidth) * 0.5));
            float y = (float)(_y - ((_canvasSize - _viewHeight) * 0.5));
            Left = x;
            Top = y;
            Right = x + _width;
            Bottom = y + _height;
        }

        private void Move(long interval, float deltaRotation3D)
        {
            _x -= (float)(_speed * interval * 5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Cos(ToRadians(60)));
            _y += (float)(_speed * interval
                * (Math.Pow(Scale, 0.5) - (5 * Math.Sin(ToRadians(deltaRotation3D)) * Math.Sin(ToRadians(60)))));
        }
    }

    private sealed class Star
    {
        private readonly long _duration;
        private long _progress;

        public Star(float centerX, float centerY, float radius, Color color, long duration)
        {
            CenterX = centerX;
            CenterY = centerY;
            Color = color;
            _duration = duration;
            Radius = (float)(radius * (0.7 + (0.3 * NextFloat())));
            ComputeAlpha();
        }

        public float CenterX { get; }

        public float CenterY { get; }

        public float Radius { get; }

        public Color Color { get; }

        public float Alpha { get; private set; }

        public void Shine(long interval)
        {
            _progress = (_progress + interval) % _duration;
            ComputeAlpha();
        }

        private void ComputeAlpha()
        {
            Alpha = _progress < 0.5 * _duration
                ? (float)(_progress / 0.5 / _duration)
                : (float)(1 - ((_progress - (0.5 * _duration)) / 0.5 / _duration));
            Alpha = (Alpha * 0.66f) + 0.33f;
        }
    }
}
