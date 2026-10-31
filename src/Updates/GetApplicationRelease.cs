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
using LibreccioWeather.Core.Settings;

namespace LibreccioWeather.Core.Updates;

/// <summary>
/// Finds out whether a newer release than the running version is published.
/// </summary>
public sealed class GetApplicationRelease(SettingsManager settings, ReleaseService service)
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(1);

    public async Task<Result> AwaitAsync(Arguments arguments, CancellationToken cancellationToken = default)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long lastChecked = settings.AppUpdateCheckLastTimestamp;

        // Checks run at most once a day unless asked for.
        if (!arguments.ForceCheck && now < lastChecked + (long)CheckInterval.TotalMilliseconds)
        {
            return new Result.NoNewUpdate();
        }

        Release? release = await service
            .LatestAsync(arguments.Org, arguments.Repository, arguments.IncludePrereleases, cancellationToken)
            .ConfigureAwait(false);

        settings.AppUpdateCheckLastTimestamp = now;

        return release is not null && IsNewVersion(arguments.VersionName, release.Version)
            ? new Result.NewUpdate(release)
            : new Result.NoNewUpdate();
    }

    /// <summary>
    /// Whether the tag names a later version than the running one, by Semantic Versioning precedence: a leading "v" and
    /// build metadata are ignored, and a pre-release such as "0.2.0-alpha1" comes before "0.2.0". A version that cannot
    /// be read is never newer.
    /// </summary>
    public static bool IsNewVersion(string versionName, string versionTag)
    {
        return Parse(versionTag) is { } newVersion
            && Parse(versionName) is { } oldVersion
            && Compare(newVersion, oldVersion) > 0;
    }

    private static (long[] Core, string[] Prerelease)? Parse(string version)
    {
        string text = version.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        int plus = text.IndexOf('+');
        if (plus >= 0)
        {
            text = text[..plus];
        }

        int dash = text.IndexOf('-');
        string core = dash >= 0 ? text[..dash] : text;
        string[] prerelease = dash >= 0 ? text[(dash + 1)..].Split('.') : [];

        string[] parts = core.Split('.');
        long[] numbers = new long[parts.Length];
        for (int index = 0; index < parts.Length; index++)
        {
            if (!long.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]))
            {
                return null;
            }
        }

        if (prerelease.Any(identifier => identifier.Length == 0))
        {
            return null;
        }

        return (numbers, prerelease);
    }

    private static int Compare((long[] Core, string[] Prerelease) a, (long[] Core, string[] Prerelease) b)
    {
        int length = Math.Max(a.Core.Length, b.Core.Length);
        for (int index = 0; index < length; index++)
        {
            long left = index < a.Core.Length ? a.Core[index] : 0;
            long right = index < b.Core.Length ? b.Core[index] : 0;
            if (left != right)
            {
                return left.CompareTo(right);
            }
        }

        // A version without a pre-release comes after the same version with one.
        if (a.Prerelease.Length == 0 || b.Prerelease.Length == 0)
        {
            return b.Prerelease.Length.CompareTo(a.Prerelease.Length) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0,
            };
        }

        for (int index = 0; index < Math.Min(a.Prerelease.Length, b.Prerelease.Length); index++)
        {
            int comparison = CompareIdentifier(a.Prerelease[index], b.Prerelease[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return a.Prerelease.Length.CompareTo(b.Prerelease.Length);
    }

    // Numeric identifiers compare as numbers and come before alphanumeric ones, which compare by character code.
    private static int CompareIdentifier(string a, string b)
    {
        bool aNumeric = long.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out long aNumber);
        bool bNumeric = long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out long bNumber);
        if (aNumeric && bNumeric)
        {
            return aNumber.CompareTo(bNumber);
        }

        if (aNumeric != bNumeric)
        {
            return aNumeric ? -1 : 1;
        }

        return string.CompareOrdinal(a, b);
    }

    public sealed record Arguments(
        string VersionName,
        string Org,
        string Repository,
        bool IncludePrereleases,
        bool ForceCheck = false);

    public abstract record Result
    {
        private Result()
        {
        }

        public sealed record NewUpdate(Release Release) : Result;

        public sealed record NoNewUpdate : Result;
    }
}
