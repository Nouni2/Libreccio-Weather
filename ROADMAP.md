# ROADMAP

Libreccio Weather aims to reproduce [Breezy Weather](https://github.com/breezy-weather/breezy-weather) on Windows: its interface, settings, weather engine, background updates and notifications. This roadmap shows how far the port has come, and it changes as the work goes on. Finished work is also recorded in [CHANGELOG.md](CHANGELOG.md).

## Port status

Each item names the part of Breezy Weather it ports. An item is ticked once every part under it is ported and has been built. Only the parts listed under Tests are checked by tests, and only the location search, the main screen header and reopening on a saved location have been tried in the running app.

### Engine

- [ ] Units and calculations (`weather-unit`)
  - [x] Quantities, unit conversions and default units by country
  - [x] Feels-like temperatures, dew point, humidity, precipitation totals, sea level pressure and pollutant concentrations
  - [x] Temperature display from the translated unit strings
  - [ ] Display of every other unit
    - [x] Unit names from CLDR through Windows' ICU, or from the translated unit strings without it
    - [ ] Precipitation values in the selected unit and localized percentages
    - [ ] Values of every other unit with their unit
  - [ ] CLDR unit patterns
    - [ ] Measured temperature and precipitation values through ICU, preserving translated narrow temperatures
    - [ ] Locale-aware percentage fallback with half-even rounding and optional decimals
  - [ ] Aggregate air-quality index and level names from the four contributing pollutants
  - [ ] Duration formatting
    - [x] Hours and minutes, such as "1 hour, 30 minutes", through ICU or the translated strings
    - [ ] Single units with decimals
  - [x] Relative times, such as "5 min. ago", through ICU
- [x] Weather and location model (`domain`): weather, current, hourly, daily and half-day forecasts, minutely precipitation, alerts, air quality, pollen, normals, wrappers, locations and source features
- [ ] Refresh (`RefreshHelper.getWeather`, `CommonConverter`)
  - [x] Requests by source, caching and fallback to previous data
  - [x] Completion of hourly, daily and current data
  - [ ] Sources other than Open-Meteo
  - [ ] Location parameters and restricted sources
  - [ ] Saving the completed weather to the database
  - [ ] Sun and moon times, with sun times from Seasonal-plus-plus and the moon later
- [ ] Current location, reverse geocoding and time zone lookup (`RefreshHelper`)
- [ ] Database
  - [x] Saved locations and the parameters of their sources (`LocationRepository`)
  - [ ] Weather (`WeatherRepository`): current conditions, daily and hourly forecasts, minutely precipitation, alerts and normals
- [x] Settings (`SettingsManager`)
  - [x] Settings stored in a JSON file with Breezy Weather's keys and defaults: units, refresh interval, notifications, dark mode, cards and charts, animations, forecast times and the ongoing notification's options for Mistray
  - [x] Language setting, which marks saved weather as outdated when it changes
  - [x] Units and language read from the settings at startup
- [ ] Background refresh (`WeatherUpdateJob`)
  - [x] Refresh at the chosen interval, waiting for a network and, when set, for Battery saver to be off, with retries 10 minutes apart
  - [x] Updates that fell due while the app was closed or the computer was asleep run when it can
  - [x] The first location, plus the next three once a day when Mistray shows several locations
  - [x] The main screen shows weather refreshed in the background
  - [ ] Current position, error notifications, alerts and precipitation notifications after an update

### Sources

- [ ] Open-Meteo (`OpenMeteoService`)
  - [x] Forecast, current conditions and 15-minute precipitation
  - [x] Air quality, and pollen in Europe
  - [x] Weather texts and forecast model identifiers
  - [x] Location search
  - [ ] Model names and per-location model settings
  - [ ] Custom server addresses
- [ ] Choosing a source for each feature (`SourceManager`)
- [ ] Other weather sources

### Translations

- [ ] English and French
  - [x] String importer
  - [x] English used for strings missing from the selected language
  - [x] CLDR plural rules for English and French
  - [x] Language taken from Windows
  - [x] Language setting
  - [x] String arrays in the importer
  - [ ] Libreccio Weather's own English and French interface text in resource files

### Main window

- [ ] Main screen top bar (`HomeFragment`)
  - [x] Location list button and place name
  - [ ] Edit and "open in another app" menu items
- [ ] Main screen header (`HeaderViewHolder`)
  - [x] Condition, temperature with its unit and feels-like temperature
  - [x] Temperatures of the next day and night halves, which depend on the time of day
  - [x] Time since the last refresh
  - [ ] Weather background
    - [ ] Static weather gradients from the weather-view resources
    - [ ] Animated sun, clouds, precipitation and stars
  - [ ] Temperature animation
  - [ ] Descriptions read by screen readers
- [ ] Vertical scrolling of the header and forecast cards
- [ ] Cards
  - [ ] Daily and hourly temperature plots with weather icons and precipitation probability
  - [ ] Daily and hourly chart tabs
    - [ ] Temperature and feels-like selection following the saved chart order and available data
    - [ ] Selected tabs retained across refreshes and location changes
    - [ ] Daily precipitation with separate day and night bars and hourly precipitation amounts
    - [ ] Hourly humidity percentages with a dew-point curve
    - [ ] Air quality, wind, UV and the remaining daily and hourly charts
  - [ ] Normal-temperature reference lines controlled by the horizontal-lines setting
  - [ ] Chart label shadows, device-pixel histogram sizing and full forecast accessibility descriptions
  - [ ] Daily and hourly detail navigation
  - [ ] Precipitation nowcast, air quality, pollen, alerts, wind, humidity, pressure, visibility, UV, sun, moon, clock and footer
- [ ] Daily and hourly detail screens
- [ ] Location search (`SearchActivity`)
  - [x] Search by name, with each result's region and country
  - [x] Attribution, and the no-results and failure messages
  - [ ] Changing the search source
  - [x] Adding the chosen place to saved locations
  - [x] Back button
- [ ] Location list and management (`ManagementFragment`)
  - [x] List of saved locations, opening one on the main screen, and the selected location highlighted
  - [x] Adding a location, with the "already exists" and "added" messages
  - [x] Sliding a card towards the end to delete it, with undo, keeping at least one location
  - [x] Dragging a card by its handle to reorder the list
  - [x] Empty list with its add button
  - [ ] Weather icons on the cards
  - [x] "Last updated" on cards whose weather is outdated
  - [ ] Sliding a card towards the start to choose its weather sources
  - [ ] Current location
  - [x] The list as a pane beside the main screen in wide windows, folded and unfolded by the list button
- [ ] Loading saved weather before refreshing on startup
- [ ] Settings screens
  - [x] Settings home, with every category
  - [x] Background updates: refresh interval with the warning before turning it off, battery saver switch and last run
  - [x] Appearance: language, applied at once, and units
  - [x] Units: temperature, precipitation, speed, distance and pressure
  - [ ] External modules: Notification widget, Persistent, Style, Temperature as status bar icon and Use feels like temperature
  - [ ] Appearance: calendar, dark mode and the rest of the theme section
  - [ ] Main screen, notifications, widgets, location, weather sources and debug settings
  - [ ] About screen
- [ ] Breezy Weather's colors and light theme, in place of the Material 3 baseline dark colors used now
- [ ] Single instance
- [ ] Closing the window to the tray, restoring it and exiting through the tray menu
- [ ] Mouse back and forward buttons moving through the screens
- [ ] Middle-button autoscroll
- [ ] Thin scrollbars in both interface modes
- [ ] Sharp rendering at each monitor's display scale, including when it changes or the window moves between monitors
- [x] Animations at the refresh rate of the monitor the window is on
- [ ] Setting to keep animations at 60 frames per second on slower computers
- [x] Main window icon of the current season at the location on screen, reversed in the southern hemisphere

### Experimental desktop interface

- [ ] Appearance option applying desktop adjustments to the existing views
- [ ] Smaller corner radii and compact controls using the existing palette, typography and layouts
- [ ] Compact actions in the existing weather top bar
- [ ] Context menus and keyboard controls for locations, settings and navigation

### Mistray

- [ ] Tray icon with the temperature or a condition icon, tooltip and menu
  - [ ] Condition artwork, with the app icon when weather is unavailable
  - [ ] Tooltip with the shown location, conditions, temperature and refresh status
  - [ ] Open, Refresh, Change location, Settings and Exit menu
  - [ ] Temperature drawn into the icon
  - [ ] Optional second icon with the condition next to the temperature
  - [ ] Menu in the app's own style, opening while the main window is open
  - [ ] Tray icon switch controls tray visibility, with the mobile defaults
  - [ ] Dismiss action available when Persistent is off, restoring the icon on refresh
- [ ] Popup in its four styles: compact, multiple locations, daily and hourly
  - [ ] Popup opening at once on a click, and close on focus loss
  - [ ] Optional double-click opening the main window, with an adjustable double-click delay
  - [ ] Current weather, air-quality or wind caption, and actual or feels-like temperatures
  - [ ] Three secondary locations, five days or five hours according to the style
- [ ] Starting with Windows in the tray, and coming back after Explorer restarts

### Notifications

- [ ] Severe weather alerts, precipitation alerts, and today's and tomorrow's forecasts

### Project

- [x] Solution, build configuration, code style, `README.md`, `CONTRIBUTING.md`, `AGENTS.md` and `CHANGELOG.md`
- [ ] Tests
  - [x] Breezy Weather's tests for temperature conversions, humidex and total precipitation
  - [x] Checks on rounding and the Beaufort scale
  - [x] Weather database migrations, quantity storage and transaction rollback
  - [x] Breezy Weather's number formatting tests
  - [ ] Breezy Weather's tests for card and chart lists and option names
  - [ ] Settings file storage
  - [ ] Plot and reference-line coordinate checks for equal minimum and maximum values, and paired histogram coordinates with a zero maximum
  - [ ] Percent-format checks for .NET rounding, optional decimals and English/French spacing differences
  - [ ] Integer-rounding saturation and NaN rejection, and air-quality index saturation
- [ ] Third-party notices for the ported parts that come from other projects
- [ ] Libreccio Weather's logo
- [ ] Installer and releases
