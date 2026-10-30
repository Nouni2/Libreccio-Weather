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
using System.Windows;
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
    private readonly Window _window;
    private readonly MainViewModel _main;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _menu;
    private readonly DispatcherTimer _tooltipTimer;
    private readonly DispatcherTimer _clickTimer;
    private WeatherViewModel? _weather;
    private TrayPopup? _popup;
    private Point _popupAnchor;
    private long _lastDoubleClickTick;
    private bool _dismissed;
    private Core.Models.Weather? _dismissedWeather;
    private Icon? _icon;
    private string? _imageKey;
    private WindowState _restoreState = WindowState.Normal;
    private bool _isExiting;
    private bool _disposed;

    public TrayIcon(Window window, MainViewModel main)
    {
        _window = window;
        _main = main;
        _menu = new Forms.ContextMenuStrip();
        _menu.Opening += OnMenuOpening;
        _notifyIcon = new Forms.NotifyIcon { ContextMenuStrip = _menu };
        _notifyIcon.MouseDoubleClick += OnMouseDoubleClick;
        _notifyIcon.MouseClick += OnMouseClick;
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
        ClosePopup();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isExiting = true;
        _clickTimer.Stop();
        _clickTimer.Tick -= OnSingleClick;
        ClosePopup();
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
        _menu.Dispose();
        _icon?.Dispose();
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

    private void ShowWindow()
    {
        _clickTimer.Stop();
        ClosePopup();
        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = _restoreState;
        }

        _window.Activate();
    }

    private void OnMouseDoubleClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            _lastDoubleClickTick = Environment.TickCount64;
            ShowWindow();
        }
    }

    private void OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Left
            || (_lastDoubleClickTick > 0 && Environment.TickCount64 - _lastDoubleClickTick < Forms.SystemInformation.DoubleClickTime))
        {
            return;
        }

        _popupAnchor = Forms.Cursor.Position;
        _clickTimer.Stop();
        _clickTimer.Start();
    }

    private void OnSingleClick(object? sender, EventArgs e)
    {
        _clickTimer.Stop();
        if (!_notifyIcon.Visible || _isExiting)
        {
            return;
        }

        if (_popup is not null)
        {
            ClosePopup();
            return;
        }

        var popup = new TrayPopup
        {
            DataContext = TrayPopupContent.Create(_weather?.ShownLocation, _main.Locations,
                _main.Settings, _main.Settings.GetTemperatureUnit(_main.RegionCode)),
        };
        popup.OpenRequested += (_, _) => ShowWindow();
        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup))
            {
                _popup = null;
            }
        };
        _popup = popup;
        popup.ShowAt(_popupAnchor);
    }

    private void ClosePopup()
    {
        TrayPopup? popup = _popup;
        _popup = null;
        popup?.Close();
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
            _notifyIcon.Visible = false;
            ClosePopup();
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
            Icon replacement = iconTemperature is null ? TrayIconImages.Create(iconPath) : TrayIconImages.CreateTemperature(iconTemperature);
            Icon? previous = _icon;
            _notifyIcon.Icon = replacement;
            _icon = replacement;
            _imageKey = imageKey;
            previous?.Dispose();
        }

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

        _notifyIcon.Text = tooltip;
        _notifyIcon.Visible = true;
        if (_popup is not null)
        {
            _popup.DataContext = TrayPopupContent.Create(location, _main.Locations,
                _main.Settings, _main.Settings.GetTemperatureUnit(_main.RegionCode));
        }
    }

    private void OnMenuOpening(object? sender, CancelEventArgs e)
    {
        _clickTimer.Stop();
        ClosePopup();
        foreach (Forms.ToolStripItem item in _menu.Items.Cast<Forms.ToolStripItem>().ToArray())
        {
            item.Dispose();
        }

        _menu.Items.Clear();
        AddMenuItem("desktop_open", ShowWindow);
        AddMenuItem("action_refresh", () =>
        {
            if (_weather?.RefreshCommand.CanExecute(null) == true)
            {
                _weather.RefreshCommand.Execute(null);
            }
        }).Enabled = _weather is { IsLoading: false } && _weather.RefreshCommand.CanExecute(null);

        var locations = new Forms.ToolStripMenuItem(Strings.Get("tray_change_location"))
        {
            Enabled = _main.Locations.Count > 0 && !_main.IsRefreshing && _main.Dialog is null,
        };
        foreach (Location location in _main.Locations)
        {
            var item = new Forms.ToolStripMenuItem(location.GetPlace().Replace("&", "&&", StringComparison.Ordinal))
            {
                Checked = location.FormattedId == _main.CurrentFormattedId,
            };
            item.Click += (_, _) =>
            {
                _main.ShowLocation(location);
                ShowWindow();
            };
            locations.DropDownItems.Add(item);
        }

        _menu.Items.Add(locations);
        AddMenuItem("action_settings", () =>
        {
            _main.OpenSettings();
            ShowWindow();
        }).Enabled = _main.Dialog is null;
        _menu.Items.Add(new Forms.ToolStripSeparator());
        if (!_main.Settings.IsWidgetNotificationPersistent)
        {
            AddMenuItem("tray_dismiss", () =>
            {
                ShowWindow();
                _dismissed = true;
                _dismissedWeather = _weather?.ShownLocation.Weather;
                _notifyIcon.Visible = false;
            });
        }

        AddMenuItem("tray_exit", () =>
        {
            PrepareForShutdown();
            Application.Current.Shutdown();
        });
        e.Cancel = false;
    }

    private Forms.ToolStripMenuItem AddMenuItem(string key, Action action)
    {
        var item = new Forms.ToolStripMenuItem(Strings.Get(key));
        item.Click += (_, _) => action();
        _menu.Items.Add(item);
        return item;
    }
}
