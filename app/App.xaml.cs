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
using LibreccioWeather.Core.Database;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Refresh;
using LibreccioWeather.Core.Sources.OpenMeteo;
using LibreccioWeather.Core.Units;
using LibreccioWeather.ViewModels;

namespace LibreccioWeather;

// Fully qualified because WPF and Windows Forms both define an Application class.
public partial class App : System.Windows.Application
{
    // One client for the whole app, so connections are reused across requests.
    private static readonly HttpClient HttpClient = CreateHttpClient();

    // Startup is an event handler, so it is the one place where async void is the only way to await.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The language is set before the first window opens, because translated text is read when XAML loads.
        Strings.Culture = AppLanguages.Resolve(chosenLanguageCode: null, CultureInfo.CurrentUICulture);

        WeatherDatabase database = new(WeatherDatabase.DefaultPath);
        database.EnsureCreated();

        TemperatureUnit temperatureUnit = TemperatureUnit.GetDefaultUnit(RegionInfo.CurrentRegion.TwoLetterISORegionName);
        OpenMeteoService openMeteo = new(HttpClient);
        WeatherRepository weather = new(database);
        MainViewModel viewModel = new(
            openMeteo, new WeatherRefresher(openMeteo, weather), new LocationRepository(database), weather, temperatureUnit);
        MainWindow window = new() { DataContext = viewModel };
        window.Show();
        await viewModel.LoadAsync();
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Libreccio-Weather");
        return client;
    }
}
