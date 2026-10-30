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
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.ViewModels.Settings;

/// <summary>
/// The settings screens, in Breezy Weather's order. Categories not ported yet are shown but cannot be opened.
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
            NotPorted("settings_main", "settings_main_summary", "IconHome", isLast: true),
            SeparatorPreference.Large,
            NotPorted("settings_notifications", "settings_notifications_summary", "IconNotifications", isFirst: true),
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
            NotPorted("settings_weather_sources", "settings_weather_sources_summary", "IconFactory", isLast: true),
            SeparatorPreference.Large,
            NotPorted("settings_debug", "settings_debug_summary", "IconBugReport", isFirst: true, isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("action_settings"), items, main.GoBack, () => Root(main));
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
            Strings.Get("settings_background_updates"), items, main.GoBack, () => BackgroundUpdates(main));
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
                () => main.NavigateTo(Units(main)),
                isLast: true),
            SeparatorPreference.Large,
            new SectionHeaderPreference(Strings.Get("desktop_interface_section")),
            new SwitchPreference(
                Strings.Get("desktop_interface_title"),
                Strings.Get("desktop_interface_on"),
                Strings.Get("desktop_interface_off"),
                main.Settings.DesktopInterfaceEnabled,
                value => main.Settings.DesktopInterfaceEnabled = value,
                isFirst: true,
                isLast: true),
        ];
        return new SettingsPageViewModel(Strings.Get("settings_appearance"), items, main.GoBack, () => Appearance(main));
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
        return new SettingsPageViewModel(Strings.Get("settings_modules"), items, main.GoBack, () => Modules(main));
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
        return new SettingsPageViewModel(Strings.Get("settings_units"), items, main.GoBack, () => Units(main));
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

    // Breezy Weather names units with Android's CLDR data, such as "kilometers per hour". That data is not available,
    // so the names come from the translated unit strings, as in Breezy Weather when its "use measure format" is off.
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
