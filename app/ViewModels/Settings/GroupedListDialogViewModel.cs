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
/// A title above a group of choices in a <see cref="GroupedListDialogViewModel"/>.
/// </summary>
public sealed record ListDialogGroupHeader(string Title);

/// <summary>
/// A choice of a grouped list, which can be shown but not chosen.
/// </summary>
public sealed record GroupedListDialogOption(string Name, bool IsSelected, bool IsEnabled, IRelayCommand ChooseCommand);

/// <summary>
/// A dialog listing values as radio buttons in titled groups; a group without a title comes first. Choosing one
/// closes it.
/// </summary>
public sealed class GroupedListDialogViewModel : ObservableObject
{
    public GroupedListDialogViewModel(
        string title,
        IReadOnlyList<(string? Group, IReadOnlyList<(string Value, string Name, bool IsEnabled)> Options)> groups,
        string selectedKey,
        Action<string> onChosen,
        Action onCancel)
    {
        Title = title;
        List<object> items = [];
        foreach ((string? group, IReadOnlyList<(string Value, string Name, bool IsEnabled)> options) in groups)
        {
            if (group is not null)
            {
                items.Add(new ListDialogGroupHeader(group));
            }

            foreach ((string value, string name, bool isEnabled) in options)
            {
                items.Add(new GroupedListDialogOption(
                    name, value == selectedKey, isEnabled, new RelayCommand(() => onChosen(value), () => isEnabled)));
            }
        }

        Items = items;
        CancelCommand = new RelayCommand(onCancel);
    }

    public string Title { get; }

    public string CancelText { get; } = SystemStrings.Cancel;

    public IReadOnlyList<object> Items { get; }

    public IRelayCommand CancelCommand { get; }
}
