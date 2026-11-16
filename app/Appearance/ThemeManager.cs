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
using System.Windows.Media;
using LibreccioWeather.Core.MaterialColors;
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
    private static readonly Uri DarkColors = new("pack://application:,,,/Theme/DarkColors.xaml", UriKind.Absolute);
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
    /// The light or dark palette of the app's screens, merged into one screen's resources so that screen follows a
    /// location's daylight instead of the app's mode, with the accent colors over it while they are on.
    /// </summary>
    public static ResourceDictionary GetScreenColors(bool light)
    {
        var palette = new ResourceDictionary { Source = light ? LightColors : DarkColors };
        if (CurrentAccentColor() is not { } accent)
        {
            return palette;
        }

        // A later merged dictionary takes precedence over an earlier one.
        var colors = new ResourceDictionary();
        colors.MergedDictionaries.Add(palette);
        colors.MergedDictionaries.Add(GetAccentColors(accent, dark: !light));
        return colors;
    }

    /// <summary>
    /// The source color of the accent colors in use, or null while they are off.
    /// </summary>
    public static int? CurrentAccentColor()
    {
        return (Application.Current?.MainWindow?.DataContext as ViewModels.MainViewModel)?.AccentColor;
    }

    /// <summary>
    /// The source color the accent colors are built from: Windows' accent color while the setting is on, or null.
    /// </summary>
    public static int? GetAccentColor(SettingsManager settings)
    {
        return settings.IsAccentColorsEnabled ? ReadSystemAccentColor() : null;
    }

    /// <summary>
    /// Windows' accent color as opaque ARGB, or null when it cannot be read. The window manager stores it as
    /// AccentColor, a DWORD in 0xAABBGGRR order.
    /// </summary>
    public static int? ReadSystemAccentColor()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
        if (key?.GetValue("AccentColor") is not int abgr)
        {
            return null;
        }

        int red = abgr & 0xFF;
        int green = (abgr >> 8) & 0xFF;
        int blue = (abgr >> 16) & 0xFF;
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }

    /// <summary>
    /// The Material roles of the app's palette built from a source color with Material's tonal spot scheme at standard
    /// contrast, which Android's wallpaper colors use. Keys without a Material role keep the palette's own colors.
    /// </summary>
    public static ResourceDictionary GetAccentColors(int sourceColor, bool dark)
    {
        var scheme = new SchemeTonalSpot(Hct.FromInt(sourceColor), dark, 0.0);
        var colors = new ResourceDictionary();
        Add(colors, "SurfaceBrush", scheme.Surface);
        Add(colors, "SurfaceContainerLowBrush", scheme.SurfaceContainerLow);
        Add(colors, "SurfaceContainerBrush", scheme.SurfaceContainer);
        Add(colors, "SurfaceContainerHighBrush", scheme.SurfaceContainerHigh);
        Add(colors, "OnSurfaceBrush", scheme.OnSurface);
        Add(colors, "OnSurfaceVariantBrush", scheme.OnSurfaceVariant);
        Add(colors, "OutlineBrush", scheme.Outline);
        Add(colors, "OutlineVariantBrush", scheme.OutlineVariant);
        Add(colors, "PrimaryBrush", scheme.Primary);
        Add(colors, "OnPrimaryBrush", scheme.OnPrimary);
        Add(colors, "PrimaryContainerBrush", scheme.PrimaryContainer);
        Add(colors, "OnPrimaryContainerBrush", scheme.OnPrimaryContainer);
        Add(colors, "SecondaryContainerBrush", scheme.SecondaryContainer);
        Add(colors, "OnSecondaryContainerBrush", scheme.OnSecondaryContainer);
        Add(colors, "TertiaryBrush", scheme.Tertiary);
        Add(colors, "OnTertiaryBrush", scheme.OnTertiary);
        Add(colors, "TertiaryContainerBrush", scheme.TertiaryContainer);
        Add(colors, "OnTertiaryContainerBrush", scheme.OnTertiaryContainer);
        Add(colors, "ErrorBrush", scheme.Error);
        Add(colors, "ErrorContainerBrush", scheme.ErrorContainer);
        Add(colors, "OnErrorContainerBrush", scheme.OnErrorContainer);
        Add(colors, "InverseSurfaceBrush", scheme.InverseSurface);
        Add(colors, "InverseOnSurfaceBrush", scheme.InverseOnSurface);
        Add(colors, "InversePrimaryBrush", scheme.InversePrimary);

        // Material's state layers: the content color at 8 % for hover and 10 % for a press.
        Add(colors, "HoverLayerBrush", (scheme.OnSurface & 0x00FFFFFF) | (0x14 << 24));
        Add(colors, "PressedLayerBrush", (scheme.OnSurface & 0x00FFFFFF) | (0x1A << 24));

        // Compose's surfaceColorAtElevation at 2 dp: the primary color at (4.5 ln 3 + 2) %, about 6.94 %, over the
        // surface, mixed per sRGB channel as Color.compositeOver does.
        Add(colors, "ElevatedSurfaceBrush", CompositeOver(scheme.Primary, (4.5f * MathF.Log(2f + 1f) + 2f) / 100f, scheme.Surface));
        return colors;
    }

    private static void Add(ResourceDictionary colors, string key, int argb)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
            (byte)((uint)argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb));
        brush.Freeze();
        colors[key] = brush;
    }

    private static int CompositeOver(int foreground, float alpha, int background)
    {
        static int Channel(int fg, int bg, float alpha) => (int)(((fg / 255f * alpha) + (bg / 255f * (1f - alpha))) * 255f + 0.5f);

        int red = Channel((foreground >> 16) & 0xFF, (background >> 16) & 0xFF, alpha);
        int green = Channel((foreground >> 8) & 0xFF, (background >> 8) & 0xFF, alpha);
        int blue = Channel(foreground & 0xFF, background & 0xFF, alpha);
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
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
    /// the element and everything in it use it in place of the app's palette, with the accent colors over it while
    /// they are on.
    /// </summary>
    public static ResourceDictionary GetViewColors(bool light)
    {
        var palette = new ResourceDictionary { Source = light ? BreezyLightColors : BreezyDarkColors };
        if (CurrentAccentColor() is not { } accent)
        {
            return palette;
        }

        // Keys the view palette lacks fall through to the application's resources, where the accent roles of the app's
        // screens sit; the app's own palette keeps them as they are without the accent colors. A later merged
        // dictionary takes precedence over an earlier one.
        bool appDark = (Application.Current?.MainWindow?.DataContext as ViewModels.MainViewModel)?.IsAppDark ?? true;
        var appPalette = new ResourceDictionary { Source = appDark ? DarkColors : LightColors };
        var fallthrough = new ResourceDictionary();
        foreach (object key in appPalette.Keys)
        {
            if (!palette.Contains(key))
            {
                fallthrough[key] = appPalette[key];
            }
        }

        var colors = new ResourceDictionary();
        colors.MergedDictionaries.Add(fallthrough);
        colors.MergedDictionaries.Add(palette);
        colors.MergedDictionaries.Add(GetViewAccentColors(accent, light));
        return colors;
    }

    /// <summary>
    /// The view palette's colors taken from the system palettes on Android 12 and later, built from a source color.
    /// The system palettes are those of Material's tonal spot scheme, and shade n of a palette is its tone
    /// 100 - n / 10. The error colors stay fixed, as they do there.
    /// </summary>
    public static ResourceDictionary GetViewAccentColors(int sourceColor, bool light)
    {
        var scheme = new SchemeTonalSpot(Hct.FromInt(sourceColor), !light, 0.0);
        int Accent1(int shade) => scheme.PrimaryPalette.Tone(100 - shade / 10);
        int Accent2(int shade) => scheme.SecondaryPalette.Tone(100 - shade / 10);
        int Accent3(int shade) => scheme.TertiaryPalette.Tone(100 - shade / 10);
        int Neutral1(int shade) => scheme.NeutralPalette.Tone(100 - shade / 10);
        int Neutral2(int shade) => scheme.NeutralVariantPalette.Tone(100 - shade / 10);

        // The shades of values-v31/colors.xml, then of values-night-v31/colors.xml.
        int surface = light ? Neutral1(10) : Neutral1(900);
        int onSurface = light ? Neutral1(900) : Neutral1(100);
        int primary = light ? Accent1(600) : Accent1(200);
        var colors = new ResourceDictionary();
        Add(colors, "SurfaceBrush", surface);
        Add(colors, "OnSurfaceBrush", onSurface);
        Add(colors, "OnSurfaceVariantBrush", light ? Neutral2(700) : Neutral2(200));
        Add(colors, "PrimaryBrush", primary);
        Add(colors, "OnPrimaryBrush", light ? Accent1(0) : Accent1(800));
        Add(colors, "PrimaryContainerBrush", light ? Accent1(100) : Accent1(700));
        Add(colors, "OnPrimaryContainerBrush", light ? Accent1(900) : Accent1(100));
        Add(colors, "SecondaryContainerBrush", light ? Accent2(100) : Accent2(700));
        Add(colors, "OnSecondaryContainerBrush", light ? Accent2(900) : Accent2(100));
        Add(colors, "TertiaryBrush", light ? Accent3(600) : Accent3(200));
        Add(colors, "OnTertiaryBrush", light ? Accent3(0) : Accent3(800));
        Add(colors, "TertiaryContainerBrush", light ? Accent3(100) : Accent3(700));
        Add(colors, "OnTertiaryContainerBrush", light ? Accent3(900) : Accent3(100));
        Add(colors, "InverseSurfaceBrush", light ? Neutral1(900) : Neutral1(100));
        Add(colors, "InverseOnSurfaceBrush", light ? Neutral1(100) : Neutral1(900));
        Add(colors, "InversePrimaryBrush", light ? Accent1(600) : Accent1(900));

        // The on-surface color at 8 % for hover.
        Add(colors, "HoverLayerBrush", (onSurface & 0x00FFFFFF) | (0x14 << 24));

        // getWidgetSurfaceColor at 2 dp: the primary color at an alpha of (4.5 ln 3 + 2) / 100 * 255 truncated to
        // 17, blended over the surface in whole channel values.
        Add(colors, "ElevatedSurfaceBrush", BlendColor(primary, (int)((4.5 * Math.Log(2.0 + 1.0) + 2.0) / 100.0 * 255), surface));
        return colors;
    }

    private static int BlendColor(int foreground, int alpha, int background)
    {
        static int Channel(int fg, int bg, int alpha) => bg * (0xff - alpha) / 0xff + fg * alpha / 0xff;

        int red = Channel((foreground >> 16) & 0xFF, (background >> 16) & 0xFF, alpha);
        int green = Channel((foreground >> 8) & 0xFF, (background >> 8) & 0xFF, alpha);
        int blue = Channel(foreground & 0xFF, background & 0xFF, alpha);
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }
}
