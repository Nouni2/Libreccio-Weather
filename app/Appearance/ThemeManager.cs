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

using System.Windows;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using Microsoft.Win32;

namespace LibreccioWeather.Appearance;

/// <summary>
/// Decides between the light and dark palettes, for the app and for the location on the main screen.
/// </summary>
public static class ThemeManager
{
    private static readonly Uri LightColors = new("pack://application:,,,/Theme/LightColors.xaml", UriKind.Absolute);
    private static readonly Uri BreezyLightColors = new("pack://application:,,,/Theme/BreezyLightColors.xaml", UriKind.Absolute);
    private static readonly Uri BreezyDarkColors = new("pack://application:,,,/Theme/BreezyDarkColors.xaml", UriKind.Absolute);

    /// <summary>
    /// Whether the app's screens use the dark palette: the dark mode setting, or Windows' app mode when it follows
    /// the system.
    /// </summary>
    public static bool IsAppDark(SettingsManager settings)
    {
        return settings.DarkMode == DarkMode.Dark
            || (settings.DarkMode == DarkMode.System && IsSystemInDarkMode());
    }

    /// <summary>
    /// Whether the main screen of a location uses the light palette. With "Always light", locations are light during
    /// the day and dark at night. With "Always dark", they stay dark unless day/night mode for locations is on. When
    /// following the system, the first rule applies in light mode and the second in dark mode.
    /// </summary>
    public static bool IsLightTheme(SettingsManager settings, bool? daylight)
    {
        if (settings.DarkMode == DarkMode.Light)
        {
            return daylight == true;
        }

        if (settings.DarkMode == DarkMode.Dark)
        {
            return settings.DayNightModeForLocations && daylight == true;
        }

        return IsSystemInDarkMode()
            ? settings.DayNightModeForLocations && daylight == true
            : daylight == true;
    }

    /// <summary>
    /// Windows' "Choose your default app mode". It is stored as AppsUseLightTheme, 0 for dark; light is assumed when
    /// it cannot be read.
    /// </summary>
    public static bool IsSystemInDarkMode()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    /// <summary>
    /// The light palette of the app's screens, merged into the application's resources over the dark default.
    /// </summary>
    public static ResourceDictionary GetLightColors()
    {
        return new ResourceDictionary { Source = LightColors };
    }

    /// <summary>
    /// Gives a view-based screen the view palette that matches the app's light or dark mode. The screen is built again
    /// when the mode changes, so this runs once per screen.
    /// </summary>
    public static void ApplyViewColors(FrameworkElement element)
    {
        bool dark = (Application.Current.MainWindow?.DataContext as ViewModels.MainViewModel)?.IsAppDark ?? true;
        element.Resources.MergedDictionaries.Add(GetViewColors(light: !dark));
    }

    /// <summary>
    /// The palette of the main screen, the location list and the order editors, merged into an element's resources so
    /// the element and everything in it use it in place of the app's palette.
    /// </summary>
    public static ResourceDictionary GetViewColors(bool light)
    {
        return new ResourceDictionary { Source = light ? BreezyLightColors : BreezyDarkColors };
    }
}
