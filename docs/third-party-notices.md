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

## Sun times

`native/spp.dll`, copied next to the application as `spp.dll`, is the Windows x64 C interface from [Seasonal++ 1.1.0](https://github.com/Nouni2/Seasonal-plus-plus/releases/tag/v1.1.0). Its library version is `1.1.0+e916f2c`, built from [commit `e916f2c`](https://github.com/Nouni2/Seasonal-plus-plus/tree/e916f2c). It is distributed under the GNU General Public License version 3, in [LICENCE.md](../LICENCE.md); the corresponding source is available at that commit.

The release DLL is 1,340,416 bytes. SHA-256: `53f7f203f249dcc66b6687d336f03f78328493dace16afe0239e501b66debacf`.
