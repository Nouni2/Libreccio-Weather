# Changelog

All notable changes to Libreccio Weather are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Libreccio Weather icon in SVG format at `assets/icons/icon.svg`.
- README describing the project, its planned features, building from source, credits and licence.
- GNU General Public License v3.0 in `LICENCE.md`.
- `AGENTS.md` with the rules for contributors: comments, licence header, text and translations, changelog, naming, folder layout and code organization, pointing to `CONTRIBUTING.md` for code style.
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
- This changelog.

### Changed

- The README logo points to `assets/icons/icon.svg`.
- Saved weather is loaded and displayed before the startup refresh, so the last stored forecast remains available when the network is unavailable. Missing sun times stay absent when weather is loaded, preserving the source's day and night flags during forecast completion.
- The native SQLite library used by `Microsoft.Data.Sqlite` is selected through an explicit `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 package reference.

### Fixed

- Weather database test compilation errors caused by unit type qualifiers resolving to the test namespace instead of the engine's units.
- Weather database test build errors from xUnit's cancellation analyzer by passing the current test's cancellation token to asynchronous calls that accept one.
