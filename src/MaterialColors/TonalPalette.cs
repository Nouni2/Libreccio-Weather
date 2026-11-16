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

/*
 * Copyright 2021 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace LibreccioWeather.Core.MaterialColors;

/// <summary>
/// The colors of one hue and chroma at every tone.
/// </summary>
public sealed class TonalPalette
{
    private readonly Dictionary<int, int> _cache = [];

    private TonalPalette(double hue, double chroma, Hct keyColor)
    {
        Hue = hue;
        Chroma = chroma;
        KeyColor = keyColor;
    }

    public double Hue { get; }

    public double Chroma { get; }

    /// <summary>
    /// The color of the palette's hue and chroma whose tone is closest to 50, where most chroma is available.
    /// </summary>
    public Hct KeyColor { get; }

    public static TonalPalette FromInt(int argb)
    {
        return FromHct(Hct.FromInt(argb));
    }

    public static TonalPalette FromHct(Hct hct)
    {
        return new TonalPalette(hct.Hue, hct.Chroma, hct);
    }

    public static TonalPalette FromHueAndChroma(double hue, double chroma)
    {
        Hct keyColor = new KeyColorFinder(hue, chroma).Create();
        return new TonalPalette(hue, chroma, keyColor);
    }

    /// <summary>
    /// The ARGB color of a tone from 0 to 100.
    /// </summary>
    public int Tone(int tone)
    {
        if (!_cache.TryGetValue(tone, out int color))
        {
            if (tone == 99 && Hct.IsYellow(Hue))
            {
                color = AverageArgb(Tone(98), Tone(100));
            }
            else
            {
                color = Hct.From(Hue, Chroma, tone).ToInt();
            }

            _cache[tone] = color;
        }

        return color;
    }

    public Hct GetHct(double tone)
    {
        if (tone == 99.0 && Hct.IsYellow(Hue))
        {
            return Hct.FromInt(Tone(99));
        }

        return Hct.From(Hue, Chroma, tone);
    }

    private static int AverageArgb(int argb1, int argb2)
    {
        int red1 = (int)((uint)argb1 >> 16) & 0xff;
        int green1 = (int)((uint)argb1 >> 8) & 0xff;
        int blue1 = argb1 & 0xff;
        int red2 = (int)((uint)argb2 >> 16) & 0xff;
        int green2 = (int)((uint)argb2 >> 8) & 0xff;
        int blue2 = argb2 & 0xff;
        int red = (int)ColorMath.JavaRound((red1 + red2) / 2f);
        int green = (int)ColorMath.JavaRound((green1 + green2) / 2f);
        int blue = (int)ColorMath.JavaRound((blue1 + blue2) / 2f);
        return (255 << 24) | ((red & 255) << 16) | ((green & 255) << 8) | (blue & 255);
    }

    /// <summary>
    /// Finds the key color of a hue and a requested chroma.
    /// </summary>
    private sealed class KeyColorFinder(double hue, double requestedChroma)
    {
        private const double MaxChromaValue = 200.0;

        // The highest chroma of each tone, which the search asks for repeatedly.
        private readonly Dictionary<int, double> _chromaCache = [];

        public Hct Create()
        {
            // Tone 50 has the most chroma available on average, so the search pivots around it.
            const int pivotTone = 50;
            const int toneStepSize = 1;

            // Accepts a chroma slightly above the one requested.
            const double epsilon = 0.01;

            // A binary search for the tone whose chroma is closest to the one requested.
            int lowerTone = 0;
            int upperTone = 100;
            while (lowerTone < upperTone)
            {
                int midTone = (lowerTone + upperTone) / 2;
                bool isAscending = MaxChroma(midTone) < MaxChroma(midTone + toneStepSize);
                bool sufficientChroma = MaxChroma(midTone) >= requestedChroma - epsilon;

                if (sufficientChroma)
                {
                    // The answer lies on both sides, so the side closer to the pivot tone is searched.
                    if (Math.Abs(lowerTone - pivotTone) < Math.Abs(upperTone - pivotTone))
                    {
                        upperTone = midTone;
                    }
                    else
                    {
                        if (lowerTone == midTone)
                        {
                            return Hct.From(hue, requestedChroma, lowerTone);
                        }

                        lowerTone = midTone;
                    }
                }
                else
                {
                    // Without enough chroma at the middle tone, the search heads towards the chroma peak.
                    if (isAscending)
                    {
                        lowerTone = midTone + toneStepSize;
                    }
                    else
                    {
                        // The middle tone may be the peak, so it stays in the range.
                        upperTone = midTone;
                    }
                }
            }

            return Hct.From(hue, requestedChroma, lowerTone);
        }

        private double MaxChroma(int tone)
        {
            if (!_chromaCache.TryGetValue(tone, out double chroma))
            {
                chroma = Hct.From(hue, MaxChromaValue, tone).Chroma;
                _chromaCache[tone] = chroma;
            }

            return chroma;
        }
    }
}
