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
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// A dialog listing the values of a preference as radio buttons. Choosing one closes it.
/// </summary>
public sealed class ListDialogViewModel : ObservableObject
{
    public ListDialogViewModel(
        string title,
        IReadOnlyList<string> values,
        IReadOnlyList<string> names,
        string selectedKey,
        Action<string> onChosen,
        Action onCancel)
    {
        Title = title;
        Options = values
            .Zip(names)
            .Select(pair => new ListDialogOption(pair.Second, pair.First == selectedKey, new RelayCommand(() => onChosen(pair.First))))
            .ToList();
        CancelCommand = new RelayCommand(onCancel);
    }

    public string Title { get; }

    public string CancelText { get; } = SystemStrings.Cancel;

    public IReadOnlyList<ListDialogOption> Options { get; }

    public IRelayCommand CancelCommand { get; }
}

public sealed record ListDialogOption(string Name, bool IsSelected, IRelayCommand ChooseCommand);
