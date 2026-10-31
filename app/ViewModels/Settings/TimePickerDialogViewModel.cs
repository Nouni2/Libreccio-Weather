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

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// Chooses a time of day by typing its hour and minute, with AM and PM when Windows uses a 12-hour clock.
/// </summary>
public sealed partial class TimePickerDialogViewModel : ObservableObject
{
    private readonly Action<int, int> _onConfirm;
    private readonly Action _onCancel;

    public TimePickerDialogViewModel(int hour, int minute, Action<int, int> onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        Is12Hour = DateFormatting.Is12Hour;
        Title = Strings.Get("dialog_time_picker_input_time");
        CancelText = SystemStrings.Cancel;
        ConfirmText = Strings.Get("action_confirm");
        AmText = CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
        PmText = CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
        IsPm = hour >= 12;
        int shownHour = Is12Hour ? (hour % 12 == 0 ? 12 : hour % 12) : hour;
        HourText = shownHour.ToString("00", CultureInfo.InvariantCulture);
        MinuteText = minute.ToString("00", CultureInfo.InvariantCulture);
    }

    public string Title { get; }

    public string CancelText { get; }

    public string ConfirmText { get; }

    public string AmText { get; }

    public string PmText { get; }

    public bool Is12Hour { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _hourText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _minuteText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAm))]
    private bool _isPm;

    public bool IsAm => !IsPm;

    [RelayCommand]
    private void SelectAm()
    {
        IsPm = false;
    }

    [RelayCommand]
    private void SelectPm()
    {
        IsPm = true;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (TryRead(out int hour, out int minute))
        {
            _onConfirm(hour, minute);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel();
    }

    private bool CanConfirm()
    {
        return TryRead(out _, out _);
    }

    // A 12-hour clock takes 1 to 12, a 24-hour clock 0 to 23, and minutes 0 to 59.
    private bool TryRead(out int hour, out int minute)
    {
        minute = 0;
        if (!int.TryParse(HourText, NumberStyles.None, CultureInfo.InvariantCulture, out hour)
            || !int.TryParse(MinuteText, NumberStyles.None, CultureInfo.InvariantCulture, out minute)
            || minute > 59)
        {
            return false;
        }

        if (Is12Hour)
        {
            if (hour is < 1 or > 12)
            {
                return false;
            }

            hour = (hour % 12) + (IsPm ? 12 : 0);
            return true;
        }

        return hour <= 23;
    }
}
