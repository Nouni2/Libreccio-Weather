# Changelog

## [0.6.0] - 2026-11-14

### Added

- Météo-France, Breezy Weather's weather source for France: forecast, current conditions and precipitation nowcasts in metropolitan France, temperature normals and alerts there, in Andorra and in the French overseas territories, and temperature normals in Monaco. Its forecast also covers the rest of the world.
- MET Norway: worldwide forecasts, current conditions and precipitation nowcasts in the Nordic countries, air quality in Norway, and alerts for Norway, Svalbard and Jan Mayen.
- The National Weather Service for the United States: forecast, current conditions from the nearest station and active alerts.
- NCEI temperature normals for the United States, Puerto Rico, the US Virgin Islands, the Northern Mariana Islands and Guam.
- The 17 ClimWeb national weather services, for alerts and temperature normals: ANAM-BF, ANAMET, DCCMS, DMN Niger, DWR Gambia, Ethio Met, GMet, IGEBU, INM-GB, Mali-Météo, Météo Bénin, Météo Tchad, Mettelsat, MSD Zimbabwe, SMA Seychelles, SMA Sudan and SSMS.

## [0.5.1] - 2026-11-13

### Fixed

- Saved Open-Meteo locations with an empty current-weather source switch to Open-Meteo current conditions on upgrade.

## [0.5.0] - 2026-11-13

### Added

- The window has its own title bar: the app icon and name, and minimize, maximize and close buttons, with snap layouts over the maximize button, dragging, double-click to maximize and the system menu. On the main screen, it also holds the location list button, the location's name, the time since the last refresh and the actions, on a faint tint of the weather's color, or of red while an alert is in force.
- Debug builds have Breezy Weather's debug locations, added from a bug button in the location list: a test city for each weather condition and each precipitation nowcast case, and further cities with a full forecast, each alert severity, upcoming alerts, air quality levels, pollen, day and night, and polar day and night.

### Changed

- The main screen's top bar and the time since the last refresh moved into the window's title bar.

## [0.4.0] - 2026-11-12

### Added

- Every language Breezy Weather is translated into, 66 in all, with regional variants such as British English, Brazilian Portuguese and Traditional Chinese. The language setting lists each language under its own name, and plurals follow each language's rules. Libreccio Weather's own few texts stay in English outside English and French.
- Right-to-left layout for Arabic, Persian, Hebrew, Urdu and Central Kurdish, mirrored as in Breezy Weather: images, icons, the weather animation and the forecast charts keep their drawing, and text in another direction keeps its order, such as "Follow system (Dark)" in Arabic.
- Arabic, Persian, Central Kurdish, Bengali and Marathi show numbers in their own digits, as in Breezy Weather.
- Bright Sky (DWD), Breezy Weather's weather source for Germany: forecast, current conditions, precipitation for the next two hours and the Deutscher Wetterdienst's alerts. Its server address can be changed in Settings → Weather sources.
- A location's weather sources list every source for each feature: recommended sources first, then worldwide sources, then each continent. New places take Breezy Weather's default sources, so German places get Bright Sky, and places using Open-Meteo also get its current conditions.
- The alert card on the main screen and the Alerts screen it opens, with each alert's headline, dates, description, instruction and source. Open-Meteo gives no alerts; they come from Bright Sky.
- Refresh errors are reported as in Breezy Weather: a single error shows its message, with Help, Change, Allow or Enable where it has help, and several errors show "Refresh completed with errors" with the list of them.
- Settings → Debug with "Share crash logs", which saves the app's recent errors with the app, Windows and device versions to a text file and offers to open it. Debug builds also have Breezy Weather's developer tools.
- Errors are written to `libreccio-weather.log` in the app's data folder.

### Changed

- The settings text dialog uses Material 3's outlined text field, as in Breezy Weather.
- The footer, the data sources dialog and the About screen's privacy policies cover every weather source.

### Fixed

- In text fields, the cursor goes where the click lands, the text is no longer pushed in by doubled padding, and a placeholder such as a server's default address starts where the cursor does.
- A setting that cannot be saved, such as on a full disk, is kept for the session instead of closing the app.
- A location search answered with an unexpected reply shows "search failed" instead of closing the app.
- Saved locations load without freezing the window, at startup and when the location list opens.
- An error after a background update no longer stops later background updates.
- Each day's sun times, air quality, pollen and sunshine take less work to compute during a refresh.
- Without Windows' ICU, numbers with a sign are written as in Breezy Weather.
- "Resource not found" warnings no longer appear in the debug output.

### Security

- `spp.dll` is loaded only from the app's folder or Windows' system folders, and `icu.dll` only from System32.

## [0.3.1] - 2026-11-11

### Fixed

- Debug builds can run alongside the installed app, with separate instance activation, settings, source preferences and saved weather. Their window title includes a translated Debug label.

## [0.3.0] - 2026-11-09

### Added

- Pull to refresh on the main screen: drag down with the mouse at the top of the screen.
- A refresh button on the main screen, and F5 to refresh, in both interfaces.
- A precipitation card for the coming hour on the main screen.
- A footer at the end of the main screen with the weather data sources.
- "Open in another app" in the main screen's menu, which opens the location in the map app Windows chooses.
- A choice of Open-Meteo weather models for each location.
- Two-finger touchpad swipes to go back and forward between screens. Over the forecast columns or the detail screens' day tabs, they scroll them sideways.
- Settings → Appearance → Interface: a touchpad scroll speed, and descriptions of forecast columns when the pointer rests on them.
- Tests of wind directions given as cardinal points, ported from Breezy Weather.

### Changed

- The About screen lists Libreccio Weather's contributors instead of Breezy Weather's. Breezy Weather's translators stay credited, since the app's text comes from their translations.
- The per-location preferences dialog's button reads "Done", since closing it saves.
- The seasonal app icon is off by default.
- Switching location or refreshing reuses the main screen's cards instead of building them again, and switching location does less work.
- The forecast cards' chart choice, and the pollutant and temperature choices in the detail screens, are connected buttons with a menu for the choices that do not fit, as in Breezy Weather.
- Forecast charts take the colors of the current weather, and the columns of today and the hours ahead stand out from the past ones.
- Detail screens are light or dark like their location's main screen, and change with Windows' app mode and the dark mode setting.
- In the detail screens, a sideways drag changes the day, the day tabs have no scroll bar and center the selected day, and explanations open as Material 3 tooltips.
- Mouse wheel and touchpad scrolling glide smoothly, and touchpad scrolling follows the fingers.
- Animations follow Windows' animation effects setting.
- Chart threshold labels sit beside the chart, so bars and lines never cover them.
- The main screen's cards sit on the raised card surface of Breezy Weather's cards.
- Choosing a search result first shows the location's weather sources, and saving them adds the location, as in Breezy Weather.
- The search screen fills the window's width.
- Switching location or refreshing replays the header's fade and temperature count, which starts from the temperature shown for the previous location.
- The main screen header, the forecast cards' titles, subtitles and icons, and the pull-to-refresh indicator use Breezy Weather's colors; the indicator's arc takes the current weather's color, brighter at night.
- In a location's weather sources, the message that the location already exists sits under the forecast row.
- Detail screen dividers follow the screen's light or dark colors, while the Sun & Moon icons and the moon outline keep the app's grey, as in Breezy Weather.

### Fixed

- Forecast chart labels use Roboto, as in Breezy Weather.
- The help icon next to "Nighttime" in the detail screens is no longer cut at the bottom.
- The header's feels-like and temperature range lines end with an ellipsis when too long, instead of wrapping.
- Screen readers read the header's temperatures with their units in full.
- Detail chart columns are no longer cut at the edges of the chart.
- New locations in India no longer get an air quality source, since its air quality would cover other times than the forecast.

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
