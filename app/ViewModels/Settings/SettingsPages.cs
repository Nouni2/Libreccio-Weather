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
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Units;
using LibreccioWeather.Diagnostics;
using LibreccioWeather.ViewModels.RefreshErrors;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// The settings screens. Categories without a screen are shown but cannot be opened.
/// </summary>
public static class SettingsPages
{
    // Unit settings store this value to follow the region.
    private const string AutoUnit = "auto";

    public static SettingsPageViewModel Root(MainViewModel main)
    {
        List<object> items =
        [
            new ClickablePreference(
                Strings.Get("settings_background_updates"),
                Strings.Get("settings_background_updates_summary"),
                () => main.NavigateTo(BackgroundUpdates(main)),
                "IconSync",
                isFirst: true,
                isLast: true),
            SeparatorPreference.Large,
            new ClickablePreference(
                Strings.Get("settings_appearance"),
                Strings.Get("settings_appearance_summary"),
                () => main.NavigateTo(Appearance(main)),
                "IconPalette",
                isFirst: true),
            SeparatorPreference.Small,
            new ClickablePreference(
                Strings.Get("settings_main"),
                Strings.Get("settings_main_summary"),
                () => main.NavigateTo(MainScreen(main)),
                "IconHome",
                isLast: true),
            SeparatorPreference.Large,
            new ClickablePreference(
                Strings.Get("settings_notifications"),
                Strings.Get("settings_notifications_summary"),
                () => main.NavigateTo(Notifications(main)),
                "IconNotifications",
                isFirst: true),
            SeparatorPreference.Small,
            new ClickablePreference(
                Strings.Get("settings_modules"),
                Strings.Get("settings_modules_summary"),
                () => main.NavigateTo(Modules(main)),
                "IconWidgets",
                isLast: true),
            SeparatorPreference.Large,
            NotPorted("settings_location", "settings_location_summary", "IconLocation", isFirst: true),
            SeparatorPreference.Small,
            new ClickablePreference(
                Strings.Get("settings_weather_sources"),
                Strings.Get("settings_weather_sources_summary"),
                () => main.NavigateTo(WeatherSources(main)),
                "IconFactory",
                isLast: true),
            SeparatorPreference.Large,
            new ClickablePreference(
                Strings.Get("settings_debug"),
                Strings.Get("settings_debug_summary"),
                () => main.NavigateTo(Debug(main)),
                "IconBugReport",
                isFirst: true,
                isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("action_settings"), items, main.GoBack, () => Root(main), () => main.NavigateTo(About(main)));
    }

    // Windows apps need no permission to notify, so Android's notification permission card has no counterpart and
    // every item is enabled. The items after the crash logs are only in debug builds.
    public static SettingsPageViewModel Debug(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        bool debugMode = Updates.AppVersion.IsDebug;
        List<object> items =
        [
            new ClickablePreference(
                Strings.Get("settings_debug_dump_crash_logs_title"),
                Strings.Get("settings_debug_dump_crash_logs_summary"),
                () => _ = DumpCrashLogsAsync(main),
                isFirst: true,
                isLast: !debugMode),
        ];

        if (debugMode)
        {
            string on = Strings.Get("settings_enabled");
            string off = Strings.Get("settings_disabled");
            bool numberFormatterSupported = FormatterSettings.IsNumberFormatterSupported;
            items.AddRange(
            [
                SeparatorPreference.Small,
                new ClickablePreference(
                    Strings.Get("settings_debug_force_weather_update"),
                    Strings.Get("debug_force_weather_update_summary"),
                    main.ForceWeatherUpdate,
                    isLast: true),
                SeparatorPreference.Large,
                new SectionHeaderPreference(Strings.Get("settings_debug_section_unit_formatting")),
                new SwitchPreference(
                    Strings.Get("settings_debug_use_numberformatter"), on, off,
                    settings.UseNumberFormatter && numberFormatterSupported,
                    value => settings.UseNumberFormatter = value,
                    isEnabled: numberFormatterSupported,
                    isFirst: true),
                SeparatorPreference.Small,
                new SwitchPreference(
                    Strings.Get("settings_debug_use_measureformat"), on, off,
                    settings.UseMeasureFormat,
                    value => settings.UseMeasureFormat = value,
                    isLast: true),
                SeparatorPreference.SectionFooter,
                SeparatorPreference.Large,
                new SectionHeaderPreference(Strings.Get("settings_debug_section_refresh_error")),
            ]);

            RefreshErrorType[] errors = Enum.GetValues<RefreshErrorType>();
            for (int index = 0; index < errors.Length; index++)
            {
                RefreshErrorType error = errors[index];
                string shortMessage = RefreshErrorHelp.GetShortMessage(error);
                items.Add(new ClickablePreference(shortMessage, null, () =>
                {
                    if (RefreshErrorHelp.GetActionButtonText(error) is { } actionText)
                    {
                        main.ShowSnackbar(shortMessage, actionText, () => RefreshErrorHelp.ShowDialog(error, main));
                    }
                    else
                    {
                        main.ShowSnackbar(shortMessage);
                    }
                }, isFirst: index == 0, isLast: index == errors.Length - 1));
                if (index != errors.Length - 1)
                {
                    items.Add(SeparatorPreference.Small);
                }
            }

            items.Add(SeparatorPreference.SectionFooter);
        }

        return new SettingsPageViewModel(Strings.Get("settings_debug"), items, main.GoBack, () => Debug(main), () => main.NavigateTo(About(main)));
    }

    // Saving reads the logs from disk, so it runs on a worker thread. Once saved, the message's Open button opens the
    // file.
    private static async Task DumpCrashLogsAsync(MainViewModel main)
    {
        string path;
        try
        {
            path = await Task.Run(() => CrashLogs.Dump());
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceWarning("Crash logs could not be saved: {0}", exception);
            main.ShowSnackbar(Strings.Get("crash_logs_failed"));
            return;
        }

        main.ShowSnackbar(
            Strings.Get("settings_debug_dump_crash_logs_saved"),
            Strings.Get("desktop_open"),
            () => CrashLogs.Open(path));
    }

    public static SettingsPageViewModel BackgroundUpdates(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        IReadOnlyList<string> values = Strings.GetArray("automatic_refresh_rate_values");

        // Each interval is named as a duration, such as "1 hour, 30 minutes"; "never" keeps its name from the array.
        IReadOnlyList<string> names = Strings.GetArray("automatic_refresh_rates")
            .Select((name, index) => UpdateInterval.Entries.FirstOrDefault(interval => interval.Id == values[index])?.Interval is { } duration
                ? DurationFormatting.FormatTime(duration, UnitWidth.Long)
                : name)
            .ToList();

        SwitchPreference batteryLow = new(
            Strings.Get("settings_background_updates_refresh_skip_when_battery_low"),
            Strings.Get("settings_enabled"),
            Strings.Get("settings_disabled"),
            settings.IgnoreUpdatesWhenBatteryLow,
            value => settings.IgnoreUpdatesWhenBatteryLow = value,
            isEnabled: settings.UpdateInterval != UpdateInterval.Never,
            isLast: true);

        ListPreference? refreshRate = null;
        refreshRate = new ListPreference(
            main,
            Strings.Get("settings_background_updates_refresh_title"),
            values,
            names,
            settings.UpdateInterval.Id,
            value =>
            {
                UpdateInterval interval = UpdateInterval.GetInstance(value);
                if (interval == UpdateInterval.Never)
                {
                    // Turning updates off asks for confirmation first.
                    main.ShowDialog(new NeverRefreshDialogViewModel(
                        () =>
                        {
                            main.CloseDialog();
                            settings.UpdateInterval = UpdateInterval.Never;
                            refreshRate!.Select(UpdateInterval.Never.Id);
                            batteryLow.IsEnabled = false;
                        },
                        main.CloseDialog));
                }
                else
                {
                    settings.UpdateInterval = interval;
                    refreshRate!.Select(interval.Id);
                    batteryLow.IsEnabled = true;
                }
            },
            isFirst: true,
            withState: false);

        long lastRun = settings.WeatherUpdateLastTimestamp;
        string? lastRunSummary = lastRun > 0
            ? Strings.Format(
                "settings_background_updates_worker_info_summary",
                DateTimeOffset.FromUnixTimeMilliseconds(lastRun).ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
            : null;

        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("settings_background_updates_section_general")),
            refreshRate,
            SeparatorPreference.Small,
            batteryLow,
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_background_updates_section_troubleshoot")),
            new ClickablePreference(
                Strings.Get("settings_background_updates_worker_info_title"),
                lastRunSummary,
                onClick: null,
                isFirst: true,
                isLast: true),
        ];
        return new SettingsPageViewModel(
            Strings.Get("settings_background_updates"), items, main.GoBack, () => BackgroundUpdates(main), () => main.NavigateTo(About(main)));
    }

    public static SettingsPageViewModel Appearance(MainViewModel main)
    {
        // The Windows language comes first, then each shipped language under its own name, sorted by that name.
        List<(string Code, string Name)> languages = AppLanguages.Codes
            .Select(code => (Code: code, Name: NativeName(code)))
            .OrderBy(language => language.Name, StringComparer.Ordinal)
            .ToList();
        languages.Insert(0, ("", Strings.Get("settings_follow_system")));
        string selected = languages.Any(language => language.Code == main.Settings.Language) ? main.Settings.Language! : "";

        SettingsManager settings = main.Settings;

        // Locations follow day and night whatever this setting says while the app is always light.
        ListPreference darkModeLocations = new(
            main,
            Strings.Get("settings_appearance_dark_mode_locations_title"),
            Strings.GetArray("dark_mode_location_values"),
            Strings.GetArray("dark_modes_location"),
            DarkModeLocationId(settings, settings.DarkMode),
            value => settings.DayNightModeForLocations = value == "day_night")
        {
            IsEnabled = settings.DarkMode != DarkMode.Light,
        };

        // "Follow system" names the mode Windows is in.
        IReadOnlyList<string> darkModeNames = Strings.GetArray("dark_modes")
            .Select((name, index) => index == 0
                ? Strings.Format(
                    "parenthesis",
                    Strings.Get("settings_follow_system"),
                    Strings.Get(global::LibreccioWeather.Appearance.ThemeManager.IsSystemInDarkMode() ? "settings_color_dark" : "settings_color_light"))
                : name)
            .ToList();
        ListPreference darkMode = new(
            main,
            Strings.Get("settings_appearance_dark_mode_title"),
            Strings.GetArray("dark_mode_values"),
            darkModeNames,
            settings.DarkMode.Id,
            value =>
            {
                DarkMode newDarkMode = DarkMode.GetInstance(value);
                settings.DarkMode = newDarkMode;
                darkModeLocations.IsEnabled = newDarkMode != DarkMode.Light;
                darkModeLocations.Select(DarkModeLocationId(settings, newDarkMode));
            },
            isFirst: true);

        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("settings_appearance_section_regional")),
            new ListPreference(
                main,
                Strings.Get("settings_appearance_language_title"),
                languages.Select(language => language.Code).ToList(),
                languages.Select(language => language.Name).ToList(),
                selected,
                value => main.ChangeLanguage(value.Length == 0 ? null : value),
                isFirst: true),
            SeparatorPreference.Small,
            new ClickablePreference(
                Strings.Get("settings_units"),
                Strings.Get("settings_units_summary"),
                () => main.NavigateTo(Units(main))),
            SeparatorPreference.Small,
            CalendarPreference(main),
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_appearance_section_theme")),
            darkMode,
            SeparatorPreference.Small,
            darkModeLocations,
            SeparatorPreference.Small,
            new ListPreference(
                main,
                Strings.Get("settings_appearance_icon_pack_title"),
                [""],
                [Strings.Get("desktop_app_name")],
                "",
                _ => { },
                isLast: true),
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("desktop_interface_section")),
            new SwitchPreference(
                Strings.Get("desktop_interface_title"),
                Strings.Get("desktop_interface_on"),
                Strings.Get("desktop_interface_off"),
                main.Settings.DesktopInterfaceEnabled,
                value => main.Settings.DesktopInterfaceEnabled = value,
                isFirst: true),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("display_refresh_rate"),
                Strings.Get("settings_enabled"),
                Strings.Get("settings_disabled"),
                main.Settings.IsScreenRefreshRateEnabled,
                value => main.Settings.IsScreenRefreshRateEnabled = value),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("seasonal_window_icon"),
                Strings.Get("settings_enabled"),
                Strings.Get("settings_disabled"),
                main.Settings.IsSeasonalWindowIconEnabled,
                value => main.Settings.IsSeasonalWindowIconEnabled = value),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("forecast_column_tooltip"),
                Strings.Get("settings_enabled"),
                Strings.Get("settings_disabled"),
                main.Settings.IsForecastColumnTooltipEnabled,
                value => main.Settings.IsForecastColumnTooltipEnabled = value),
            SeparatorPreference.Small,

            // 100 % follows Windows' wheel setting, which reset returns to. The slider spans a tenth to twice that
            // distance; the field accepts up to ten times.
            new SliderPreference(
                Strings.Get("touchpad_scroll_speed"), Strings.GetUnit("ratio_percent_display_name_short"),
                main.Settings.TouchpadScrollSpeed ?? 100,
                minimum: 1, maximum: 1000, sliderMinimum: 10, sliderMaximum: 200, recommended: 100,
                value => main.Settings.TouchpadScrollSpeed = value,
                () => main.Settings.TouchpadScrollSpeed = null,
                isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("settings_appearance"), items, main.GoBack, () => Appearance(main), () => main.NavigateTo(About(main)));
    }

    public static SettingsPageViewModel About(MainViewModel main)
    {
        return ViewModels.About.AboutPages.About(main);
    }

    public static SettingsPageViewModel MainScreen(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        string on = Strings.Get("settings_enabled");
        string off = Strings.Get("settings_disabled");

        ClickablePreference? cards = null;
        ClickablePreference? dailyTrends = null;
        ClickablePreference? hourlyTrends = null;

        // The summaries follow the lists saved by the editors when they are left.
        void UpdateSummaries()
        {
            cards!.Summary = CardDisplay.GetSummary(settings.CardDisplayList);
            dailyTrends!.Summary = DailyTrendDisplay.GetSummary(settings.DailyTrendDisplayList);
            hourlyTrends!.Summary = HourlyTrendDisplay.GetSummary(settings.HourlyTrendDisplayList);
        }

        cards = new ClickablePreference(
            Strings.Get("settings_main_cards_title"),
            CardDisplay.GetSummary(settings.CardDisplayList),
            () => main.NavigateTo(DisplayManageViewModel.CardDisplayManage(main, UpdateSummaries)),
            isFirst: true);
        dailyTrends = new ClickablePreference(
            Strings.Get("settings_main_daily_trends_title"),
            DailyTrendDisplay.GetSummary(settings.DailyTrendDisplayList),
            () => main.NavigateTo(DisplayManageViewModel.DailyTrendDisplayManage(main, UpdateSummaries)));
        hourlyTrends = new ClickablePreference(
            Strings.Get("settings_main_hourly_trends_title"),
            HourlyTrendDisplay.GetSummary(settings.HourlyTrendDisplayList),
            () => main.NavigateTo(DisplayManageViewModel.HourlyTrendDisplayManage(main, UpdateSummaries)),
            isLast: true);

        void OfferRestart()
        {
            main.ShowSnackbar(
                Strings.Get("settings_changes_apply_after_restart"),
                Strings.Get("action_restart"),
                main.RecreateAllScreens);
        }

        // With Windows' "Animation effects" off, the animation switches are shown off and unavailable.
        bool animationsEnabled = System.Windows.SystemParameters.ClientAreaAnimation;
        string unavailable = Strings.Get("settings_unavailable_no_animations");

        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("settings_main_section_displayed_data")),
            cards,
            SeparatorPreference.Small,
            dailyTrends,
            SeparatorPreference.Small,
            hourlyTrends,
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_main_section_options")),
            new SwitchPreference(
                Strings.Get("settings_main_background_title"), on, off,
                settings.MainScreenBackgroundEnabled,
                value => settings.MainScreenBackgroundEnabled = value,
                isFirst: true),
            new SwitchPreference(
                Strings.Get("settings_main_threshold_lines_on_charts"), on, off,
                settings.IsTrendHorizontalLinesEnabled,
                value =>
                {
                    settings.IsTrendHorizontalLinesEnabled = value;
                    OfferRestart();
                },
                isLast: true),
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_main_section_animations")),
            new ListPreference(
                main,
                Strings.Get("settings_main_background_animation_title"),
                Strings.GetArray("background_animation_values"),
                Strings.GetArray("background_animation"),
                settings.BackgroundAnimationMode.Id,
                value =>
                {
                    settings.BackgroundAnimationMode = BackgroundAnimationMode.GetInstance(value);
                    OfferRestart();
                },
                isFirst: true),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("settings_main_cards_fade_in_switch"),
                animationsEnabled ? on : unavailable,
                animationsEnabled ? off : unavailable,
                settings.IsCardsFadeInEnabled && animationsEnabled,
                value => settings.IsCardsFadeInEnabled = value,
                isEnabled: animationsEnabled),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("settings_main_cards_other_element_animations_switch"),
                animationsEnabled ? on : unavailable,
                animationsEnabled ? off : unavailable,
                settings.IsElementsAnimationEnabled && animationsEnabled,
                value => settings.IsElementsAnimationEnabled = value,
                isEnabled: animationsEnabled,
                isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("settings_main"), items, main.GoBack, () => MainScreen(main), () => main.NavigateTo(About(main)));
    }

    public static SettingsPageViewModel Notifications(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        string on = Strings.Get("settings_enabled");
        string off = Strings.Get("settings_disabled");

        // Alerts and precipitation are found by background updates, so they need them.
        bool backgroundUpdates = settings.UpdateInterval != UpdateInterval.Never;
        string offOrUnavailable = backgroundUpdates ? off : Strings.Get("settings_unavailable_no_background_updates");

        SwitchPreference prereleases = new(
            Strings.Get("app_updates_prereleases"), on, off,
            settings.AppUpdateCheckPrereleases ?? Updates.AppVersion.IsPrerelease,
            value => settings.AppUpdateCheckPrereleases = value,
            isEnabled: settings.IsAppUpdateCheckEnabled,
            isLast: true);

        TimePickerPreference todayTime = new(
            main, Strings.Get("settings_notifications_forecast_time_today_title"),
            settings.TodayForecastTime, value => settings.TodayForecastTime = value,
            isEnabled: settings.IsTodayForecastEnabled);
        TimePickerPreference tomorrowTime = new(
            main, Strings.Get("settings_notifications_forecast_time_tomorrow_title"),
            settings.TomorrowForecastTime, value => settings.TomorrowForecastTime = value,
            isEnabled: settings.IsTomorrowForecastEnabled,
            isLast: true);

        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("notification_channel_app_updates")),
            new SwitchPreference(
                Strings.Get("settings_notifications_app_updates_check"), on, off,
                settings.IsAppUpdateCheckEnabled,
                value =>
                {
                    settings.IsAppUpdateCheckEnabled = value;
                    prereleases.IsEnabled = value;
                },
                isFirst: true),
            SeparatorPreference.Small,
            prereleases,
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_notifications_section_general")),
            new SwitchPreference(
                Strings.Get("settings_notifications_alerts_title"), on, offOrUnavailable,
                settings.IsAlertPushEnabled && backgroundUpdates,
                value => settings.IsAlertPushEnabled = value,
                isEnabled: backgroundUpdates,
                isFirst: true),
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("settings_notifications_precipitations_title"), on, offOrUnavailable,
                settings.IsPrecipitationPushEnabled && backgroundUpdates,
                value => settings.IsPrecipitationPushEnabled = value,
                isEnabled: backgroundUpdates,
                isLast: true),
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("settings_notifications_section_forecast")),
            new SwitchPreference(
                Strings.Get("settings_notifications_forecast_today_title"), on, off,
                settings.IsTodayForecastEnabled,
                value =>
                {
                    settings.IsTodayForecastEnabled = value;
                    todayTime.IsEnabled = value;
                },
                isFirst: true),
            SeparatorPreference.Small,
            todayTime,
            SeparatorPreference.Small,
            new SwitchPreference(
                Strings.Get("settings_notifications_forecast_tomorrow_title"), on, off,
                settings.IsTomorrowForecastEnabled,
                value =>
                {
                    settings.IsTomorrowForecastEnabled = value;
                    tomorrowTime.IsEnabled = value;
                }),
            SeparatorPreference.Small,
            tomorrowTime,
        ];
        return new SettingsPageViewModel(Strings.Get("settings_notifications"), items, main.GoBack, () => Notifications(main), () => main.NavigateTo(About(main)));
    }

    // A server address: https, a host name and an optional path, ending with "/".
    private static readonly System.Text.RegularExpressions.Regex UrlRegex = new(
        "^https://(www[.])?[-a-zA-Z0-9@:%._+~#=]{1,256}[.][a-zA-Z0-9()]{1,6}([-a-zA-Z0-9()!@:%_+.~#?&//=]*)/$");

    public static SettingsPageViewModel WeatherSources(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        OpenMeteoService openMeteo = main.OpenMeteo;

        // Open-Meteo is the only worldwide forecast source; any other stored choice reads as automatic.
        List<(string Id, string Name)> worldwideSources = [(OpenMeteoService.Id, OpenMeteoService.Name)];
        string defaultSource = worldwideSources.Any(source => source.Id == settings.DefaultForecastSource)
            ? settings.DefaultForecastSource
            : "auto";

        string invalid = Strings.Get("settings_source_instance_invalid");
        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("settings_weather_sources_section_general")),
            new ListPreference(
                main,
                Strings.Get("settings_weather_sources_default_source"),
                ["auto", .. worldwideSources.Select(source => source.Id)],
                [Strings.Get("settings_automatic"), .. worldwideSources.Select(source => source.Name)],
                defaultSource,
                value => settings.DefaultForecastSource = value,
                isFirst: true,
                isLast: true),
            SeparatorPreference.Large,
            new SectionHeaderPreference(OpenMeteoService.Name),
            InstancePreference(
                main, "settings_weather_source_open_meteo_instance_forecast", openMeteo.ForecastInstance,
                OpenMeteoService.ForecastBaseUrl, invalid, value => openMeteo.ForecastInstance = value, isFirst: true),
            SeparatorPreference.Small,
            InstancePreference(
                main, "settings_weather_source_open_meteo_instance_air_quality", openMeteo.AirQualityInstance,
                OpenMeteoService.AirQualityBaseUrl, invalid, value => openMeteo.AirQualityInstance = value),
            SeparatorPreference.Small,
            InstancePreference(
                main, "settings_weather_source_open_meteo_instance_geocoding", openMeteo.GeocodingInstance,
                OpenMeteoService.GeocodingBaseUrl, invalid, value => openMeteo.GeocodingInstance = value, isLast: true),
        ];
        return new SettingsPageViewModel(
            Strings.Get("settings_weather_sources"), items, main.GoBack, () => WeatherSources(main), () => main.NavigateTo(About(main)));
    }

    // The field is empty while the default server is used; the default address then shows as the summary and the
    // hint, and saving the default or an empty field goes back to it.
    private static EditTextPreference InstancePreference(
        MainViewModel main,
        string titleKey,
        string? instance,
        string defaultUrl,
        string regexError,
        Action<string?> setInstance,
        bool isFirst = false,
        bool isLast = false)
    {
        return new EditTextPreference(
            main,
            Strings.Get(titleKey),
            content => content.Length == 0 ? defaultUrl : content,
            instance != defaultUrl ? instance : null,
            defaultUrl,
            UrlRegex,
            regexError,
            value => setInstance(value == defaultUrl || value.Length == 0 ? null : value),
            isFirst,
            isLast);
    }

    public static SettingsPageViewModel Modules(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        bool enabled = settings.IsWidgetNotificationEnabled;
        string on = Strings.Get("settings_enabled");
        string off = Strings.Get("settings_disabled");
        SwitchPreference persistent = new(
            Strings.Get("settings_modules_notification_persistent_switch"), on, off,
            settings.IsWidgetNotificationPersistent, value => settings.IsWidgetNotificationPersistent = value,
            isEnabled: enabled);
        ListPreference style = new(
            main, Strings.Get("settings_modules_notification_style_title"),
            Strings.GetArray("notification_style_values"), Strings.GetArray("notification_styles"),
            settings.WidgetNotificationStyle.Id,
            value => settings.WidgetNotificationStyle = NotificationStyle.GetInstance(value))
        {
            IsEnabled = enabled,
        };

        // The weather icon is a second tray icon beside the temperature, so it needs the temperature icon.
        SwitchPreference weatherIcon = new(
            Strings.Get("tray_weather_icon"), on, off,
            settings.IsTrayWeatherIconEnabled, value => settings.IsTrayWeatherIconEnabled = value,
            isEnabled: enabled && settings.IsWidgetNotificationTemperatureIconEnabled);
        SwitchPreference temperatureIcon = new(
            Strings.Get("tray_temperature_icon"), on, off,
            settings.IsWidgetNotificationTemperatureIconEnabled,
            value =>
            {
                settings.IsWidgetNotificationTemperatureIconEnabled = value;
                weatherIcon.IsEnabled = value;
            }, isEnabled: enabled);
        SwitchPreference feelsLike = new(
            Strings.Get("settings_modules_notification_feels_like_switch"), on, off,
            settings.IsWidgetNotificationUsingFeelsLike,
            value => settings.IsWidgetNotificationUsingFeelsLike = value, isEnabled: enabled);

        bool doubleClickEnabled = settings.IsTrayDoubleClickEnabled;
        SectionHeaderPreference accessibility = new(Strings.Get("tray_accessibility")) { IsVisible = doubleClickEnabled };

        // The field accepts anything Windows accepts for its own double-click time, up to 5,000 ms. The slider spans
        // Windows' own double-click speed slider, 100 to 900 ms. The recommended value is Windows' double-click time,
        // which the tray follows until a delay is chosen here.
        int recommendedDelay = System.Windows.Forms.SystemInformation.DoubleClickTime;
        SliderPreference delay = new(
            Strings.Get("tray_double_click_delay"), Strings.Get("tray_milliseconds"),
            settings.TrayDoubleClickDelay ?? recommendedDelay,
            minimum: 1, maximum: 5000, sliderMinimum: 100, sliderMaximum: 900, recommendedDelay,
            value => settings.TrayDoubleClickDelay = value,
            () => settings.TrayDoubleClickDelay = null,
            isEnabled: enabled, isFirst: true, isLast: true)
        {
            IsVisible = doubleClickEnabled,
        };
        SwitchPreference doubleClick = new(
            Strings.Get("tray_double_click"), on, off,
            doubleClickEnabled,
            value =>
            {
                settings.IsTrayDoubleClickEnabled = value;
                accessibility.IsVisible = value;
                delay.IsVisible = value;
            }, isEnabled: enabled, isLast: true);
        SwitchPreference notification = new(
            Strings.Get("tray_icon"), on, off,
            enabled, value =>
            {
                settings.IsWidgetNotificationEnabled = value;
                persistent.IsEnabled = value;
                style.IsEnabled = value;
                temperatureIcon.IsEnabled = value;
                weatherIcon.IsEnabled = value && settings.IsWidgetNotificationTemperatureIconEnabled;
                feelsLike.IsEnabled = value;
                doubleClick.IsEnabled = value;
                delay.IsEnabled = value;
            }, isFirst: true);
        List<object> items =
        [
            new SectionHeaderPreference(Strings.Get("tray_section")),
            notification,
            SeparatorPreference.Small,
            persistent,
            SeparatorPreference.Small,
            style,
            SeparatorPreference.Small,
            temperatureIcon,
            SeparatorPreference.Small,
            weatherIcon,
            SeparatorPreference.Small,
            feelsLike,
            SeparatorPreference.Small,
            doubleClick,
            accessibility,
            delay,
        ];
        return new SettingsPageViewModel(Strings.Get("settings_modules"), items, main.GoBack, () => Modules(main), () => main.NavigateTo(About(main)));
    }

    public static SettingsPageViewModel Units(MainViewModel main)
    {
        SettingsManager settings = main.Settings;
        string region = main.RegionCode;

        string defaultPrecipitation = PrecipitationUnit.GetDefaultUnit(region) == PrecipitationUnit.GetDefaultSnowfallUnit(region)
            ? Name(PrecipitationUnit.GetDefaultUnit(region))
            : Name(PrecipitationUnit.GetDefaultUnit(region)) + "/" + Name(PrecipitationUnit.GetDefaultSnowfallUnit(region));

        List<object> items =
        [
            UnitList(
                main,
                "settings_units_temperature",
                TemperatureUnit.All.Where(unit => unit != TemperatureUnit.DeciCelsius),
                Name(TemperatureUnit.GetDefaultUnit(region)),
                settings.TemperatureUnit,
                id => settings.TemperatureUnit = TemperatureUnit.GetUnit(id),
                isFirst: true),
            SeparatorPreference.Small,
            UnitList(
                main,
                "settings_units_precipitation",
                PrecipitationUnit.All.Where(unit => unit != PrecipitationUnit.Micrometer),
                defaultPrecipitation,
                settings.PrecipitationUnit,
                id => settings.PrecipitationUnit = PrecipitationUnit.GetUnit(id)),
            SeparatorPreference.Small,
            UnitList(
                main,
                "settings_units_speed",
                SpeedUnit.All.Where(unit => unit != SpeedUnit.CentimeterPerSecond),
                Name(SpeedUnit.GetDefaultUnit(region)),
                settings.SpeedUnit,
                id => settings.SpeedUnit = SpeedUnit.GetUnit(id)),
            SeparatorPreference.Small,
            UnitList(
                main,
                "settings_units_distance",
                DistanceUnit.All,
                Name(DistanceUnit.GetDefaultUnit(region)),
                settings.DistanceUnit,
                id => settings.DistanceUnit = DistanceUnit.GetUnit(id)),
            SeparatorPreference.Small,
            UnitList(
                main,
                "settings_units_pressure",
                PressureUnit.All.Where(unit => unit != PressureUnit.Pascal),
                Name(PressureUnit.GetDefaultUnit(region)),
                settings.PressureUnit,
                id => settings.PressureUnit = PressureUnit.GetUnit(id),
                isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("settings_units"), items, main.GoBack, () => Units(main), () => main.NavigateTo(About(main)));
    }

    // A stored calendar that is no longer offered selects the regional preference.
    private static ListPreference CalendarPreference(MainViewModel main)
    {
        IReadOnlyList<CalendarHelper.AlternateCalendar> calendars = CalendarHelper.GetCalendars();
        string current = main.Settings.AlternateCalendar;
        return new ListPreference(
            main,
            Strings.Get("settings_appearance_calendar_title"),
            calendars.Select(calendar => calendar.Id).ToList(),
            calendars.Select(calendar => calendar.DisplayName).ToList(),
            calendars.FirstOrDefault(calendar => calendar.Id == current)?.Id ?? "",
            value => main.Settings.AlternateCalendar = value,
            isLast: true);
    }

    private static string DarkModeLocationId(SettingsManager settings, DarkMode darkMode)
    {
        return darkMode == DarkMode.Light || settings.DayNightModeForLocations ? "day_night" : "dark";
    }

    // "auto" follows the region, and its name says which unit that gives, such as "Regional preference (°C)".
    // The setter receives the chosen identifier, for which an unknown unit means "auto".
    private static ListPreference UnitList(
        MainViewModel main,
        string titleKey,
        IEnumerable<WeatherUnit> units,
        string regionalUnitName,
        WeatherUnit? selected,
        Action<string> setUnit,
        bool isFirst = false,
        bool isLast = false)
    {
        List<WeatherUnit> allowed = units.ToList();
        List<string> values = [AutoUnit, .. allowed.Select(unit => unit.Id)];
        List<string> names =
        [
            Strings.Format("parenthesis", Strings.Get("settings_regional_preference"), regionalUnitName),
            .. allowed.Select(Name),
        ];
        return new ListPreference(main, Strings.Get(titleKey), values, names, selected?.Id ?? AutoUnit, setUnit, isFirst, isLast);
    }

    // Units are named in full, such as "kilometers per hour": with CLDR's names through ICU, or with the translated
    // unit strings without it.
    private static string Name(WeatherUnit unit)
    {
        return UnitFormatting.GetDisplayName(unit, UnitWidth.Long);
    }

    private static ClickablePreference NotPorted(
        string titleKey,
        string summaryKey,
        string iconKey,
        bool isFirst = false,
        bool isLast = false)
    {
        return new ClickablePreference(
            Strings.Get(titleKey), Strings.Get(summaryKey), onClick: null, iconKey, isFirst, isLast, isEnabled: false);
    }

    // A language's name in that language, starting with a capital, such as "Français".
    private static string NativeName(string code)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(code);
        string name = culture.NativeName;
        return name.Length == 0 ? code : culture.TextInfo.ToUpper(name[0]) + name[1..];
    }
}
