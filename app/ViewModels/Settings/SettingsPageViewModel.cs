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

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// A settings screen: a top bar with its title and a back button, above a list of preferences.
/// </summary>
public sealed class SettingsPageViewModel : ObservableObject
{
    private readonly Func<SettingsPageViewModel> _rebuild;

    public SettingsPageViewModel(string title, IReadOnlyList<object> items, Action onBack, Func<SettingsPageViewModel> rebuild)
    {
        Title = title;
        Items = items;
        BackCommand = new RelayCommand(onBack);
        _rebuild = rebuild;
    }

    public string Title { get; }

    public IReadOnlyList<object> Items { get; }

    public IRelayCommand BackCommand { get; }

    /// <summary>
    /// The same screen built again, with text in the current language.
    /// </summary>
    public SettingsPageViewModel Rebuild()
    {
        return _rebuild();
    }
}
