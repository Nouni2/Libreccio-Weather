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

namespace LibreccioWeather.ViewModels.Locations;

/// <summary>
/// A testing location: the source's name in bold over the place. A place already saved is shown faded and cannot be
/// chosen.
/// </summary>
public sealed class DebugLocationEntry(string sourceName, string place, bool isEnabled, Action onClick)
{
    public string SourceName { get; } = sourceName;

    public string Place { get; } = place;

    public bool IsEnabled { get; } = isEnabled;

    public IRelayCommand ChooseCommand { get; } = new RelayCommand(onClick, () => isEnabled);
}

/// <summary>
/// A source's name followed by its testing locations.
/// </summary>
public sealed class DebugLocationGroup(string name, IReadOnlyList<DebugLocationEntry> entries)
{
    public string Name { get; } = name;

    public IReadOnlyList<DebugLocationEntry> Entries { get; } = entries;
}

/// <summary>
/// The testing locations of every source that has some, offered in debug builds from the location list. Choosing one
/// closes the dialog and adds it; closing or dismissing adds nothing.
/// </summary>
public sealed class DebugLocationDialogViewModel : ObservableObject
{
    public DebugLocationDialogViewModel(MainViewModel main)
    {
        Title = Strings.Get("action_add_debug_location");
        CloseText = Strings.Get("action_close");
        CloseCommand = new RelayCommand(main.CloseDialog);
        Groups =
        [
            .. main.Sources.GetWeatherSources()
                .Where(source => source.TestingLocations.Count > 0)
                .Select(source => new DebugLocationGroup(source.Name, [
                    .. source.TestingLocations.Select(location => new DebugLocationEntry(
                        source.Name,
                        location.GetPlace(),
                        !main.LocationExists(location),
                        () =>
                        {
                            main.CloseDialog();
                            _ = main.AddWithMessageAsync(location);
                        })),
                ])),
        ];
    }

    public string Title { get; }

    public string CloseText { get; }

    public IReadOnlyList<DebugLocationGroup> Groups { get; }

    public IRelayCommand CloseCommand { get; }

    public IRelayCommand CancelCommand => CloseCommand;
}
