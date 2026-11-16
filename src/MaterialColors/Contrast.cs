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
/// Contrast ratios of tones, as the WCAG define them, and the tones that reach a ratio.
/// </summary>
internal static class Contrast
{
    // A ratio is (lighter + 5) / (darker + 5) with luminances from 0 to 100, so it lies between 1 and 21.
    public const double RatioMin = 1.0;
    public const double RatioMax = 21.0;
    public const double Ratio30 = 3.0;
    public const double Ratio45 = 4.5;
    public const double Ratio70 = 7.0;

    // A luminance found for a ratio may miss it once computed back; beyond this difference it counts as unreached.
    // 0.04 keeps the resulting ratio rounding to the same tenth.
    private const double ContrastRatioEpsilon = 0.04;

    // RGB is quantized, so a display color can miss a requested lightness slightly; the tones returned are pushed
    // this far past the exact value so the ratio is still reached once the color is mapped to sRGB.
    private const double LuminanceGamutMapTolerance = 0.4;

    /// <summary>
    /// The contrast ratio of two Y values of XYZ, from 0 to 100.
    /// </summary>
    public static double RatioOfYs(double y1, double y2)
    {
        double lighter = Math.Max(y1, y2);
        double darker = (lighter == y2) ? y1 : y2;
        return (lighter + 5.0) / (darker + 5.0);
    }

    /// <summary>
    /// The contrast ratio of two tones.
    /// </summary>
    public static double RatioOfTones(double t1, double t2)
    {
        return RatioOfYs(ColorConversions.YFromLstar(t1), ColorConversions.YFromLstar(t2));
    }

    /// <summary>
    /// The tone that reaches the ratio above the given tone, or -1.0 when none does.
    /// </summary>
    public static double Lighter(double tone, double ratio)
    {
        if (tone < 0.0 || tone > 100.0)
        {
            return -1.0;
        }

        // The ratio's equation solved for the lighter Y.
        double darkY = ColorConversions.YFromLstar(tone);
        double lightY = ratio * (darkY + 5.0) - 5.0;
        if (lightY < 0.0 || lightY > 100.0)
        {
            return -1.0;
        }

        double realContrast = RatioOfYs(lightY, darkY);
        double delta = Math.Abs(realContrast - ratio);
        if (realContrast < ratio && delta > ContrastRatioEpsilon)
        {
            return -1.0;
        }

        double returnValue = ColorConversions.LstarFromY(lightY) + LuminanceGamutMapTolerance;

        // The conversions it relies on can change, so the result is checked again.
        if (returnValue < 0 || returnValue > 100)
        {
            return -1.0;
        }

        return returnValue;
    }

    /// <summary>
    /// The lighter tone that reaches the ratio, or 100 when none does, which may then miss the ratio.
    /// </summary>
    public static double LighterUnsafe(double tone, double ratio)
    {
        double lighterSafe = Lighter(tone, ratio);
        return lighterSafe < 0.0 ? 100.0 : lighterSafe;
    }

    /// <summary>
    /// The tone that reaches the ratio below the given tone, or -1.0 when none does.
    /// </summary>
    public static double Darker(double tone, double ratio)
    {
        if (tone < 0.0 || tone > 100.0)
        {
            return -1.0;
        }

        // The ratio's equation solved for the darker Y.
        double lightY = ColorConversions.YFromLstar(tone);
        double darkY = ((lightY + 5.0) / ratio) - 5.0;
        if (darkY < 0.0 || darkY > 100.0)
        {
            return -1.0;
        }

        double realContrast = RatioOfYs(lightY, darkY);
        double delta = Math.Abs(realContrast - ratio);
        if (realContrast < ratio && delta > ContrastRatioEpsilon)
        {
            return -1.0;
        }

        double returnValue = ColorConversions.LstarFromY(darkY) - LuminanceGamutMapTolerance;

        // The conversions it relies on can change, so the result is checked again.
        if (returnValue < 0 || returnValue > 100)
        {
            return -1.0;
        }

        return returnValue;
    }

    /// <summary>
    /// The darker tone that reaches the ratio, or 0 when none does, which may then miss the ratio.
    /// </summary>
    public static double DarkerUnsafe(double tone, double ratio)
    {
        double darkerSafe = Darker(tone, ratio);
        return Math.Max(0.0, darkerSafe);
    }
}
