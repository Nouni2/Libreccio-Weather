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
using System.Net.Http;
using System.Windows;
using LibreccioWeather.Background;
using LibreccioWeather.Core.Background;
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Display;
using LibreccioWeather.Icons;
using LibreccioWeather.Tray;
using LibreccioWeather.ViewModels;

namespace LibreccioWeather;

// Fully qualified because WPF and Windows Forms both define an Application class.
public partial class App : System.Windows.Application
{
    // One client for the whole app, so connections are reused across requests.
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private WeatherUpdateScheduler? _updateScheduler;
    private SeasonalWindowIcon? _windowIcon;
    private TrayIcon? _trayIcon;

    // Startup is an event handler, so it is the one place where async void is the only way to await.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SettingsManager settings = new(new ConfigStore(SettingsManager.DefaultPath));
        RefreshRate.MatchesMonitor = settings.IsScreenRefreshRateEnabled;
        settings.SettingsChanged += (_, _) => RefreshRate.MatchesMonitor = settings.IsScreenRefreshRateEnabled;

        // The language is set before the first window opens, because translated text is read when XAML loads.
        Strings.Culture = AppLanguages.Resolve(settings.Language, CultureInfo.CurrentUICulture);

        WeatherDatabase database = new(WeatherDatabase.DefaultPath);
        database.EnsureCreated();

        OpenMeteoService openMeteo = new(HttpClient);
        WeatherRepository weather = new(database);
        LocationRepository locations = new(database);
        WeatherRefresher refresher = new(openMeteo, weather, settings);
        WeatherUpdateJob updateJob = new(refresher, locations, weather, settings);
        MainViewModel viewModel = new(
            openMeteo, refresher, locations, weather, updateJob, settings, RegionInfo.CurrentRegion.TwoLetterISORegionName);
        MainWindow window = new() { DataContext = viewModel };
        _windowIcon = new SeasonalWindowIcon(window);
        viewModel.ShownLocationChanged += (_, location) => _windowIcon.ShowLocation(location);

        // The window opens once the first location is known, so it never shows another season's icon first.
        await viewModel.LoadAsync();
        window.Show();
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        _trayIcon = new TrayIcon(window, viewModel);

        // Started after the first screen has loaded, so a due update does not compete with its refresh.
        _updateScheduler = new WeatherUpdateScheduler(updateJob, settings);
        _updateScheduler.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _updateScheduler?.Dispose();
        _windowIcon?.Dispose();
        base.OnExit(e);
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        _trayIcon?.PrepareForShutdown();
        base.OnSessionEnding(e);
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Libreccio-Weather");
        return client;
    }
}
