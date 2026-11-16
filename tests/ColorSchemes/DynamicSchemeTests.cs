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

using LibreccioWeather.Core.MaterialColors;

namespace LibreccioWeather.Core.Tests.ColorSchemes;

public sealed class DynamicSchemeTests
{
    // The reference tests compare to 0.4 decimal digits, which allows a difference below 10^-0.4 / 2.
    private static readonly double Tolerance = Math.Pow(10, -0.4) / 2;

    [Fact]
    public void GetRotatedHue_EmptyInput()
    {
        double hue = DynamicScheme.GetRotatedHue(Hct.From(43, 16, 16), [], []);

        Assert.Equal(43, hue, Tolerance);
    }

    [Fact]
    public void GetRotatedHue_OneBreakpointWithoutRotation()
    {
        double hue = DynamicScheme.GetRotatedHue(Hct.From(43, 16, 16), [0], [0]);

        Assert.Equal(43, hue, Tolerance);
    }

    [Fact]
    public void GetRotatedHue_LengthMismatch()
    {
        double hue = DynamicScheme.GetRotatedHue(Hct.From(43, 16, 16), [0], [0, 1]);

        Assert.Equal(43, hue, Tolerance);
    }

    [Fact]
    public void GetRotatedHue_OnBoundary()
    {
        double hue = DynamicScheme.GetRotatedHue(Hct.From(43, 16, 16), [0, 42, 360], [0, 15, 0]);

        Assert.Equal(43 + 15, hue, Tolerance);
    }

    [Fact]
    public void GetRotatedHue_PastFullTurn_Wraps()
    {
        double hue = DynamicScheme.GetRotatedHue(Hct.From(43, 16, 16), [0, 42, 360], [0, 480, 0]);

        Assert.Equal(163, hue, Tolerance);
    }
}
