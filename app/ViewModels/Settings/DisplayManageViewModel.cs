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
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// An entry of an order editor: a card or a chart, with the editor it opens for its own settings, if any.
/// </summary>
public sealed class DisplayItem
{
    public DisplayItem(object value, string name, Action? configure)
    {
        Value = value;
        Name = name;
        IsConfigurable = configure is not null;
        ConfigureDescription = Strings.Format("action_configure_item", name);
        ConfigureCommand = new RelayCommand(() => configure?.Invoke());
    }

    public object Value { get; }

    public string Name { get; }

    public bool IsConfigurable { get; }

    public string ConfigureDescription { get; }

    public IRelayCommand ConfigureCommand { get; }
}

/// <summary>
/// Chooses which cards or charts are shown and in which order. The shown entries are a list that is reordered with
/// their handles and loses an entry to its close button; the others are chips below it that add themselves back at
/// the end. The list is saved when the screen is left.
/// </summary>
public sealed partial class DisplayManageViewModel : ObservableObject
{
    private readonly IReadOnlyList<DisplayItem> _entries;
    private readonly IReadOnlyList<object> _defaults;
    private readonly Func<IReadOnlyList<object>> _read;
    private readonly Action<IReadOnlyList<object>> _write;

    private DisplayManageViewModel(
        MainViewModel main,
        string title,
        IReadOnlyList<DisplayItem> entries,
        IReadOnlyList<object> defaults,
        Func<IReadOnlyList<object>> read,
        Action<IReadOnlyList<object>> write)
    {
        Title = title;
        _entries = entries;
        _defaults = defaults;
        _read = read;
        _write = write;
        BackCommand = new RelayCommand(main.GoBack);
        Fill(read());
    }

    public string Title { get; }

    public IRelayCommand BackCommand { get; }

    public ObservableCollection<DisplayItem> DisplayedItems { get; } = [];

    public ObservableCollection<DisplayItem> OtherItems { get; } = [];

    /// <summary>
    /// The bar of chips is shown only while some entry is left out.
    /// </summary>
    [ObservableProperty]
    private bool _hasOtherItems;

    /// <param name="onSaved">Called after a changed list is saved, so the screen that opened the editor can follow.</param>
    public static DisplayManageViewModel CardDisplayManage(MainViewModel main, Action onSaved)
    {
        SettingsManager settings = main.Settings;
        List<DisplayItem> entries = CardDisplay.Entries
            .Select(card => new DisplayItem(card, card.Name, ConfigureCard(main, card, onSaved)))
            .ToList();
        return new DisplayManageViewModel(
            main,
            Strings.Get("settings_main_cards_title"),
            entries,
            CardDisplay.ToCardDisplayList(SettingsManager.DefaultCardDisplay),
            () => settings.CardDisplayList,
            list =>
            {
                settings.CardDisplayList = list.Cast<CardDisplay>().ToList();

                // The card list does not raise SettingsChanged, so the main screen does not reload while cards are
                // dragged on it; the change is announced here instead.
                main.RebuildMainScreen();
                onSaved();
            });
    }

    public static DisplayManageViewModel DailyTrendDisplayManage(MainViewModel main, Action onSaved)
    {
        SettingsManager settings = main.Settings;
        List<DisplayItem> entries = DailyTrendDisplay.Entries
            .Select(chart => new DisplayItem(chart, chart.Name, null))
            .ToList();
        return new DisplayManageViewModel(
            main,
            Strings.Get("settings_main_daily_trends_title"),
            entries,
            DailyTrendDisplay.ToDailyTrendDisplayList(SettingsManager.DefaultDailyTrendDisplay),
            () => settings.DailyTrendDisplayList,
            list =>
            {
                settings.DailyTrendDisplayList = list.Cast<DailyTrendDisplay>().ToList();
                onSaved();
            });
    }

    public static DisplayManageViewModel HourlyTrendDisplayManage(MainViewModel main, Action onSaved)
    {
        SettingsManager settings = main.Settings;
        List<DisplayItem> entries = HourlyTrendDisplay.Entries
            .Select(chart => new DisplayItem(chart, chart.Name, null))
            .ToList();
        return new DisplayManageViewModel(
            main,
            Strings.Get("settings_main_hourly_trends_title"),
            entries,
            HourlyTrendDisplay.ToHourlyTrendDisplayList(SettingsManager.DefaultHourlyTrendDisplay),
            () => settings.HourlyTrendDisplayList,
            list =>
            {
                settings.HourlyTrendDisplayList = list.Cast<HourlyTrendDisplay>().ToList();
                onSaved();
            });
    }

    // The daily and hourly forecast cards open the editor of their charts.
    private static Action? ConfigureCard(MainViewModel main, CardDisplay card, Action onSaved)
    {
        if (card == CardDisplay.DailyForecast)
        {
            return () => main.NavigateTo(DailyTrendDisplayManage(main, onSaved));
        }

        if (card == CardDisplay.HourlyForecast)
        {
            return () => main.NavigateTo(HourlyTrendDisplayManage(main, onSaved));
        }

        return null;
    }

    /// <summary>
    /// Saves the list if it differs from the stored one.
    /// </summary>
    public void Stop()
    {
        List<object> newList = DisplayedItems.Select(item => item.Value).ToList();
        if (!_read().SequenceEqual(newList))
        {
            _write(newList);
        }
    }

    /// <summary>
    /// Moves a shown entry while its handle is dragged.
    /// </summary>
    public void MoveItem(int fromPosition, int toPosition)
    {
        DisplayedItems.Move(fromPosition, toPosition);
    }

    [RelayCommand]
    private void RemoveItem(DisplayItem item)
    {
        if (DisplayedItems.Remove(item))
        {
            OtherItems.Add(item);
            ResetBottomBarVisibility();
        }
    }

    [RelayCommand]
    private void InsertItem(DisplayItem item)
    {
        if (OtherItems.Remove(item))
        {
            DisplayedItems.Add(item);
            ResetBottomBarVisibility();
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        Fill(_defaults);
    }

    // The shown entries in the given order, and every other entry as a chip in the order of the entries.
    private void Fill(IReadOnlyList<object> displayed)
    {
        DisplayedItems.Clear();
        foreach (object value in displayed)
        {
            if (_entries.FirstOrDefault(entry => entry.Value == value) is { } item)
            {
                DisplayedItems.Add(item);
            }
        }

        OtherItems.Clear();
        foreach (DisplayItem entry in _entries)
        {
            if (!displayed.Contains(entry.Value))
            {
                OtherItems.Add(entry);
            }
        }

        ResetBottomBarVisibility();
    }

    private void ResetBottomBarVisibility()
    {
        HasOtherItems = OtherItems.Count != 0;
    }
}
