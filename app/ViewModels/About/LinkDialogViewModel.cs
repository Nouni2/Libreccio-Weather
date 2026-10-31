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
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.About;

/// <summary>
/// Asks before opening a link outside the app, showing its address.
/// </summary>
public sealed class LinkDialogViewModel(string linkToOpen, Action onConfirm, Action onClose) : ObservableObject
{
    public string Message { get; } = Strings.Format("about_open_link_message", linkToOpen);

    public string CancelText { get; } = SystemStrings.Cancel;

    public string ConfirmText { get; } = Strings.Get("action_confirm");

    public IRelayCommand ConfirmCommand { get; } = new RelayCommand(onConfirm);

    public IRelayCommand CancelCommand { get; } = new RelayCommand(onClose);
}
