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

using CommunityToolkit.Mvvm.Input;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// A short message at the bottom of the window, with an optional action such as Undo.
/// </summary>
public sealed class SnackbarViewModel
{
    public SnackbarViewModel(string text, string? actionText, Action? action)
    {
        Text = text;
        ActionText = action is null ? null : actionText;
        ActionCommand = new RelayCommand(() => action?.Invoke());
    }

    public string Text { get; }

    public string? ActionText { get; }

    public IRelayCommand ActionCommand { get; }
}
