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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LibreccioWeather.Controls;
using LibreccioWeather.Controls.WeatherCards;
using LibreccioWeather.ViewModels;
using LibreccioWeather.ViewModels.Forecast;
using LibreccioWeather.ViewModels.WeatherCards;

namespace LibreccioWeather.Views;

public partial class WeatherView : UserControl
{
    // The time since the last refresh is written again every minute, and only while the screen is shown.
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(1) };

    private readonly ObservableCollection<WeatherCardHost> _cardHosts = [];

    public WeatherView()
    {
        InitializeComponent();
        CardsList.ItemsSource = _cardHosts;
        _clock.Tick += (_, _) => (DataContext as WeatherViewModel)?.UpdateRefreshTime();
        Loaded += (_, _) =>
        {
            (DataContext as WeatherViewModel)?.UpdateRefreshTime();
            _clock.Start();
            if (FindCardsPanel(WeatherScroll) is { } panel)
            {
                if (panel != _cardsPanel)
                {
                    _cardsPanel = panel;
                    panel.HeaderEnteredScreen += (_, _) => TemperatureNumber.Start();
                }

                panel.AttachHeader(WeatherHeader);
            }
        };
        Unloaded += (_, _) =>
        {
            _clock.Stop();
            _wheelRelease.Stop();
            SetPull(0);
        };
        DataContextChanged += OnDataContextChanged;
        WeatherScroll.ScrollChanged += (_, _) => UpdateAnimationScroll();

        PullIndicator.RenderTransform = _pullOffset;
        _wheelRelease.Tick += (_, _) =>
        {
            _wheelRelease.Stop();
            ReleasePull();
        };
        WeatherScroll.PreviewMouseWheel += OnPullWheel;
        WeatherScroll.PreviewMouseLeftButtonDown += OnPullDragStart;
        WeatherScroll.PreviewMouseMove += OnPullDragMove;
        WeatherScroll.PreviewMouseLeftButtonUp += OnPullDragEnd;
        WeatherScroll.LostMouseCapture += (_, _) => CancelPullDrag();
    }

    // Pull to refresh with the mouse standing in for a finger: at the top of the screen, a drag down pulls the
    // indicator down at half the distance moved, the wheel turned up pulls it by notches, and releasing past 64
    // refreshes.
    private const double PullDragRate = 0.5;
    private const double PullRefreshDistance = 64;

    // A wheel notch moves the pull by 40, so two deliberate notches reach the refresh distance. The wheel has no
    // release, so the pull is released once the wheel has been still for 140 ms.
    private const double WheelPullPerNotch = 40;

    // A wheel pull only starts after the wheel has been still for 200 ms. Scrolling up to the top and carrying on, and
    // a touchpad's momentum after the fingers lift, send wheel input without such a pause, and must not refresh.
    private const long WheelRestMilliseconds = 200;

    private readonly DispatcherTimer _wheelRelease = new() { Interval = TimeSpan.FromMilliseconds(140) };
    private long _lastWheelAt = long.MinValue;
    private readonly TranslateTransform _pullOffset = new();
    private double _pull;
    private System.Windows.Point? _dragStart;
    private bool _dragging;

    private bool CanPull => WeatherScroll.VerticalOffset <= 0
        && DataContext is WeatherViewModel { IsLoading: false } weather && weather.RefreshCommand.CanExecute(null);

    private void OnPullWheel(object sender, MouseWheelEventArgs e)
    {
        long now = Environment.TickCount64;
        bool rested = _lastWheelAt == long.MinValue || now - _lastWheelAt >= WheelRestMilliseconds;
        _lastWheelAt = now;
        if (_dragging || Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        // Turning the wheel down while pulling takes the pull back before the page scrolls again.
        if (_pull > 0 || (e.Delta > 0 && rested && CanPull))
        {
            e.Handled = true;
            SetPull(_pull + (e.Delta / 120.0 * WheelPullPerNotch));
            _wheelRelease.Stop();
            if (_pull > 0)
            {
                _wheelRelease.Start();
            }
        }
    }

    private void OnPullDragStart(object sender, MouseButtonEventArgs e)
    {
        _dragStart = CanPull ? e.GetPosition(WeatherScroll) : null;
    }

    private void OnPullDragMove(object sender, MouseEventArgs e)
    {
        if (_dragStart is not { } start || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        double moved = e.GetPosition(WeatherScroll).Y - start.Y;
        if (!_dragging)
        {
            // A small movement stays a click, and a drag up is not a pull.
            if (moved < -SystemParameters.MinimumVerticalDragDistance)
            {
                _dragStart = null;
                return;
            }

            if (moved <= SystemParameters.MinimumVerticalDragDistance || !WeatherScroll.CaptureMouse())
            {
                return;
            }

            _wheelRelease.Stop();
            _dragging = true;
        }

        e.Handled = true;
        SetPull(Math.Max(0, moved) * PullDragRate);
    }

    // Releasing a drag keeps the click from reaching the card it started on.
    private void OnPullDragEnd(object sender, MouseButtonEventArgs e)
    {
        _dragStart = null;
        if (!_dragging)
        {
            return;
        }

        e.Handled = true;
        _dragging = false;
        WeatherScroll.ReleaseMouseCapture();
        ReleasePull();
    }

    private void CancelPullDrag()
    {
        if (_dragging)
        {
            _dragging = false;
            _dragStart = null;
            SetPull(0);
        }
    }

    private void ReleasePull()
    {
        bool refresh = _pull >= PullRefreshDistance;
        SetPull(0);
        if (refresh && DataContext is WeatherViewModel weather && weather.RefreshCommand.CanExecute(null))
        {
            weather.RefreshCommand.Execute(null);
        }
    }

    // The indicator comes down from just above the content with the pull, and twice as slowly past the refresh
    // distance, up to twice that distance.
    private void SetPull(double pull)
    {
        _pull = Math.Clamp(pull, 0, PullRefreshDistance * 3);
        if (_pull <= 0)
        {
            PullIndicator.Visibility = Visibility.Collapsed;
            PullIndicator.Progress = 0;
            return;
        }

        double travel = _pull <= PullRefreshDistance ? _pull : PullRefreshDistance + ((_pull - PullRefreshDistance) / 2);
        _pullOffset.Y = travel - PullRefreshIndicator.Diameter;
        PullIndicator.Progress = _pull / PullRefreshDistance;
        PullIndicator.Visibility = Visibility.Visible;
    }

    // The animated weather is gone once the screen has scrolled by 500 physical pixels, a distance that keeps it
    // from blinking as the first cards scroll up.
    private const double AnimationFadeDistance = 500;

    private void UpdateAnimationScroll()
    {
        double fadeDistance = AnimationFadeDistance / VisualTreeHelper.GetDpi(this).DpiScaleY;
        WeatherAnimation.ScrollRate = Math.Min(1.0, WeatherScroll.VerticalOffset / fadeDistance);
    }

    private WeatherCardsPanel? _cardsPanel;

    private ResourceDictionary? _palette;
    private bool? _paletteIsLight;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is WeatherViewModel previous)
        {
            previous.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is WeatherViewModel current)
        {
            current.PropertyChanged += OnViewModelPropertyChanged;
            ApplyPalette(current.IsLightTheme);
            SyncCards(current.Cards);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WeatherViewModel.IsLightTheme) && sender is WeatherViewModel viewModel)
        {
            ApplyPalette(viewModel.IsLightTheme);
        }
        else if (e.PropertyName == nameof(WeatherViewModel.Cards) && sender is WeatherViewModel refreshed)
        {
            SyncCards(refreshed.Cards);
        }
    }

    // A location switch or a refresh brings new cards. When the kinds of cards and their order are unchanged, each
    // position keeps its view and takes the new card, and the cards' entrance plays again as it does for new views;
    // forecast cards also keep their columns. Otherwise the views are built anew.
    private void SyncCards(IReadOnlyList<object> cards)
    {
        bool sameKinds = cards.Count == _cardHosts.Count
            && cards.Select((card, index) => card.GetType() == _cardHosts[index].Card.GetType()).All(same => same);
        if (!sameKinds)
        {
            _cardHosts.Clear();
            foreach (object card in cards)
            {
                _cardHosts.Add(new WeatherCardHost(card));
            }

            return;
        }

        // The entrance restarts before the new data arrives, so cards that animate their content on new data wait
        // for their entrance first.
        if (cards.Count > 0)
        {
            _cardsPanel?.RestartCardEntrance();
        }

        for (int index = 0; index < cards.Count; index++)
        {
            WeatherCardHost host = _cardHosts[index];
            if (ReferenceEquals(host.Card, cards[index]))
            {
                continue;
            }

            if (cards[index] is ForecastCardViewModel next && host.Card is ForecastCardViewModel previous)
            {
                next.AdoptColumns(previous);
            }

            host.Card = cards[index];
        }
    }

    // The screen's own palette overrides the app's, so a location can be light while the rest of the app is dark.
    // Swapping it makes every color of the screen resolve again, so it is only swapped when it changes.
    private void ApplyPalette(bool light)
    {
        if (_paletteIsLight == light)
        {
            return;
        }

        _paletteIsLight = light;
        if (_palette is not null)
        {
            Resources.MergedDictionaries.Remove(_palette);
        }

        _palette = Appearance.ThemeManager.GetViewColors(light);
        Resources.MergedDictionaries.Add(_palette);
    }

    private static WeatherCardsPanel? FindCardsPanel(DependencyObject parent)
    {
        if (parent is WeatherCardsPanel panel)
        {
            return panel;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            if (FindCardsPanel(VisualTreeHelper.GetChild(parent, i)) is { } child)
            {
                return child;
            }
        }

        return null;
    }

    private void OnAstroCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AstroCardViewModel } && DataContext is WeatherViewModel weather)
        {
            weather.OpenAstroDetailsCommand.Execute(null);
        }
    }

    private void OnConditionsCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ConditionsCardViewModel card } && DataContext is WeatherViewModel weather)
        {
            weather.OpenCardDetailsCommand.Execute(card);
        }
    }
}
