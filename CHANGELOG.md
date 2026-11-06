# Changelog

## [0.2.2-alpha1] - 2026-11-06

### Changed

- Switching between forecast tabs is faster, and weather icons load once.
- With background movement off, the weather background is drawn once instead of on every frame, which lowers CPU and GPU use while the window is open.

### Removed

- The "already running" notice: launching the app again brings its window forward.

### Fixed

- The header's condition, feels-like and temperature range are light instead of bold, and the °C sits level with the temperature, as in Breezy Weather.
- Daily and hourly forecast columns fill a wide window instead of leaving empty space after the last day or hour. When they do not fit, they keep their width and scroll.
- Forecast columns widen with Windows' text size.
- Forecast columns no longer show a tooltip.
- Detail chart axes and guidelines are faint, labels are readable on a light theme, and lines are no longer cut at the top and bottom of the chart.
- The shading under the forecast curve is lighter on a light theme.

## [0.2.1-alpha1] - 2026-11-01

### Added

- A setting to use the seasonal app icon or the app's usual icon, in Settings → Appearance.

## [0.2.0-alpha1] - 2026-11-01

### Added

- Light theme and a dark mode setting that follows Windows or stays light or dark. The main screen of a location is light during the day and dark at night, and can stay dark while the app is dark.
- Main screen settings: background, threshold lines on charts, card fade-in and element animations, and editors to choose and order the cards and the daily and hourly charts.
- Weather sources settings, with the default source and the addresses of a self-hosted Open-Meteo.
- Weather sources for each location, feature by feature, opened by sliding a location card towards the start.
- An edit button on the main screen for a location's sources, a custom name and deleting it.
- Calendar setting showing Chinese, Dangi, Hebrew, Indian, Islamic or Persian dates in the detail screens and Mistray.
- Optional checks for new releases on GitHub, with or without pre-releases, turned on in Settings → Notifications or from the location list.
- About screen with the version, the source code, privacy policies, dependencies and Breezy Weather's contributors and translators.
- Weather icons on the location cards.
- Animated weather behind the main screen: sun, stars and meteors, clouds, fog, haze, rain, sleet, thunderstorms, snow, hail and wind, following the background animation setting.
- The main screen's temperature counts up to its new value.

### Changed

- Settings and other screens use Material 3's colors, and the main screen and location cards use Breezy Weather's blue colors, as in Breezy Weather. Setting and location cards stand out from the background.
- The desktop interface option is labelled experimental.

### Fixed

- Refreshing the same location again within 10 seconds shows a message instead of downloading the forecast again.

## [0.1.0-alpha1]

### Added

- First public alpha (`0.1.0-alpha1`) of Libreccio Weather, a native Windows port of Breezy Weather.
- Open-Meteo forecasts, current conditions, air quality and pollen where available.
- Multiple saved locations, daily and hourly charts, and detailed weather pages.
- Sun and moon times, moon phases and altitude charts.
- Mistray tray icons with temperature, conditions and weather popups.
- Background updates and saved forecasts available offline.
- English and French, selectable units and an experimental desktop interface.
- Per-monitor display scaling and animations at the monitor's refresh rate.
