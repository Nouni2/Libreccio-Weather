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
/// Finds the dark yellow-greens that people widely dislike, which reminded test subjects of biological waste, and
/// lightens them.
/// </summary>
internal static class DislikeAnalyzer
{
    public static bool IsDisliked(Hct hct)
    {
        bool huePasses = ColorMath.JavaRound(hct.Hue) >= 90.0 && ColorMath.JavaRound(hct.Hue) <= 111.0;
        bool chromaPasses = ColorMath.JavaRound(hct.Chroma) > 16.0;
        bool tonePasses = ColorMath.JavaRound(hct.Tone) < 65.0;

        return huePasses && chromaPasses && tonePasses;
    }

    /// <summary>
    /// The color lightened to tone 70 when it is disliked, or the color itself.
    /// </summary>
    public static Hct FixIfDisliked(Hct hct)
    {
        if (IsDisliked(hct))
        {
            return Hct.From(hct.Hue, hct.Chroma, 70.0);
        }

        return hct;
    }
}
