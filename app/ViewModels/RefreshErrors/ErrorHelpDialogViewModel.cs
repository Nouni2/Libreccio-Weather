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

namespace LibreccioWeather.ViewModels.RefreshErrors;

/// <summary>
/// Explains a refresh error, with Close and, for some errors, a button that leads to where it can be solved.
/// </summary>
public sealed class ErrorHelpDialogViewModel(string title, string content, string? actionText, Action? onAction, Action onClose)
    : ObservableObject
{
    public string Title { get; } = title;

    public string Content { get; } = content;

    public string CloseText { get; } = Strings.Get("action_close");

    public string? ActionText { get; } = onAction is null ? null : actionText;

    public bool HasAction => ActionText is not null;

    public IRelayCommand CloseCommand { get; } = new RelayCommand(onClose);

    public IRelayCommand ActionCommand { get; } = new RelayCommand(() => onAction?.Invoke());
}
