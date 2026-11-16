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
/// A calm scheme of muted colors: the source color's hue at a moderate chroma, a less colorful secondary, a
/// tertiary 60 degrees away and nearly gray surfaces.
/// </summary>
public sealed class SchemeTonalSpot(Hct sourceColorHct, bool isDark, double contrastLevel) : DynamicScheme(
    sourceColorHct,
    Variant.TonalSpot,
    isDark,
    contrastLevel,
    ColorSpec2021.GetPrimaryPalette(Variant.TonalSpot, sourceColorHct),
    ColorSpec2021.GetSecondaryPalette(Variant.TonalSpot, sourceColorHct),
    ColorSpec2021.GetTertiaryPalette(Variant.TonalSpot, sourceColorHct),
    ColorSpec2021.GetNeutralPalette(Variant.TonalSpot, sourceColorHct),
    ColorSpec2021.GetNeutralVariantPalette(Variant.TonalSpot, sourceColorHct),
    ColorSpec2021.GetErrorPalette(Variant.TonalSpot));
