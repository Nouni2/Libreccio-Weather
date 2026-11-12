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

using CommunityToolkit.Mvvm.Input;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.ViewModels.About;
using LibreccioWeather.ViewModels.Locations;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace LibreccioWeather.ViewModels.WeatherCards;

/// <summary>
/// A piece of text, underlined and clickable when it has a command.
/// </summary>
public sealed record TextSegment(string Text, IRelayCommand? Command = null);

/// <summary>
/// The end of the main screen: the credit of the forecast source, with a link to every source the location uses.
/// </summary>
public sealed class FooterViewModel
{
    public FooterViewModel(MainViewModel main, Location location, bool lightBackground)
    {
        var openSources = new RelayCommand(() => main.ShowDialog(new DataSourcesDialogViewModel(main, location)));
        List<TextSegment> segments = [];
        IWeatherSource? forecastSource = main.Sources.GetWeatherSource(location.ForecastSource);
        if (forecastSource is not null && forecastSource.SupportedFeatures.TryGetValue(SourceFeature.Forecast, out string? credit))
        {
            segments.AddRange(LinkCredit(main, forecastSource, credit));
            segments.Add(new TextSegment(Strings.Get("dot_separator")));
            segments.Add(new TextSegment(Strings.Get("action_more"), openSources));
        }
        else
        {
            segments.Add(new TextSegment(Strings.Get("data_sources"), openSources));
        }

        Segments = segments;

        // Black on a light weather background in a light theme, white otherwise.
        Foreground = lightBackground ? Brushes.Black : Brushes.White;
    }

    public IReadOnlyList<TextSegment> Segments { get; }

    public Brush Foreground { get; }

    /// <summary>
    /// A source's credit with each name that has a link of its own made clickable; the link opens once confirmed.
    /// </summary>
    internal static IEnumerable<TextSegment> LinkCredit(MainViewModel main, ISource source, string credit)
    {
        if (source is not IHttpSource { AttributionLinks.Count: > 0 } http)
        {
            return [new TextSegment(credit)];
        }

        return SplitKeeping(credit, http.AttributionLinks.Keys).Select(part =>
            http.AttributionLinks.TryGetValue(part, out string? link)
                ? new TextSegment(part, new RelayCommand(() => AboutPages.OpenLink(main, link)))
                : new TextSegment(part));
    }

    // Splits the text around each delimiter in turn, keeping the delimiters as parts of their own and dropping empty
    // parts.
    private static List<string> SplitKeeping(string text, IEnumerable<string> delimiters)
    {
        List<string> parts = [text];
        foreach (string delimiter in delimiters)
        {
            parts = parts.SelectMany(part => SplitKeeping(part, delimiter)).ToList();
        }

        return parts;
    }

    private static IEnumerable<string> SplitKeeping(string text, string delimiter)
    {
        string[] pieces = text.Split(delimiter);
        return pieces.SelectMany(piece => new[] { piece, delimiter }).SkipLast(1).Where(part => part.Length > 0);
    }
}

/// <summary>
/// One feature of the location and the source that provides it.
/// </summary>
public sealed record DataSourceItem(string Title, IReadOnlyList<TextSegment> Credit, IRelayCommand? PrivacyPolicyCommand, string PrivacyPolicyText)
{
    public bool HasPrivacyPolicy => PrivacyPolicyCommand is not null;
}

/// <summary>
/// The source of each feature the location uses, with its credit and privacy policy.
/// </summary>
public sealed class DataSourcesDialogViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public DataSourcesDialogViewModel(MainViewModel main, Location location)
    {
        (SourceFeature Feature, string? Source)[] sources =
        [
            (SourceFeature.Forecast, location.ForecastSource),
            (SourceFeature.Current, location.CurrentSource),
            (SourceFeature.AirQuality, location.AirQualitySource),
            (SourceFeature.Pollen, location.PollenSource),
            (SourceFeature.Minutely, location.MinutelySource),
            (SourceFeature.Alert, location.AlertSource),
            (SourceFeature.Normals, location.NormalsSource),
            (SourceFeature.ReverseGeocoding, location.ReverseGeocodingSource),
        ];

        // A feature is listed when its source is known and gives it; a source without a privacy policy has no button.
        Items = sources
            .Where(source => !string.IsNullOrEmpty(source.Source))
            .Select(source => (source.Feature, Source: main.Sources.GetFeatureSource(source.Source!)))
            .Where(source => source.Source is not null && source.Source.SupportedFeatures.ContainsKey(source.Feature))
            .Select(source => new DataSourceItem(
                Strings.Get(SecondarySourcesDialogViewModel.FeatureNameKey(source.Feature)),
                FooterViewModel.LinkCredit(main, source.Source!, source.Source!.SupportedFeatures[source.Feature]).ToList(),
                source.Source is IHttpSource { PrivacyPolicyUrl.Length: > 0 } http
                    ? new RelayCommand(() => AboutPages.OpenLink(main, http.PrivacyPolicyUrl))
                    : null,
                Strings.Get("about_privacy_policy")))
            .ToList();
        Title = Strings.Get("data_sources");
        CloseText = Strings.Get("action_close");
        CloseCommand = new RelayCommand(main.CloseDialog);
    }

    public string Title { get; }

    public string CloseText { get; }

    public IReadOnlyList<DataSourceItem> Items { get; }

    public IRelayCommand CloseCommand { get; }
}
