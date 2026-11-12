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
using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.Localization;

/// <summary>
/// The layout direction of the selected language, as Android takes it from the locale: right to left for Arabic,
/// Persian, Hebrew, Urdu and Central Kurdish.
/// </summary>
/// <remarks>
/// WPF mirrors everything under a right-to-left element, drawings and images included, where Android mirrors layouts
/// but draws canvases and images as they are. A control that upstream draws unmirrored is therefore set left to right
/// and reads the direction here to flip only what upstream flips.
/// </remarks>
public static class LayoutDirection
{
    /// <summary>
    /// The application resource that holds the current flow direction, for windows, menus and tooltips, which do not
    /// inherit it from the main window.
    /// </summary>
    public const string ResourceKey = "AppFlowDirection";

    public static bool IsRightToLeft => Strings.Culture.TextInfo.IsRightToLeft;

    public static FlowDirection Current => IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>
    /// Publishes the selected language's direction to the application's resources.
    /// </summary>
    public static void Apply(ResourceDictionary resources)
    {
        resources[ResourceKey] = Current;
    }
}
