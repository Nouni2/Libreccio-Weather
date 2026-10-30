# ROADMAP

Libreccio Weather aims to reproduce [Breezy Weather](https://github.com/breezy-weather/breezy-weather) on Windows: its interface, settings, weather engine, background updates and notifications. This roadmap shows how far the port has come, and it changes as the work goes on. Finished work is also recorded in [CHANGELOG.md](CHANGELOG.md).

## Port status

Each item names the part of Breezy Weather it ports. An item is ticked once every part under it is ported and has been built. Only the parts listed under Tests are checked by tests. The parts tried in the running app are the location search and list, the main screen with its forecast cards, Mistray's icons, popup, menu and settings, display scaling, animations at the monitor's refresh rate and switching between locations.

### Engine

- [ ] Units and calculations (`weather-unit`)
  - [x] Quantities, unit conversions and default units by country
  - [x] Feels-like temperatures, dew point, humidity, precipitation totals, sea level pressure and pollutant concentrations
  - [x] Temperature display from the translated unit strings
  - [ ] Display of every other unit
    - [x] Unit names from CLDR through Windows' ICU, or from the translated unit strings without it
    - [x] Precipitation values in the selected unit and localized percentages
    - [x] Speed, distance and pressure values with selected precision and localized unit names
    - [ ] Pollutant and pollen values with their units
  - [x] CLDR unit patterns
    - [x] Measured temperature and precipitation values through ICU, preserving translated narrow temperatures
    - [x] Locale-aware percentage fallback with half-even rounding and optional decimals
  - [x] Aggregate air-quality index and level names from the four contributing pollutants
  - [ ] Duration formatting
    - [x] Hours and minutes, such as "1 hour, 30 minutes", through ICU or the translated strings
    - [x] Hours with decimals, including the daily sunshine chart labels
    - [ ] Other single duration units with decimals
  - [x] Relative times, such as "5 min. ago", through ICU
- [x] Weather and location model (`domain`): weather, current, hourly, daily and half-day forecasts, minutely precipitation, alerts, air quality, pollen, normals, wrappers, locations and source features
- [ ] Refresh (`RefreshHelper.getWeather`, `CommonConverter`)
  - [x] Requests by source, caching and fallback to previous data
  - [x] Completion of hourly, daily and current data
  - [ ] Sources other than Open-Meteo
  - [ ] Location parameters and restricted sources
  - [x] Saving the completed weather to the database
  - [ ] Sun and moon times, with sun times from Seasonal-plus-plus and the moon later
- [ ] Current location, reverse geocoding and time zone lookup (`RefreshHelper`)
- [x] Database
  - [x] Saved locations and the parameters of their sources (`LocationRepository`)
  - [x] Weather (`WeatherRepository`): current conditions, daily and hourly forecasts, minutely precipitation, alerts and normals
- [x] Settings (`SettingsManager`)
  - [x] Settings stored in a JSON file with Breezy Weather's keys and defaults: units, refresh interval, notifications, dark mode, cards and charts, animations, forecast times and the ongoing notification's options for Mistray
  - [x] Language setting, which marks saved weather as outdated when it changes
  - [x] Units and language read from the settings at startup
- [ ] Background refresh (`WeatherUpdateJob`)
  - [x] Refresh at the chosen interval, waiting for a network and, when set, for Battery saver to be off, with retries 10 minutes apart
  - [x] Updates that fell due while the app was closed or the computer was asleep run when it can
  - [x] The first location, plus the next three once a day when Mistray shows several locations
  - [x] The main screen shows weather refreshed in the background
  - [x] Failed background jobs retried with linear backoff, with pending retries retained across network and power events
  - [x] Per-location exceptions and transient source errors reported as failed jobs, keeping failed secondary cities eligible for retry
  - [ ] Current position, error notifications, alerts and precipitation notifications after an update
- [x] Forecast and location consistency
  - [x] Tomorrow and current-day detail values use the weather snapshot's reference day
  - [x] Source parameter replacement removes obsolete sources and parameter names
  - [x] Location identifier changes preserve source parameters and every cached forecast table
  - [x] Adding, restoring and reordering locations publish changes only after a successful save

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

- [x] English and French
  - [x] String importer
  - [x] English used for strings missing from the selected language
  - [x] CLDR plural rules for English and French
  - [x] Language taken from Windows
  - [x] Language setting
  - [x] String arrays in the importer
  - [x] Libreccio Weather's own English and French interface text in resource files

### Main window

- [ ] Main screen top bar (`HomeFragment`)
  - [x] Location list button and place name
  - [ ] Edit and "open in another app" menu items
- [ ] Main screen header (`HeaderViewHolder`)
  - [x] Condition, temperature with its unit and feels-like temperature
  - [x] Temperatures of the next day and night halves, which depend on the time of day
  - [x] Time since the last refresh
  - [ ] Weather background
    - [x] Static weather gradients from the weather-view resources
    - [ ] Animated sun, clouds, precipitation and stars
  - [ ] Temperature animation
  - [ ] Descriptions read by screen readers
- [x] Vertical scrolling of the header and forecast cards
- [ ] Cards
  - [x] Daily and hourly temperature plots with weather icons and precipitation probability
  - [x] Daily and hourly chart tabs
    - [x] Temperature and feels-like selection following the saved chart order and available data
    - [x] Selected tabs retained across refreshes and location changes
    - [x] Daily precipitation with separate day and night bars and hourly precipitation amounts
    - [x] Hourly humidity percentages with a dew-point curve
    - [x] Daily and hourly UV bars with risk colors and an alert-level reference line
    - [x] Hourly pressure and visibility curves, and cloud-cover bars
    - [x] Daily and hourly air quality with index colors and reference levels
    - [x] Daily and hourly wind speed, direction arrows and stronger-gust bars and labels
    - [x] Daily sunshine duration, scaled against available day lengths or forecast sunshine
  - [x] Normal-temperature reference lines controlled by the horizontal-lines setting
  - [x] Chart label shadows, device-pixel histogram sizing and full forecast accessibility descriptions
  - [ ] Daily and hourly detail navigation from each chart column
  - [ ] Current-weather cards
    - [x] Half-day precipitation totals with rain and snow labels
    - [x] Wind speed, direction and stronger gusts
    - [x] Air-quality index and risk gauge, with today's data as a fallback
    - [x] The two highest-risk pollens, with negligible-pollen and mold-exclusion rules
    - [x] Humidity waves and dew point
    - [x] UV index, risk label and source artwork
    - [x] Visibility value and description
    - [x] Pressure value, selected unit and progress gauge
    - [x] Square cards in saved order, adapting to the available weather-pane width
    - [ ] Card enter animations and detail actions
    - [ ] Precipitation nowcast, alerts, sun, moon, clock and footer
- [ ] Daily and hourly detail screens (`DetailsActivity`)
  - [x] Updated weather reaches open detail screens and screens kept in navigation history, preserving the selected date and page
  - [x] Current wind appears when daily wind summaries are missing
  - [ ] A tab for each day and a page menu of the available kinds of data
  - [ ] Conditions with real and feels-like temperatures, normals, daily summary, degree days and precipitation probability
  - [ ] Precipitation amounts, types, probability and duration
  - [ ] Wind with gusts, direction arrows and the Beaufort scale
  - [ ] Air quality with its index or each pollutant, pollutant details and scales
  - [ ] Pollen with the primary pollen and every measured pollen
  - [ ] UV, humidity with dew point, pressure with its trend, cloud cover with sunshine duration, and visibility, each with its scale or explanation
  - [ ] Sun, twilight and moon times
  - [ ] Sun and moon altitude chart and moon phase
  - [ ] Swiping between days
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
  - [x] Location cards and the in-memory location list updated after a manual weather refresh
- [x] Loading saved weather before refreshing on startup
- [ ] Settings screens
  - [x] Settings home, with every category
  - [x] Background updates: refresh interval with the warning before turning it off, battery saver switch and last run
  - [x] Appearance: language, applied at once, and units
  - [x] Units: temperature, precipitation, speed, distance and pressure
  - [x] External modules: Notification widget, Persistent, Style, Temperature as status bar icon and Use feels like temperature
  - [ ] Appearance: calendar, dark mode and the rest of the theme section
  - [ ] Main screen, notifications, widgets, location, weather sources and debug settings
  - [ ] About screen
- [ ] Breezy Weather's colors and light theme, in place of the Material 3 baseline dark colors used now
- [ ] Single instance
  - [x] One process per Windows user, enforced before settings and database access
  - [x] A second launch restores the existing window, including from the tray or a minimized state
  - [ ] Already-running notice on a second launch, with English and French text
- [x] Closing the window to the tray, restoring it and exiting through the tray menu
- [x] Mouse back and forward buttons moving through the screens
- [x] Middle-button autoscroll
- [x] Thin scrollbars in both interface modes
- [x] Sharp rendering at each monitor's display scale, including when it changes or the window moves between monitors
- [x] Animations at the refresh rate of the monitor the window is on
- [x] Setting to keep animations at 60 frames per second on slower computers
- [x] Main window icon of the current season at the location on screen, reversed in the southern hemisphere

### Experimental desktop interface

- [x] Appearance option applying desktop adjustments to the existing views
- [x] Smaller corner radii and compact controls using the existing palette, typography and layouts
- [x] Compact actions in the existing weather top bar
- [x] Context menus and keyboard controls for locations, settings and navigation

### Mistray

- [x] Tray icon with the temperature or a condition icon, tooltip and menu
  - [x] Condition artwork, with the app icon when weather is unavailable
  - [x] Tooltip with the shown location, conditions, temperature and refresh status
  - [x] Open, Refresh, Change location, Settings and Exit menu
  - [x] Temperature drawn into the icon
  - [x] Optional second icon with the condition next to the temperature
  - [x] Menu in the app's own style, opening while the main window is open
  - [x] Tray icon switch controls tray visibility, with the mobile defaults
  - [x] Dismiss action available when Persistent is off, restoring the icon on refresh
- [x] Popup in its four styles: compact, multiple locations, daily and hourly
  - [x] Popup opening at once on a click, and close on focus loss
  - [x] Optional double-click opening the main window, with an adjustable double-click delay
  - [x] Current weather, air-quality or wind caption, and actual or feels-like temperatures
  - [x] Three secondary locations, five days or five hours according to the style
- [ ] Starting with Windows in the tray, and coming back after Explorer restarts

### Notifications

- [ ] Severe weather alerts, precipitation alerts, and today's and tomorrow's forecasts

### Project

- [x] Solution, build configuration, code style, `README.md`, `CONTRIBUTING.md`, `AGENTS.md` and `CHANGELOG.md`
- [ ] Tests
  - [x] Breezy Weather's tests for temperature conversions, humidex and total precipitation
  - [x] Checks on rounding and the Beaufort scale
  - [x] Weather database migrations, quantity storage and transaction rollback
  - [x] Location identifier changes, rename rollback and full source parameter replacement
  - [x] Breezy Weather's number formatting tests
  - [x] Breezy Weather's tests for card and chart lists and option names
  - [x] Settings file storage
  - [x] Plot and reference-line coordinate checks for equal minimum and maximum values, and paired histogram coordinates with a zero maximum
  - [x] Percent-format checks for .NET rounding, optional decimals and English/French spacing differences
  - [x] Integer-rounding saturation and NaN rejection, and air-quality index saturation
  - [x] Quantity conversion limits, large exact integers and values immediately below half-rounding boundaries
  - [x] Negative duration components and number formatting above the .NET decimal range
  - [x] Windows instance-gate lifetime and repeated activation through named pipes
  - [ ] Detail chart top-label saturation and hours skipped by a daylight saving change
- [ ] Third-party notices for the ported parts that come from other projects
- [ ] Libreccio Weather's logo
- [ ] Installer and releases
