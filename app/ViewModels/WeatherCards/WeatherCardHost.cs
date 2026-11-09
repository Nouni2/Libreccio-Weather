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

using CommunityToolkit.Mvvm.ComponentModel;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// One card position on the main screen. A new card of the same kind takes the position's place, so the card's view
/// is kept and only shows the new data, as a recycled list item is rebound.
/// </summary>
public sealed partial class WeatherCardHost : ObservableObject
{
    // Set by the constructor through the property.
    [ObservableProperty]
    private object _card = null!;

    public WeatherCardHost(object card)
    {
        Card = card;
    }
}
