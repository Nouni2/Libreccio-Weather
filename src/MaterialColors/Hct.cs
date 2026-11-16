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
/// A color by hue and chroma from CAM16 and tone, the L* of L*a*b*. The tone sets the contrast between two colors
/// alone, which lets a scheme pick accessible colors by tone. Setting a value that no sRGB color reaches keeps the
/// closest color in chroma.
/// </summary>
public sealed class Hct
{
    private Hct(int argb)
    {
        SetInternalState(argb);
    }

    /// <summary>
    /// The hue, in degrees from 0 inclusive to 360 exclusive.
    /// </summary>
    public double Hue { get; private set; }

    /// <summary>
    /// The chroma, from 0 upwards; the highest chroma depends on the hue and the tone.
    /// </summary>
    public double Chroma { get; private set; }

    /// <summary>
    /// The tone, from 0 to 100.
    /// </summary>
    public double Tone { get; private set; }

    public int Argb { get; private set; }

    public static Hct From(double hue, double chroma, double tone)
    {
        int argb = HctSolver.SolveToInt(hue, chroma, tone);
        return new Hct(argb);
    }

    public static Hct FromInt(int argb)
    {
        return new Hct(argb);
    }

    public int ToInt()
    {
        return Argb;
    }

    public void SetHue(double newHue)
    {
        SetInternalState(HctSolver.SolveToInt(newHue, Chroma, Tone));
    }

    public void SetChroma(double newChroma)
    {
        SetInternalState(HctSolver.SolveToInt(Hue, newChroma, Tone));
    }

    public void SetTone(double newTone)
    {
        SetInternalState(HctSolver.SolveToInt(Hue, Chroma, newTone));
    }

    public override string ToString()
    {
        return "HCT("
            + (int)ColorMath.JavaRound(Hue)
            + ", "
            + (int)ColorMath.JavaRound(Chroma)
            + ", "
            + (int)ColorMath.JavaRound(Tone)
            + ")";
    }

    public static bool IsBlue(double hue)
    {
        return hue >= 250 && hue < 270;
    }

    public static bool IsYellow(double hue)
    {
        return hue >= 105 && hue < 125;
    }

    public static bool IsCyan(double hue)
    {
        return hue >= 170 && hue < 207;
    }

    /// <summary>
    /// The color as it looks in other viewing conditions, expressed in the default ones.
    /// </summary>
    internal Hct InViewingConditions(ViewingConditions vc)
    {
        // The XYZ coordinates of the color seen in the given conditions.
        Cam16 cam16 = Cam16.FromInt(ToInt());
        double[] viewedInVc = cam16.XyzInViewingConditions(vc, null);

        // Those coordinates as CAM16 in the default conditions.
        Cam16 recastInVc = Cam16.FromXyzInViewingConditions(viewedInVc[0], viewedInVc[1], viewedInVc[2], ViewingConditions.Default);

        // Their hue and chroma, with the L* of their Y as the tone.
        return From(recastInVc.Hue, recastInVc.Chroma, ColorConversions.LstarFromY(viewedInVc[1]));
    }

    private void SetInternalState(int argb)
    {
        Argb = argb;
        Cam16 cam = Cam16.FromInt(argb);
        Hue = cam.Hue;
        Chroma = cam.Chroma;
        Tone = ColorConversions.LstarFromArgb(argb);
    }
}
