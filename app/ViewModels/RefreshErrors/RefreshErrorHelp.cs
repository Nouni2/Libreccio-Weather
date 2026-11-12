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
using System.Diagnostics;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels.RefreshErrors;

/// <summary>
/// What each refresh error shows: its short message, and for some errors a help dialog opened from the message's button.
/// </summary>
public static class RefreshErrorHelp
{
    // Windows' location privacy page, where location access is allowed for the device and for desktop apps.
    private const string LocationSettingsUri = "ms-settings:privacy-location";

    public static string GetShortMessage(RefreshErrorType error)
    {
        return Strings.Get(error switch
        {
            RefreshErrorType.NetworkUnavailable => "message_network_unavailable",
            RefreshErrorType.ServerTimeout => "message_server_timeout",
            RefreshErrorType.ApiKeyRequiredMissing => "weather_api_key_required_missing_title",
            RefreshErrorType.ApiLimitReached => "weather_api_limit_reached_title",
            RefreshErrorType.ApiUnauthorized => "weather_api_unauthorized_title",
            RefreshErrorType.ServerUnavailable => "message_server_unavailable_title",
            RefreshErrorType.ServerInsecure => "message_server_insecure_title",
            RefreshErrorType.ParsingError => "message_parsing_error_title",
            RefreshErrorType.SourceNotInstalled => "message_source_not_installed_error_title",
            RefreshErrorType.NonFreeNetworkSource => "message_source_non_free_network_title",
            RefreshErrorType.LocationFailed => "location_message_failed_to_locate",
            RefreshErrorType.AccessLocationPermissionMissing => "location_message_permission_missing",
            RefreshErrorType.AccessBackgroundLocationPermissionMissing => "location_message_permission_background_missing",
            RefreshErrorType.LocationAccessOff => "location_message_location_access_off",
            RefreshErrorType.ReverseGeocodingFailed => "location_message_reverse_geocoding_failed",
            RefreshErrorType.LocationSearchFailed => "location_message_search_failed",
            RefreshErrorType.InvalidLocation => "weather_message_invalid_location",
            RefreshErrorType.UnsupportedFeature => "weather_message_unsupported_feature",
            RefreshErrorType.InvalidIncompleteData => "message_invalid_incomplete_data",
            RefreshErrorType.OutdatedServerData => "message_outdated_server_data",
            RefreshErrorType.IncompatibleForecastTimes => "message_incompatible_times_title",
            RefreshErrorType.DataRefreshFailed => "weather_message_data_refresh_failed",
            _ => throw new InvalidEnumArgumentException(nameof(error), (int)error, typeof(RefreshErrorType)),
        });
    }

    /// <summary>
    /// The text of the message's button, or null for an error whose message has no button.
    /// </summary>
    public static string? GetActionButtonText(RefreshErrorType error)
    {
        return error switch
        {
            RefreshErrorType.ApiKeyRequiredMissing or RefreshErrorType.ApiLimitReached or RefreshErrorType.ApiUnauthorized
                or RefreshErrorType.ServerUnavailable or RefreshErrorType.ParsingError or RefreshErrorType.LocationFailed
                or RefreshErrorType.IncompatibleForecastTimes => Strings.Get("action_help"),
            RefreshErrorType.SourceNotInstalled or RefreshErrorType.NonFreeNetworkSource => Strings.Get("action_change"),
            RefreshErrorType.AccessLocationPermissionMissing or RefreshErrorType.AccessBackgroundLocationPermissionMissing =>
                Strings.Get("action_allow"),
            RefreshErrorType.LocationAccessOff => Strings.Get("action_enable"),
            _ => null,
        };
    }

    /// <summary>
    /// What the message's button does: a help dialog, or the screen where the problem is solved.
    /// </summary>
    public static void ShowDialog(RefreshErrorType error, MainViewModel main)
    {
        // The main screen and the location list stand for the activity that can edit the shown location.
        bool onMainScreen = main.CurrentPage is WeatherViewModel or ManagementViewModel;
        Action openSourceSettings = () => main.NavigateTo(SettingsPages.WeatherSources(main));
        Action editLocation = () => main.WeatherPage?.EditLocationCommand.Execute(null);
        switch (error)
        {
            case RefreshErrorType.ApiKeyRequiredMissing:
                ShowErrorHelp(main, "weather_api_key_required_missing_title", "weather_api_key_required_missing_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ApiLimitReached:
                ShowErrorHelp(main, "weather_api_limit_reached_title", "weather_api_limit_reached_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ApiUnauthorized:
                ShowErrorHelp(main, "weather_api_unauthorized_title", "weather_api_unauthorized_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ServerUnavailable:
                ShowErrorHelp(main, "message_server_unavailable_title", "message_server_unavailable_content");
                break;
            case RefreshErrorType.ParsingError:
                ShowErrorHelp(main, "message_parsing_error_title", "message_parsing_error_content");
                break;
            case RefreshErrorType.SourceNotInstalled:
                ShowErrorHelp(main, "message_source_not_installed_error_title", "message_source_not_installed_error_content",
                    onMainScreen ? "action_change" : null, onMainScreen ? editLocation : null);
                break;
            case RefreshErrorType.NonFreeNetworkSource:
                ShowErrorHelp(main, "message_source_non_free_network_title", "message_source_non_free_network_content",
                    onMainScreen ? "action_change" : null, onMainScreen ? editLocation : null);
                break;
            case RefreshErrorType.LocationFailed:
                main.ShowDialog(new LocationHelpDialogViewModel(main));
                break;
            case RefreshErrorType.AccessLocationPermissionMissing or RefreshErrorType.AccessBackgroundLocationPermissionMissing
                or RefreshErrorType.LocationAccessOff:
                OpenLocationSettings();
                break;
            case RefreshErrorType.IncompatibleForecastTimes:
                // The button is offered everywhere, but only edits a location from the main screen.
                ShowErrorHelp(main, "message_incompatible_times_title", "message_incompatible_times_content",
                    "action_change", () =>
                    {
                        if (onMainScreen)
                        {
                            editLocation();
                        }
                    });
                break;
        }
    }

    private static void ShowErrorHelp(
        MainViewModel main, string titleKey, string contentKey, string? actionKey = null, Action? onAction = null)
    {
        main.ShowDialog(new ErrorHelpDialogViewModel(
            Strings.Get(titleKey),
            Strings.Get(contentKey),
            actionKey is not null && onAction is not null ? Strings.Get(actionKey) : null,
            onAction is null ? null : () =>
            {
                main.CloseDialog();
                onAction();
            },
            main.CloseDialog));
    }

    private static void OpenLocationSettings()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(LocationSettingsUri) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            Trace.TraceWarning("Could not open {0}: {1}", LocationSettingsUri, exception.Message);
        }
    }
}
