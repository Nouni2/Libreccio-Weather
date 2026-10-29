# Changelog

All notable changes to Libreccio Weather are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Libreccio Weather icon in SVG format at `assets/icons/icon.svg`.
- README describing the project, its planned features, building from source, credits and licence.
- GNU General Public License v3.0 in `LICENCE.md`.
- `AGENTS.md` with the rules for contributors: fidelity to Breezy Weather, comments, licence header, text and translations, changelog, naming, folder layout and code organization, pointing to `CONTRIBUTING.md` for code style.
- `CONTRIBUTING.md` explaining how to report bugs, when to discuss changes first, how to set up, build and test, the pull request checklist, the code style, and the licence of contributions.
- `Libreccio-Weather.slnx` solution with four projects, each defined by a project file in the root:
  - `Libreccio-Weather.Core`, the engine library, built from `src/`.
  - `Libreccio-Weather`, the WPF application, built from `app/`.
  - `Libreccio-Weather.Core.Tests`, the xUnit test project, built from `tests/`.
  - `Libreccio-Weather.StringImporter`, the string importer, built from `tools/StringImporter/`.
- `global.json` pinning the .NET 10 SDK and selecting Microsoft.Testing.Platform for `dotnet test`.
- `Directory.Build.props` with shared build settings: .NET 10 for Windows, nullable reference types, implicit usings, warnings treated as errors, and all build output sent to `build/`.
- `Directory.Packages.props` with central NuGet package versions, including CommunityToolkit.Mvvm for the app's view models.
- `.editorconfig` with the standard .NET code style: encoding, CRLF line endings, indentation, file-scoped namespaces, braces, `var` usage and naming rules.
- `.gitattributes` normalizing line endings, with LF kept for Bash scripts.
- `.gitignore`, which also ignores the `build/` output folder.
- Weather quantities in `src/Units/`: temperature, distance, pressure, speed (including the Beaufort scale), precipitation, ratio, pollen concentration and pollutant concentration. Each is stored as a whole number in a fixed internal unit, converts to every unit Breezy Weather offers, and has its default unit by country.
- Weather calculations in `src/Calculations/`: apparent temperature, wind chill, humidex, dew point, relative humidity, total precipitation with snow and ice converted to liquid, precipitation probability, mean sea level pressure and pollutant concentration conversions.
- The weather model in `src/Models/`: current conditions, hourly, daily and half-day forecasts, minutely precipitation, alerts, air quality, pollen, wind, UV, sun and moon, normals, and the wrappers used before data from sources is merged.
- Locations in `src/Locations/` and source features in `src/Sources/`.
- SQLite storage for saved locations, their order, the source chosen for each weather feature and the parameters of those sources. The app opens on the first saved location, or on location search when no location is saved.
- SQLite storage for current conditions, daily and hourly forecasts, minutely precipitation, alerts and temperature normals. A completed refresh replaces all parts of a location's weather in one transaction, keeping the previous forecast if saving fails. Quantities retain their internal integer units, dates are stored as Unix milliseconds and durations as nanoseconds.
- A database migration that adds weather storage to existing location databases without recreating saved locations or their source parameters.
- Translations in English and French:
  - The string importer turns Breezy Weather's Android strings, plurals and unit strings into `.resx` files in `translations/`, for each shipped language. Breezy Weather's translations are under LGPL-3.0-only.
  - Text is looked up by Breezy Weather's keys, with English used for any string missing from the selected language, and plurals follow the CLDR rules of English and French.
  - The app uses the Windows display language when it is English or French, and English otherwise.
  - Temperatures are displayed with their translated unit, such as "4 °C" or "Ressenti 2°".
- Open-Meteo as a weather source: forecast, current conditions, 15-minute precipitation, air quality and pollen (pollen in Europe only), weather texts for each weather code, the choice of forecast models, and location search by name.
- The weather refresh: each feature is requested from the source chosen for it, data that is still fresh is reused, previous data is kept when a source fails, and the result is completed into one weather model. The completion computes day and night from the hourly forecast, guesses missing weather codes, fills in feels-like temperatures, dew point, precipitation totals, daily air quality, pollen, UV, humidity, pressure, cloud cover and visibility, and takes current conditions from the current hour. Sun and moon times are not computed.
- A first interface:
  - A location search screen, listing each place with its region and country, with Open-Meteo's attribution and a message when nothing is found or the search fails.
  - The top of the main screen for the chosen place: condition, temperature with its unit, feels-like temperature when it differs, and today's and tonight's temperatures.
  - Material 3 dark colors.
- Tests for temperature conversions, humidex, total precipitation, rounding and the Beaufort scale.
- Three weather database tests covering migration of a location database, restoration of stored quantities and forecasts, and rollback of a failed save.
- `ROADMAP.md` listing the ported and remaining parts of the engine, sources, translations, interface, Mistray and notifications.
- `docs/porting-pitfalls.md` listing the Kotlin, Java and Android constructs whose closest C# equivalent behaves differently, such as rounding, number formatting, `?:` after an `if`, sorting stability and network detection, with how to reproduce upstream's behaviour in C#.
- `docs/port-map.md` giving the Breezy Weather file behind each C# file and test, with deliberate differences from upstream.
- The Libreccio Weather icon on the executable and the main window, from `assets/icons/icon.ico`.
- Settings in `src/Settings/`, with Breezy Weather's keys and defaults, stored in `%LOCALAPPDATA%\Libreccio Weather\settings.json`: units, refresh interval, alert and precipitation notifications, dark mode, the cards and charts of the main screen, animations, today's and tomorrow's forecast times, and the ongoing notification's options for Mistray. A missing or damaged file gives the defaults, and each change is written through a temporary file. Settings with no meaning on Windows, such as the icon provider, the gravity sensor and home screen widgets, are left out.
- A language setting, stored with the other settings and chosen in Settings, Appearance: the Windows language, English or French, each under its own name. The new language applies at once to every open screen, and saved weather is marked as outdated, so the next refresh downloads texts in that language.
- The app reads the temperature unit and the language from the settings at startup.
- String arrays in the string importer. Each item is stored under the array name and its position, such as `dark_modes_0`, and every language gets every array with its `@string/` items resolved in that language. Options such as dark mode, refresh interval, notification style and background animation take their names from these arrays.
- Background refresh at the interval chosen in the settings, counted from the last update, so an update that fell due while the app was closed or the computer was asleep runs as soon as possible. It waits for a network, and for Windows' Battery saver to be off when "ignore updates when battery is low" is on, and retries 10, 20, 30 minutes later and so on when it cannot run. It refreshes the first location, plus the next three once a day when Mistray shows several locations, and the main screen shows the new weather. Debug builds do not refresh in the background by default.
- Breezy Weather's tests for number formatting, card and chart lists and option names, and a test of the settings file.
- A main window icon that follows the meteorological season at the location on screen, reversed in the southern hemisphere, from `assets/icons/winter.ico`, `spring.ico`, `summer.ico` and `autumn.ico`, with their sources `winter.svg`, `spring.svg`, `summer.svg` and `autumn.svg`, which are not part of the build. It is checked again at midnight, when the system clock changes and when Windows resumes. The executable keeps `icon.ico`.
- The main screen's top bar, with a button that opens the location list.
- The time since the last refresh at the top of the main screen header, such as "5 min. ago", next to a clock icon and written again every minute.
- The location list, as in Breezy Weather: a card for each saved location with its current weather, or "Last updated" with the time when its weather is outdated. The location on the main screen is highlighted, and clicking a card opens its location. Sliding a card towards the end past half its width deletes it, with an undo, while at least one location is kept. Dragging a card by its handle reorders the list. The add button opens the location search, then reports whether the place was added or already saved.
- Settings, opened from the location list: the settings home with every category, background updates (refresh interval, with a warning that must be read before turning updates off, the battery saver switch and the time of the last background update), appearance (language and units) and units (temperature, precipitation, speed, distance and pressure, each following the region or chosen). Categories not ported yet are shown greyed out. Changing a unit or the language rebuilds the main screen with it.
- Messages at the bottom of the window, such as "Location deleted" with Undo, shown for three seconds.
- Unit names, durations such as "1 hour, 30 minutes" and relative times such as "5 min. ago" in Unicode CLDR's words, through the ICU library that Windows 10 version 1903 and later include as `icu.dll`. The app checks for each function it needs. Without them, unit names and durations come from Breezy Weather's translated strings, such as "km/h" and "1 h, 30 min", and relative times are not shown.
- A "CLDR text" section in `docs/porting-pitfalls.md`, and the settings, background, seasons and new screen files in `docs/port-map.md`.
- This changelog.

### Changed

- The README logo points to `assets/icons/icon.svg`.
- Saved weather is loaded and displayed before the startup refresh, so the last stored forecast remains available when the network is unavailable. Missing sun times stay absent when weather is loaded, preserving the source's day and night flags during forecast completion.
- The native SQLite library used by `Microsoft.Data.Sqlite` is selected through an explicit `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 package reference.
- An updated app icon in `assets/icons/icon.ico` and `assets/icons/icon.svg`.
- At first launch the app opens on the empty location list, with a button to add a location, as Breezy Weather does, instead of on the location search.
- The main window opens once the first location has loaded, so it shows the right season's icon and the location from the start.
- The location search has a back button.
- The Cancel button of dialogs uses Windows' own text, as Breezy Weather uses Android's.

### Fixed

- Weather database test compilation errors caused by unit type qualifiers resolving to the test namespace instead of the engine's units.
- Weather database test build errors from xUnit's cancellation analyzer by passing the current test's cancellation token to asynchronous calls that accept one.
- Temperatures are formatted like Breezy Weather: whole numbers round halves upwards, so −2.5 °C shows as "−2" and no longer as "−3", decimals round halves to even without trailing zeros, so 4.0 °C shows as "4", and thousands are grouped. The feels-like, day and night temperatures no longer use a different rounding from the main temperature.
- The current air quality falls back to the current hour's when the reused current conditions have none.
- An empty day list from the forecast source keeps both the saved days and the saved hours, instead of mixing new hours with saved days.
- New locations get Open-Meteo pollen only within its European coverage, so refreshes elsewhere no longer report an unsupported feature.
- Today, the next 24 hours and the current alerts are worked out from when the weather was loaded, so the main screen no longer changes day around midnight before Breezy Weather would.
- Weather texts and other strings that Breezy Weather defines only as a pointer to another string, such as Open-Meteo's "Clear sky", are now translated: the French app shows "Ciel dégagé" instead of the English text.
