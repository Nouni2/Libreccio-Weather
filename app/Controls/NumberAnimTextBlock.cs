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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using LibreccioWeather.Display;

namespace LibreccioWeather.Controls;

/// <summary>
/// Text that counts from one number to another, such as a temperature rising from the previous value to the new one,
/// once <see cref="Start"/> is called. The numbers in between keep as many decimals as the longer of the two, rounded
/// half to even and grouped by thousands in the computer's language; the last one is shown exactly as given. Text
/// that is not a plain number is shown at once.
/// </summary>
public sealed partial class NumberAnimTextBlock : TextBlock
{
    public static readonly DependencyProperty NumberStartProperty = DependencyProperty.Register(
        nameof(NumberStart), typeof(string), typeof(NumberAnimTextBlock), new PropertyMetadata("0"));

    public static readonly DependencyProperty NumberEndProperty = DependencyProperty.Register(
        nameof(NumberEnd), typeof(string), typeof(NumberAnimTextBlock), new PropertyMetadata(null, OnNumberChanged));

    public static readonly DependencyProperty IsAnimEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimEnabled), typeof(bool), typeof(NumberAnimTextBlock), new PropertyMetadata(true));

    private static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        "Progress", typeof(double), typeof(NumberAnimTextBlock), new PropertyMetadata(0.0, OnProgressChanged));

    private decimal _start;
    private decimal _end;
    private bool _animating;
    private bool _started;
    private bool _isInt;
    private int _decimals;

    public string NumberStart
    {
        get => (string)GetValue(NumberStartProperty);
        set => SetValue(NumberStartProperty, value);
    }

    public string? NumberEnd
    {
        get => (string?)GetValue(NumberEndProperty);
        set => SetValue(NumberEndProperty, value);
    }

    public bool IsAnimEnabled
    {
        get => (bool)GetValue(IsAnimEnabledProperty);
        set => SetValue(IsAnimEnabledProperty, value);
    }

    // ASCII digits only: a number written with other digits is shown without counting.
    [GeneratedRegex("^-?[0-9]*$")]
    private static partial Regex IntegerRegex();

    // The separator matches any single character; a number whose separator is not a point cannot be read and is
    // then shown as it is.
    [GeneratedRegex("^(-?[1-9][0-9]*.[0-9]*|-?0.[0-9]*[1-9][0-9]*)$")]
    private static partial Regex DecimalRegex();

    private static void OnNumberChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var text = (NumberAnimTextBlock)sender;
        if (text._started)
        {
            text.Start();
        }
        else
        {
            text.Text = text.NumberEnd ?? "";
        }
    }

    private static void OnProgressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var text = (NumberAnimTextBlock)sender;
        if (text._animating)
        {
            decimal value = ((text._end - text._start) * (decimal)(double)e.NewValue) + text._start;
            text.Text = text.Format(value);
        }
    }

    /// <summary>
    /// Counts from the start number to the end number, and again from the start whenever the end number changes.
    /// </summary>
    public void Start()
    {
        _started = true;
        BeginAnimation(ProgressProperty, null);
        _animating = false;
        string end = NumberEnd ?? "";
        string start = NumberStart;
        if (!CheckNumString(start, end)
            || !TryParse(start, out _start)
            || !TryParse(end, out _end))
        {
            Text = end;
            return;
        }

        _decimals = _isInt ? 0 : FractionLength(start, end);
        if (!IsAnimEnabled)
        {
            Text = Format(_end);
            return;
        }

        // At least 1 second, at most 2, and 20 ms per unit in between.
        double milliseconds = Math.Clamp((double)Math.Abs(_end - _start) * 20.0, 1000.0, 2000.0);
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(milliseconds))
        {
            // Slows down towards the end, as 1 − (1 − t)⁶.
            EasingFunction = new PowerEase { Power = 6, EasingMode = EasingMode.EaseOut },
        };
        animation.Completed += (_, _) =>
        {
            _animating = false;
            Text = end;
        };
        RefreshRate.Apply(animation, this);
        _animating = true;
        Text = Format(_start);
        BeginAnimation(ProgressProperty, animation);
    }

    // Two whole numbers, a decimal from 0, or two decimals.
    private bool CheckNumString(string start, string end)
    {
        _isInt = IntegerRegex().IsMatch(end) && IntegerRegex().IsMatch(start);
        if (_isInt)
        {
            return true;
        }

        if (start == "0" && DecimalRegex().IsMatch(end))
        {
            return true;
        }

        return DecimalRegex().IsMatch(end) && DecimalRegex().IsMatch(start);
    }

    // The decimals of the number split into more parts, or of the end number when both split alike. Empty parts at
    // the end do not count, so "2." has no decimals.
    private static int FractionLength(string start, string end)
    {
        string[] startParts = SplitDropLastEmpty(start);
        string[] endParts = SplitDropLastEmpty(end);
        string[] parts = startParts.Length > endParts.Length ? startParts : endParts;
        return parts.Length > 1 ? parts[1].Length : 0;
    }

    private static string[] SplitDropLastEmpty(string number)
    {
        string[] parts = number.Split('.');
        int count = parts.Length;
        while (count > 0 && parts[count - 1].Length == 0)
        {
            count--;
        }

        return parts[..count];
    }

    // An empty number or another separator than a point cannot be read, and is then shown as it is.
    private static bool TryParse(string number, out decimal value)
    {
        return decimal.TryParse(
            number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    private string Format(decimal value)
    {
        string pattern = _decimals > 0 ? "#,##0." + new string('0', _decimals) : "#,##0";
        return Math.Round(value, _decimals, MidpointRounding.ToEven).ToString(pattern, CultureInfo.CurrentCulture);
    }
}
