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
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// A section title in primary color above a group of preferences.
/// </summary>
public sealed partial class SectionHeaderPreference(string title) : ObservableObject
{
    public string Title { get; } = title;

    [ObservableProperty]
    private bool _isVisible = true;
}

/// <summary>
/// Space between two preferences: 2 pixels inside a group, 16 between groups.
/// </summary>
public sealed record SeparatorPreference(double Height)
{
    public static SeparatorPreference Small { get; } = new(2);

    public static SeparatorPreference Large { get; } = new(16);
}

/// <summary>
/// A preference drawn as a card. Cards in a group touch, with large outer corners on the first and the last.
/// </summary>
public abstract partial class CardPreference : ObservableObject
{
    private const double OuterCorner = 28;
    private const double InnerCorner = 2;

    protected CardPreference(string title, bool isFirst, bool isLast, bool isEnabled)
    {
        Title = title;
        IsEnabled = isEnabled;
        Corners = new CornerRadius(
            isFirst ? OuterCorner : InnerCorner,
            isFirst ? OuterCorner : InnerCorner,
            isLast ? OuterCorner : InnerCorner,
            isLast ? OuterCorner : InnerCorner);
    }

    public string Title { get; }

    public CornerRadius Corners { get; }

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private string? _summary;
}

/// <summary>
/// A preference that opens another screen or runs an action when clicked.
/// </summary>
public sealed class ClickablePreference : CardPreference
{
    public ClickablePreference(
        string title,
        string? summary,
        Action? onClick,
        string? iconKey = null,
        bool isFirst = false,
        bool isLast = false,
        bool isEnabled = true)
        : base(title, isFirst, isLast, isEnabled)
    {
        Summary = summary;
        IsClickable = onClick is not null;
        ClickCommand = new RelayCommand(() => onClick?.Invoke());
        if (iconKey is not null)
        {
            Icon = (Geometry)Application.Current.FindResource(iconKey);

            // Upstream icons come in two viewport sizes, which the view scales to the same size.
            IconViewport = Icon.Bounds.Right > 24 || Icon.Bounds.Bottom > 24 ? 960 : 24;
        }
    }

    public bool IsClickable { get; }

    public IRelayCommand ClickCommand { get; }

    public Geometry? Icon { get; }

    public double IconViewport { get; } = 24;
}

/// <summary>
/// A preference whose value is chosen from a list in a dialog. The summary is the name of the chosen value.
/// </summary>
public sealed class ListPreference : CardPreference
{
    private readonly MainViewModel _main;
    private readonly IReadOnlyList<string> _values;
    private readonly IReadOnlyList<string> _names;
    private readonly Action<string> _onValueChanged;
    private readonly bool _withState;
    private string _selectedKey;

    /// <param name="withState">False when the change waits for a confirmation, so the summary keeps the stored value.</param>
    public ListPreference(
        MainViewModel main,
        string title,
        IReadOnlyList<string> values,
        IReadOnlyList<string> names,
        string selectedKey,
        Action<string> onValueChanged,
        bool isFirst = false,
        bool isLast = false,
        bool withState = true)
        : base(title, isFirst, isLast, isEnabled: true)
    {
        _main = main;
        _values = values;
        _names = names;
        _selectedKey = selectedKey;
        _onValueChanged = onValueChanged;
        _withState = withState;
        OpenCommand = new RelayCommand(Open);
        UpdateSummary();
    }

    public IRelayCommand OpenCommand { get; }

    /// <summary>
    /// Marks a value as chosen after it was confirmed outside the dialog.
    /// </summary>
    public void Select(string value)
    {
        _selectedKey = value;
        UpdateSummary();
    }

    private void Open()
    {
        _main.ShowDialog(new ListDialogViewModel(Title, _values, _names, _selectedKey, value =>
        {
            _main.CloseDialog();
            if (_withState)
            {
                Select(value);
            }

            _onValueChanged(value);
        }, _main.CloseDialog));
    }

    // An unknown value shows the first name, as upstream does for values no longer offered.
    private void UpdateSummary()
    {
        int index = _values.ToList().IndexOf(_selectedKey);
        Summary = _names[index < 0 ? 0 : index];
    }
}

/// <summary>
/// A preference turned on or off with a switch. The summary says whether it is on.
/// </summary>
public sealed partial class SwitchPreference : CardPreference
{
    private readonly string _summaryOn;
    private readonly string _summaryOff;
    private readonly Action<bool> _onValueChanged;

    // The stored value is shown without being written back.
    private readonly bool _initialized;

    public SwitchPreference(
        string title,
        string summaryOn,
        string summaryOff,
        bool isChecked,
        Action<bool> onValueChanged,
        bool isEnabled = true,
        bool isFirst = false,
        bool isLast = false)
        : base(title, isFirst, isLast, isEnabled)
    {
        _summaryOn = summaryOn;
        _summaryOff = summaryOff;
        _onValueChanged = onValueChanged;
        IsChecked = isChecked;
        Summary = isChecked ? summaryOn : summaryOff;
        _initialized = true;
    }

    [ObservableProperty]
    private bool _isChecked;

    // Clicking anywhere on the card flips the switch, as on Android.
    [RelayCommand]
    private void Toggle()
    {
        if (IsEnabled)
        {
            IsChecked = !IsChecked;
        }
    }

    partial void OnIsCheckedChanged(bool value)
    {
        Summary = value ? _summaryOn : _summaryOff;
        if (_initialized)
        {
            _onValueChanged(value);
        }
    }
}

/// <summary>
/// A whole number chosen with a slider or typed in a field next to the title. The slider covers a narrower range than
/// the field, so a rare extreme value does not squeeze the usual ones together; a value outside it puts the handle at
/// the nearest end. A stop indicator marks the recommended value, which the reset button restores.
/// </summary>
public sealed partial class SliderPreference : CardPreference
{
    private readonly Action<int> _onValueChanged;
    private readonly Action _onReset;

    // The stored value is shown without being written back, and a reset is stored by its own action.
    private bool _writesValue;

    public SliderPreference(
        string title,
        string unitText,
        int value,
        int minimum,
        int maximum,
        int sliderMinimum,
        int sliderMaximum,
        int recommended,
        Action<int> onValueChanged,
        Action onReset,
        bool isEnabled = true,
        bool isFirst = false,
        bool isLast = false)
        : base(title, isFirst, isLast, isEnabled)
    {
        UnitText = unitText;
        Minimum = minimum;
        Maximum = maximum;
        SliderMinimum = sliderMinimum;
        SliderMaximum = sliderMaximum;
        Recommended = recommended;
        _onValueChanged = onValueChanged;
        _onReset = onReset;
        Value = value;
        _writesValue = true;
    }

    public string UnitText { get; }

    public int Minimum { get; }

    public int Maximum { get; }

    public int SliderMinimum { get; }

    public int SliderMaximum { get; }

    public int Recommended { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderValue))]
    private int _value;

    [ObservableProperty]
    private string _valueText = "";

    /// <summary>
    /// The value as the slider shows it, held at the nearest end of the slider's range.
    /// </summary>
    public double SliderValue
    {
        get => Math.Clamp(Value, SliderMinimum, SliderMaximum);
        set
        {
            int moved = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (moved != Math.Clamp(Value, SliderMinimum, SliderMaximum))
            {
                Value = Math.Clamp(moved, SliderMinimum, SliderMaximum);
            }
        }
    }

    /// <summary>
    /// Applies the typed text, clamped to the allowed range. Text that is not a number puts the current value back.
    /// </summary>
    [RelayCommand]
    private void CommitText(string? text)
    {
        if (int.TryParse(text, NumberStyles.Integer | NumberStyles.AllowThousands, Strings.Culture, out int typed))
        {
            Value = Math.Clamp(typed, Minimum, Maximum);
        }

        ValueText = Value.ToString(Strings.Culture);
    }

    [RelayCommand]
    private void Reset()
    {
        _writesValue = false;
        Value = Recommended;
        _writesValue = true;
        _onReset();
    }

    partial void OnValueChanged(int value)
    {
        ValueText = value.ToString(Strings.Culture);
        if (_writesValue)
        {
            _onValueChanged(value);
        }
    }

    // The field's binding updates when it loses focus, which commits what was typed.
    partial void OnValueTextChanged(string value)
    {
        if (_writesValue)
        {
            CommitText(value);
        }
    }
}
