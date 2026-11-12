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
using CommunityToolkit.Mvvm.ComponentModel;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.ViewModels.Locations;
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
    /// The source the error came from, followed by the feature in parentheses when known, or null without a source.
    /// </summary>
    public static string? GetSourceWithOptionalFeature(RefreshError error, SourceManager sources)
    {
        if (string.IsNullOrEmpty(error.Source))
        {
            return null;
        }

        string sourceName = sources.GetSource(error.Source)?.Name ?? error.Source;
        return error.Feature is { } feature
            ? Strings.Format("parenthesis", sourceName, Strings.Get(SecondarySourcesDialogViewModel.FeatureNameKey(feature)))
            : sourceName;
    }

    /// <summary>
    /// The short message, after the source and feature when the error has a source.
    /// </summary>
    public static string GetMessage(RefreshError error, SourceManager sources)
    {
        return GetSourceWithOptionalFeature(error, sources) is { } source
            ? source + Strings.Get("colon_separator") + GetShortMessage(error.Error)
            : GetShortMessage(error.Error);
    }

    /// <summary>
    /// The message after a refresh: several errors offer a list of them, and a single one shows its message, with its
    /// button when it has help. No error shows nothing.
    /// </summary>
    public static void ShowErrors(IReadOnlyList<RefreshError> errors, MainViewModel main)
    {
        if (errors.Count > 1)
        {
            main.ShowSnackbar(
                Strings.Get("message_multiple_refresh_errors"),
                Strings.Get("action_show"),
                () => main.ShowDialog(new RefreshErrorDetailsDialogViewModel(main, errors)));
        }
        else if (errors.Count == 1)
        {
            RefreshError error = errors[0];
            string message = GetMessage(error, main.Sources);
            if (GetActionButtonText(error.Error) is { } actionText)
            {
                main.ShowSnackbar(message, actionText, () => ShowDialog(error.Error, main));
            }
            else
            {
                main.ShowSnackbar(message);
            }
        }
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
    /// <param name="returnTo">The dialog shown again when the help dialog is closed, such as the list of errors it was
    /// opened from; only one dialog shows at a time.</param>
    public static void ShowDialog(RefreshErrorType error, MainViewModel main, ObservableObject? returnTo = null)
    {
        // The main screen and the location list stand for the activity that can edit the shown location.
        bool onMainScreen = main.CurrentPage is WeatherViewModel or ManagementViewModel;
        Action close = returnTo is null ? main.CloseDialog : () => main.ShowDialog(returnTo);
        Action openSourceSettings = () => main.NavigateTo(SettingsPages.WeatherSources(main));
        Action editLocation = () => main.WeatherPage?.EditLocationCommand.Execute(null);
        switch (error)
        {
            case RefreshErrorType.ApiKeyRequiredMissing:
                ShowErrorHelp(main, close, "weather_api_key_required_missing_title", "weather_api_key_required_missing_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ApiLimitReached:
                ShowErrorHelp(main, close, "weather_api_limit_reached_title", "weather_api_limit_reached_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ApiUnauthorized:
                ShowErrorHelp(main, close, "weather_api_unauthorized_title", "weather_api_unauthorized_content",
                    "action_settings", openSourceSettings);
                break;
            case RefreshErrorType.ServerUnavailable:
                ShowErrorHelp(main, close, "message_server_unavailable_title", "message_server_unavailable_content");
                break;
            case RefreshErrorType.ParsingError:
                ShowErrorHelp(main, close, "message_parsing_error_title", "message_parsing_error_content");
                break;
            case RefreshErrorType.SourceNotInstalled:
                ShowErrorHelp(main, close, "message_source_not_installed_error_title", "message_source_not_installed_error_content",
                    onMainScreen ? "action_change" : null, onMainScreen ? editLocation : null);
                break;
            case RefreshErrorType.NonFreeNetworkSource:
                ShowErrorHelp(main, close, "message_source_non_free_network_title", "message_source_non_free_network_content",
                    onMainScreen ? "action_change" : null, onMainScreen ? editLocation : null);
                break;
            case RefreshErrorType.LocationFailed:
                main.ShowDialog(new LocationHelpDialogViewModel(main, close));
                break;
            case RefreshErrorType.AccessLocationPermissionMissing or RefreshErrorType.AccessBackgroundLocationPermissionMissing
                or RefreshErrorType.LocationAccessOff:
                OpenLocationSettings();
                break;
            case RefreshErrorType.IncompatibleForecastTimes:
                // The button is offered everywhere, but only edits a location from the main screen.
                ShowErrorHelp(main, close, "message_incompatible_times_title", "message_incompatible_times_content",
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

    // The action leaves every dialog, as it leads to another screen or edits the location.
    private static void ShowErrorHelp(
        MainViewModel main, Action close, string titleKey, string contentKey, string? actionKey = null, Action? onAction = null)
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
            close));
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
