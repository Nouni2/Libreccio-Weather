# Third-party notices

## Weather artwork and backgrounds

The weather icons in `assets/weather/` are the static artwork distributed by Breezy Weather, originating from Geometric Weather. The background gradients in `app/Theme/WeatherBackgrounds.xaml` reproduce Breezy Weather's weather-view resources.

These resources are distributed under the GNU Lesser General Public License version 3. The license text is included in [assets/weather/LICENCE.md](../assets/weather/LICENCE.md); the incorporated GNU General Public License version 3 is in [LICENCE.md](../LICENCE.md).

Credits belong to the Breezy Weather and Geometric Weather developers and contributors. The local source files are in `upstream/app/src/main/res/drawable/` and `upstream/ui-weather-view/src/main/res/`. The upstream project is [Breezy Weather](https://github.com/breezy-weather/breezy-weather).

## Temperature-icon font

`assets/fonts/asap_condensed_bold.ttf` is the Asap Condensed Bold font bundled with Breezy Weather for temperature notification icons. Copyright 2018 The Asap Project Authors ([Asap project](https://github.com/Omnibus-Type/Asap)). It is distributed under the SIL Open Font License 1.1, included in [assets/fonts/LICENCE.md](../assets/fonts/LICENCE.md).

## Card fonts

`assets/fonts/Roboto-Medium.ttf` and `Roboto-Bold.ttf` are the hinted Roboto faces from [Google Fonts' Roboto repository](https://github.com/googlefonts/roboto-2/tree/main/src/hinted), used for the sun and moon cards. They are distributed under the Apache License 2.0, included in [assets/fonts/roboto-licence.md](../assets/fonts/roboto-licence.md).

## Moon calculations

The moonrise, moonset, moon phase and moon altitude calculations in `src/Astronomy/` are a C# translation of [commons-suncalc](https://github.com/shred/commons-suncalc) 2.14, the library Breezy Weather uses for them. Copyright (C) 2017 Richard "Shred" Körber. It is distributed under the Apache License 2.0, included in [apache-licence.md](apache-licence.md).

## Default units by country

The default unit of each country in `src/Units/TemperatureUnit.cs`, `DistanceUnit.cs`, `PrecipitationUnit.cs`, `SpeedUnit.cs` and `PressureUnit.cs` comes from the unit preferences of [Unicode CLDR](https://github.com/unicode-org/cldr/blob/3f3967f3cbadc56bbb44a9aed20784e82ac64c67/common/supplemental/units.xml), as Breezy Weather's `weather-unit` module takes them, with the changes it makes. Copyright © 1991-Present Unicode, Inc. It is distributed under the [Unicode License v3](https://www.unicode.org/license.txt).

## Background updates, update checks and crash logs

Breezy Weather takes part of its background update, update check, network and crash log code from [Mihon](https://github.com/mihonapp/mihon), distributed under the Apache License 2.0, included in [apache-licence.md](apache-licence.md). The C# files below translate those Breezy Weather files, so they derive from Mihon's code:

| File | Breezy Weather file | Mihon file |
|---|---|---|
| `src/Background/WeatherUpdateJob.cs`, `app/Background/WeatherUpdateScheduler.cs` | `background/weather/WeatherUpdateJob.kt` | [`LibraryUpdateJob.kt`](https://github.com/mihonapp/mihon/blob/88e9fefa59b3f7f77ab3ddcab1b039f81534c83e/app/src/main/java/eu/kanade/tachiyomi/data/library/LibraryUpdateJob.kt) |
| `src/Updates/GetApplicationRelease.cs` | `background/updater/interactor/GetApplicationRelease.kt` | [`GetApplicationRelease.kt`](https://github.com/mihonapp/mihon/blob/aa498360db90350f2642e6320dc55e7d474df1fd/domain/src/main/java/tachiyomi/domain/release/interactor/GetApplicationRelease.kt) |
| `src/Updates/Release.cs` | `background/updater/model/Release.kt` | [`Release.kt`](https://github.com/mihonapp/mihon/blob/c83037eeab3b180c7b82355331131df6950f5d45/domain/src/main/java/tachiyomi/domain/release/model/Release.kt) |
| `src/Updates/ReleaseService.cs` | `background/updater/data/ReleaseService.kt`, `GithubRelease.kt` | [`ReleaseServiceImpl.kt`](https://github.com/mihonapp/mihon/blob/02864ebd60ac9eb974a1b54b06368d20b0ca3ce5/data/src/main/java/tachiyomi/data/release/ReleaseServiceImpl.kt), [`GithubRelease.kt`](https://github.com/mihonapp/mihon/blob/d29b7c4e5735dc137d578d3bcb3da1f0a02573e8/data/src/main/java/tachiyomi/data/release/GithubRelease.kt) |
| `app/Updates/AppUpdateChecker.cs` | `background/updater/AppUpdateChecker.kt` | [`AppUpdateChecker.kt`](https://github.com/mihonapp/mihon/blob/aa498360db90350f2642e6320dc55e7d474df1fd/app/src/main/java/eu/kanade/tachiyomi/data/updater/AppUpdateChecker.kt) |
| `app/Diagnostics/CrashLogs.cs` | `common/utils/CrashLogUtils.kt`, `createFileInCacheDir` in `common/extensions/ContextExtensions.kt` | [`CrashLogUtil.kt`](https://github.com/mihonapp/mihon/blob/aa498360db90350f2642e6320dc55e7d474df1fd/app/src/main/java/eu/kanade/tachiyomi/util/CrashLogUtil.kt), [`ContextExtensions.kt`](https://github.com/mihonapp/mihon/blob/162b6397050e1577c113a88e7b7cfe9f98e6a45c/app/src/main/java/eu/kanade/tachiyomi/util/system/ContextExtensions.kt) |
## Sun times

`native/spp.dll`, copied next to the application as `spp.dll`, is the Windows x64 C interface from [Seasonal++ 1.1.0](https://github.com/Nouni2/Seasonal-plus-plus/releases/tag/v1.1.0). Its library version is `1.1.0+e916f2c`, built from [commit `e916f2c`](https://github.com/Nouni2/Seasonal-plus-plus/tree/e916f2c). It is distributed under the GNU General Public License version 3, in [LICENCE.md](../LICENCE.md); the corresponding source is available at that commit.

The release DLL is 1,340,416 bytes. SHA-256: `53f7f203f249dcc66b6687d336f03f78328493dace16afe0239e501b66debacf`.
