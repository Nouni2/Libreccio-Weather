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

namespace LibreccioWeather.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
        Loaded += (_, _) => SearchInput.Focus();
        SearchInput.SizeChanged += (_, _) => AlignPlaceholder();

        // The box lays out its text after it is loaded, so its first character's position is read once that is done.
        SearchInput.Loaded += (_, _) => Dispatcher.InvokeAsync(AlignPlaceholder, DispatcherPriority.Loaded);
    }

    private void AlignPlaceholder()
    {
        Rect first = SearchInput.GetRectFromCharacterIndex(0);
        if (!first.IsEmpty)
        {
            Placeholder.Margin = new Thickness(first.Left, 0, 0, 0);
        }
    }
}
