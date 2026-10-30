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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Icons;
using LibreccioWeather.ViewModels;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;
using Point = System.Drawing.Point;

namespace LibreccioWeather.Tray;

public sealed class TrayIcon : IDisposable
{
    // How long a click waits before the popup opens while double-click is on. Short, so a click stays quick; a slower
    // double-click shows the popup briefly before the window opens.
    private const int PopupWait = 75;

    private readonly Window _window;
    private readonly MainViewModel _main;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.NotifyIcon _weatherIcon;
    private readonly ContextMenu _menu;
    private readonly TrayPopup _popup;
    private readonly DispatcherTimer _tooltipTimer;
    private readonly DispatcherTimer _clickTimer;
    private WeatherViewModel? _weather;
    private Point _popupAnchor;
    private long _firstClickTick;
    private bool _awaitingSecondClick;
    private bool _dismissed;
    private Core.Models.Weather? _dismissedWeather;
    private Icon? _icon;
    private string? _imageKey;
    private Icon? _weatherImage;
    private string? _weatherImageKey;
    private WindowState _restoreState = WindowState.Normal;
    private bool _isExiting;
    private bool _disposed;

    public TrayIcon(Window window, MainViewModel main)
    {
        _window = window;
        _main = main;
        _menu = new ContextMenu { Placement = PlacementMode.MousePoint };
        _menu.SetResourceReference(FrameworkElement.StyleProperty, "AppContextMenu");
        _notifyIcon = CreateNotifyIcon();
        _weatherIcon = CreateNotifyIcon();
        _popup = new TrayPopup();
        _popup.OpenRequested += OnPopupOpenRequested;
        _window.Closing += OnWindowClosing;
        _window.StateChanged += OnWindowStateChanged;
        _main.PropertyChanged += OnMainPropertyChanged;
        _main.Settings.SettingsChanged += OnSettingsChanged;
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
        _tooltipTimer = new DispatcherTimer(DispatcherPriority.Background, window.Dispatcher)
        {
            Interval = TimeSpan.FromMinutes(1),
        };
        _tooltipTimer.Tick += OnTooltipTick;
        _clickTimer = new DispatcherTimer(DispatcherPriority.Normal, window.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(Forms.SystemInformation.DoubleClickTime),
        };
        _clickTimer.Tick += OnSingleClick;
        BindWeather();
        _tooltipTimer.Start();
    }

    public void PrepareForShutdown()
    {
        _isExiting = true;
        _clickTimer.Stop();
        _menu.IsOpen = false;
        _popup.Dismiss();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        PrepareForShutdown();
        _clickTimer.Tick -= OnSingleClick;
        _popup.OpenRequested -= OnPopupOpenRequested;

        // Application shutdown may already have closed the popup, which removes its presentation source.
        if (PresentationSource.FromVisual(_popup) is not null)
        {
            _popup.Close();
        }

        _window.Closing -= OnWindowClosing;
        _window.StateChanged -= OnWindowStateChanged;
        _main.PropertyChanged -= OnMainPropertyChanged;
        _main.Settings.SettingsChanged -= OnSettingsChanged;
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        if (_weather is not null)
        {
            _weather.PropertyChanged -= OnWeatherPropertyChanged;
        }

        _tooltipTimer.Stop();
        _tooltipTimer.Tick -= OnTooltipTick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _weatherIcon.Visible = false;
        _weatherIcon.Dispose();
        _icon?.Dispose();
        _weatherImage?.Dispose();
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var icon = new Forms.NotifyIcon();
        icon.MouseDown += OnMouseDown;
        icon.MouseClick += OnMouseClick;
        icon.MouseDoubleClick += OnMouseDoubleClick;
        icon.MouseUp += OnMouseUp;
        return icon;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            if (_notifyIcon.Visible)
            {
                e.Cancel = true;
                _window.Hide();
            }
            else
            {
                PrepareForShutdown();
                _ = _window.Dispatcher.InvokeAsync(() => Application.Current.Shutdown());
            }
        }
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (_window.WindowState != WindowState.Minimized)
        {
            _restoreState = _window.WindowState;
        }
    }

    private void OnPopupOpenRequested(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    public void ShowWindow()
    {
        _clickTimer.Stop();
        _awaitingSecondClick = false;
        _popup.Dismiss();
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = _restoreState;
        }

        _window.Activate();
    }

    // Held within what Windows accepts for its own double-click time, whatever a hand-edited settings file contains.
    private int DoubleClickDelay =>
        Math.Clamp(_main.Settings.TrayDoubleClickDelay ?? Forms.SystemInformation.DoubleClickTime, 1, 5000);

    // Windows reports a double-click only within its own double-click time, so with the chosen delay the clicks are
    // paired here, on the button presses, which Windows reports for both clicks of a pair.
    private void OnMouseDown(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Left || !_main.Settings.IsTrayDoubleClickEnabled)
        {
            return;
        }

        long now = Environment.TickCount64;
        if (_awaitingSecondClick && now - _firstClickTick <= DoubleClickDelay)
        {
            _awaitingSecondClick = false;
            ShowWindow();
            return;
        }

        // The popup waits only briefly, however long the double-click delay: a second click that comes after the popup
        // has opened still opens the window, and the popup closes on its own when the tray icon takes the focus.
        _awaitingSecondClick = true;
        _firstClickTick = now;
        _popupAnchor = Forms.Cursor.Position;
        _clickTimer.Stop();
        _clickTimer.Interval = TimeSpan.FromMilliseconds(Math.Min(DoubleClickDelay, PopupWait));
        _clickTimer.Start();
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left && !_main.Settings.IsTrayDoubleClickEnabled)
        {
            _popupAnchor = Forms.Cursor.Position;
            TogglePopup();
        }
    }

    // The second click of a quick pair raises no click of its own, so without double-click it counts here.
    private void OnMouseDoubleClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left && !_main.Settings.IsTrayDoubleClickEnabled)
        {
            _popupAnchor = Forms.Cursor.Position;
            TogglePopup();
        }
    }

    private void OnSingleClick(object? sender, EventArgs e)
    {
        TogglePopup();
    }

    private void TogglePopup()
    {
        _clickTimer.Stop();
        if (!_notifyIcon.Visible || _isExiting)
        {
            return;
        }

        if (_popup.IsVisible)
        {
            _popup.Dismiss();
            return;
        }

        _popup.DataContext = CreatePopupContent(_weather?.ShownLocation);
        _popup.ShowAt(_popupAnchor);
    }

    private TrayPopupContent CreatePopupContent(Location? location)
    {
        return TrayPopupContent.Create(location, _main.Locations, _main.Settings, _main.Settings.GetTemperatureUnit(_main.RegionCode));
    }

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        BindWeather();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        _dismissed = false;
        BindWeather();
    }

    private void OnSystemPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        _ = _window.Dispatcher.InvokeAsync(() =>
        {
            if (!_disposed && !_isExiting)
            {
                _imageKey = null;
                _weatherImageKey = null;
                Update();
            }
        });
    }

    private void OnWeatherPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WeatherViewModel.IsLoading) && _weather?.IsLoading == false)
        {
            _dismissed = false;
        }

        Update();
    }

    private void OnTooltipTick(object? sender, EventArgs e)
    {
        Update();
    }

    private void BindWeather()
    {
        if (!_window.Dispatcher.CheckAccess())
        {
            _ = _window.Dispatcher.InvokeAsync(BindWeather);
            return;
        }

        if (_disposed || _isExiting)
        {
            return;
        }

        WeatherViewModel? weather = _main.WeatherPage;
        if (!ReferenceEquals(_weather, weather))
        {
            if (_weather is not null)
            {
                _weather.PropertyChanged -= OnWeatherPropertyChanged;
            }

            _weather = weather;
            if (_weather is not null)
            {
                _weather.PropertyChanged += OnWeatherPropertyChanged;
            }
        }

        Update();
    }

    private void Update()
    {
        if (!_window.Dispatcher.CheckAccess())
        {
            _ = _window.Dispatcher.InvokeAsync(Update);
            return;
        }

        if (_disposed || _isExiting)
        {
            return;
        }

        Location? location = _weather?.ShownLocation;
        Core.Models.Weather? weather = location?.Weather;
        if (!_main.Settings.IsWidgetNotificationEnabled)
        {
            HideIcons();
            _popup.Dismiss();
            if (!_window.IsVisible)
            {
                ShowWindow();
            }

            return;
        }

        if (_dismissed && !ReferenceEquals(_dismissedWeather, weather))
        {
            _dismissed = false;
        }

        if (_dismissed)
        {
            return;
        }

        Core.Units.Temperature? temperature = weather?.Current?.Temperature?.Value;
        if (_main.Settings.IsWidgetNotificationUsingFeelsLike)
        {
            temperature = weather?.Current?.Temperature?.FeelsLikeTemperature ?? temperature;
        }

        bool daylight = weather?.NextHourlyForecast.FirstOrDefault()?.IsDaylight ?? true;
        string? iconPath = WeatherIcons.GetPath(weather?.Current?.WeatherCode, daylight);
        string? iconTemperature = _main.Settings.IsWidgetNotificationTemperatureIconEnabled && temperature is { } iconValue
            ? UnitFormatting.Format(iconValue, _main.Settings.GetTemperatureUnit(_main.RegionCode), UnitWidth.Narrow, UnitWidth.Narrow)
            : null;
        string? imageKey = iconTemperature is null ? iconPath
            : "temperature:" + iconTemperature + ":" + TrayIconImages.IsLightTaskbar;
        if (_icon is null || _imageKey != imageKey)
        {
            SetImage(_notifyIcon, ref _icon,
                iconTemperature is null ? TrayIconImages.Create(iconPath) : TrayIconImages.CreateTemperature(iconTemperature));
            _imageKey = imageKey;
        }

        // Without a temperature the main icon already shows the condition, so a second one would repeat it.
        bool showWeatherIcon = iconTemperature is not null && _main.Settings.IsTrayWeatherIconEnabled;
        if (showWeatherIcon && (_weatherImage is null || _weatherImageKey != iconPath))
        {
            SetImage(_weatherIcon, ref _weatherImage, TrayIconImages.Create(iconPath));
            _weatherImageKey = iconPath;
        }

        string tooltip = BuildTooltip(location, weather, temperature);
        _notifyIcon.Text = tooltip;
        _weatherIcon.Text = tooltip;
        _notifyIcon.Visible = true;
        _weatherIcon.Visible = showWeatherIcon;
        if (_popup.IsVisible)
        {
            _popup.DataContext = CreatePopupContent(location);
        }
    }

    private static void SetImage(Forms.NotifyIcon target, ref Icon? current, Icon replacement)
    {
        Icon? previous = current;
        target.Icon = replacement;
        current = replacement;
        previous?.Dispose();
    }

    private string BuildTooltip(Location? location, Core.Models.Weather? weather, Core.Units.Temperature? temperature)
    {
        List<string> lines = [location?.GetPlace() ?? Strings.Get("desktop_app_name")];
        List<string> conditions = [];
        if (weather?.Current?.WeatherText is { Length: > 0 } text)
        {
            conditions.Add(text);
        }

        if (temperature is { } value)
        {
            conditions.Add(UnitFormatting.Format(value, _main.Settings.GetTemperatureUnit(_main.RegionCode)));
        }

        if (conditions.Count > 0)
        {
            lines.Add(string.Join(Strings.Get("dot_separator"), conditions));
        }

        if (_weather?.IsLoading == true)
        {
            lines.Add(Strings.Get("notification_updating_weather_data"));
        }
        else if (location is not null && weather?.Base.RefreshTime is { } refreshTime)
        {
            string time = RelativeTimeFormatting.GetRelativeTimeInSentence(refreshTime)
                ?? TimeZoneInfo.ConvertTime(refreshTime, location.TimeZone).ToString("g", Strings.Culture);
            lines.Add(Strings.Format("location_last_updated_x", time));
        }

        // Windows limits tray tooltips to 127 UTF-16 code units.
        string tooltip = string.Join(Environment.NewLine, lines);
        if (tooltip.Length > 127)
        {
            int length = 126;
            if (char.IsHighSurrogate(tooltip[length - 1]))
            {
                length--;
            }

            tooltip = tooltip[..length] + "…";
        }

        return tooltip;
    }

    private void HideIcons()
    {
        _notifyIcon.Visible = false;
        _weatherIcon.Visible = false;
    }

    private void OnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Right && !_isExiting)
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        _clickTimer.Stop();
        _awaitingSecondClick = false;
        _popup.Dismiss();
        _menu.IsOpen = false;
        _menu.Items.Clear();
        AddMenuItem(_menu, Strings.Get("desktop_open"), ShowWindow);
        AddMenuItem(_menu, Strings.Get("action_refresh"), () =>
        {
            if (_weather?.RefreshCommand.CanExecute(null) == true)
            {
                _weather.RefreshCommand.Execute(null);
            }
        }).IsEnabled = _weather is { IsLoading: false } && _weather.RefreshCommand.CanExecute(null);

        var locations = new MenuItem
        {
            Header = Strings.Get("tray_change_location"),
            IsEnabled = _main.Locations.Count > 0 && !_main.IsRefreshing && _main.Dialog is null,
        };
        foreach (Location location in _main.Locations)
        {
            var item = new MenuItem
            {
                Header = location.GetPlace(),
                IsCheckable = true,
                IsChecked = location.FormattedId == _main.CurrentFormattedId,
            };
            item.Click += (_, _) =>
            {
                _main.ShowLocation(location);
                ShowWindow();
            };
            locations.Items.Add(item);
        }

        _menu.Items.Add(locations);
        AddMenuItem(_menu, Strings.Get("action_settings"), () =>
        {
            _main.OpenSettings();
            ShowWindow();
        }).IsEnabled = _main.Dialog is null;
        _menu.Items.Add(new Separator());
        if (!_main.Settings.IsWidgetNotificationPersistent)
        {
            AddMenuItem(_menu, Strings.Get("tray_dismiss"), () =>
            {
                ShowWindow();
                _dismissed = true;
                _dismissedWeather = _weather?.ShownLocation.Weather;
                HideIcons();
            });
        }

        AddMenuItem(_menu, Strings.Get("tray_exit"), () =>
        {
            PrepareForShutdown();
            Application.Current.Shutdown();
        });
        _menu.IsOpen = true;

        // A menu only closes on a click elsewhere while its window is in the foreground, which a tray click does not give.
        if (PresentationSource.FromVisual(_menu) is HwndSource source)
        {
            SetForegroundWindow(source.Handle);
        }
    }

    private static MenuItem AddMenuItem(ItemsControl parent, string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        parent.Items.Add(item);
        return item;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);
}
