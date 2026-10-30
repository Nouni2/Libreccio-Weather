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

using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace LibreccioWeather.ViewModels.Details;

public enum DetailsChartKind
{
    Line,
    Bar,
}

/// <summary>
/// A color at a value of the vertical axis. A line takes the colors of the values it crosses, blended between stops.
/// </summary>
public sealed record ChartColorStop(double Value, Color Color);

/// <summary>
/// One line of a chart. X values are Unix milliseconds.
/// </summary>
public sealed record DetailsChartSeries(IReadOnlyList<long> X, IReadOnlyList<double> Y, IReadOnlyList<ChartColorStop> Colors);

public sealed record ChartAxisLabel(double Value, string Text);

/// <summary>
/// A chart of one day, from midnight to midnight. Pressing on it reports the hour under the pointer, and releasing it
/// reports null.
/// </summary>
public sealed record DetailsChartData(
    DetailsChartKind Kind,
    long MinX,
    long MaxX,
    double MinY,
    double MaxY,
    IReadOnlyList<DetailsChartSeries> Series,
    IReadOnlyList<ChartAxisLabel> EndAxisLabels,
    IReadOnlyList<ChartAxisLabel> BottomAxisLabels,
    IReadOnlyList<ChartAxisLabel> HorizontalLines,
    Action<long?> SetActiveX)
{
    public Color BarColor { get; init; }

    /// <summary>
    /// The hours that may get a text above the chart, and the text for each.
    /// </summary>
    public IReadOnlyList<long> TopValues { get; init; } = [];

    public Func<long, string>? TopLabel { get; init; }

    /// <summary>
    /// The hours that may get an icon above the chart, and the icon for each.
    /// </summary>
    public IReadOnlyList<long> TopIconValues { get; init; } = [];

    public Func<long, ImageSource?>? TopIcon { get; init; }

    /// <summary>
    /// The data hours, which the pointer snaps to.
    /// </summary>
    public IReadOnlyList<long> XValues => Series.Count == 0 ? [] : Series[0].X;
}
