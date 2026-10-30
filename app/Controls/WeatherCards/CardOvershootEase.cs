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

namespace LibreccioWeather.Controls.WeatherCards;

internal sealed class CardOvershootEase : EasingFunctionBase
{
    public float Tension { get; init; } = 1;

    public CardOvershootEase()
    {
        EasingMode = EasingMode.EaseIn;
    }

    protected override double EaseInCore(double normalizedTime)
    {
        float t = (float)normalizedTime - 1;
        return t * t * ((Tension + 1) * t + Tension) + 1;
    }

    protected override Freezable CreateInstanceCore() => new CardOvershootEase { Tension = Tension };
}
