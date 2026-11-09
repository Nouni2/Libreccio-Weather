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
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Updates;
using LibreccioWeather.Updates;
using LibreccioWeather.ViewModels.Settings;

namespace LibreccioWeather.ViewModels.About;

/// <summary>
/// The app's name, icon and version at the top of the About screen.
/// </summary>
public sealed record AboutHeader(string Name, string Version);

/// <summary>
/// A small title above a group of the About screen's cards.
/// </summary>
public sealed record AboutSectionTitle(string Title);

/// <summary>
/// The About screen and the screens it opens.
/// </summary>
public static class AboutPages
{
    public static SettingsPageViewModel About(MainViewModel main)
    {
        List<object> items =
        [
            new AboutHeader(Strings.Get("desktop_app_name"), VersionFormatted),
            CheckForUpdates(main),
            SeparatorPreference.Large,
            new AboutSectionTitle(Strings.Get("about_contact")),
            new ClickablePreference(
                Strings.Get("about_source_code"), null, () => OpenLink(main, AppVersion.SourceCodeLink), "IconCode",
                isFirst: true, isLast: true),
            SeparatorPreference.Large,
            new AboutSectionTitle(Strings.Get("about_app")),
            new ClickablePreference(
                Strings.Get("about_privacy_policy"), null, () => main.NavigateTo(PrivacyPolicy(main)), "IconShieldLock",
                isFirst: true),
            SeparatorPreference.Small,
            new ClickablePreference(
                Strings.Get("about_dependencies"), null, () => main.NavigateTo(Dependencies(main)), "IconContract",
                isLast: true),
            SeparatorPreference.Large,
            new AboutSectionTitle(Strings.Get("about_contributors")),
        ];

        IReadOnlyList<ContributorItem> contributors = Contributors.AppContributors;
        for (int index = 0; index < contributors.Count; index++)
        {
            ContributorItem item = contributors[index];
            if (index > 0)
            {
                items.Add(SeparatorPreference.Small);
            }

            items.Add(new ClickablePreference(
                item.Name,
                item.Contribution is { } contribution ? Strings.Get(contribution) : null,
                OpenLinkIfAny(main, item.Link),
                isFirst: index == 0,
                isLast: index == contributors.Count - 1));
        }

        items.Add(SeparatorPreference.Large);
        items.Add(new AboutSectionTitle(Strings.Get("about_translators")));
        IReadOnlyList<TranslatorItem> translators = FilteredTranslators();
        for (int index = 0; index < translators.Count; index++)
        {
            TranslatorItem item = translators[index];
            if (index > 0)
            {
                items.Add(SeparatorPreference.Small);
            }

            items.Add(new ClickablePreference(
                item.Name,
                null,
                OpenLinkIfAny(main, item.Link),
                isFirst: index == 0,
                isLast: index == translators.Count - 1));
        }

        return new SettingsPageViewModel(Strings.Get("action_about"), items, main.GoBack, () => About(main));
    }

    /// <summary>
    /// The app's own data and privacy statement, then the privacy policies of the weather sources, sorted by name in
    /// the selected language.
    /// </summary>
    public static SettingsPageViewModel PrivacyPolicy(MainViewModel main)
    {
        List<(string Name, string Url)> sources =
        [
            (OpenMeteoService.Name, OpenMeteoService.PrivacyPolicyUrl),
        ];
        sources.Sort((a, b) => StringComparer.Create(Strings.Culture, ignoreCase: false).Compare(a.Name, b.Name));
        sources.Insert(0, (Strings.Get("desktop_app_name"), AppVersion.PrivacyPolicyLink));

        List<object> items = [];
        for (int index = 0; index < sources.Count; index++)
        {
            (string name, string url) = sources[index];
            if (index > 0)
            {
                items.Add(SeparatorPreference.Small);
            }

            items.Add(new ClickablePreference(
                name, url, () => LinkOpener.Open(url), isFirst: index == 0, isLast: index == sources.Count - 1));
        }

        return new SettingsPageViewModel(Strings.Get("about_privacy_policy"), items, main.GoBack, () => PrivacyPolicy(main));
    }

    /// <summary>
    /// The libraries, data and artwork the app is built with, each with its authors and licence. A click opens its
    /// project page.
    /// </summary>
    public static SettingsPageViewModel Dependencies(MainViewModel main)
    {
        IReadOnlyList<Dependency> dependencies = Dependency.All;
        List<object> items = [];
        for (int index = 0; index < dependencies.Count; index++)
        {
            Dependency dependency = dependencies[index];
            if (index > 0)
            {
                items.Add(SeparatorPreference.Small);
            }

            items.Add(new ClickablePreference(
                dependency.Version is { } version ? dependency.Name + " " + version : dependency.Name,
                dependency.Author + Strings.Get("dot_separator") + dependency.Licence,
                () => OpenLink(main, dependency.Link),
                isFirst: index == 0,
                isLast: index == dependencies.Count - 1));
        }

        return new SettingsPageViewModel(Strings.Get("action_dependencies"), items, main.GoBack, () => Dependencies(main));
    }

    // Release builds show their version, development builds the commit they were built from.
    private static string VersionFormatted => AppVersion.IsDebug
        ? Strings.Format("about_version_debug", AppVersion.CommitSha ?? AppVersion.VersionName)
        : Strings.Format("about_version_release", AppVersion.VersionName);

    private static ClickablePreference CheckForUpdates(MainViewModel main)
    {
        bool isChecking = false;
        return new ClickablePreference(
            Strings.Get("about_check_for_app_updates"),
            null,
            async () =>
            {
                if (isChecking || main.UpdateChecker is not { } checker)
                {
                    return;
                }

                isChecking = true;
                try
                {
                    // A new release shows its own message from the checker.
                    if (await checker.CheckForUpdateAsync(forceCheck: true) is GetApplicationRelease.Result.NoNewUpdate)
                    {
                        main.ShowSnackbar(Strings.Get("about_no_new_updates"));
                    }
                }
                catch (Exception exception)
                {
                    main.ShowSnackbar(exception.Message);
                }
                finally
                {
                    isChecking = false;
                }
            },
            "IconSync",
            isFirst: true,
            isLast: true);
    }

    // The translators of the selected language, or of English when that language has none listed.
    private static IReadOnlyList<TranslatorItem> FilteredTranslators()
    {
        CultureInfo culture = Strings.Culture;
        string language = culture.TwoLetterISOLanguageName;
        string languageWithCountry = language
            + (!culture.IsNeutralCulture && culture.Name.Contains('-') ? "_r" + new RegionInfo(culture.Name).TwoLetterISORegionName : "");
        List<TranslatorItem> filtered = Contributors.AppTranslators
            .Where(item => item.Lang.Contains(language) || item.Lang.Contains(languageWithCountry))
            .ToList();
        return filtered.Count > 0
            ? filtered
            : Contributors.AppTranslators.Where(item => item.Lang.Contains("en")).ToList();
    }

    private static Action? OpenLinkIfAny(MainViewModel main, string link)
    {
        return link.Length == 0 ? null : () => OpenLink(main, link);
    }

    // A link is opened only once confirmed, with the address shown.
    internal static void OpenLink(MainViewModel main, string link)
    {
        main.ShowDialog(new LinkDialogViewModel(link, () =>
        {
            main.CloseDialog();
            LinkOpener.Open(link);
        }, main.CloseDialog));
    }
}
