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
 * Copyright 2023 Google LLC
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
/// The Material color roles. Later specifications extend each role only for schemes on their own version, so
/// for a scheme on the 2021 specification every role is the 2021 one.
/// </summary>
public static class MaterialDynamicColors
{
    public static DynamicColor HighestSurface(DynamicScheme s) => ColorSpec2021.HighestSurface(s);

    public static DynamicColor PrimaryPaletteKeyColor() => ColorSpec2021.PrimaryPaletteKeyColor();

    public static DynamicColor SecondaryPaletteKeyColor() => ColorSpec2021.SecondaryPaletteKeyColor();

    public static DynamicColor TertiaryPaletteKeyColor() => ColorSpec2021.TertiaryPaletteKeyColor();

    public static DynamicColor NeutralPaletteKeyColor() => ColorSpec2021.NeutralPaletteKeyColor();

    public static DynamicColor NeutralVariantPaletteKeyColor() => ColorSpec2021.NeutralVariantPaletteKeyColor();

    public static DynamicColor ErrorPaletteKeyColor() => ColorSpec2021.ErrorPaletteKeyColor();

    public static DynamicColor Background() => ColorSpec2021.Background();

    public static DynamicColor OnBackground() => ColorSpec2021.OnBackground();

    public static DynamicColor Surface() => ColorSpec2021.Surface();

    public static DynamicColor SurfaceDim() => ColorSpec2021.SurfaceDim();

    public static DynamicColor SurfaceBright() => ColorSpec2021.SurfaceBright();

    public static DynamicColor SurfaceContainerLowest() => ColorSpec2021.SurfaceContainerLowest();

    public static DynamicColor SurfaceContainerLow() => ColorSpec2021.SurfaceContainerLow();

    public static DynamicColor SurfaceContainer() => ColorSpec2021.SurfaceContainer();

    public static DynamicColor SurfaceContainerHigh() => ColorSpec2021.SurfaceContainerHigh();

    public static DynamicColor SurfaceContainerHighest() => ColorSpec2021.SurfaceContainerHighest();

    public static DynamicColor OnSurface() => ColorSpec2021.OnSurface();

    public static DynamicColor SurfaceVariant() => ColorSpec2021.SurfaceVariant();

    public static DynamicColor OnSurfaceVariant() => ColorSpec2021.OnSurfaceVariant();

    public static DynamicColor InverseSurface() => ColorSpec2021.InverseSurface();

    public static DynamicColor InverseOnSurface() => ColorSpec2021.InverseOnSurface();

    public static DynamicColor Outline() => ColorSpec2021.Outline();

    public static DynamicColor OutlineVariant() => ColorSpec2021.OutlineVariant();

    public static DynamicColor Shadow() => ColorSpec2021.Shadow();

    public static DynamicColor Scrim() => ColorSpec2021.Scrim();

    public static DynamicColor SurfaceTint() => ColorSpec2021.SurfaceTint();

    public static DynamicColor Primary() => ColorSpec2021.Primary();

    public static DynamicColor OnPrimary() => ColorSpec2021.OnPrimary();

    public static DynamicColor PrimaryContainer() => ColorSpec2021.PrimaryContainer();

    public static DynamicColor OnPrimaryContainer() => ColorSpec2021.OnPrimaryContainer();

    public static DynamicColor InversePrimary() => ColorSpec2021.InversePrimary();

    public static DynamicColor Secondary() => ColorSpec2021.Secondary();

    public static DynamicColor OnSecondary() => ColorSpec2021.OnSecondary();

    public static DynamicColor SecondaryContainer() => ColorSpec2021.SecondaryContainer();

    public static DynamicColor OnSecondaryContainer() => ColorSpec2021.OnSecondaryContainer();

    public static DynamicColor Tertiary() => ColorSpec2021.Tertiary();

    public static DynamicColor OnTertiary() => ColorSpec2021.OnTertiary();

    public static DynamicColor TertiaryContainer() => ColorSpec2021.TertiaryContainer();

    public static DynamicColor OnTertiaryContainer() => ColorSpec2021.OnTertiaryContainer();

    public static DynamicColor Error() => ColorSpec2021.Error();

    public static DynamicColor OnError() => ColorSpec2021.OnError();

    public static DynamicColor ErrorContainer() => ColorSpec2021.ErrorContainer();

    public static DynamicColor OnErrorContainer() => ColorSpec2021.OnErrorContainer();
}
