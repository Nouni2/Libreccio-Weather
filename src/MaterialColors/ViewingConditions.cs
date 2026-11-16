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
/// The environment in which a color is seen, which CAM16 needs to give its appearance: the white point, the
/// light level, the background lightness and the surround.
/// </summary>
internal sealed class ViewingConditions
{
    /// <summary>
    /// sRGB-like conditions: a D65 white point, a mid-gray background and an average surround.
    /// </summary>
    public static readonly ViewingConditions Default = DefaultWithBackgroundLstar(50.0);

    private ViewingConditions(
        double n,
        double aw,
        double nbb,
        double ncb,
        double c,
        double nc,
        double[] rgbD,
        double fl,
        double flRoot,
        double z)
    {
        N = n;
        Aw = aw;
        Nbb = nbb;
        Ncb = ncb;
        C = c;
        Nc = nc;
        RgbD = rgbD;
        Fl = fl;
        FlRoot = flRoot;
        Z = z;
    }

    public double Aw { get; }

    public double N { get; }

    public double Nbb { get; }

    public double Ncb { get; }

    public double C { get; }

    public double Nc { get; }

    public double[] RgbD { get; }

    public double Fl { get; }

    public double FlRoot { get; }

    public double Z { get; }

    public static ViewingConditions Make(
        double[] whitePoint,
        double adaptingLuminance,
        double backgroundLstar,
        double surround,
        bool discountingIlluminant)
    {
        // A pure black background is not physical and leads to infinities: no color can be seen against it.
        backgroundLstar = Math.Max(0.1, backgroundLstar);

        // The white point's XYZ as cone responses.
        double[][] matrix = Cam16.XyzToCam16Rgb;
        double[] xyz = whitePoint;
        double rW = (xyz[0] * matrix[0][0]) + (xyz[1] * matrix[0][1]) + (xyz[2] * matrix[0][2]);
        double gW = (xyz[0] * matrix[1][0]) + (xyz[1] * matrix[1][1]) + (xyz[2] * matrix[1][2]);
        double bW = (xyz[0] * matrix[2][0]) + (xyz[1] * matrix[2][1]) + (xyz[2] * matrix[2][2]);
        double f = 0.8 + (surround / 10.0);
        double c = (f >= 0.9)
            ? ColorMath.Lerp(0.59, 0.69, (f - 0.9) * 10.0)
            : ColorMath.Lerp(0.525, 0.59, (f - 0.8) * 10.0);
        double d = discountingIlluminant
            ? 1.0
            : f * (1.0 - ((1.0 / 3.6) * Math.Exp((-adaptingLuminance - 42.0) / 92.0)));
        d = ColorMath.ClampDouble(0.0, 1.0, d);
        double nc = f;
        double[] rgbD =
        [
            d * (100.0 / rW) + 1.0 - d,
            d * (100.0 / gW) + 1.0 - d,
            d * (100.0 / bW) + 1.0 - d,
        ];
        double k = 1.0 / (5.0 * adaptingLuminance + 1.0);
        double k4 = k * k * k * k;
        double k4F = 1.0 - k4;
        double fl = (k4 * adaptingLuminance) + (0.1 * k4F * k4F * Math.Cbrt(5.0 * adaptingLuminance));
        double n = ColorConversions.YFromLstar(backgroundLstar) / whitePoint[1];
        double z = 1.48 + Math.Sqrt(n);
        double nbb = 0.725 / Math.Pow(n, 0.2);
        double ncb = nbb;
        double[] rgbAFactors =
        [
            Math.Pow(fl * rgbD[0] * rW / 100.0, 0.42),
            Math.Pow(fl * rgbD[1] * gW / 100.0, 0.42),
            Math.Pow(fl * rgbD[2] * bW / 100.0, 0.42),
        ];

        double[] rgbA =
        [
            (400.0 * rgbAFactors[0]) / (rgbAFactors[0] + 27.13),
            (400.0 * rgbAFactors[1]) / (rgbAFactors[1] + 27.13),
            (400.0 * rgbAFactors[2]) / (rgbAFactors[2] + 27.13),
        ];

        double aw = ((2.0 * rgbA[0]) + rgbA[1] + (0.05 * rgbA[2])) * nbb;
        return new ViewingConditions(n, aw, nbb, ncb, c, nc, rgbD, fl, Math.Pow(fl, 0.25), z);
    }

    /// <summary>
    /// The default conditions with another background lightness.
    /// </summary>
    public static ViewingConditions DefaultWithBackgroundLstar(double lstar)
    {
        return Make(
            ColorConversions.WhitePointD65,
            200.0 / Math.PI * ColorConversions.YFromLstar(50.0) / 100.0,
            lstar,
            2.0,
            false);
    }
}
