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

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Details;

public enum DetailsRunStyle
{
    Normal,

    // A unit or level after a large value, at the size of a small headline.
    Smaller,

    // A level after a large value: smaller, in the variant text color.
    SmallerVariant,

    // The digits of a chemical formula, such as the 2 of NO2.
    Subscript,
}

public sealed record DetailsTextRun(string Text, DetailsRunStyle Style = DetailsRunStyle.Normal);

/// <summary>
/// A short label, with an optional explanation behind a help icon.
/// </summary>
public sealed record DetailsLabel(string Text, string? Help = null);

public sealed record DetailsSpacer(double Height);

public sealed record DetailsDivider;

public sealed record DetailsHeadline(string Text);

public sealed record DetailsBodySmall(string Text);

/// <summary>
/// A message in italics, shown in place of data that a day does not have.
/// </summary>
public sealed record DetailsMessage(string Text);

public sealed record DetailsSectionHeader(IReadOnlyList<DetailsTextRun> Title, IReadOnlyList<DetailsTextRun>? Subtitle = null);

/// <summary>
/// One or two paragraphs on a card.
/// </summary>
public sealed record DetailsCardText(string Text1, string? Text2 = null);

public sealed record DetailsUnavailableChart(string Text);

/// <summary>
/// A labelled value at the top of a section, such as the day's temperature or the value at the pressed hour.
/// </summary>
public sealed record DetailsHeaderItem(
    DetailsLabel? Label,
    IReadOnlyList<DetailsTextRun> Value,
    string? ValueName = null,
    DetailsLabel? Subtext = null,
    bool SubtextIsVariant = false,
    Color? Dot = null,
    string? WeatherIcon = null,
    string? WeatherText = null,
    bool ShowWeather = false,
    DetailsLabel? Description = null);

/// <summary>
/// The daytime and nighttime halves side by side. A missing half leaves its column empty.
/// </summary>
public sealed record DetailsHeaderRow(DetailsHeaderItem? Day, DetailsHeaderItem? Night);

/// <summary>
/// The header of a section, which changes to the hour pressed on its chart.
/// </summary>
public sealed partial class DetailsHeader : ObservableObject
{
    [ObservableProperty]
    private object? _content;

    public DetailsHeader(object? content)
    {
        Content = content;
    }
}

/// <summary>
/// An item with a bold headline, as in a list of precipitation types.
/// </summary>
public sealed record DetailsItem(
    string Headline, string? Supporting, string? IconKey = null, string? Help = null, string? AutomationName = null);

/// <summary>
/// Items for the daytime and nighttime halves, under their labels.
/// </summary>
public sealed record DetailsHalfDayColumns(IReadOnlyList<object> Day, IReadOnlyList<object> Night);

/// <summary>
/// A large value such as a duration, with smaller units.
/// </summary>
public sealed record DetailsLargeValue(IReadOnlyList<DetailsTextRun> Value, string? AutomationName = null);

public sealed record DetailsToggle(IReadOnlyList<DetailsTextRun> Text, bool IsChecked, ICommand Command);

/// <summary>
/// Connected toggle buttons, of which one is checked.
/// </summary>
public sealed record DetailsToggleGroup(IReadOnlyList<DetailsToggle> Toggles, bool FillWidth);

public sealed record DetailsScaleCell(string Text, Color? Dot = null);

/// <summary>
/// A table of the levels of a scale. Each column takes a share of the width given by its weight, and the last column is
/// aligned to the end.
/// </summary>
public sealed record DetailsScale(
    IReadOnlyList<double> Weights, IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<DetailsScaleCell>> Rows);

public sealed record DetailsPollutantItem(
    IReadOnlyList<DetailsTextRun> Title, Color Color, double Progress, string Content, string AutomationName);

public sealed record DetailsPollutantGrid(IReadOnlyList<DetailsPollutantItem> Items);

public sealed record DetailsPollenItem(string Title, string Subtitle, Color Color);

public sealed record DetailsPollenGrid(IReadOnlyList<DetailsPollenItem> Items);

/// <summary>
/// A row of sun or moon times with its icon, or the Moon's phase drawn from its angle.
/// </summary>
public sealed record DetailsAstroItem(string? IconKey, string Text, string AutomationName, int? MoonPhaseAngle = null);

public sealed record DetailsAstroList(IReadOnlyList<DetailsAstroItem> Items);
