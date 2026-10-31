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

using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Localization;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// Edits a line of text, checked against a pattern when one is given.
/// </summary>
public sealed partial class EditTextDialogViewModel : ObservableObject
{
    private readonly Regex? _regex;
    private readonly Action<string> _onDone;
    private readonly Action _onCancel;

    public EditTextDialogViewModel(
        string title,
        string content,
        string? placeholder,
        Regex? regex,
        string? regexError,
        Action<string> onDone,
        Action onCancel)
    {
        Title = title;
        Placeholder = placeholder;
        _regex = regex;
        ErrorText = string.IsNullOrEmpty(regexError) ? Strings.Get("settings_invalid_syntax") : regexError;
        _onDone = onDone;
        _onCancel = onCancel;
        DoneText = Strings.Get("action_done");
        CancelText = SystemStrings.Cancel;
        Input = content;
    }

    public string Title { get; }

    public string? Placeholder { get; }

    public string ErrorText { get; }

    public string DoneText { get; }

    public string CancelText { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsError), nameof(HasPlaceholder))]
    [NotifyCanExecuteChangedFor(nameof(DoneCommand))]
    private string _input = "";

    public bool HasPlaceholder => !string.IsNullOrEmpty(Placeholder) && Input.Length == 0;

    /// <summary>
    /// Whether the text does not match. With a placeholder, an empty text is allowed.
    /// </summary>
    public bool IsError => _regex is not null && !Matches(_regex, Input) && (string.IsNullOrEmpty(Placeholder) || Input.Length > 0);

    [RelayCommand(CanExecute = nameof(CanDone))]
    private void Done()
    {
        _onDone(Input);
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel();
    }

    private bool CanDone()
    {
        return _regex is null || Input.Length == 0 || Matches(_regex, Input);
    }

    // The whole text must match: the pattern's "$" alone would also accept a final line break.
    private static bool Matches(Regex regex, string text)
    {
        Match match = regex.Match(text);
        return match.Success && match.Index == 0 && match.Length == text.Length;
    }
}
