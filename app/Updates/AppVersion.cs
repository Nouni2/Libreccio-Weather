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

using System.Reflection;

namespace LibreccioWeather.Updates;

/// <summary>
/// The running version and where its releases are published.
/// </summary>
public static class AppVersion
{
    public const string GithubOrg = "Nouni2";

    public const string GithubRepo = "Libreccio-Weather";

    public const string SourceCodeLink = "https://github.com/" + GithubOrg + "/" + GithubRepo;

    /// <summary>
    /// The README's "Data and privacy" section, which says what the app sends and stores.
    /// </summary>
    public const string PrivacyPolicyLink = SourceCodeLink + "#data-and-privacy";

    // The informational version is the project version, followed after a "+" by the commit it was built from when the
    // build knows it.
    private static readonly string InformationalVersion =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "";

    /// <summary>
    /// The project version, such as "0.1.0-alpha1".
    /// </summary>
    public static string VersionName { get; } = InformationalVersion.Split('+')[0];

    /// <summary>
    /// The first eight characters of the commit, as "git rev-parse --short=8" gives them, or null when unknown.
    /// </summary>
    public static string? CommitSha { get; } =
        InformationalVersion.IndexOf('+') is var plus and >= 0 && InformationalVersion.Length > plus + 1
            ? InformationalVersion[(plus + 1)..][..Math.Min(8, InformationalVersion.Length - plus - 1)]
            : null;

    public static bool IsPrerelease => VersionName.Contains('-');

    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
