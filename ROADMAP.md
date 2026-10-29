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
  - [ ] CLDR unit patterns
  - [ ] Duration formatting
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
- [ ] Settings (`SettingsManager`)
- [ ] Background refresh at the chosen interval, and after reconnecting or resuming (`WeatherUpdateJob`)

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
  - [ ] Language setting
  - [ ] String arrays in the importer
  - [ ] A place for Libreccio Weather's own text

### Main window

- [ ] Main screen header (`HeaderViewHolder`)
  - [x] Condition, temperature with its unit and feels-like temperature
  - [x] Temperatures of the next day and night halves, which depend on the time of day
  - [ ] Time since the last refresh
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
- [ ] Location list and management
- [ ] Loading saved weather before refreshing on startup
- [ ] Settings and weather sources screens
- [ ] Breezy Weather's colors and light theme, in place of the Material 3 baseline dark colors used now
- [ ] Single instance, and closing the window to the tray

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
  - [ ] Weather database migrations, quantity storage and transaction rollback
  - [ ] Breezy Weather's formatting tests, which need unit formatting
- [ ] Third-party notices for the ported parts that come from other projects
- [ ] Libreccio Weather's logo
- [ ] Installer and releases
