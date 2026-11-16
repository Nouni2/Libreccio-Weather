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
 * Copyright 2022 Google LLC
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
/// The settings a scheme's colors are worked out from: its source color, variant, light or dark mode and contrast
/// level, with its six palettes. Only the 2021 color specification is implemented.
/// </summary>
public class DynamicScheme
{
    public DynamicScheme(
        Hct sourceColorHct,
        Variant variant,
        bool isDark,
        double contrastLevel,
        TonalPalette primaryPalette,
        TonalPalette secondaryPalette,
        TonalPalette tertiaryPalette,
        TonalPalette neutralPalette,
        TonalPalette neutralVariantPalette,
        TonalPalette? errorPalette = null)
    {
        SourceColorHct = sourceColorHct;
        SourceColorArgb = sourceColorHct.ToInt();
        Variant = variant;
        IsDark = isDark;
        ContrastLevel = contrastLevel;
        PrimaryPalette = primaryPalette;
        SecondaryPalette = secondaryPalette;
        TertiaryPalette = tertiaryPalette;
        NeutralPalette = neutralPalette;
        NeutralVariantPalette = neutralVariantPalette;
        ErrorPalette = errorPalette ?? TonalPalette.FromHueAndChroma(25.0, 84.0);
    }

    public int SourceColorArgb { get; }

    public Hct SourceColorHct { get; }

    public Variant Variant { get; }

    public bool IsDark { get; }

    /// <summary>
    /// From -1 for the least contrast to 1 for the most; 0 is the contrast of the specification.
    /// </summary>
    public double ContrastLevel { get; }

    public TonalPalette PrimaryPalette { get; }

    public TonalPalette SecondaryPalette { get; }

    public TonalPalette TertiaryPalette { get; }

    public TonalPalette NeutralPalette { get; }

    public TonalPalette NeutralVariantPalette { get; }

    public TonalPalette ErrorPalette { get; }

    /// <summary>
    /// The value of the range the source color's hue falls in: hues[i] when the hue lies from hueBreakpoints[i]
    /// inclusive to hueBreakpoints[i + 1] exclusive, or the source hue when it lies in none.
    /// </summary>
    public static double GetPiecewiseValue(Hct sourceColorHct, double[] hueBreakpoints, double[] hues)
    {
        int size = Math.Min(hueBreakpoints.Length - 1, hues.Length);
        double sourceHue = sourceColorHct.Hue;
        for (int i = 0; i < size; i++)
        {
            if (sourceHue >= hueBreakpoints[i] && sourceHue < hueBreakpoints[i + 1])
            {
                return ColorMath.SanitizeDegreesDouble(hues[i]);
            }
        }

        return sourceHue;
    }

    /// <summary>
    /// The source color's hue rotated by the rotation of the range it falls in, as GetPiecewiseValue finds it.
    /// </summary>
    public static double GetRotatedHue(Hct sourceColorHct, double[] hueBreakpoints, double[] rotations)
    {
        double rotation = GetPiecewiseValue(sourceColorHct, hueBreakpoints, rotations);
        if (Math.Min(hueBreakpoints.Length - 1, rotations.Length) <= 0)
        {
            rotation = 0;
        }

        return ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue + rotation);
    }

    public Hct GetHct(DynamicColor dynamicColor)
    {
        return dynamicColor.GetHct(this);
    }

    public int GetArgb(DynamicColor dynamicColor)
    {
        return dynamicColor.GetArgb(this);
    }

    public int Background => GetArgb(MaterialDynamicColors.Background());

    public int OnBackground => GetArgb(MaterialDynamicColors.OnBackground());

    public int Surface => GetArgb(MaterialDynamicColors.Surface());

    public int SurfaceDim => GetArgb(MaterialDynamicColors.SurfaceDim());

    public int SurfaceBright => GetArgb(MaterialDynamicColors.SurfaceBright());

    public int SurfaceContainerLowest => GetArgb(MaterialDynamicColors.SurfaceContainerLowest());

    public int SurfaceContainerLow => GetArgb(MaterialDynamicColors.SurfaceContainerLow());

    public int SurfaceContainer => GetArgb(MaterialDynamicColors.SurfaceContainer());

    public int SurfaceContainerHigh => GetArgb(MaterialDynamicColors.SurfaceContainerHigh());

    public int SurfaceContainerHighest => GetArgb(MaterialDynamicColors.SurfaceContainerHighest());

    public int OnSurface => GetArgb(MaterialDynamicColors.OnSurface());

    public int SurfaceVariant => GetArgb(MaterialDynamicColors.SurfaceVariant());

    public int OnSurfaceVariant => GetArgb(MaterialDynamicColors.OnSurfaceVariant());

    public int InverseSurface => GetArgb(MaterialDynamicColors.InverseSurface());

    public int InverseOnSurface => GetArgb(MaterialDynamicColors.InverseOnSurface());

    public int Outline => GetArgb(MaterialDynamicColors.Outline());

    public int OutlineVariant => GetArgb(MaterialDynamicColors.OutlineVariant());

    public int Shadow => GetArgb(MaterialDynamicColors.Shadow());

    public int Scrim => GetArgb(MaterialDynamicColors.Scrim());

    public int SurfaceTint => GetArgb(MaterialDynamicColors.SurfaceTint());

    public int Primary => GetArgb(MaterialDynamicColors.Primary());

    public int OnPrimary => GetArgb(MaterialDynamicColors.OnPrimary());

    public int PrimaryContainer => GetArgb(MaterialDynamicColors.PrimaryContainer());

    public int OnPrimaryContainer => GetArgb(MaterialDynamicColors.OnPrimaryContainer());

    public int InversePrimary => GetArgb(MaterialDynamicColors.InversePrimary());

    public int Secondary => GetArgb(MaterialDynamicColors.Secondary());

    public int OnSecondary => GetArgb(MaterialDynamicColors.OnSecondary());

    public int SecondaryContainer => GetArgb(MaterialDynamicColors.SecondaryContainer());

    public int OnSecondaryContainer => GetArgb(MaterialDynamicColors.OnSecondaryContainer());

    public int Tertiary => GetArgb(MaterialDynamicColors.Tertiary());

    public int OnTertiary => GetArgb(MaterialDynamicColors.OnTertiary());

    public int TertiaryContainer => GetArgb(MaterialDynamicColors.TertiaryContainer());

    public int OnTertiaryContainer => GetArgb(MaterialDynamicColors.OnTertiaryContainer());

    public int Error => GetArgb(MaterialDynamicColors.Error());

    public int OnError => GetArgb(MaterialDynamicColors.OnError());

    public int ErrorContainer => GetArgb(MaterialDynamicColors.ErrorContainer());

    public int OnErrorContainer => GetArgb(MaterialDynamicColors.OnErrorContainer());
}
