<div align="center">

<img src="assets/icons/icon.svg" alt="Libreccio Weather logo" width="128">

# Libreccio Weather

[![Windows 11](https://img.shields.io/badge/Windows-0078D4?logo=windows&logoColor=white)](#download)
[![C#](https://img.shields.io/badge/C%23-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF-512BD4)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![Licence](https://img.shields.io/github/license/Nouni2/Libreccio-Weather)](LICENCE.md)
[![Latest release](https://img.shields.io/github/v/release/Nouni2/Libreccio-Weather)](https://github.com/Nouni2/Libreccio-Weather/releases/latest)

#### Libreccio Weather is an unofficial free and open source native Windows port of [Breezy Weather](https://github.com/breezy-weather/breezy-weather), aiming to faithfully reproduce its interface, features, and behavior.

</div>

## Download

[![GitHub](https://img.shields.io/badge/Get%20it%20on-GitHub-181717?logo=github&style=for-the-badge)](https://github.com/Nouni2/Libreccio-Weather/releases/latest)

Download the latest version from the [releases page](https://github.com/Nouni2/Libreccio-Weather/releases/latest). Libreccio Weather was developed and tested on Windows 11.

Unit names, durations and relative times, such as "kilometers per hour", "1 hour, 30 minutes" and "5 min. ago", use the ICU library included in Windows from Windows 10 version 1903. On Windows without it, units and durations use short forms such as "km/h" and "1 h, 30 min", and relative times are not shown.

## Screenshots

<p align="center">
  <img src="docs/images/main-light.png" alt="Main window, light theme" width="49%">
  <img src="docs/images/main-dark.png" alt="Main window, dark theme" width="49%">
</p>
<p align="center">
  <img src="docs/images/tray-popup.png" alt="Mistray popup" width="32%">
  <img src="docs/images/settings.png" alt="Settings" width="32%">
  <img src="docs/images/sources.png" alt="Weather sources" width="32%">
</p>

## Features

- Mistray, a tray icon showing the current temperature or a condition icon, using the actual or feels-like temperature.
- A Mistray popup with current conditions, in one of four styles: native, multiple locations, daily or hourly.
- A main window with current weather, hourly graphs, daily forecasts, wind, humidity, pressure, visibility, UV, sun and moon, air quality and pollen.
- A separate weather source for each kind of data: forecast, current conditions, air quality, pollen, minute-by-minute precipitation, alerts and temperature normals.
- Windows notifications for severe weather alerts, precipitation and daily forecasts.
- Background updates at a chosen interval, with the last forecast kept available offline.
- Multiple locations, found by searching for a place name.
- Light and dark themes, following the Windows setting or chosen manually.
- An option to start with Windows directly in the tray.

<details>
<summary>Free and open source</summary>

The full source code is in this repository under the [GNU General Public License v3.0](LICENCE.md).

</details>

<details>
<summary>Privacy</summary>

Libreccio Weather connects directly to the weather sources you select to fetch weather data for your locations. Each source handles those requests under its own privacy policy. The source code is open, so what the app sends can be checked.

</details>

## Building from source

Building requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) on Windows.

```powershell
git clone https://github.com/Nouni2/Libreccio-Weather.git
cd Libreccio-Weather
dotnet build Libreccio-Weather.slnx
dotnet run --project Libreccio-Weather.csproj
```

## Contribute

Report bugs and request features in [issues](https://github.com/Nouni2/Libreccio-Weather/issues). Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## Origin

I wanted a good weather app on Windows. I tried plenty of them, and none were good enough for my liking. Breezy Weather was one I really liked on Android, so I decided to make my own by porting it.

## Credits

Libreccio Weather reproduces the design and behaviour of [Breezy Weather](https://github.com/breezy-weather/breezy-weather), created by the Breezy Weather developers and contributors.

Its translations come from Breezy Weather's translators, under the GNU Lesser General Public License v3.0 only.

## Licence

Libreccio Weather is licensed under the [GNU General Public License v3.0](LICENCE.md).

Its code is clearly based on [Breezy Weather](https://github.com/breezy-weather/breezy-weather), which is licensed under the GNU Lesser General Public License v3.0. I want this code to remain free and open source, so anything built from it has to be released under the same licence.
