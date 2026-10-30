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
using System.Windows.Controls;

namespace LibreccioWeather.Controls;

/// <summary>
/// A Material 3 slider that can mark one value on its track with a stop indicator.
/// </summary>
public sealed class MaterialSlider : Slider
{
    public static readonly DependencyProperty StopValueProperty = DependencyProperty.Register(
        nameof(StopValue), typeof(double), typeof(MaterialSlider), new PropertyMetadata(double.NaN));

    /// <summary>
    /// The value marked on the track, or NaN for none.
    /// </summary>
    public double StopValue
    {
        get => (double)GetValue(StopValueProperty);
        set => SetValue(StopValueProperty, value);
    }
}
