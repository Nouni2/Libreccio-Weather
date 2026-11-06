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

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LibreccioWeather.Controls.Forecast;
using LibreccioWeather.ViewModels.Forecast;
using Point = System.Windows.Point;

namespace LibreccioWeather.Views.Weather;

public partial class ForecastCardView : UserControl
{
    private ForecastCardViewModel? _card;
    private PolylineAndHistogramView? _firstChart;

    public ForecastCardView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Attach(DataContext as ForecastCardViewModel);
            if (DataContext is ForecastCardViewModel card)
            {
                // Columns are only scrolled while they keep their minimum width.
                ForecastScroller.ScrollToHorizontalOffset(card.InitialIndex * card.MinColumnWidth);
            }
        };

        // Following the card only while loaded keeps a card that outlives this view from holding on to it.
        Unloaded += (_, _) => Attach(null);
        DataContextChanged += (_, _) =>
        {
            if (IsLoaded)
            {
                Attach(DataContext as ForecastCardViewModel);
            }
        };
    }

    private void Attach(ForecastCardViewModel? card)
    {
        if (_card == card)
        {
            return;
        }

        if (_card is not null)
        {
            _card.PropertyChanged -= OnCardPropertyChanged;
        }

        _card = card;
        if (card is not null)
        {
            card.PropertyChanged += OnCardPropertyChanged;
        }
    }

    // A tab switch keeps the columns, so their charts are not loaded again and may keep their size while the rows
    // around them change. The reference lines are placed again once the new tab is laid out.
    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForecastCardViewModel.SelectedTrend) && _firstChart is { } chart)
        {
            _ = Dispatcher.InvokeAsync(() => UpdateChartBounds(chart), DispatcherPriority.Loaded);
        }
    }

    private void OnChartLoaded(object sender, RoutedEventArgs e)
    {
        UpdateChartBounds((PolylineAndHistogramView)sender);
    }

    private void OnChartSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateChartBounds((PolylineAndHistogramView)sender);
    }

    private void UpdateChartBounds(PolylineAndHistogramView chart)
    {
        if (!chart.IsLoaded || DataContext is not ForecastCardViewModel card
            || !ReferenceEquals(chart.DataContext, card.Columns.FirstOrDefault()))
        {
            return;
        }

        _firstChart = chart;

        // Reference lines stay fixed to the viewport while the forecast columns scroll beneath them.
        Point origin = chart.TranslatePoint(new Point(0, 0), ForecastViewport);
        ReferenceLines.ChartTop = (int)(origin.Y + (chart.Data?.HasTopPolylineLabel == true ? 24 : 12));
        ReferenceLines.ChartBottom = (int)(origin.Y + (int)chart.ActualHeight - 36);
    }

    private void OnColumnClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ForecastColumnSlot column } && DataContext is ForecastCardViewModel card)
        {
            card.OpenDetailsCommand.Execute(column);
        }
    }

    private void OnForecastMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            ForecastScroller.ScrollToHorizontalOffset(ForecastScroller.HorizontalOffset - e.Delta);
        }
        else
        {
            // The nested viewer only scrolls horizontally, so vertical wheel input belongs to the page.
            RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
            });
        }
    }
}
