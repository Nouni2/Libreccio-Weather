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

using System.Windows;
using System.Windows.Media.Animation;

namespace LibreccioWeather.Display;

/// <summary>
/// Windows' "Animation effects" switch, which stands for the system animation scale.
/// </summary>
public static class AnimationEffects
{
    // Fast enough that the first frame already lies past the end, without being an infinite speed WPF rejects.
    private const double InstantSpeedRatio = 1e9;

    public static bool AreEnabled => SystemParameters.ClientAreaAnimation;

    /// <summary>
    /// With animation effects off, makes the animation end as soon as it starts, its delay included, so it shows its
    /// final value at once and still raises its completion. Callers that wait for the original delay keep waiting.
    /// </summary>
    public static void Apply(Timeline animation)
    {
        if (!AreEnabled)
        {
            animation.BeginTime = TimeSpan.Zero;
            animation.SpeedRatio = InstantSpeedRatio;
        }
    }
}
