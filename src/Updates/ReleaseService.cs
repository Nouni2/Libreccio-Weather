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

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace LibreccioWeather.Core.Updates;

/// <summary>
/// Reads the releases of a GitHub repository.
/// </summary>
public sealed class ReleaseService(HttpClient httpClient)
{
    private const string BaseUrl = "https://api.github.com/";

    /// <summary>
    /// The newest published release, or null when there is none. GitHub's "latest release" only ever names a full
    /// release, so the list is read instead and pre-releases are taken only when asked for. The list comes newest
    /// first, which is also the order GitHub's "latest release" follows.
    /// </summary>
    public async Task<Release?> LatestAsync(
        string org,
        string repository,
        bool includePrereleases,
        CancellationToken cancellationToken = default)
    {
        string url = $"{BaseUrl}repos/{Uri.EscapeDataString(org)}/{Uri.EscapeDataString(repository)}/releases";
        List<GithubRelease> releases = await httpClient
            .GetFromJsonAsync<List<GithubRelease>>(url, cancellationToken)
            .ConfigureAwait(false) ?? [];

        GithubRelease? latest = releases.FirstOrDefault(release => !release.Draft && (includePrereleases || !release.Prerelease));
        return latest is null
            ? null
            : new Release(
                latest.Version,
                latest.Info ?? "",
                latest.ReleaseLink,
                latest.Prerelease,
                latest.Assets?.Select(asset => asset.DownloadLink).ToList() ?? []);
    }

    private sealed class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string Version { get; init; } = "";

        [JsonPropertyName("body")]
        public string? Info { get; init; }

        [JsonPropertyName("html_url")]
        public string ReleaseLink { get; init; } = "";

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("draft")]
        public bool Draft { get; init; }

        [JsonPropertyName("assets")]
        public List<GithubAsset>? Assets { get; init; }
    }

    private sealed class GithubAsset
    {
        [JsonPropertyName("browser_download_url")]
        public string DownloadLink { get; init; } = "";
    }
}
