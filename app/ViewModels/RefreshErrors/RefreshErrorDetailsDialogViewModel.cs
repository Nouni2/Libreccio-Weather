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
using LibreccioWeather.Core.Refresh;

namespace LibreccioWeather.ViewModels.RefreshErrors;

/// <summary>
/// One error of a refresh: its source and feature as the title with the message below, or the message alone without a
/// source. An error with help shows a help icon and opens its help when clicked.
/// </summary>
public sealed class RefreshErrorItem
{
    public RefreshErrorItem(RefreshError error, MainViewModel main, ObservableObject list)
    {
        string message = RefreshErrorHelp.GetShortMessage(error.Error);
        string? source = RefreshErrorHelp.GetSourceWithOptionalFeature(error, main.Sources);
        Headline = source ?? message;
        Supporting = source is null ? null : message;
        HasHelp = RefreshErrorHelp.GetActionButtonText(error.Error) is not null;
        HelpText = Strings.Get("action_help");
        ClickCommand = new RelayCommand(() => RefreshErrorHelp.ShowDialog(error.Error, main, list), () => HasHelp);
    }

    public string Headline { get; }

    public string? Supporting { get; }

    public bool HasSupporting => Supporting is not null;

    public bool HasHelp { get; }

    public string HelpText { get; }

    public IRelayCommand ClickCommand { get; }
}

/// <summary>
/// Every error of the last refresh, shown from the message that announced several of them. It only closes with Close:
/// it has no cancel command, so a click outside it and Escape do nothing.
/// </summary>
public sealed class RefreshErrorDetailsDialogViewModel : ObservableObject
{
    public RefreshErrorDetailsDialogViewModel(MainViewModel main, IReadOnlyList<RefreshError> errors)
    {
        Title = Strings.Get("dialog_refresh_error_details_title");
        Content = Strings.Get("dialog_refresh_error_details_content");
        CloseText = Strings.Get("action_close");
        CloseCommand = new RelayCommand(main.CloseDialog);
        Items = [.. errors.Select(error => new RefreshErrorItem(error, main, this))];
    }

    public string Title { get; }

    public string Content { get; }

    public string CloseText { get; }

    public IReadOnlyList<RefreshErrorItem> Items { get; }

    public IRelayCommand CloseCommand { get; }
}
