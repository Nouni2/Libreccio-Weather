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

namespace LibreccioWeather.ViewModels;

public sealed class AlreadyRunningDialogViewModel : ObservableObject
{
    public AlreadyRunningDialogViewModel(Action close)
    {
        CancelCommand = new RelayCommand(close);
    }

    public string Title { get; } = Strings.Get("desktop_app_name");

    public string Message { get; } = Strings.Get("app_already_running");

    public string CloseText { get; } = Strings.Get("action_close");

    public IRelayCommand CancelCommand { get; }
}
