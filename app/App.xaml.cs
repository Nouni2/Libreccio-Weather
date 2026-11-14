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
using System.Globalization;
using System.Net.Http;
using System.Windows;
using LibreccioWeather.Background;
using LibreccioWeather.Core.Background;
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Processes;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Sources;
using LibreccioWeather.Core.Sources.BrightSky;
using LibreccioWeather.Core.Sources.ClimWeb;
using LibreccioWeather.Core.Sources.Debug;
using LibreccioWeather.Core.Sources.MeteoFrance;
using LibreccioWeather.Core.Sources.MetNorway;
using LibreccioWeather.Core.Sources.Ncei;
using LibreccioWeather.Core.Sources.NaturalEarth;
using LibreccioWeather.Core.Sources.Nws;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Updates;
using LibreccioWeather.Diagnostics;
using LibreccioWeather.Display;
using LibreccioWeather.Icons;
using LibreccioWeather.Input;
using LibreccioWeather.Localization;
using LibreccioWeather.Tray;
using LibreccioWeather.Updates;
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
    private SingleInstance? _instance;
    private bool _windowReady;
    private bool _activationPending;

    // Startup is an event handler, so it is the one place where async void is the only way to await.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        _instance = new SingleInstance(Core.Configuration.ApplicationIdentity.InstanceName);
        if (!_instance.IsFirstInstance)
        {
            if (!await _instance.ActivateExistingAsync())
            {
                Trace.TraceWarning("The running instance did not acknowledge activation.");
            }

            Shutdown();
            return;
        }

        _instance.StartListening(() => _ = Dispatcher.InvokeAsync(ActivateMainWindow));

        // Unhandled exceptions are recorded and still end the process, so the app never runs on in a broken state.
        ErrorLog.Start();
        DispatcherUnhandledException += (_, args) => ErrorLog.Record("Unhandled exception on the interface thread", args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                ErrorLog.Record("Unhandled exception", exception);
            }
        };
        TaskScheduler.UnobservedTaskException += (_, args) => ErrorLog.Record("Unobserved task exception", args.Exception);

        ProportionalWheelScroll.Register();
        BidiText.Register();

        SettingsManager settings = new(new ConfigStore(SettingsManager.DefaultPath));
        RefreshRate.MatchesMonitor = settings.IsScreenRefreshRateEnabled;
        ProportionalWheelScroll.TouchpadSpeed = (settings.TouchpadScrollSpeed ?? 100) / 100.0;
        FormatterSettings.UseNumberFormatter = settings.UseNumberFormatter;
        FormatterSettings.UseMeasureFormat = settings.UseMeasureFormat;
        settings.SettingsChanged += (_, _) =>
        {
            RefreshRate.MatchesMonitor = settings.IsScreenRefreshRateEnabled;
            ProportionalWheelScroll.TouchpadSpeed = (settings.TouchpadScrollSpeed ?? 100) / 100.0;
            FormatterSettings.UseNumberFormatter = settings.UseNumberFormatter;
            FormatterSettings.UseMeasureFormat = settings.UseMeasureFormat;
        };

        // The language is set before the first window opens, because translated text is read when XAML loads.
        Strings.Culture = AppLanguages.Resolve(settings.Language, CultureInfo.CurrentUICulture);

        WeatherDatabase database = new(WeatherDatabase.DefaultPath);
        database.EnsureCreated();

        OpenMeteoService openMeteo = new(HttpClient, new ConfigStore(SettingsManager.GetSourcePath(OpenMeteoService.Id)));
        WeatherRepository weather = new(database);
        LocationRepository locations = new(database);
        BrightSkyService brightSky = new(HttpClient, new ConfigStore(SettingsManager.GetSourcePath(BrightSkyService.Id)));
        MfService meteoFrance = new(HttpClient, new ConfigStore(SettingsManager.GetSourcePath(MfService.Id)));
        MetNoService metNorway = new(HttpClient);
        NceiService ncei = new(HttpClient);
        // The debug source, with its testing locations, only exists in debug builds, after the worldwide sources.
        IReadOnlyList<IWeatherSource> worldwideSources = AppVersion.IsDebug
            ? [openMeteo, metNorway, ncei, new DebugService()] : [openMeteo, metNorway, ncei];
        SourceManager sources = new(worldwideWeatherSources: worldwideSources, nationalWeatherSources:
        [
            brightSky, meteoFrance, new NwsService(HttpClient),
            new AnamBfService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("anambf"))),
            new AnametService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("anamet"))),
            new DccmsService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("dccms"))),
            new DmnNeService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("dmnne"))),
            new DwrGmService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("dwrgm"))),
            new EthioMetService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("ethiomet"))),
            new GMetService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("gmet"))),
            new IgebuService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("igebu"))),
            new InmgbService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("inmgb"))),
            new MaliMeteoService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("malimeteo"))),
            new MeteoBeninService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("meteobenin"))),
            new MeteoTchadService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("meteotchad"))),
            new MettelsatService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("mettelsat"))),
            new MsdZwService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("msdzw"))),
            new SmaScService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("smasc"))),
            new SmaSuService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("smasu"))),
            new SsmsService(HttpClient, new ConfigStore(SettingsManager.GetSourcePath("ssms"))),
        ], otherSources: [new NaturalEarthService()]);
        WeatherRefresher refresher = new(sources, weather, settings, locations);
        WeatherUpdateJob updateJob = new(refresher, locations, weather, settings);
        MainViewModel viewModel = new(
            openMeteo, sources, refresher, locations, weather, updateJob, settings, RegionInfo.CurrentRegion.TwoLetterISORegionName);
        AppUpdateChecker updateChecker = new(
            settings, new GetApplicationRelease(settings, new ReleaseService(HttpClient)), Dispatcher) { Main = viewModel };
        viewModel.UpdateChecker = updateChecker;
        MainWindow window = new() { DataContext = viewModel };
        _windowIcon = new SeasonalWindowIcon(window, settings);
        viewModel.ShownLocationChanged += (_, location) => _windowIcon.ShowLocation(location);

        // The window opens once the first location is known, so it never shows another season's icon first.
        await viewModel.LoadAsync();
        window.Show();
        _trayIcon = new TrayIcon(window, viewModel);
        _windowReady = true;
        if (_activationPending)
        {
            ActivateMainWindow();
        }

        // Started after the first screen has loaded, so a due update does not compete with its refresh.
        _updateScheduler = new WeatherUpdateScheduler(updateJob, settings);
        _updateScheduler.JobFinished += (_, _) => _ = updateChecker.CheckIfEnabledAsync();
        _updateScheduler.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // An order editor left open saves its list, as it does when its screen is left.
        if (MainWindow?.DataContext is MainViewModel { CurrentPage: ViewModels.Settings.DisplayManageViewModel editor })
        {
            editor.Stop();
        }

        _windowReady = false;
        _trayIcon?.Dispose();
        _updateScheduler?.Dispose();
        _windowIcon?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    private void ActivateMainWindow()
    {
        if (!_windowReady)
        {
            _activationPending = true;
            return;
        }

        _activationPending = false;
        _trayIcon?.ShowWindow();
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
