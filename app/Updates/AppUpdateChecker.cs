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

using System.Diagnostics;
using System.Windows.Threading;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Updates;
using LibreccioWeather.ViewModels;

namespace LibreccioWeather.Updates;

/// <summary>
/// Checks GitHub for a newer release of Libreccio Weather and tells the user about it.
/// </summary>
public sealed class AppUpdateChecker(SettingsManager settings, GetApplicationRelease getApplicationRelease, Dispatcher dispatcher)
{
    /// <summary>
    /// The window's view model, which shows the message about a new release.
    /// </summary>
    public MainViewModel? Main { get; set; }

    public async Task<GetApplicationRelease.Result> CheckForUpdateAsync(bool forceCheck = false)
    {
        GetApplicationRelease.Result result = await getApplicationRelease.AwaitAsync(
            new GetApplicationRelease.Arguments(
                AppVersion.VersionName,
                AppVersion.GithubOrg,
                AppVersion.GithubRepo,
                settings.AppUpdateCheckPrereleases ?? AppVersion.IsPrerelease,
                forceCheck)).ConfigureAwait(false);

        if (result is GetApplicationRelease.Result.NewUpdate update)
        {
            await dispatcher.InvokeAsync(() => PromptUpdate(update.Release));
        }

        return result;
    }

    /// <summary>
    /// Checks when automatic checks are on. A failure is only logged, since nobody asked for this check.
    /// </summary>
    public async Task CheckIfEnabledAsync()
    {
        if (!settings.IsAppUpdateCheckEnabled)
        {
            return;
        }

        try
        {
            await CheckForUpdateAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Update check failed: {0}", exception.Message);
        }
    }

    // The release page opens from the message, as the notification's Download action opens it.
    private void PromptUpdate(Release release)
    {
        Main?.ShowSnackbar(
            Strings.Get("notification_app_update_available") + " " + release.Version,
            Strings.Get("action_download"),
            () => LinkOpener.Open(release.ReleaseLink));
    }
}
