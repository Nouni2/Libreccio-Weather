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

using System.Runtime.CompilerServices;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Settings;

/// <summary>
/// The app's settings. Keys and defaults are those of Breezy Weather, so its settings keep their meaning.
/// </summary>
public sealed class SettingsManager
{
    public const string DefaultCardDisplay =
        "nowcast&daily_forecast&hourly_forecast&precipitation&wind&air_quality&pollen&humidity&uv&visibility&pressure&sun&moon";

    public const string DefaultDailyTrendDisplay =
        "temperature&air_quality&wind&uv_index&precipitation&sunshine&feels_like";

    public const string DefaultHourlyTrendDisplay =
        "temperature&air_quality&wind&uv_index&precipitation&feels_like&humidity&pressure&cloud_cover&visibility";

    public const string DefaultTodayForecastTime = "07:00";
    public const string DefaultTomorrowForecastTime = "21:00";

    private const string DefaultLocationSource = "native";
    private const string DefaultWeatherSource = "auto";

    // Unit settings store "auto" to follow the region instead of a fixed unit.
    private const string AutoUnit = "auto";

    // Debug builds do not refresh in the background unless asked, so testing does not send requests on its own.
#if DEBUG
    private const string DefaultUpdateInterval = "never";
#else
    private const string DefaultUpdateInterval = "1:30";
#endif

    private readonly ConfigStore _config;

    public SettingsManager(ConfigStore config)
    {
        _config = config;
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Libreccio Weather",
        "settings.json");

    /// <summary>
    /// Raised after most settings change, so the interface and background work can follow them.
    /// </summary>
    public event EventHandler? SettingsChanged;

    // App updates

    public int LastVersionCode
    {
        get => _config.GetInt("last_version_code", 0);
        set => _config.PutInt("last_version_code", value);
    }

    public bool IsAppUpdateCheckEnabled
    {
        get => _config.GetBoolean("app_update_check_switch", false);
        set => _config.PutBoolean("app_update_check_switch", value);
    }

    public bool IsAppUpdateCheckPromptAlreadyAsked
    {
        get => _config.GetBoolean("app_update_check_prompt", false);
        set => _config.PutBoolean("app_update_check_prompt", value);
    }

    /// <summary>
    /// Unix time in milliseconds.
    /// </summary>
    public long AppUpdateCheckLastTimestamp
    {
        get => _config.GetLong("app_update_check_last_timestamp", 0);
        set => _config.PutLong("app_update_check_last_timestamp", value);
    }

    // Weather updates

    /// <summary>
    /// Unix time in milliseconds.
    /// </summary>
    public long WeatherUpdateLastTimestamp
    {
        get => _config.GetLong("weather_update_last_timestamp", 0);
        set => _config.PutLong("weather_update_last_timestamp", value);
    }

    /// <summary>
    /// Unix time in milliseconds.
    /// </summary>
    public long WeatherManualUpdateLastTimestamp
    {
        get => _config.GetLong("weather_manual_update_last_timestamp", 0);
        set => _config.PutLong("weather_manual_update_last_timestamp", value);
    }

    public string WeatherManualUpdateLastLocationId
    {
        get => _config.GetString("weather_manual_update_last_location_id", null) ?? "";
        set => _config.PutString("weather_manual_update_last_location_id", value);
    }

    // Basic

    public bool IsAlertPushEnabled
    {
        get => _config.GetBoolean("alert_notification_switch", true);
        set => SetAndNotify(() => _config.PutBoolean("alert_notification_switch", value));
    }

    public bool IsPrecipitationPushEnabled
    {
        get => _config.GetBoolean("precipitation_notification_switch", false);
        set => SetAndNotify(() => _config.PutBoolean("precipitation_notification_switch", value));
    }

    public UpdateInterval UpdateInterval
    {
        get => UpdateInterval.GetInstance(_config.GetString("refresh_rate", null) ?? DefaultUpdateInterval);
        set => SetAndNotify(() => _config.PutString("refresh_rate", value.Id));
    }

    public bool IgnoreUpdatesWhenBatteryLow
    {
        get => _config.GetBoolean("refresh_ignore_battery_low", true);
        set => SetAndNotify(() => _config.PutBoolean("refresh_ignore_battery_low", value));
    }

    public DarkMode DarkMode
    {
        get => DarkMode.GetInstance(_config.GetString("dark_mode", null) ?? "system");
        set => SetAndNotify(() => _config.PutString("dark_mode", value.Id));
    }

    // Off by default, because Windows has its own dark mode to follow.
    public bool DayNightModeForLocations
    {
        get => _config.GetBoolean("day_night_mode_locations", false);
        set => SetAndNotify(() => _config.PutBoolean("day_night_mode_locations", value));
    }

    // Language

    /// <summary>
    /// The language code chosen in the settings, or null to follow the Windows display language.
    /// Changing it marks saved weather as outdated, because its texts are in the previous language.
    /// </summary>
    public string? Language
    {
        get => _config.GetString("language", null);
        set
        {
            if (value == Language)
            {
                return;
            }

            _config.PutString("language", value);
            LanguageUpdateLastTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Unix time in milliseconds of the last language change.
    /// </summary>
    public long LanguageUpdateLastTimestamp
    {
        get => _config.GetLong("language_update_last_timestamp", 0);
        set => _config.PutLong("language_update_last_timestamp", value);
    }

    // Sources

    public string LocationSource
    {
        get => _config.GetString("location_service", null) ?? DefaultLocationSource;
        set => SetAndNotify(() => _config.PutString("location_service", value));
    }

    public string DefaultForecastSource
    {
        get => _config.GetString("default_weather_source", null) ?? DefaultWeatherSource;
        set => SetAndNotify(() => _config.PutString("default_weather_source", value));
    }

    // Units. A null unit follows the region.

    public TemperatureUnit? TemperatureUnit
    {
        get => TemperatureUnit.GetUnit(_config.GetString("temperature_unit", AutoUnit) ?? AutoUnit);
        set => SetAndNotify(() => _config.PutString("temperature_unit", value?.Id ?? AutoUnit));
    }

    public TemperatureUnit GetTemperatureUnit(string? countryCode)
    {
        return TemperatureUnit ?? TemperatureUnit.GetDefaultUnit(countryCode);
    }

    public DistanceUnit? DistanceUnit
    {
        get => DistanceUnit.GetUnit(_config.GetString("distance_unit", AutoUnit) ?? AutoUnit);
        set => SetAndNotify(() => _config.PutString("distance_unit", value?.Id ?? AutoUnit));
    }

    public DistanceUnit GetDistanceUnit(string? countryCode)
    {
        return DistanceUnit ?? DistanceUnit.GetDefaultUnit(countryCode);
    }

    public PrecipitationUnit? PrecipitationUnit
    {
        get => PrecipitationUnit.GetUnit(_config.GetString("precipitation_unit", AutoUnit) ?? AutoUnit);
        set => SetAndNotify(() => _config.PutString("precipitation_unit", value?.Id ?? AutoUnit));
    }

    public PrecipitationUnit GetPrecipitationUnit(string? countryCode)
    {
        return PrecipitationUnit ?? PrecipitationUnit.GetDefaultUnit(countryCode);
    }

    public PrecipitationUnit GetSnowfallUnit(string? countryCode)
    {
        return PrecipitationUnit ?? PrecipitationUnit.GetDefaultSnowfallUnit(countryCode);
    }

    public SpeedUnit? SpeedUnit
    {
        get => SpeedUnit.GetUnit(_config.GetString("speed_unit", AutoUnit) ?? AutoUnit);
        set => SetAndNotify(() => _config.PutString("speed_unit", value?.Id ?? AutoUnit));
    }

    public SpeedUnit GetSpeedUnit(string? countryCode)
    {
        return SpeedUnit ?? SpeedUnit.GetDefaultUnit(countryCode);
    }

    public PressureUnit? PressureUnit
    {
        get => PressureUnit.GetUnit(_config.GetString("pressure_unit", AutoUnit) ?? AutoUnit);
        set => SetAndNotify(() => _config.PutString("pressure_unit", value?.Id ?? AutoUnit));
    }

    public PressureUnit GetPressureUnit(string? countryCode)
    {
        return PressureUnit ?? PressureUnit.GetDefaultUnit(countryCode);
    }

    // Appearance

    public bool DesktopInterfaceEnabled
    {
        get => _config.GetBoolean("desktop_interface", false);
        set
        {
            if (value != DesktopInterfaceEnabled)
            {
                SetAndNotify(() => _config.PutBoolean("desktop_interface", value));
            }
        }
    }

    /// <summary>
    /// Runs animations at the refresh rate of the monitor the window is on, rather than 60 frames per second, which
    /// asks less of slower computers.
    /// </summary>
    public bool IsScreenRefreshRateEnabled
    {
        get => _config.GetBoolean("screen_refresh_rate", true);
        set => SetAndNotify(() => _config.PutBoolean("screen_refresh_rate", value));
    }

    // Changing the cards does not raise SettingsChanged; the main screen reads them again when it is shown.
    public List<CardDisplay> CardDisplayList
    {
        get => CardDisplay.ToCardDisplayList(_config.GetString("card_display", DefaultCardDisplay));
        set => _config.PutString("card_display", CardDisplay.ToValue(value));
    }

    public List<DailyTrendDisplay> DailyTrendDisplayList
    {
        get => DailyTrendDisplay.ToDailyTrendDisplayList(_config.GetString("daily_trend_display", DefaultDailyTrendDisplay));
        set => SetAndNotify(() => _config.PutString("daily_trend_display", DailyTrendDisplay.ToValue(value)));
    }

    public List<HourlyTrendDisplay> HourlyTrendDisplayList
    {
        get => HourlyTrendDisplay.ToHourlyTrendDisplayList(_config.GetString("hourly_trend_display", DefaultHourlyTrendDisplay));
        set => SetAndNotify(() => _config.PutString("hourly_trend_display", HourlyTrendDisplay.ToValue(value)));
    }

    public bool MainScreenBackgroundEnabled
    {
        get => _config.GetBoolean("main_screen_background", true);
        set => SetAndNotify(() => _config.PutBoolean("main_screen_background", value));
    }

    public bool IsTrendHorizontalLinesEnabled
    {
        get => _config.GetBoolean("trend_horizontal_line_switch", true);
        set => SetAndNotify(() => _config.PutBoolean("trend_horizontal_line_switch", value));
    }

    public BackgroundAnimationMode BackgroundAnimationMode
    {
        get => BackgroundAnimationMode.GetInstance(_config.GetString("background_animation_mode", "system") ?? "");
        set => SetAndNotify(() => _config.PutString("background_animation_mode", value.Id));
    }

    public bool IsCardsFadeInEnabled
    {
        get => _config.GetBoolean("list_animation_switch", true);
        set => SetAndNotify(() => _config.PutBoolean("list_animation_switch", value));
    }

    public bool IsElementsAnimationEnabled
    {
        get => _config.GetBoolean("item_animation_switch", true);
        set => SetAndNotify(() => _config.PutBoolean("item_animation_switch", value));
    }

    public string AlternateCalendar
    {
        get => _config.GetString("calendar_alternate", null) ?? "";
        set => _config.PutString("calendar_alternate", value);
    }

    // Forecast notifications

    public bool IsTodayForecastEnabled
    {
        get => _config.GetBoolean("timing_forecast_switch_today", false);
        set => SetAndNotify(() => _config.PutBoolean("timing_forecast_switch_today", value));
    }

    /// <summary>
    /// The time of day as "HH:mm".
    /// </summary>
    public string TodayForecastTime
    {
        get => _config.GetString("forecast_time_today", DefaultTodayForecastTime) ?? DefaultTodayForecastTime;
        set => SetAndNotify(() => _config.PutString("forecast_time_today", value));
    }

    public bool IsTomorrowForecastEnabled
    {
        get => _config.GetBoolean("timing_forecast_switch_tomorrow", false);
        set => SetAndNotify(() => _config.PutBoolean("timing_forecast_switch_tomorrow", value));
    }

    /// <summary>
    /// The time of day as "HH:mm".
    /// </summary>
    public string TomorrowForecastTime
    {
        get => _config.GetString("forecast_time_tomorrow", DefaultTomorrowForecastTime) ?? DefaultTomorrowForecastTime;
        set => SetAndNotify(() => _config.PutString("forecast_time_tomorrow", value));
    }

    // Mistray, which takes the place of the ongoing notification and keeps its settings

    public bool IsWidgetNotificationEnabled
    {
        get => _config.GetBoolean("notification_widget_switch", false);
        set => SetAndNotify(() => _config.PutBoolean("notification_widget_switch", value));
    }

    public bool IsWidgetNotificationPersistent
    {
        get => _config.GetBoolean("notification_widget_persistent_switch", true);
        set => SetAndNotify(() => _config.PutBoolean("notification_widget_persistent_switch", value));
    }

    public NotificationStyle WidgetNotificationStyle
    {
        get => NotificationStyle.GetInstance(_config.GetString("notification_widget_style", "daily") ?? "");
        set => SetAndNotify(() => _config.PutString("notification_widget_style", value.Id));
    }

    public bool IsWidgetNotificationTemperatureIconEnabled
    {
        get => _config.GetBoolean("notification_widget_temp_icon_switch", false);
        set => SetAndNotify(() => _config.PutBoolean("notification_widget_temp_icon_switch", value));
    }

    public bool IsWidgetNotificationUsingFeelsLike
    {
        get => _config.GetBoolean("notification_widget_feelslike", false);
        set => SetAndNotify(() => _config.PutBoolean("notification_widget_feelslike", value));
    }

    /// <summary>
    /// Shows the condition in a second tray icon next to the temperature icon.
    /// </summary>
    public bool IsTrayWeatherIconEnabled
    {
        get => _config.GetBoolean("tray_weather_icon", false);
        set => SetAndNotify(() => _config.PutBoolean("tray_weather_icon", value));
    }

    /// <summary>
    /// Makes a double-click on the tray icon open the main window.
    /// </summary>
    public bool IsTrayDoubleClickEnabled
    {
        get => _config.GetBoolean("tray_double_click", false);
        set => SetAndNotify(() => _config.PutBoolean("tray_double_click", value));
    }

    /// <summary>
    /// Milliseconds within which a second click on the tray icon makes a double-click, or null to follow Windows' setting.
    /// </summary>
    public int? TrayDoubleClickDelay
    {
        get => _config.Contains("tray_double_click_delay") ? _config.GetInt("tray_double_click_delay", 0) : null;
        set => SetAndNotify(() =>
        {
            if (value is { } delay)
            {
                _config.PutInt("tray_double_click_delay", delay);
            }
            else
            {
                _config.Remove("tray_double_click_delay");
            }
        });
    }

    private void SetAndNotify(Action write, [CallerMemberName] string propertyName = "")
    {
        write();
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(propertyName));
    }
}
