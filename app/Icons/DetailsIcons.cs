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
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.Icons;

/// <summary>
/// The icons of the detail screen, from Icons.xaml. Each keeps the viewport its path was drawn on.
/// </summary>
public static class DetailsIcons
{
    private static readonly HashSet<string> Viewport960 =
    [
        "IconThermostat", "IconAllergy", "IconHumidityPercentage", "IconCircle", "IconModeHeat", "IconModeCool",
        "IconArrowUpwardAlt", "IconArrowDownwardAlt", "IconEqual", "IconTwilight", "IconHelp",
    ];

    private static readonly Dictionary<(string Key, Color Color), ImageSource> Images = [];

    public static int GetViewport(string key)
    {
        return key == "IconSunshineDuration" ? 21 : Viewport960.Contains(key) ? 960 : 24;
    }

    /// <summary>
    /// An icon in one color, for drawing into a chart.
    /// </summary>
    public static ImageSource? CreateImage(string key, Color color)
    {
        if (Images.TryGetValue((key, color), out ImageSource? cached))
        {
            return cached;
        }

        if (Application.Current?.TryFindResource(key) is not Geometry geometry)
        {
            return null;
        }

        int viewport = GetViewport(key);
        Brush brush = new SolidColorBrush(color);
        brush.Freeze();
        var drawing = new DrawingGroup();
        // A transparent square keeps the icon's padding inside its viewport.
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, viewport, viewport))));
        drawing.Children.Add(new GeometryDrawing(brush, null, geometry));
        var image = new DrawingImage(drawing);
        image.Freeze();
        Images[(key, color)] = image;
        return image;
    }
}
