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
using LibreccioWeather.Icons;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.Details;

/// <summary>
/// An icon of Icons.xaml named by its key, scaled from its viewport to the size of the element.
/// </summary>
public sealed class DetailsIcon : FrameworkElement
{
    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        nameof(Key), typeof(string), typeof(DetailsIcon),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
        nameof(Foreground), typeof(Brush), typeof(DetailsIcon),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public DetailsIcon()
    {
        Width = 24;
        Height = 24;
    }

    public string? Key
    {
        get => (string?)GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Width, Height);

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (Key is not { } key || TryFindResource(key) is not Geometry geometry)
        {
            return;
        }

        double viewport = DetailsIcons.GetViewport(key);
        drawingContext.PushTransform(new ScaleTransform(ActualWidth / viewport, ActualHeight / viewport));
        drawingContext.DrawGeometry(Foreground, null, geometry);
        drawingContext.Pop();
    }
}
