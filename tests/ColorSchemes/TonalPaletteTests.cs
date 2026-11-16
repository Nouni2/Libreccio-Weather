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

using LibreccioWeather.Core.MaterialColors;

namespace LibreccioWeather.Core.Tests.ColorSchemes;

public sealed class TonalPaletteTests
{
    [Theory]
    [InlineData(100, 0xffffffffu)]
    [InlineData(95, 0xfff1efffu)]
    [InlineData(90, 0xffe0e0ffu)]
    [InlineData(80, 0xffbec2ffu)]
    [InlineData(70, 0xff9da3ffu)]
    [InlineData(60, 0xff7c84ffu)]
    [InlineData(50, 0xff5a64ffu)]
    [InlineData(40, 0xff343dffu)]
    [InlineData(30, 0xff0000efu)]
    [InlineData(20, 0xff0001acu)]
    [InlineData(10, 0xff00006eu)]
    [InlineData(0, 0xff000000u)]
    public void Tone_OfBlue(int tone, uint expected)
    {
        TonalPalette blue = TonalPalette.FromInt(unchecked((int)0xff0000ffu));

        Assert.Equal(unchecked((int)expected), blue.Tone(tone));
    }

    [Fact]
    public void KeyColor_WithExactChroma()
    {
        Hct result = TonalPalette.FromHueAndChroma(50.0, 60.0).KeyColor;

        Assert.True(Math.Abs(result.Hue - 50.0) < 10.0);
        Assert.True(Math.Abs(result.Chroma - 60.0) < 0.5);
        Assert.True(result.Tone > 0 && result.Tone < 100);
    }

    // For hue 149 the chroma peaks at 89.6, at tone 87.9.
    [Fact]
    public void KeyColor_WithUnusuallyHighChroma()
    {
        Hct result = TonalPalette.FromHueAndChroma(149.0, 200.0).KeyColor;

        Assert.True(Math.Abs(result.Hue - 149.0) < 10.0);
        Assert.True(result.Chroma > 89.0);
        Assert.True(result.Tone > 0 && result.Tone < 100);
    }

    // Most tones reach a low chroma, so the key color stays close to tone 50.
    [Fact]
    public void KeyColor_WithUnusuallyLowChroma()
    {
        Hct result = TonalPalette.FromHueAndChroma(50.0, 3.0).KeyColor;

        Assert.True(Math.Abs(result.Hue - 50.0) < 10.0);
        Assert.True(Math.Abs(result.Chroma - 3.0) < 0.5);
        Assert.True(Math.Abs(result.Tone - 50.0) < 0.5);
    }
}
