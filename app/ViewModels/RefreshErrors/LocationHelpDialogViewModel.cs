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
using LibreccioWeather.ViewModels.Locations;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels.RefreshErrors;

/// <summary>
/// Shown when the current position cannot be found: choose another location source, or add the place by hand.
/// </summary>
public sealed class LocationHelpDialogViewModel : ObservableObject
{
    public LocationHelpDialogViewModel(MainViewModel main)
    {
        Title = Strings.Get("location_message_failed_to_locate");
        CloseText = Strings.Get("action_close");
        CloseCommand = new RelayCommand(main.CloseDialog);
        Entries =
        [
            // The location settings are where the location source is chosen; they open on the settings screen that
            // lists them.
            new EditLocationEntry("IconFactory", Strings.Get("location_dialog_failed_to_locate_action_select_source"), "", () =>
            {
                main.CloseDialog();
                main.NavigateTo(SettingsPages.Root(main));
            }),
            new EditLocationEntry("IconList",
                Strings.Format("location_dialog_failed_to_locate_action_add_manually", Strings.Get("location_current")), "", () =>
                {
                    main.CloseDialog();
                    main.OpenLocationList();
                }),
        ];
    }

    public string Title { get; }

    public string CloseText { get; }

    public IReadOnlyList<EditLocationEntry> Entries { get; }

    public IRelayCommand CloseCommand { get; }
}
