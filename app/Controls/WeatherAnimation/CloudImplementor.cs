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
/// Partly cloudy, cloudy, thunder, fog and haze: translucent circles that slowly swell and shrink, with stars on a
/// partly cloudy night and flashes of light for thunder.
/// </summary>
public sealed class CloudImplementor : WeatherAnimationImplementor
{
    public const int TypeCloud = 1;
    public const int TypeCloudy = 3;
    public const int TypeThunder = 5;
    public const int TypeFog = 6;
    public const int TypeHaze = 7;

    /// <summary>
    /// The weather's own color, as packed ARGB, which tints the pull-to-refresh indicator.
    /// </summary>
    public static uint GetThemeColor(int type, bool daylight) => type switch
    {
        TypeCloudy => daylight ? 0xFF9DAFC1 : 0xFF263238,
        TypeThunder => daylight ? 0xFFB296BD : 0xFF231739,
        TypeFog => daylight ? 0xFFA3AEC2 : 0xFF4F5D68,
        TypeHaze => daylight ? 0xFFE1C899 : 0xFF6C5C49,
        _ => daylight ? 0xFF00A5D9 : 0xFF222D43,
    };

    private Cloud[] _clouds = [];
    private Star[] _stars = [];
    private readonly Thunder? _thunder;

    /// <param name="darkMode">Whether the app is dark; thunder clouds take night as dark mode instead.</param>
    public CloudImplementor(int[] canvasSizes, bool animate, int type, bool daylight, bool? darkMode = null)
    {
        bool dark = darkMode ?? !daylight;
        int viewWidth = canvasSizes[0];
        int viewHeight = canvasSizes[1];
        if (type is TypeFog or TypeHaze)
        {
            Color color = type == TypeFog
                ? Hex(daylight && !dark ? "#93B9FF" : "#2A5476")
                : Hex(daylight && !dark ? "#FFDDA1" : "#4D3314");
            Color[] cloudColors = [color, color, color];
            float[] cloudAlphas = [0.3f, 0.3f, 0.3f];
            Cloud[] clouds =
            [
                .. SixClouds(viewWidth, cloudColors, cloudAlphas),
                new(viewWidth * 0.9250f, viewWidth * ((0.9250f * 0.0249f) + 0.1500f), viewWidth * 0.3166f, 1.15f,
                    GetRandomFactor(1.8f, 2.2f), cloudColors[2], cloudAlphas[2], 7000, 0),
                new(viewWidth * 0.4694f, viewWidth * ((0.4694f * 0.0489f) + 0.1500f), viewWidth * 0.3166f, 1.15f,
                    GetRandomFactor(1.8f, 2.2f), cloudColors[2], cloudAlphas[2], 8200, 0),
                new(viewWidth * 0.0250f, viewWidth * ((0.0250f * 0.6820f) + 0.1500f), viewWidth * 0.3166f, 1.15f,
                    GetRandomFactor(1.8f, 2.2f), cloudColors[2], cloudAlphas[2], 7700, 0),
            ];
            Initialize(clouds);
        }
        else if (type is TypeCloudy or TypeThunder)
        {
            Color color = type == TypeCloudy
                ? (daylight && !dark ? Rgb(160, 179, 191) : Rgb(95, 104, 108))
                : Hex(daylight && !dark ? "#AB90DB" : "#2C1C4D");
            Color[] cloudColors = [color, color];
            float[] cloudAlphas = [0.3f, 0.3f];
            Initialize(SixClouds(viewWidth, cloudColors, cloudAlphas));
        }
        else
        {
            Color cloudColor = daylight && !dark ? Rgb(203, 245, 255) : Rgb(151, 168, 202);
            float[] cloudAlphas = [0.40f, 0.10f];
            Cloud[] clouds =
            [
                new((float)(viewWidth * 0.1529), (float)((viewWidth * 0.1529 * 0.5568) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.2649), 1.20f, GetRandomFactor(1.5f, 1.8f), cloudColor, cloudAlphas[0], 7000, 0),
                new((float)(viewWidth * 0.4793), (float)((viewWidth * 0.4793 * 0.2185) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.2426), 1.20f, GetRandomFactor(1.5f, 1.8f), cloudColor, cloudAlphas[0], 8500, 0),
                new((float)(viewWidth * 0.8531), (float)((viewWidth * 0.8531 * 0.1286) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.2970), 1.20f, GetRandomFactor(1.5f, 1.8f), cloudColor, cloudAlphas[0], 7050, 0),
                new((float)(viewWidth * 0.0551), (float)((viewWidth * 0.0551 * 2.8600) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.4125), 1.15f, GetRandomFactor(1.3f, 1.5f), cloudColor, cloudAlphas[1], 9500, 0),
                new((float)(viewWidth * 0.4928), (float)((viewWidth * 0.4928 * 0.3897) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.3521), 1.15f, GetRandomFactor(1.3f, 1.5f), cloudColor, cloudAlphas[1], 10500, 0),
                new((float)(viewWidth * 1.0499), (float)((viewWidth * 1.0499 * 0.1875) + (viewWidth * 0.050)),
                    (float)(viewWidth * 0.4186), 1.15f, GetRandomFactor(1.3f, 1.5f), cloudColor, cloudAlphas[1], 9000, 0),
            ];
            if (daylight)
            {
                Initialize(clouds);
            }
            else
            {
                Color[] colors = MeteorShowerImplementor.StarColors;
                int canvasSize = (int)Math.Sqrt(Math.Pow(viewWidth, 2.0) + Math.Pow(viewHeight, 2.0));
                int width = canvasSize;
                int height = (int)(((canvasSize - viewHeight) * 0.5) + (viewWidth * 1.1111));
                float radius = (float)(0.00125 * canvasSize * (0.5 + NextFloat()));
                Star[] stars = new Star[50];
                for (int i = 0; i < stars.Length; i++)
                {
                    int x = (int)(NextInt(width) - (0.5 * (canvasSize - viewWidth)));
                    int y = (int)(NextInt(height) - (0.5 * (canvasSize - viewHeight)));
                    long duration = (long)(2500 + (NextFloat() * 2500));
                    stars[i] = new Star(x, y, radius, colors[i % colors.Length], duration, animate);
                }

                Initialize(clouds, stars);
            }
        }

        _thunder = type == TypeThunder ? new Thunder() : null;
    }

    public override void UpdateData(int[] canvasSizes, long interval, float rotation2D, float rotation3D)
    {
        foreach (Cloud cloud in _clouds)
        {
            cloud.Move(interval, rotation2D, rotation3D);
        }

        foreach (Star star in _stars)
        {
            star.Shine(interval);
        }

        _thunder?.Shine(interval);
    }

    public override void Draw(int[] canvasSizes, AnimationCanvas canvas, float scrollRate, float rotation2D, float rotation3D)
    {
        if (scrollRate >= 1)
        {
            return;
        }

        if (_thunder is { } thunder)
        {
            canvas.DrawColor(Color.FromArgb(
                (byte)Math.Clamp((int)((1 - scrollRate) * thunder.Alpha * 255 * 0.66), 0, 255),
                Thunder.Red, Thunder.Green, Thunder.Blue));
        }

        foreach (Star star in _stars)
        {
            canvas.DrawCircle(star.CenterX, star.CenterY, star.Radius, WithAlpha(star.Color, (1 - scrollRate) * star.Alpha * 255));
        }

        foreach (Cloud cloud in _clouds)
        {
            canvas.DrawCircle(cloud.CenterX, cloud.CenterY, cloud.Radius, WithAlpha(cloud.Color, (1 - scrollRate) * cloud.Alpha * 255));
        }
    }

    // The six clouds shared by fog, haze, cloudy and thunder, in two rows of three.
    private static Cloud[] SixClouds(int viewWidth, Color[] cloudColors, float[] cloudAlphas)
    {
        return
        [
            new(viewWidth * 1.0699f, viewWidth * ((1.1900f * 0.2286f) + 0.11f), viewWidth * (0.4694f * 0.9f), 1.1f,
                GetRandomFactor(1.3f, 1.8f), cloudColors[0], cloudAlphas[0], 9000, 0),
            new(viewWidth * 0.4866f, viewWidth * ((0.4866f * 0.6064f) + 0.085f), viewWidth * (0.3946f * 0.9f), 1.1f,
                GetRandomFactor(1.3f, 1.8f), cloudColors[0], cloudAlphas[0], 10500, 0),
            new(viewWidth * 0.0351f, viewWidth * ((0.1701f * 1.4327f) + 0.11f), viewWidth * (0.4627f * 0.9f), 1.1f,
                GetRandomFactor(1.3f, 1.8f), cloudColors[0], cloudAlphas[0], 9000, 0),
            new(viewWidth * 0.8831f, viewWidth * ((1.0270f * 0.1671f) + 0.07f), viewWidth * (0.3238f * 0.9f), 1.15f,
                GetRandomFactor(1.6f, 2f), cloudColors[1], cloudAlphas[1], 7000, 0),
            new(viewWidth * 0.4663f, viewWidth * ((0.4663f * 0.3520f) + 0.050f), viewWidth * (0.2906f * 0.9f), 1.15f,
                GetRandomFactor(1.6f, 2f), cloudColors[1], cloudAlphas[1], 8500, 0),
            new(viewWidth * 0.1229f, viewWidth * ((0.0234f * 5.7648f) + 0.07f), viewWidth * (0.2972f * 0.9f), 1.15f,
                GetRandomFactor(1.6f, 2f), cloudColors[1], cloudAlphas[1], 7000, 0),
        ];
    }

    // The remainder keeps the factor within the range, as it is never more than the range's width.
    private static float GetRandomFactor(float from, float to)
    {
        return from + (NextFloat() % (to - from));
    }

    private void Initialize(Cloud[] clouds, Star[]? stars = null)
    {
        _clouds = clouds;
        _stars = stars ?? [];
    }

    private sealed class Cloud
    {
        private readonly float _initCX;
        private readonly float _initCY;
        private readonly float _initRadius;
        private readonly float _scaleRatio;
        private readonly float _moveFactor;
        private readonly long _duration;
        private long _progress;

        public Cloud(
            float initCX,
            float initCY,
            float radius,
            float scaleRatio,
            float moveFactor,
            Color color,
            float alpha,
            long duration,
            long initProgress)
        {
            _initCX = initCX;
            _initCY = initCY;
            CenterX = initCX;
            CenterY = initCY;
            Radius = radius;
            _initRadius = radius;
            _scaleRatio = scaleRatio;
            _moveFactor = moveFactor;
            Color = color;
            Alpha = alpha;
            _duration = duration;
            _progress = initProgress % duration;
            ComputeRadius();
        }

        public float CenterX { get; private set; }

        public float CenterY { get; private set; }

        public float Radius { get; private set; }

        public Color Color { get; }

        public float Alpha { get; }

        public void Move(long interval, float rotation2D, float rotation3D)
        {
            CenterX = (float)(_initCX + (Math.Sin(ToRadians(rotation2D)) * 0.40 * Radius * _moveFactor));
            CenterY = (float)(_initCY - (Math.Sin(ToRadians(rotation3D)) * 0.50 * Radius * _moveFactor));
            _progress = (_progress + interval) % _duration;
            ComputeRadius();
        }

        private void ComputeRadius()
        {
            Radius = _progress < 0.5 * _duration
                ? (float)(_initRadius * (1 + ((_scaleRatio - 1) * _progress / 0.5 / _duration)))
                : (float)(_initRadius * (_scaleRatio - ((_scaleRatio - 1) * (_progress - (0.5 * _duration)) / 0.5 / _duration)));
        }
    }

    private sealed class Star
    {
        private readonly long _duration;
        private readonly bool _animate;
        private long _progress;

        public Star(float centerX, float centerY, float radius, Color color, long duration, bool animate)
        {
            CenterX = centerX;
            CenterY = centerY;
            Color = color;
            _duration = duration;
            _animate = animate;
            Radius = (float)(radius * (0.7 + (0.3 * NextFloat())));
            if (!animate)
            {
                Alpha = NextFloat();
            }
            else
            {
                ComputeAlpha();
            }
        }

        public float CenterX { get; }

        public float CenterY { get; }

        public float Radius { get; }

        public Color Color { get; }

        public float Alpha { get; private set; }

        public void Shine(long interval)
        {
            if (!_animate)
            {
                return;
            }

            _progress = (_progress + interval) % _duration;
            ComputeAlpha();
        }

        private void ComputeAlpha()
        {
            Alpha = _progress < 0.5 * _duration
                ? (float)(_progress / 0.5 / _duration)
                : (float)(1 - ((_progress - (0.5 * _duration)) / 0.5 / _duration));
        }
    }

    /// <summary>
    /// A purple flash that lights up twice in 300 ms, then waits 2 to 7 seconds.
    /// </summary>
    internal sealed class Thunder
    {
        public const byte Red = 81;
        public const byte Green = 67;
        public const byte Blue = 168;

        private readonly int _minimumDelay;
        private long _progress;
        private long _duration;
        private long _delay;

        public Thunder(int minimumDelay = 2000)
        {
            _minimumDelay = minimumDelay;
            Init();
            ComputeFrame();
        }

        public float Alpha { get; private set; }

        public void Shine(long interval)
        {
            _progress += interval;
            if (_progress > _duration + _delay)
            {
                Init();
            }

            ComputeFrame();
        }

        private void Init()
        {
            _progress = 0;
            _duration = 300;
            _delay = NextInt(5000) + _minimumDelay;
        }

        private void ComputeFrame()
        {
            if (_progress >= _duration)
            {
                Alpha = 0f;
            }
            else if (_progress < 0.25 * _duration)
            {
                Alpha = (float)(_progress / 0.25 / _duration);
            }
            else if (_progress < 0.5 * _duration)
            {
                Alpha = (float)(1 - ((_progress - (0.25 * _duration)) / 0.25 / _duration));
            }
            else if (_progress < 0.75 * _duration)
            {
                Alpha = (float)((_progress - (0.5 * _duration)) / 0.25 / _duration);
            }
            else
            {
                Alpha = (float)(1 - ((_progress - (0.75 * _duration)) / 0.25 / _duration));
            }
        }
    }
}
