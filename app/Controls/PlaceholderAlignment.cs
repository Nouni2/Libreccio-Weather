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
using System.Windows.Threading;

namespace LibreccioWeather.Controls;

/// <summary>
/// Starts a hint laid over an empty text box where the box puts its first character, so the caret and the typed text
/// start where the hint does. Besides its padding, the box insets its text by a small margin of its own, which a fixed
/// margin on the hint misses. The hint shares the box's top-left corner.
/// </summary>
public static class PlaceholderAlignment
{
    public static readonly DependencyProperty FieldProperty = DependencyProperty.RegisterAttached(
        "Field", typeof(TextBox), typeof(PlaceholderAlignment), new PropertyMetadata(null, OnFieldChanged));

    private static readonly DependencyProperty AlignerProperty = DependencyProperty.RegisterAttached(
        "Aligner", typeof(Aligner), typeof(PlaceholderAlignment), new PropertyMetadata(null));

    public static TextBox? GetField(DependencyObject element) => (TextBox?)element.GetValue(FieldProperty);

    public static void SetField(DependencyObject element, TextBox? value) => element.SetValue(FieldProperty, value);

    private static void OnFieldChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        (element.GetValue(AlignerProperty) as Aligner)?.Detach();
        element.ClearValue(AlignerProperty);
        if (element is FrameworkElement hint && e.NewValue is TextBox field)
        {
            var aligner = new Aligner(hint, field);
            element.SetValue(AlignerProperty, aligner);
            aligner.Attach();
        }
    }

    private sealed class Aligner(FrameworkElement hint, TextBox field)
    {
        public void Attach()
        {
            field.SizeChanged += OnSizeChanged;
            field.Loaded += OnLoaded;
            if (field.IsLoaded)
            {
                QueueAlign();
            }
        }

        public void Detach()
        {
            field.SizeChanged -= OnSizeChanged;
            field.Loaded -= OnLoaded;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Align();

        // The box lays out its text after it is loaded, so its first character's position is read once that is done.
        private void OnLoaded(object sender, RoutedEventArgs e) => QueueAlign();

        private void QueueAlign() => _ = field.Dispatcher.InvokeAsync(Align, DispatcherPriority.Loaded);

        private void Align()
        {
            Rect first = field.GetRectFromCharacterIndex(0);
            if (!first.IsEmpty)
            {
                Thickness margin = hint.Margin;
                hint.Margin = new Thickness(first.Left, margin.Top, margin.Right, margin.Bottom);
            }
        }
    }
}
