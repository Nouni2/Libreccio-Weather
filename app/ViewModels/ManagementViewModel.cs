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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;

namespace LibreccioWeather.ViewModels;

/// <summary>
/// The list of saved locations. A card opens its location on the main screen, sliding it towards the end deletes it
/// with an undo, and its handle drags it to another position.
/// </summary>
public sealed partial class ManagementViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly bool _isPane;
    private bool _desktopEditInProgress;

    /// <param name="isPane">True for the list shown beside the main screen, which has nothing to go back to.</param>
    public ManagementViewModel(MainViewModel main, bool isPane = false)
    {
        _main = main;
        _isPane = isPane;
        Refresh();
    }

    public ObservableCollection<LocationItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool _hasLocations;

    [ObservableProperty]
    private bool _canGoBack;

    /// <summary>
    /// Builds the cards again from the main view model's list.
    /// </summary>
    public void Refresh()
    {
        Items.Clear();
        foreach (Location location in _main.Locations)
        {
            Items.Add(new LocationItemViewModel(location, location.FormattedId == _main.CurrentFormattedId, _main.Settings));
        }

        HasLocations = Items.Count > 0;
        CanGoBack = !_isPane && _main.CanGoBack;
    }

    [RelayCommand]
    private void Back()
    {
        _main.GoBack();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _main.OpenSettings();
    }

    [RelayCommand]
    private void AddLocation()
    {
        _main.OpenSearch();
    }

    [RelayCommand]
    private void Open(LocationItemViewModel? item)
    {
        if (item is not null)
        {
            _main.ShowLocation(item.Location);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(LocationItemViewModel? item)
    {
        if (item is null || _desktopEditInProgress)
        {
            return;
        }

        _desktopEditInProgress = true;
        try
        {
            await DeleteAsync(item);
        }
        finally
        {
            _desktopEditInProgress = false;
        }
    }

    [RelayCommand]
    private Task MoveUpAsync(LocationItemViewModel? item)
    {
        return MoveFromButtonAsync(item, -1);
    }

    [RelayCommand]
    private Task MoveDownAsync(LocationItemViewModel? item)
    {
        return MoveFromButtonAsync(item, 1);
    }

    private async Task MoveFromButtonAsync(LocationItemViewModel? item, int direction)
    {
        if (item is null || _desktopEditInProgress)
        {
            return;
        }

        int from = Items.IndexOf(item);
        int to = from + direction;
        if (from >= 0 && to >= 0 && to < Items.Count)
        {
            _desktopEditInProgress = true;
            try
            {
                await EndDragAsync(from, to);
            }
            finally
            {
                _desktopEditInProgress = false;
            }
        }
    }

    /// <summary>
    /// Moves a card while it is dragged. The new order is saved once the drag ends.
    /// </summary>
    public void MoveWhileDragging(int from, int to)
    {
        if (from != to)
        {
            Items.Move(from, to);
        }
    }

    /// <summary>
    /// Saves the order after a drag, or puts the cards back when the list cannot change during a refresh.
    /// </summary>
    public async Task EndDragAsync(int from, int to)
    {
        if (_main.IsRefreshing)
        {
            _main.ShowSnackbar(Strings.Get("message_please_wait_refresh"));
            Refresh();
            return;
        }

        await _main.MoveLocationAsync(from, to);
        Refresh();
    }

    /// <summary>
    /// Deletes the location of a card slid towards the end, keeping at least one location.
    /// </summary>
    public async Task DeleteAsync(LocationItemViewModel item)
    {
        int index = Items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        if (Items.Count <= 1)
        {
            _main.ShowSnackbar(Strings.Get("location_message_list_cannot_be_empty"));
            return;
        }

        if (_main.IsRefreshing)
        {
            _main.ShowSnackbar(Strings.Get("message_please_wait_refresh"));
            return;
        }

        Location deleted = await _main.DeleteLocationAsync(index);
        Refresh();
        _main.ShowSnackbar(
            Strings.Get("location_message_deleted"),
            Strings.Get("action_undo"),
            () => _ = UndoDeleteAsync(deleted, index));
    }

    private async Task UndoDeleteAsync(Location location, int index)
    {
        await _main.AddLocationAsync(location, index);
        Refresh();
    }
}
