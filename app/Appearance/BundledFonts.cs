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
using System.Windows.Media;
using FontFamily = System.Windows.Media.FontFamily;

namespace LibreccioWeather.Appearance;

/// <summary>
/// The fonts shipped inside the app, for text whose size and position must not depend on the Windows font.
/// </summary>
internal static class BundledFonts
{
    /// <summary>
    /// Roboto, with its medium and bold faces. Scripts Roboto lacks, such as Arabic, Chinese, Indic and Thai letters and
    /// digits, fall back to WPF's system composite font; the line metrics stay Roboto's.
    /// </summary>
    public static FontFamily Roboto { get; } =
        new(new Uri("pack://application:,,,/"), "./assets/fonts/#Roboto, Global User Interface");

    public static Typeface RobotoMedium { get; } = new(Roboto, FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);
}
