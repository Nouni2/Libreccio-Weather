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
/// A color in the CAM16 appearance model, with its CAM16-UCS coordinates for color distances.
/// </summary>
internal sealed class Cam16
{
    // XYZ coordinates to CAM16 cone responses.
    internal static readonly double[][] XyzToCam16Rgb =
    [
        [0.401288, 0.650173, -0.051461],
        [-0.250268, 1.204414, 0.045854],
        [-0.002079, 0.048952, 0.953127],
    ];

    // CAM16 cone responses to XYZ coordinates.
    internal static readonly double[][] Cam16RgbToXyz =
    [
        [1.8620678, -1.0112547, 0.14918678],
        [0.38752654, 0.62144744, -0.00897398],
        [-0.01584150, -0.03412294, 1.0499644],
    ];

    // Reused by every conversion to XYZ, so the conversions allocate nothing.
    private readonly double[] _tempArray = [0.0, 0.0, 0.0];

    private Cam16(
        double hue,
        double chroma,
        double j,
        double q,
        double m,
        double s,
        double jstar,
        double astar,
        double bstar)
    {
        Hue = hue;
        Chroma = chroma;
        J = j;
        Q = q;
        M = m;
        S = s;
        Jstar = jstar;
        Astar = astar;
        Bstar = bstar;
    }

    /// <summary>
    /// The hue, in degrees from 0 inclusive to 360 exclusive.
    /// </summary>
    public double Hue { get; }

    /// <summary>
    /// How colorful the color is relative to a white of the same brightness.
    /// </summary>
    public double Chroma { get; }

    /// <summary>
    /// The lightness.
    /// </summary>
    public double J { get; }

    /// <summary>
    /// The brightness: the lightness scaled by the viewing conditions' white.
    /// </summary>
    public double Q { get; }

    /// <summary>
    /// The colorfulness: the chroma scaled by the viewing conditions' white.
    /// </summary>
    public double M { get; }

    /// <summary>
    /// The saturation: the colorfulness relative to the brightness.
    /// </summary>
    public double S { get; }

    public double Jstar { get; }

    public double Astar { get; }

    public double Bstar { get; }

    /// <summary>
    /// The CAM16-UCS distance to another color, on a scale where 1.0 is a just noticeable difference.
    /// </summary>
    public double Distance(Cam16 other)
    {
        double dJ = Jstar - other.Jstar;
        double dA = Astar - other.Astar;
        double dB = Bstar - other.Bstar;
        double dEPrime = Math.Sqrt(dJ * dJ + dA * dA + dB * dB);
        double dE = 1.41 * Math.Pow(dEPrime, 0.63);
        return dE;
    }

    public static Cam16 FromInt(int argb)
    {
        return FromIntInViewingConditions(argb, ViewingConditions.Default);
    }

    // The sRGB to XYZ coefficients are written out as in the reference implementation, which keeps them identical
    // across implementations.
    internal static Cam16 FromIntInViewingConditions(int argb, ViewingConditions viewingConditions)
    {
        int red = (argb & 0x00ff0000) >> 16;
        int green = (argb & 0x0000ff00) >> 8;
        int blue = argb & 0x000000ff;
        double redL = ColorConversions.Linearized(red);
        double greenL = ColorConversions.Linearized(green);
        double blueL = ColorConversions.Linearized(blue);
        double x = 0.41233895 * redL + 0.35762064 * greenL + 0.18051042 * blueL;
        double y = 0.2126 * redL + 0.7152 * greenL + 0.0722 * blueL;
        double z = 0.01932141 * redL + 0.11916382 * greenL + 0.95034478 * blueL;

        return FromXyzInViewingConditions(x, y, z, viewingConditions);
    }

    internal static Cam16 FromXyzInViewingConditions(double x, double y, double z, ViewingConditions viewingConditions)
    {
        // XYZ to cone responses.
        double[][] matrix = XyzToCam16Rgb;
        double rT = (x * matrix[0][0]) + (y * matrix[0][1]) + (z * matrix[0][2]);
        double gT = (x * matrix[1][0]) + (y * matrix[1][1]) + (z * matrix[1][2]);
        double bT = (x * matrix[2][0]) + (y * matrix[2][1]) + (z * matrix[2][2]);

        // Discounting the illuminant.
        double rD = viewingConditions.RgbD[0] * rT;
        double gD = viewingConditions.RgbD[1] * gT;
        double bD = viewingConditions.RgbD[2] * bT;

        // Chromatic adaptation.
        double rAF = Math.Pow(viewingConditions.Fl * Math.Abs(rD) / 100.0, 0.42);
        double gAF = Math.Pow(viewingConditions.Fl * Math.Abs(gD) / 100.0, 0.42);
        double bAF = Math.Pow(viewingConditions.Fl * Math.Abs(bD) / 100.0, 0.42);
        double rA = ColorMath.JavaSignum(rD) * 400.0 * rAF / (rAF + 27.13);
        double gA = ColorMath.JavaSignum(gD) * 400.0 * gAF / (gAF + 27.13);
        double bA = ColorMath.JavaSignum(bD) * 400.0 * bAF / (bAF + 27.13);

        // Redness-greenness and yellowness-blueness.
        double a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
        double b = (rA + gA - 2.0 * bA) / 9.0;

        double u = (20.0 * rA + 20.0 * gA + 21.0 * bA) / 20.0;
        double p2 = (40.0 * rA + 20.0 * gA + bA) / 20.0;

        double atan2 = Math.Atan2(b, a);
        double atanDegrees = ColorMath.JavaToDegrees(atan2);
        double hue = ColorMath.SanitizeDegreesDouble(atanDegrees);
        double hueRadians = ColorMath.JavaToRadians(hue);

        // Achromatic response.
        double ac = p2 * viewingConditions.Nbb;

        double j = 100.0 * Math.Pow(ac / viewingConditions.Aw, viewingConditions.C * viewingConditions.Z);
        double q = 4.0
            / viewingConditions.C
            * Math.Sqrt(j / 100.0)
            * (viewingConditions.Aw + 4.0)
            * viewingConditions.FlRoot;

        double huePrime = (hue < 20.14) ? hue + 360 : hue;
        double eHue = 0.25 * (Math.Cos(ColorMath.JavaToRadians(huePrime) + 2.0) + 3.8);
        double p1 = 50000.0 / 13.0 * eHue * viewingConditions.Nc * viewingConditions.Ncb;
        double t = p1 * double.Hypot(a, b) / (u + 0.305);
        double alpha = Math.Pow(1.64 - Math.Pow(0.29, viewingConditions.N), 0.73) * Math.Pow(t, 0.9);
        double c = alpha * Math.Sqrt(j / 100.0);
        double m = c * viewingConditions.FlRoot;
        double s = 50.0 * Math.Sqrt((alpha * viewingConditions.C) / (viewingConditions.Aw + 4.0));

        double jstar = (1.0 + 100.0 * 0.007) * j / (1.0 + 0.007 * j);
        double mstar = 1.0 / 0.0228 * ColorMath.Log1p(0.0228 * m);
        double astar = mstar * Math.Cos(hueRadians);
        double bstar = mstar * Math.Sin(hueRadians);

        return new Cam16(hue, c, j, q, m, s, jstar, astar, bstar);
    }

    internal static Cam16 FromJch(double j, double c, double h)
    {
        return FromJchInViewingConditions(j, c, h, ViewingConditions.Default);
    }

    private static Cam16 FromJchInViewingConditions(double j, double c, double h, ViewingConditions viewingConditions)
    {
        double q = 4.0
            / viewingConditions.C
            * Math.Sqrt(j / 100.0)
            * (viewingConditions.Aw + 4.0)
            * viewingConditions.FlRoot;
        double m = c * viewingConditions.FlRoot;
        double alpha = c / Math.Sqrt(j / 100.0);
        double s = 50.0 * Math.Sqrt((alpha * viewingConditions.C) / (viewingConditions.Aw + 4.0));

        double hueRadians = ColorMath.JavaToRadians(h);
        double jstar = (1.0 + 100.0 * 0.007) * j / (1.0 + 0.007 * j);
        double mstar = 1.0 / 0.0228 * ColorMath.Log1p(0.0228 * m);
        double astar = mstar * Math.Cos(hueRadians);
        double bstar = mstar * Math.Sin(hueRadians);
        return new Cam16(h, c, j, q, m, s, jstar, astar, bstar);
    }

    public static Cam16 FromUcs(double jstar, double astar, double bstar)
    {
        return FromUcsInViewingConditions(jstar, astar, bstar, ViewingConditions.Default);
    }

    public static Cam16 FromUcsInViewingConditions(double jstar, double astar, double bstar, ViewingConditions viewingConditions)
    {
        double m = double.Hypot(astar, bstar);
        double m2 = ColorMath.Expm1(m * 0.0228) / 0.0228;
        double c = m2 / viewingConditions.FlRoot;
        double h = Math.Atan2(bstar, astar) * (180.0 / Math.PI);
        if (h < 0.0)
        {
            h += 360.0;
        }

        double j = jstar / (1.0 - (jstar - 100.0) * 0.007);
        return FromJchInViewingConditions(j, c, h, viewingConditions);
    }

    /// <summary>
    /// The ARGB color, as seen in the default viewing conditions, which are close to sRGB's.
    /// </summary>
    public int ToInt()
    {
        return Viewed(ViewingConditions.Default);
    }

    internal int Viewed(ViewingConditions viewingConditions)
    {
        double[] xyz = XyzInViewingConditions(viewingConditions, _tempArray);
        return ColorConversions.ArgbFromXyz(xyz[0], xyz[1], xyz[2]);
    }

    internal double[] XyzInViewingConditions(ViewingConditions viewingConditions, double[]? returnArray)
    {
        double alpha = (Chroma == 0.0 || J == 0.0) ? 0.0 : Chroma / Math.Sqrt(J / 100.0);

        double t = Math.Pow(alpha / Math.Pow(1.64 - Math.Pow(0.29, viewingConditions.N), 0.73), 1.0 / 0.9);
        double hRad = ColorMath.JavaToRadians(Hue);

        double eHue = 0.25 * (Math.Cos(hRad + 2.0) + 3.8);
        double ac = viewingConditions.Aw * Math.Pow(J / 100.0, 1.0 / viewingConditions.C / viewingConditions.Z);
        double p1 = eHue * (50000.0 / 13.0) * viewingConditions.Nc * viewingConditions.Ncb;
        double p2 = ac / viewingConditions.Nbb;

        double hSin = Math.Sin(hRad);
        double hCos = Math.Cos(hRad);

        double gamma = 23.0 * (p2 + 0.305) * t / (23.0 * p1 + 11.0 * t * hCos + 108.0 * t * hSin);
        double a = gamma * hCos;
        double b = gamma * hSin;
        double rA = (460.0 * p2 + 451.0 * a + 288.0 * b) / 1403.0;
        double gA = (460.0 * p2 - 891.0 * a - 261.0 * b) / 1403.0;
        double bA = (460.0 * p2 - 220.0 * a - 6300.0 * b) / 1403.0;

        double rCBase = Math.Max(0, (27.13 * Math.Abs(rA)) / (400.0 - Math.Abs(rA)));
        double rC = ColorMath.JavaSignum(rA) * (100.0 / viewingConditions.Fl) * Math.Pow(rCBase, 1.0 / 0.42);
        double gCBase = Math.Max(0, (27.13 * Math.Abs(gA)) / (400.0 - Math.Abs(gA)));
        double gC = ColorMath.JavaSignum(gA) * (100.0 / viewingConditions.Fl) * Math.Pow(gCBase, 1.0 / 0.42);
        double bCBase = Math.Max(0, (27.13 * Math.Abs(bA)) / (400.0 - Math.Abs(bA)));
        double bC = ColorMath.JavaSignum(bA) * (100.0 / viewingConditions.Fl) * Math.Pow(bCBase, 1.0 / 0.42);
        double rF = rC / viewingConditions.RgbD[0];
        double gF = gC / viewingConditions.RgbD[1];
        double bF = bC / viewingConditions.RgbD[2];

        double[][] matrix = Cam16RgbToXyz;
        double x = (rF * matrix[0][0]) + (gF * matrix[0][1]) + (bF * matrix[0][2]);
        double y = (rF * matrix[1][0]) + (gF * matrix[1][1]) + (bF * matrix[1][2]);
        double z = (rF * matrix[2][0]) + (gF * matrix[2][1]) + (bF * matrix[2][2]);

        if (returnArray is not null)
        {
            returnArray[0] = x;
            returnArray[1] = y;
            returnArray[2] = z;
            return returnArray;
        }

        return [x, y, z];
    }
}
