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
 * Copyright 2025 Google LLC
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
/// The 2021 Material color specification: each role's palette, tone and contrast, how a role's tone is worked
/// out in a scheme, and each variant's palettes.
/// </summary>
internal static class ColorSpec2021
{
    public static DynamicColor PrimaryPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("primary_palette_key_color")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s => s.PrimaryPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor SecondaryPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("secondary_palette_key_color")
        .SetPalette(s => s.SecondaryPalette)
        .SetTone(s => s.SecondaryPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor TertiaryPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("tertiary_palette_key_color")
        .SetPalette(s => s.TertiaryPalette)
        .SetTone(s => s.TertiaryPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor NeutralPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("neutral_palette_key_color")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.NeutralPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor NeutralVariantPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("neutral_variant_palette_key_color")
        .SetPalette(s => s.NeutralVariantPalette)
        .SetTone(s => s.NeutralVariantPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor ErrorPaletteKeyColor() => new DynamicColor.Builder()
        .SetName("error_palette_key_color")
        .SetPalette(s => s.ErrorPalette)
        .SetTone(s => s.ErrorPalette.KeyColor.Tone)
        .Build();

    public static DynamicColor Background() => new DynamicColor.Builder()
        .SetName("background")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 6.0 : 98.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor OnBackground() => new DynamicColor.Builder()
        .SetName("on_background")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 90.0 : 10.0)
        .SetBackground(_ => Background())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 3.0, 4.5, 7.0))
        .Build();

    public static DynamicColor Surface() => new DynamicColor.Builder()
        .SetName("surface")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 6.0 : 98.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceDim() => new DynamicColor.Builder()
        .SetName("surface_dim")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 6.0 : new ContrastCurve(87.0, 87.0, 80.0, 75.0).Get(s.ContrastLevel))
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceBright() => new DynamicColor.Builder()
        .SetName("surface_bright")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? new ContrastCurve(24.0, 24.0, 29.0, 34.0).Get(s.ContrastLevel) : 98.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceContainerLowest() => new DynamicColor.Builder()
        .SetName("surface_container_lowest")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? new ContrastCurve(4.0, 4.0, 2.0, 0.0).Get(s.ContrastLevel) : 100.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceContainerLow() => new DynamicColor.Builder()
        .SetName("surface_container_low")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark
            ? new ContrastCurve(10.0, 10.0, 11.0, 12.0).Get(s.ContrastLevel)
            : new ContrastCurve(96.0, 96.0, 96.0, 95.0).Get(s.ContrastLevel))
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceContainer() => new DynamicColor.Builder()
        .SetName("surface_container")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark
            ? new ContrastCurve(12.0, 12.0, 16.0, 20.0).Get(s.ContrastLevel)
            : new ContrastCurve(94.0, 94.0, 92.0, 90.0).Get(s.ContrastLevel))
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceContainerHigh() => new DynamicColor.Builder()
        .SetName("surface_container_high")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark
            ? new ContrastCurve(17.0, 17.0, 21.0, 25.0).Get(s.ContrastLevel)
            : new ContrastCurve(92.0, 92.0, 88.0, 85.0).Get(s.ContrastLevel))
        .SetIsBackground(true)
        .Build();

    public static DynamicColor SurfaceContainerHighest() => new DynamicColor.Builder()
        .SetName("surface_container_highest")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark
            ? new ContrastCurve(22.0, 22.0, 26.0, 30.0).Get(s.ContrastLevel)
            : new ContrastCurve(90.0, 90.0, 84.0, 80.0).Get(s.ContrastLevel))
        .SetIsBackground(true)
        .Build();

    public static DynamicColor OnSurface() => new DynamicColor.Builder()
        .SetName("on_surface")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 90.0 : 10.0)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor SurfaceVariant() => new DynamicColor.Builder()
        .SetName("surface_variant")
        .SetPalette(s => s.NeutralVariantPalette)
        .SetTone(s => s.IsDark ? 30.0 : 90.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor OnSurfaceVariant() => new DynamicColor.Builder()
        .SetName("on_surface_variant")
        .SetPalette(s => s.NeutralVariantPalette)
        .SetTone(s => s.IsDark ? 80.0 : 30.0)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 11.0))
        .Build();

    public static DynamicColor InverseSurface() => new DynamicColor.Builder()
        .SetName("inverse_surface")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 90.0 : 20.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor InverseOnSurface() => new DynamicColor.Builder()
        .SetName("inverse_on_surface")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(s => s.IsDark ? 20.0 : 95.0)
        .SetBackground(_ => InverseSurface())
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor Outline() => new DynamicColor.Builder()
        .SetName("outline")
        .SetPalette(s => s.NeutralVariantPalette)
        .SetTone(s => s.IsDark ? 60.0 : 50.0)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.5, 3.0, 4.5, 7.0))
        .Build();

    public static DynamicColor OutlineVariant() => new DynamicColor.Builder()
        .SetName("outline_variant")
        .SetPalette(s => s.NeutralVariantPalette)
        .SetTone(s => s.IsDark ? 30.0 : 80.0)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.0, 1.0, 3.0, 4.5))
        .Build();

    public static DynamicColor Shadow() => new DynamicColor.Builder()
        .SetName("shadow")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(_ => 0.0)
        .Build();

    public static DynamicColor Scrim() => new DynamicColor.Builder()
        .SetName("scrim")
        .SetPalette(s => s.NeutralPalette)
        .SetTone(_ => 0.0)
        .Build();

    public static DynamicColor SurfaceTint() => new DynamicColor.Builder()
        .SetName("surface_tint")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s => s.IsDark ? 80.0 : 40.0)
        .SetIsBackground(true)
        .Build();

    public static DynamicColor Primary() => new DynamicColor.Builder()
        .SetName("primary")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 100.0 : 0.0;
            }

            return s.IsDark ? 80.0 : 40.0;
        })
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 7.0))
        .SetToneDeltaPair(_ => new ToneDeltaPair(PrimaryContainer(), Primary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnPrimary() => new DynamicColor.Builder()
        .SetName("on_primary")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 10.0 : 90.0;
            }

            return s.IsDark ? 20.0 : 100.0;
        })
        .SetBackground(_ => Primary())
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor PrimaryContainer() => new DynamicColor.Builder()
        .SetName("primary_container")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s =>
        {
            if (IsFidelity(s))
            {
                return s.SourceColorHct.Tone;
            }

            if (IsMonochrome(s))
            {
                return s.IsDark ? 85.0 : 25.0;
            }

            return s.IsDark ? 30.0 : 90.0;
        })
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.0, 1.0, 3.0, 4.5))
        .SetToneDeltaPair(_ => new ToneDeltaPair(PrimaryContainer(), Primary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnPrimaryContainer() => new DynamicColor.Builder()
        .SetName("on_primary_container")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s =>
        {
            if (IsFidelity(s))
            {
                return DynamicColor.ForegroundTone(PrimaryContainer().Tone(s), 4.5);
            }

            if (IsMonochrome(s))
            {
                return s.IsDark ? 0.0 : 100.0;
            }

            return s.IsDark ? 90.0 : 30.0;
        })
        .SetBackground(_ => PrimaryContainer())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 11.0))
        .Build();

    public static DynamicColor InversePrimary() => new DynamicColor.Builder()
        .SetName("inverse_primary")
        .SetPalette(s => s.PrimaryPalette)
        .SetTone(s => s.IsDark ? 40.0 : 80.0)
        .SetBackground(_ => InverseSurface())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 7.0))
        .Build();

    public static DynamicColor Secondary() => new DynamicColor.Builder()
        .SetName("secondary")
        .SetPalette(s => s.SecondaryPalette)
        .SetTone(s => s.IsDark ? 80.0 : 40.0)
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 7.0))
        .SetToneDeltaPair(_ => new ToneDeltaPair(SecondaryContainer(), Secondary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnSecondary() => new DynamicColor.Builder()
        .SetName("on_secondary")
        .SetPalette(s => s.SecondaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 10.0 : 100.0;
            }

            return s.IsDark ? 20.0 : 100.0;
        })
        .SetBackground(_ => Secondary())
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor SecondaryContainer() => new DynamicColor.Builder()
        .SetName("secondary_container")
        .SetPalette(s => s.SecondaryPalette)
        .SetTone(s =>
        {
            double initialTone = s.IsDark ? 30.0 : 90.0;
            if (IsMonochrome(s))
            {
                return s.IsDark ? 30.0 : 85.0;
            }

            if (!IsFidelity(s))
            {
                return initialTone;
            }

            return FindDesiredChromaByTone(s.SecondaryPalette.Hue, s.SecondaryPalette.Chroma, initialTone, !s.IsDark);
        })
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.0, 1.0, 3.0, 4.5))
        .SetToneDeltaPair(_ => new ToneDeltaPair(SecondaryContainer(), Secondary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnSecondaryContainer() => new DynamicColor.Builder()
        .SetName("on_secondary_container")
        .SetPalette(s => s.SecondaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 90.0 : 10.0;
            }

            if (!IsFidelity(s))
            {
                return s.IsDark ? 90.0 : 30.0;
            }

            return DynamicColor.ForegroundTone(SecondaryContainer().Tone(s), 4.5);
        })
        .SetBackground(_ => SecondaryContainer())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 11.0))
        .Build();

    public static DynamicColor Tertiary() => new DynamicColor.Builder()
        .SetName("tertiary")
        .SetPalette(s => s.TertiaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 90.0 : 25.0;
            }

            return s.IsDark ? 80.0 : 40.0;
        })
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 7.0))
        .SetToneDeltaPair(_ => new ToneDeltaPair(TertiaryContainer(), Tertiary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnTertiary() => new DynamicColor.Builder()
        .SetName("on_tertiary")
        .SetPalette(s => s.TertiaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 10.0 : 90.0;
            }

            return s.IsDark ? 20.0 : 100.0;
        })
        .SetBackground(_ => Tertiary())
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor TertiaryContainer() => new DynamicColor.Builder()
        .SetName("tertiary_container")
        .SetPalette(s => s.TertiaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 60.0 : 49.0;
            }

            if (!IsFidelity(s))
            {
                return s.IsDark ? 30.0 : 90.0;
            }

            Hct proposedHct = s.TertiaryPalette.GetHct(s.SourceColorHct.Tone);
            return DislikeAnalyzer.FixIfDisliked(proposedHct).Tone;
        })
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.0, 1.0, 3.0, 4.5))
        .SetToneDeltaPair(_ => new ToneDeltaPair(TertiaryContainer(), Tertiary(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnTertiaryContainer() => new DynamicColor.Builder()
        .SetName("on_tertiary_container")
        .SetPalette(s => s.TertiaryPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 0.0 : 100.0;
            }

            if (!IsFidelity(s))
            {
                return s.IsDark ? 90.0 : 30.0;
            }

            return DynamicColor.ForegroundTone(TertiaryContainer().Tone(s), 4.5);
        })
        .SetBackground(_ => TertiaryContainer())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 11.0))
        .Build();

    public static DynamicColor Error() => new DynamicColor.Builder()
        .SetName("error")
        .SetPalette(s => s.ErrorPalette)
        .SetTone(s => s.IsDark ? 80.0 : 40.0)
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 7.0))
        .SetToneDeltaPair(_ => new ToneDeltaPair(ErrorContainer(), Error(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnError() => new DynamicColor.Builder()
        .SetName("on_error")
        .SetPalette(s => s.ErrorPalette)
        .SetTone(s => s.IsDark ? 20.0 : 100.0)
        .SetBackground(_ => Error())
        .SetContrastCurve(_ => new ContrastCurve(4.5, 7.0, 11.0, 21.0))
        .Build();

    public static DynamicColor ErrorContainer() => new DynamicColor.Builder()
        .SetName("error_container")
        .SetPalette(s => s.ErrorPalette)
        .SetTone(s => s.IsDark ? 30.0 : 90.0)
        .SetIsBackground(true)
        .SetBackground(HighestSurface)
        .SetContrastCurve(_ => new ContrastCurve(1.0, 1.0, 3.0, 4.5))
        .SetToneDeltaPair(_ => new ToneDeltaPair(ErrorContainer(), Error(), 10.0, TonePolarity.Nearer, false))
        .Build();

    public static DynamicColor OnErrorContainer() => new DynamicColor.Builder()
        .SetName("on_error_container")
        .SetPalette(s => s.ErrorPalette)
        .SetTone(s =>
        {
            if (IsMonochrome(s))
            {
                return s.IsDark ? 90.0 : 10.0;
            }

            return s.IsDark ? 90.0 : 30.0;
        })
        .SetBackground(_ => ErrorContainer())
        .SetContrastCurve(_ => new ContrastCurve(3.0, 4.5, 7.0, 11.0))
        .Build();

    public static DynamicColor HighestSurface(DynamicScheme s)
    {
        return s.IsDark ? SurfaceBright() : SurfaceDim();
    }

    private static bool IsFidelity(DynamicScheme scheme)
    {
        return scheme.Variant is Variant.Fidelity or Variant.Content;
    }

    private static bool IsMonochrome(DynamicScheme scheme)
    {
        return scheme.Variant == Variant.Monochrome;
    }

    private static double FindDesiredChromaByTone(double hue, double chroma, double tone, bool byDecreasingTone)
    {
        double answer = tone;

        Hct closestToChroma = Hct.From(hue, chroma, tone);
        if (closestToChroma.Chroma < chroma)
        {
            double chromaPeak = closestToChroma.Chroma;
            while (closestToChroma.Chroma < chroma)
            {
                answer += byDecreasingTone ? -1.0 : 1.0;
                Hct potentialSolution = Hct.From(hue, chroma, answer);
                if (chromaPeak > potentialSolution.Chroma)
                {
                    break;
                }

                if (Math.Abs(potentialSolution.Chroma - chroma) < 0.4)
                {
                    break;
                }

                double potentialDelta = Math.Abs(potentialSolution.Chroma - chroma);
                double currentDelta = Math.Abs(closestToChroma.Chroma - chroma);
                if (potentialDelta < currentDelta)
                {
                    closestToChroma = potentialSolution;
                }

                chromaPeak = Math.Max(chromaPeak, potentialSolution.Chroma);
            }
        }

        return answer;
    }

    /// <summary>
    /// The role's color in the scheme: its tone for contrast, with the chroma of its palette, so a role whose usual
    /// tone has little chroma recovers chroma as contrast rises.
    /// </summary>
    public static Hct GetHct(DynamicScheme scheme, DynamicColor color)
    {
        double tone = GetTone(scheme, color);
        return color.Palette(scheme).GetHct(tone);
    }

    public static double GetTone(DynamicScheme scheme, DynamicColor color)
    {
        bool decreasingContrast = scheme.ContrastLevel < 0;
        ToneDeltaPair? toneDeltaPair = color.ToneDeltaPair?.Invoke(scheme);

        // Two roles that keep a distance between their tones.
        if (toneDeltaPair is not null)
        {
            DynamicColor roleA = toneDeltaPair.RoleA;
            DynamicColor roleB = toneDeltaPair.RoleB;
            double delta = toneDeltaPair.Delta;
            TonePolarity polarity = toneDeltaPair.Polarity;
            bool stayTogether = toneDeltaPair.StayTogether;

            bool aIsNearer = polarity == TonePolarity.Nearer
                || (polarity == TonePolarity.Lighter && !scheme.IsDark)
                || (polarity == TonePolarity.Darker && !scheme.IsDark);
            DynamicColor nearer = aIsNearer ? roleA : roleB;
            DynamicColor farther = aIsNearer ? roleB : roleA;
            bool amNearer = color.Name == nearer.Name;
            double expansionDir = scheme.IsDark ? 1 : -1;
            double nTone = nearer.Tone(scheme);
            double fTone = farther.Tone(scheme);

            // First round: each role at the least tone that reaches its contrast.
            if (color.Background is not null && nearer.ContrastCurve is not null && farther.ContrastCurve is not null)
            {
                DynamicColor? bg = color.Background(scheme);
                ContrastCurve? nContrastCurve = nearer.ContrastCurve(scheme);
                ContrastCurve? fContrastCurve = farther.ContrastCurve(scheme);
                if (bg is not null && nContrastCurve is not null && fContrastCurve is not null)
                {
                    double nContrast = nContrastCurve.Get(scheme.ContrastLevel);
                    double fContrast = fContrastCurve.Get(scheme.ContrastLevel);
                    double bgTone = bg.GetTone(scheme);

                    // A tone that already reaches its contrast is kept.
                    if (Contrast.RatioOfTones(bgTone, nTone) < nContrast)
                    {
                        nTone = DynamicColor.ForegroundTone(bgTone, nContrast);
                    }

                    if (Contrast.RatioOfTones(bgTone, fTone) < fContrast)
                    {
                        fTone = DynamicColor.ForegroundTone(bgTone, fContrast);
                    }

                    if (decreasingContrast)
                    {
                        // Lowered contrast takes the least tone that still reaches the ratio.
                        nTone = DynamicColor.ForegroundTone(bgTone, nContrast);
                        fTone = DynamicColor.ForegroundTone(bgTone, fContrast);
                    }
                }
            }

            if ((fTone - nTone) * expansionDir < delta)
            {
                // Second round: the farther tone moves out to the distance.
                fTone = ColorMath.ClampDouble(0, 100, nTone + delta * expansionDir);
                if ((fTone - nTone) * expansionDir < delta)
                {
                    // Third round: the nearer tone moves in to the distance.
                    nTone = ColorMath.ClampDouble(0, 100, fTone - delta * expansionDir);
                }
            }

            // Keeps out of the awkward tones 50 to 59.
            if (50 <= nTone && nTone < 60)
            {
                // The nearer tone moves out, taking the farther one with it.
                if (expansionDir > 0)
                {
                    nTone = 60;
                    fTone = Math.Max(fTone, nTone + delta * expansionDir);
                }
                else
                {
                    nTone = 49;
                    fTone = Math.Min(fTone, nTone + delta * expansionDir);
                }
            }
            else if (50 <= fTone && fTone < 60)
            {
                if (stayTogether)
                {
                    // Both move, so they never end on opposite sides of the awkward tones.
                    if (expansionDir > 0)
                    {
                        nTone = 60;
                        fTone = Math.Max(fTone, nTone + delta * expansionDir);
                    }
                    else
                    {
                        nTone = 49;
                        fTone = Math.Min(fTone, nTone + delta * expansionDir);
                    }
                }
                else
                {
                    // Only the farther tone moves.
                    if (expansionDir > 0)
                    {
                        fTone = 60;
                    }
                    else
                    {
                        fTone = 49;
                    }
                }
            }

            return amNearer ? nTone : fTone;
        }
        else
        {
            // A role on its own.
            double answer = color.Tone(scheme);

            if (color.Background?.Invoke(scheme) is not { } background || color.ContrastCurve?.Invoke(scheme) is not { } contrastCurve)
            {
                // A role without a background keeps its tone.
                return answer;
            }

            double bgTone = background.GetTone(scheme);
            double desiredRatio = contrastCurve.Get(scheme.ContrastLevel);

            // A tone that already reaches the ratio is kept.
            if (Contrast.RatioOfTones(bgTone, answer) < desiredRatio)
            {
                answer = DynamicColor.ForegroundTone(bgTone, desiredRatio);
            }

            if (decreasingContrast)
            {
                answer = DynamicColor.ForegroundTone(bgTone, desiredRatio);
            }

            if (color.IsBackground && 50 <= answer && answer < 60)
            {
                if (Contrast.RatioOfTones(49, bgTone) >= desiredRatio)
                {
                    answer = 49;
                }
                else
                {
                    answer = 60;
                }
            }

            if (color.SecondBackground?.Invoke(scheme) is not { } secondBackground)
            {
                return answer;
            }

            // A role on two backgrounds.
            double bgTone1 = background.GetTone(scheme);
            double bgTone2 = secondBackground.GetTone(scheme);

            double upper = Math.Max(bgTone1, bgTone2);
            double lower = Math.Min(bgTone1, bgTone2);

            if (Contrast.RatioOfTones(upper, answer) >= desiredRatio && Contrast.RatioOfTones(lower, answer) >= desiredRatio)
            {
                return answer;
            }

            // The darkest light tone and the lightest dark tone that reach the ratio, or -1 when none does.
            double lightOption = Contrast.Lighter(upper, desiredRatio);
            double darkOption = Contrast.Darker(lower, desiredRatio);

            List<double> availables = [];
            if (lightOption != -1)
            {
                availables.Add(lightOption);
            }

            if (darkOption != -1)
            {
                availables.Add(darkOption);
            }

            bool prefersLight = DynamicColor.TonePrefersLightForeground(bgTone1) || DynamicColor.TonePrefersLightForeground(bgTone2);
            if (prefersLight)
            {
                return (lightOption == -1) ? 100 : lightOption;
            }

            if (availables.Count == 1)
            {
                return availables[0];
            }

            return (darkOption == -1) ? 0 : darkOption;
        }
    }

    public static TonalPalette GetPrimaryPalette(Variant variant, Hct sourceColorHct)
    {
        return variant switch
        {
            Variant.Content or Variant.Fidelity => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, sourceColorHct.Chroma),
            Variant.FruitSalad => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue - 50.0), 48.0),
            Variant.Monochrome => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.Neutral => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 12.0),
            Variant.Rainbow => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 48.0),
            Variant.TonalSpot => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 36.0),
            Variant.Expressive => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue + 240), 40),
            Variant.Vibrant => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 200.0),
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }

    public static TonalPalette GetSecondaryPalette(Variant variant, Hct sourceColorHct)
    {
        return variant switch
        {
            Variant.Content or Variant.Fidelity => TonalPalette.FromHueAndChroma(
                sourceColorHct.Hue, Math.Max(sourceColorHct.Chroma - 32.0, sourceColorHct.Chroma * 0.5)),
            Variant.FruitSalad => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue - 50.0), 36.0),
            Variant.Monochrome => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.Neutral => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 8.0),
            Variant.Rainbow => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 16.0),
            Variant.TonalSpot => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 16.0),
            Variant.Expressive => TonalPalette.FromHueAndChroma(
                DynamicScheme.GetRotatedHue(sourceColorHct, [0, 21, 51, 121, 151, 191, 271, 321, 360], [45, 95, 45, 20, 45, 90, 45, 45, 45]),
                24.0),
            Variant.Vibrant => TonalPalette.FromHueAndChroma(
                DynamicScheme.GetRotatedHue(sourceColorHct, [0, 41, 61, 101, 131, 181, 251, 301, 360], [18, 15, 10, 12, 15, 18, 15, 12, 12]),
                24.0),
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }

    // Content and fidelity take their tertiary palette from color temperature, which is not ported.
    public static TonalPalette GetTertiaryPalette(Variant variant, Hct sourceColorHct)
    {
        return variant switch
        {
            Variant.FruitSalad => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 36.0),
            Variant.Monochrome => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.Neutral => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 16.0),
            Variant.Rainbow or Variant.TonalSpot => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue + 60.0), 24.0),
            Variant.Expressive => TonalPalette.FromHueAndChroma(
                DynamicScheme.GetRotatedHue(sourceColorHct, [0, 21, 51, 121, 151, 191, 271, 321, 360], [120, 120, 20, 45, 20, 15, 20, 120, 120]),
                32.0),
            Variant.Vibrant => TonalPalette.FromHueAndChroma(
                DynamicScheme.GetRotatedHue(sourceColorHct, [0, 41, 61, 101, 131, 181, 251, 301, 360], [35, 30, 20, 25, 30, 35, 30, 25, 25]),
                32.0),
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }

    public static TonalPalette GetNeutralPalette(Variant variant, Hct sourceColorHct)
    {
        return variant switch
        {
            Variant.Content or Variant.Fidelity => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, sourceColorHct.Chroma / 8.0),
            Variant.FruitSalad => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 10.0),
            Variant.Monochrome => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.Neutral => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 2.0),
            Variant.Rainbow => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.TonalSpot => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 6.0),
            Variant.Expressive => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue + 15), 8),
            Variant.Vibrant => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 10),
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }

    public static TonalPalette GetNeutralVariantPalette(Variant variant, Hct sourceColorHct)
    {
        return variant switch
        {
            Variant.Content => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, (sourceColorHct.Chroma / 8.0) + 4.0),
            Variant.Fidelity => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, (sourceColorHct.Chroma / 8.0) + 4.0),
            Variant.FruitSalad => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 16.0),
            Variant.Monochrome => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.Neutral => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 2.0),
            Variant.Rainbow => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 0.0),
            Variant.TonalSpot => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 8.0),
            Variant.Expressive => TonalPalette.FromHueAndChroma(ColorMath.SanitizeDegreesDouble(sourceColorHct.Hue + 15), 12),
            Variant.Vibrant => TonalPalette.FromHueAndChroma(sourceColorHct.Hue, 12),
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }

    /// <summary>
    /// The error palette of a variant, or null for the scheme's default one.
    /// </summary>
    public static TonalPalette? GetErrorPalette(Variant variant)
    {
        return variant switch
        {
            Variant.Content or Variant.Fidelity or Variant.FruitSalad or Variant.Monochrome or Variant.Neutral
                or Variant.Rainbow or Variant.TonalSpot or Variant.Expressive or Variant.Vibrant => null,
            _ => throw new ArgumentException(variant + " variant is not supported in current spec.", nameof(variant)),
        };
    }
}

/// <summary>
/// The themes a dynamic scheme can take.
/// </summary>
public enum Variant
{
    Monochrome,
    Neutral,
    TonalSpot,
    Vibrant,
    Expressive,
    Fidelity,
    Content,
    Rainbow,
    FruitSalad,
    Cmf,
}
