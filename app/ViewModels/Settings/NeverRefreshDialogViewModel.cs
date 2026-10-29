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

using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// The warning shown before turning background updates off. Continue only becomes available after a countdown,
/// so the warning is read.
/// </summary>
public sealed partial class NeverRefreshDialogViewModel : ObservableObject
{
    private const int CountdownSeconds = 10;

    // The monthly data use, in megabytes, that the warning says background updates stay under.
    private const int MonthlyDataMegabytes = 5;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Action _onContinue;
    private readonly Action _onCancel;
    private int _timeLeft = CountdownSeconds;

    public NeverRefreshDialogViewModel(Action onContinue, Action onCancel)
    {
        _onContinue = onContinue;
        _onCancel = onCancel;
        Warning1 = Strings.Get("settings_background_updates_refresh_never_warning1");
        Warning2 = Strings.Format("settings_background_updates_refresh_never_warning2", MonthlyDataMegabytes);
        Warning3 = Strings.Get("settings_background_updates_refresh_never_warning3");
        CancelText = SystemStrings.Cancel;
        UpdateContinue();
        _timer.Tick += (_, _) =>
        {
            _timeLeft--;
            if (_timeLeft <= 0)
            {
                _timer.Stop();
            }

            UpdateContinue();
        };
        _timer.Start();
    }

    public string Warning1 { get; }

    public string Warning2 { get; }

    public string Warning3 { get; }

    public string CancelText { get; }

    [ObservableProperty]
    private string _continueText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private bool _canContinue;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue()
    {
        _onContinue();
    }

    [RelayCommand]
    private void Cancel()
    {
        _timer.Stop();
        _onCancel();
    }

    private void UpdateContinue()
    {
        string action = Strings.Get("action_continue");
        CanContinue = _timeLeft <= 0;
        ContinueText = CanContinue
            ? action
            : Strings.Format("parenthesis", action, NumberFormatting.Format(_timeLeft, 0, Strings.Culture));
    }
}
