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
/// The contrast ratio a color needs at each contrast level: low at -1, normal at 0, medium at 0.5 and high at 1,
/// interpolated linearly between them.
/// </summary>
internal sealed class ContrastCurve(double low, double normal, double medium, double high)
{
    public double Get(double contrastLevel)
    {
        if (contrastLevel <= -1.0)
        {
            return low;
        }
        else if (contrastLevel < 0.0)
        {
            return ColorMath.Lerp(low, normal, (contrastLevel - -1) / 1);
        }
        else if (contrastLevel < 0.5)
        {
            return ColorMath.Lerp(normal, medium, (contrastLevel - 0) / 0.5);
        }
        else if (contrastLevel < 1.0)
        {
            return ColorMath.Lerp(medium, high, (contrastLevel - 0.5) / 0.5);
        }
        else
        {
            return high;
        }
    }
}
