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
using LibreccioWeather.Core.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.Icons;

public static class WindIcons
{
    private static readonly Geometry[] Arrows =
    [
        Geometry.Parse("F1 M108.04,151.24C99.97,168.05 76.03,168.05 67.96,151.24L27.21,66.3C18.79,48.75 35.4,29.63 53.96,35.5L81.29,44.15C85.66,45.54 90.34,45.54 94.71,44.15L122.04,35.5C140.6,29.63 157.21,48.75 148.79,66.3L108.04,151.24Z"),
        Geometry.Parse("F1 M58.39,145.66C40.79,151.84 23.87,134.91 30.05,117.32L61.3,28.44C67.75,10.08 93.02,8.3 101.99,25.58L115.2,51.03C117.31,55.09 120.62,58.4 124.68,60.51L150.13,73.72C167.41,82.69 165.63,107.96 147.26,114.41L58.39,145.66Z"),
        Geometry.Parse("F1 M24.76,108.04C7.95,99.97 7.95,76.03 24.76,67.96L109.7,27.21C127.25,18.79 146.38,35.4 140.5,53.96L131.85,81.29C130.46,85.66 130.46,90.34 131.85,94.71L140.5,122.04C146.38,140.6 127.25,157.21 109.7,148.79L24.76,108.04Z"),
        Geometry.Parse("F1 M30.05,60.39C23.87,42.79 40.79,25.87 58.39,32.05L147.26,63.3C165.63,69.75 167.41,95.02 150.13,103.99L124.68,117.19C120.62,119.31 117.31,122.62 115.2,126.68L101.99,152.13C93.02,169.41 67.75,167.63 61.3,149.26L30.05,60.39Z"),
        Geometry.Parse("F1 M67.96,26.95C76.03,10.13 99.97,10.13 108.04,26.95L148.79,111.89C157.21,129.43 140.6,148.56 122.04,142.68L94.71,134.03C90.34,132.65 85.66,132.65 81.29,134.03L53.96,142.68C35.4,148.56 18.79,129.43 27.21,111.89L67.96,26.95Z"),
        Geometry.Parse("F1 M118.33,31.88C135.93,25.7 152.85,42.62 146.67,60.22L115.42,149.1C108.97,167.46 83.7,169.24 74.73,151.96L61.52,126.51C59.41,122.45 56.1,119.14 52.04,117.03L26.59,103.82C9.31,94.85 11.09,69.58 29.46,63.13L118.33,31.88Z"),
        Geometry.Parse("F1 M151.6,68.96C168.42,77.03 168.42,100.97 151.6,109.04L66.66,149.79C49.12,158.21 29.99,141.6 35.87,123.04L44.52,95.71C45.9,91.34 45.9,86.66 44.52,82.29L35.87,54.96C29.99,36.4 49.12,19.79 66.66,28.21L151.6,68.96Z"),
        Geometry.Parse("F1 M145.66,116.32C151.84,133.91 134.91,150.84 117.32,144.66L28.44,113.41C10.08,106.96 8.3,81.69 25.58,72.72L51.03,59.51C55.09,57.4 58.4,54.09 60.51,50.03L73.72,24.58C82.69,7.3 107.96,9.08 114.41,27.44L145.66,116.32Z"),
        Geometry.Parse("F1 M480.21,920Q398,920 326,889Q254,858 200.5,804.5Q147,751 116,679Q85,607 85,526L211,526Q211,637.99 289.3,715.99Q367.6,794 480,794Q591.99,794 669.99,715.7Q748,637.4 748,525Q748,413.01 669.7,335.01Q591.4,257 479,257L472,257L526,311L459,380L273,194L459,8L526,78L473,131L479,131Q561.28,131 633.14,162Q705,193 759,246.5Q813,300 844,371.56Q875,443.12 875,525.06Q875,607 844,679Q813,751 759.5,804.5Q706,858 634.21,889Q562.41,920 480.21,920Z"),
    ];

    public static ImageSource? GetDrawableArrow(Wind? wind, Color color)
    {
        if (wind?.Degree is not { } degree)
        {
            return null;
        }

        int index = degree switch
        {
            >= 0 and <= 22.5 => 0,
            >= 22.5 and <= 67.5 => 1,
            >= 67.5 and <= 112.5 => 2,
            >= 112.5 and <= 157.5 => 3,
            >= 157.5 and <= 202.5 => 4,
            >= 202.5 and <= 247.5 => 5,
            >= 247.5 and <= 292.5 => 6,
            >= 292.5 and <= 337.5 => 7,
            >= 337.5 and <= 360 => 0,
            _ => 8,
        };
        int viewport = index == 8 ? 960 : 176;
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, viewport, viewport))));
        Brush brush = new SolidColorBrush(color);
        drawing.Children.Add(new GeometryDrawing(brush, null, Arrows[index]));
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }
}
