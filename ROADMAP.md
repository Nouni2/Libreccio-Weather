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
    - [ ] Values of every other unit with their unit
  - [ ] CLDR unit patterns
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
  - [ ] A place for Libreccio Weather's own text

### Main window

- [ ] Main screen top bar (`HomeFragment`)
  - [x] Location list button and place name
  - [ ] Edit and "open in another app" menu items
- [ ] Main screen header (`HeaderViewHolder`)
  - [x] Condition, temperature with its unit and feels-like temperature
  - [x] Temperatures of the next day and night halves, which depend on the time of day
  - [x] Time since the last refresh
  - [ ] Weather background
  - [ ] Temperature animation
  - [ ] Descriptions read by screen readers
- [ ] Cards: hourly, daily, precipitation nowcast, air quality, pollen, alerts, wind, humidity, pressure, visibility, UV, sun, moon, clock and footer
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
- [ ] Loading saved weather before refreshing on startup
- [ ] Settings screens
  - [x] Settings home, with every category
  - [x] Background updates: refresh interval with the warning before turning it off, battery saver switch and last run
  - [x] Appearance: language, applied at once, and units
  - [x] Units: temperature, precipitation, speed, distance and pressure
  - [ ] Appearance: calendar, dark mode and the rest of the theme section
  - [ ] Main screen, notifications, widgets, location, weather sources and debug settings
  - [ ] About screen
- [ ] Breezy Weather's colors and light theme, in place of the Material 3 baseline dark colors used now
- [ ] Single instance, and closing the window to the tray
- [x] Main window icon of the current season at the location on screen, reversed in the southern hemisphere

### Mistray

- [ ] Tray icon with the temperature or a condition icon, tooltip and menu
- [ ] Popup in its four styles: compact, multiple locations, daily and hourly
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
- [ ] Third-party notices for the ported parts that come from other projects
- [ ] Libreccio Weather's logo
- [ ] Installer and releases
