# Port map

The Breezy Weather files each C# file ports. Paths on the right are relative to the folder named under each heading in the Breezy Weather repository.

## Units

`src/Units/` ports `weather-unit/src/main/kotlin/org/breezyweather/unit/`.

| C# file | Breezy Weather file |
|---|---|
| `Distance.cs`, `DistanceUnit.cs` | `distance/Distance.kt`, `distance/DistanceUnit.kt` |
| `PollenConcentration.cs`, `PollenConcentrationUnit.cs` | `pollen/PollenConcentration.kt`, `pollen/PollenConcentrationUnit.kt` |
| `PollutantConcentration.cs`, `PollutantConcentrationUnit.cs` | `pollutant/PollutantConcentration.kt`, `pollutant/PollutantConcentrationUnit.kt` |
| `Precipitation.cs`, `PrecipitationUnit.cs` | `precipitation/Precipitation.kt`, `precipitation/PrecipitationUnit.kt` |
| `Pressure.cs`, `PressureUnit.cs` | `pressure/Pressure.kt`, `pressure/PressureUnit.kt` |
| `Ratio.cs`, `RatioUnit.cs` | `ratio/Ratio.kt`, `ratio/RatioUnit.kt` |
| `Speed.cs`, `SpeedUnit.cs` | `speed/Speed.kt`, `speed/SpeedUnit.kt` |
| `Temperature.cs`, `TemperatureUnit.cs` | `temperature/Temperature.kt`, `temperature/TemperatureUnit.kt` |
| `UnitDecimals.cs`, `UnitWidth.cs` | `formatting/UnitDecimals.kt`, `formatting/UnitWidth.kt` |
| `WeatherUnit.cs` | `WeatherUnit.kt` |
| `UnitRounding.cs` | No file. It reproduces Kotlin's `roundToLong()` and `Double.toLong()`: ties towards positive infinity, saturation at the long bounds, NaN rejection for rounding and NaN-to-zero for truncation. The fractional part is compared before addition to avoid a floating-point addition changing the rounding decision. |

## Calculations

`src/Calculations/` ports `weather-unit/src/main/kotlin/org/breezyweather/unit/computing/`.

| C# file | Breezy Weather file |
|---|---|
| `HumidityCalculations.cs` | `HumidityComputing.kt` |
| `PollutantCalculations.cs` | `PollutantComputing.kt` |
| `PrecipitationCalculations.cs` | `PrecipitationComputing.kt` |
| `PressureCalculations.cs` | `PressureComputing.kt` |
| `TemperatureCalculations.cs` | `TemperatureComputing.kt` |

`src/Calculations/AirQualityIndex.cs` ports the composite `AirQuality.getIndex` in `app/src/main/kotlin/org/breezyweather/domain/weather/model/AirQuality.kt` and the contributing pollutant thresholds, interpolation and `getAqiToLevel` in `domain/weather/index/PollutantIndex.kt`. The maximum uses O3, NO2, PM10 and PM25, excluding SO2 and CO. Integer conversion retains Kotlin's upward half-rounding and saturation.

## Localization

| C# file | Breezy Weather file |
|---|---|
| `src/Localization/NumberFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/NumberFormatting.kt`. Windows' ICU supplies the number pattern, grouping and optional fraction digits. Integer precision first uses saturating `roundToInt()` semantics. Without ICU, .NET formatting preserves optional decimals and values above the decimal range; minimum grouping digits then match only the shipped English and French locales. |
| `src/Localization/UnitFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/MeasureUnitFormatting.kt`, `WeatherValue.formatValue`, `WeatherUnit.getDisplayName`, `WeatherUnit.format` and the temperature, precipitation, ratio, distance, pressure and speed unit definitions. Chart numbers follow selected-unit precision. Measured values use Windows' ICU; narrow temperatures retain the translated degree-only pattern. Speed retains the Beaufort language exceptions, French-Canadian and Swiss-German unit corrections, and compound per-second units. Distance, pressure and speed retain the Traditional Chinese unit correction. Percentages use the percent formatter when ICU number formatting is unavailable. |
| `src/Localization/PercentFormatting.cs` | The `NumberFormat.getPercentInstance` fallback in `ratio/RatioUnit.kt`. Preserves fraction conversion, half-even rounding, maximum fraction digits, nonpositive clamping, grouping and locale-specific percent placement. French CLDR's non-breaking space is restored because .NET's `PercentPositivePattern` retains placement but discards the spacing character. |
| `src/Localization/Icu.cs` | No file. It calls Windows' ICU, which Android's `android.icu` classes are built on. Each formatter is opened once per pattern, language and time zone and reused, behind one lock, since opening one costs far more than formatting with it. |
| `src/Localization/DateFormatting.cs` | `getFormattedShortDayAndMonth`, `getFormattedFullDayAndMonth`, `getWeek`, `getHour` and `getFormattedTime` in `app/src/main/kotlin/org/breezyweather/common/extensions/DateExtensions.kt`. Keeps the ICU boundary inside Core, location time zones and first-letter capitalization. Hour format follows Windows' 12/24-hour preference while text follows the selected language. Without ICU, full dates use the culture's month/day pattern and numeric dates use `MM-dd`. |
| `src/Localization/DurationFormatting.cs` | `format`, `formatValue` and `formatWithAndroidTranslations` in `weather-unit/src/main/kotlin/org/breezyweather/unit/duration/Duration.kt`, `DurationUnit.getPrecision` and `formatTime` in `app/src/main/kotlin/org/breezyweather/common/extensions/UnitExtensions.kt`. Compound hours/minutes/seconds use components truncated towards zero; single hours use zero, one or two optional decimals. Without ICU list support, the compound call falls back to decimal hours, matching the source call that omits `smallestUnit` when forwarding to its translated fallback. |
| `src/Localization/WindFormatting.cs` | `getDirection` and `getContentDescription` in `app/src/main/kotlin/org/breezyweather/domain/weather/model/Wind.kt`, with strength names from `wind_strength_descriptions`. Shared direction boundaries belong to the preceding range; gusts are described only above sustained speed. |
| `src/Localization/RelativeTimeFormatting.cs` | `getRelativeTime` in `app/src/main/kotlin/org/breezyweather/common/extensions/DateExtensions.kt`. The rules it calls are Android's `RelativeDateTimeFormatter`, which is not part of Breezy Weather. |
| `src/Localization/Strings.cs` | No file. It replaces Android's string resources. |
| `src/Localization/PluralRules.cs` | No file. It replaces Android's plural resources. |
| `src/Localization/AppLanguages.cs` | No file. |

## Settings

| C# file | Breezy Weather file |
|---|---|
| `src/Settings/SettingsManager.cs` | `app/src/main/kotlin/org/breezyweather/domain/settings/SettingsManager.kt`. Settings with no meaning on Windows are left out: the icon provider, the gravity sensor, home screen widgets and the choice of Android number formatters. The language is stored under `language`, where Android keeps it outside the app's settings, and changing it sets `language_update_last_timestamp`. |
| `src/Settings/ConfigStore.cs` | `app/src/main/kotlin/org/breezyweather/domain/settings/ConfigStore.kt`. A JSON file replaces Android's shared preferences. |
| `src/Settings/SettingsChangedEventArgs.cs` | No file. Carries the changed property name so notification-only settings can update the tray without rebuilding or refreshing the weather screen. |
| `src/Settings/Options/DarkMode.cs`, `UpdateInterval.cs`, `NotificationStyle.cs` | `app/src/main/kotlin/org/breezyweather/common/options/` |
| `src/Settings/Options/BackgroundAnimationMode.cs`, `CardDisplay.cs`, `DailyTrendDisplay.cs`, `HourlyTrendDisplay.cs` | `app/src/main/kotlin/org/breezyweather/common/options/appearance/` |
| `src/Settings/Options/OptionNames.cs` | `getName` and `getNameByValue` in `app/src/main/kotlin/org/breezyweather/common/utils/UnitUtils.kt` |

## Background

| C# file | Breezy Weather file |
|---|---|
| `src/Background/WeatherUpdateJob.cs` | `app/src/main/kotlin/org/breezyweather/background/weather/WeatherUpdateJob.kt`. The location itself is not refreshed, as current position and reverse geocoding are not ported. Widgets, shortcuts, data broadcasts and app update checks have no Windows counterpart. Error, alert and precipitation notifications are not ported. |
| `app/Background/WeatherUpdateScheduler.cs` | `setupTask` in `WeatherUpdateJob.kt`, with Android's WorkManager replaced by a timer counted from `weather_update_last_timestamp`. Windows' Battery saver takes the place of Android's low battery state: updates wait while it is on. |
| `app/Background/BatterySaver.cs` | No file. It reads whether Windows' Battery saver is on. |
| `src/Refresh/NetworkStatus.cs` | `isOnline` in `app/src/main/kotlin/org/breezyweather/common/extensions/NetworkExtensions.kt` |

The job timestamp records the start of an attempt, as upstream does. Deliberate difference: the Windows scheduler retries `Failure` as well as `Retry` and `AlreadyRunning`, rather than waiting for the next normal interval after a failed job. Retries use linear delays of 10, 20, 30 minutes and so on, capped at five hours. The pending retry deadline survives network and power events and is unaffected by unrelated settings changes. Turning background updates off cancels pending retries; disposal prevents a completing job from arming the timer again. The job reports failure after a per-location exception or a network, timeout, rate-limit, server-unavailable, parsing, incomplete-data, outdated-data or generic refresh error, even when other locations succeed. Available weather is still published for each returned result. Failed secondary locations remain eligible on the next attempt even when completion advanced their refresh timestamp; a successful retry clears this exception to the once-daily rule. Only locations in the automatic selection retain pending retries. Configuration errors retain upstream's job-success behavior, and cancellation still counts as success. Upstream records per-location failures without failing the job.

Incompatible forecast and air-quality hours also request a retry.

## Process lifetime

| File | Responsibility and source relationship |
|---|---|
| `src/Processes/SingleInstance.cs` | No upstream file. A named mutex in Windows' global namespace, qualified by the user's security identifier, permits one instance per user before settings or SQLite are opened. The mutex is an existence marker, without thread ownership, so asynchronous continuations can dispose it safely. A duplex named pipe accepts activation requests from the same user and elevation level. The launching process receives the existing process ID and calls `AllowSetForegroundWindow` before requesting activation. Connections and handshakes have a five-second timeout. |
| `app/App.xaml.cs` | Starts the activation listener before initializing persistence. A later launch requests activation and exits without opening settings or the database. Activation received during startup is retained until the window is ready, then uses Mistray's existing show/restore behavior, including its previous non-minimized window state, and shows the already-running notice. |
| `app/ViewModels/AlreadyRunningDialogViewModel.cs`, `app/MainWindow.xaml`, `app/Theme/Theme.xaml` | Windows-only duplicate-launch notice using the `AlertDialog` pattern in `app/src/main/kotlin/org/breezyweather/ui/main/dialogs/LocationHelpDialog.kt`: title, body and one `action_close` text button. `DialogSurface` shares the existing dialog dimensions, theme colors and corner resource across the list, background-update warning and notice. The notice preserves a dialog already open underneath it; repeated launches reuse the same notice. Its English and French message is in `resources/InterfaceStrings.resx` and `InterfaceStrings.fr.resx`. The duplicate-launch message has no upstream counterpart. |
| `tests/Processes/SingleInstanceTests.cs` | No upstream test. Checks Windows mutex-handle lifetime and repeated activation handshakes through real named pipes, including disposal after asynchronous continuations. |

Windows can deny foreground activation, and the named pipe rejects a different elevation level. Such a request does not start another instance. The already-running notice is shown by the existing instance after an activation request reaches it.

## Seasons

| C# file | Breezy Weather file |
|---|---|
| `src/Seasons/Season.cs`, `src/Seasons/MeteorologicalSeasons.cs` | No file. They give the season that chooses Libreccio Weather's window icon, which Breezy Weather does not have. |

## Model

`src/Models/` ports `domain/src/main/kotlin/breezyweather/domain/weather/`.

| C# file | Breezy Weather file |
|---|---|
| `AirQuality.cs`, `Alert.cs`, `Astro.cs`, `Base.cs`, `Current.cs`, `Daily.cs`, `DailyAvgMinMax.cs`, `DegreeDay.cs`, `HalfDay.cs`, `Hourly.cs`, `Minutely.cs`, `MoonPhase.cs`, `Normals.cs`, `Pollen.cs`, `Precipitation.cs`, `PrecipitationDuration.cs`, `PrecipitationProbability.cs`, `Temperature.cs`, `UV.cs`, `Weather.cs`, `Wind.cs` | The file of the same name in `model/` |
| `AlertSeverity.cs`, `Month.cs`, `WeatherCode.cs` | The file of the same name in `reference/` |
| `Wrappers/*.cs` | The file of the same name in `wrappers/` |

Deliberate difference in `Weather.cs`: `Tomorrow` uses the weather object's construction time, like `TodayIndex` and the upcoming hours. Upstream's `tomorrow` getter reads the clock on every access. The port keeps the day selection consistent within a weather snapshot across midnight.

## Locations

`src/Locations/` ports `domain/src/main/kotlin/breezyweather/domain/location/model/`.

| C# file | Breezy Weather file |
|---|---|
| `Location.cs` | `Location.kt` |
| `LocationAddressInfo.cs` | `LocationAddressInfo.kt` |

## Sources

| C# file | Breezy Weather file |
|---|---|
| `src/Sources/SourceContinent.cs`, `SourceFeature.cs` | `domain/src/main/kotlin/breezyweather/domain/source/SourceContinent.kt`, `SourceFeature.kt` |
| `src/Sources/SourceExceptions.cs` | `app/src/main/kotlin/org/breezyweather/common/exceptions/InvalidLocationException.kt`, `LocationSearchException.kt` |
| `src/Sources/OpenMeteo/OpenMeteoService.cs` | `app/src/main/kotlin/org/breezyweather/sources/openmeteo/OpenMeteoService.kt`, `OpenMeteoForecastApi.kt`, `OpenMeteoAirQualityApi.kt`, `OpenMeteoGeocodingApi.kt` |
| `src/Sources/OpenMeteo/OpenMeteoJson.cs` | `app/src/main/kotlin/org/breezyweather/sources/openmeteo/json/` |
| `src/Sources/OpenMeteo/OpenMeteoWeatherModel.cs` | `app/src/main/kotlin/org/breezyweather/sources/openmeteo/OpenMeteoWeatherModel.kt` |

## Refresh

`src/Refresh/` ports `app/src/main/kotlin/org/breezyweather/`.

| C# file | Breezy Weather file |
|---|---|
| `RefreshError.cs` | `common/source/RefreshError.kt` |
| `WeatherCompletion.cs` | `sources/CommonConverter.kt`. Sun and civil twilight use Seasonal++ through `AstroCalculation`; moon times and phase use the commons-suncalc translation. The Sun and moon section records the calculation differences. |
| `WeatherRefresher.cs` | `sources/RefreshHelper.kt` |

## Database

`src/Database/` ports `data/src/main/`.

| C# file | Breezy Weather file |
|---|---|
| `LocationRepository.cs` | `kotlin/breezyweather/data/location/LocationRepository.kt`, `sqldelight/breezyweather/data/locations.sq`, `location_parameters.sq` |
| `WeatherRepository.cs`, `WeatherWriter.cs` | `kotlin/breezyweather/data/weather/WeatherRepository.kt` |
| `WeatherReader.cs` | `kotlin/breezyweather/data/weather/WeatherMapper.kt` |
| `WeatherSchema.cs`, `WeatherDatabase.cs` | `sqldelight/breezyweather/data/weathers.sq`, `dailys.sq`, `hourlys.sq`, `minutelys.sq`, `alerts.sq`, `normals.sq` |

Deliberate differences in `LocationRepository.cs`: source parameters are replaced as a complete set in the surrounding transaction, removing obsolete names even when they equal a retained source identifier. Upstream compares parameter names with source identifiers. A formatted-ID rename defers foreign-key checks until commit and updates the seven dependent tables before committing; a collision or a failed dependent update rolls back the transaction. Upstream updates only the parent identifier. The schema remains unchanged, so the same transaction handles existing databases.

## Application

`app/` ports `app/src/main/kotlin/org/breezyweather/ui/`.

| C# file | Breezy Weather file |
|---|---|
| `MainWindow.xaml.cs` | `main/MainActivity.kt` |
| `ViewModels/MainViewModel.cs` | `main/MainActivityViewModel.kt` |
| `ViewModels/WeatherViewModel.cs`, `Views/WeatherView.xaml.cs` | `main/adapters/main/holder/HeaderViewHolder.kt` |
| `ViewModels/SearchViewModel.cs` | `search/SearchViewModel.kt` |
| `Views/SearchView.xaml.cs` | `search/SearchActivity.kt` |
| `ViewModels/LocationResult.cs` | No file. |
| `ViewModels/MainViewModel.cs`, `ViewModels/WeatherViewModel.cs` | `onUpdateResult`, `updateInnerData`, `updateLocationFromBackground` and `setLocation` in `main/MainActivityViewModel.kt`. Manual and background results both replace the matching saved location in memory and refresh the visible location list and pane. Returning to a location list in navigation history rebuilds its cards, and selection resolves the latest saved in-memory record by its formatted ID. A result for a location no longer selected does not switch the current screen. A result already rendered by a weather screen is not rendered again by the shared update path or a screen following that refresh. |
| `ViewModels/MainViewModel.cs` | `main/MainActivity.kt`, `main/MainActivityViewModel.kt`, for the screens, the location list and the messages. `setManagementFragmentVisibility` and `onManageIconClicked` become `UpdateWindowShape` and the list button folding the pane: it opens in landscape or with no location, and stays folded with exactly one. The pane appears only beside the main screen, since settings and the search are separate activities in Breezy Weather. Showing a location refreshes it only when its weather is no longer valid for the update interval, as `setCurrentLocation` does through `checkToUpdateCurrentLocation`; rebuilding the main screen after a settings change follows the same rule. Deliberate difference: weather refreshed before the last language change is also refreshed when shown, so its weather texts follow the new language at once; upstream keeps them in the previous language until the update interval passes. As upstream's `updating` flag does, a refresh never starts while one of the same location is running: choosing the location already shown keeps its screen, as `setLocation` ignores the current location, and a rebuild during a refresh shows that refresh's result instead of starting another. Upstream's wait for `initCompleted` has no counterpart, because no location is shown before `LoadAsync` has loaded them all. |
| `ViewModels/ManagementViewModel.cs`, `Views/ManagementView.xaml.cs` | `main/fragments/ManagementFragment.kt`, `main/widgets/LocationItemTouchCallback.kt`. A mouse drag stands for a finger swipe; sliding towards the start only slides back, as source settings are not ported. |
| `Controls/DrawerPanel.cs`, `MainWindow.xaml` | `common/widgets/DrawerLayout.kt`, with `res/layout-w640dp/activity_main.xml` and `getTabletListAdaptiveWidth` in `common/extensions/DisplayExtensions.kt`. In windows at least 640 pixels wide, the location list is a pane beside the main screen: the window width minus 600, between 280 and 320 pixels, sliding over 450 ms for a full fold with the 1 − (1 − t)⁴ deceleration of `DecelerateInterpolator(2f)`. The tablet list width, 600, always applies, as a Windows window is not a phone. Windows has no screen orientation, so landscape means a window wider than tall, and crossing 640 pixels or that proportion lays the screen out again as Android's recreated activity does. The pane has no 2 dp elevation shadow, and widths are not truncated to whole pixels. With no saved location, the location list stays a full screen instead of a pane beside an empty main screen. |
| `ViewModels/LocationItemViewModel.cs` | `main/adapters/location/LocationModel.kt`, `LocationHolder.kt`. Without ICU, outdated weather does not show "Last updated". |
| `ViewModels/SnackbarViewModel.cs` | `app/src/main/kotlin/org/breezyweather/common/utils/helpers/SnackbarHelper.kt` |
| `ViewModels/Settings/SettingsPages.cs` | `settings/compose/RootSettingsScreen.kt`, `BackgroundUpdatesSettingsScreen.kt`, `AppearanceSettingsScreen.kt`, `UnitSettingsScreen.kt` and the notification-widget section of `ModulesSettingsScreen.kt`. The notification section preserves its five controls, order, source labels, options, saved keys, defaults and dependent enabled states. Windows tray icons need no Android notification permission request. Categories not ported are shown disabled. Without ICU, unit and interval names come from strings, as in Breezy Weather with "use measure format" off. |
| `ViewModels/Settings/PreferenceItems.cs`, `ListDialogViewModel.cs`, `SettingsPageViewModel.cs` | `settings/preference/` |
| `ViewModels/Settings/NeverRefreshDialogViewModel.cs` | The dialog in `settings/compose/BackgroundUpdatesSettingsScreen.kt` |
| `Localization/SystemStrings.cs` | No file. It takes the Cancel text from Windows, as Breezy Weather takes it from Android. |
| `App.xaml.cs`, `Localization/TranslateExtension.cs` | No file. |
| `Icons/SeasonalWindowIcon.cs` | No file. It gives the main window Libreccio Weather's icon for the current season, which Breezy Weather does not have. |

Deliberate difference in `MainViewModel.cs`: adding, restoring and reordering locations publish the new in-memory list only after `LocationRepository.AddAllAsync` succeeds. A failed save keeps the existing list and selection, reports the failure and does not show the location-added message. Weather received while the save was pending is retained. Upstream updates memory before the write and ignores the repository's failure result. The save-failure snackbar has no matching upstream string; its English and French text is in `resources/InterfaceStrings.resx` and `InterfaceStrings.fr.resx`.

`SearchViewModel.SearchAsync` uses the generated asynchronous relay command with its default concurrent-execution setting. `SearchView.xaml` binds Enter to that command, so the normal WPF interaction cannot start an overlapping search. No request-version tracking is added.

## Main-screen scrolling and forecasts

Both interface modes use the scrolling weather screen in `app/Views/WeatherView.xaml`. Its header wraps its content, as `container_main_header.xml` does, and shares the vertical scroll viewer with the forecast cards. Daily and hourly cards follow their order in `SettingsManager.CardDisplayList`, and are omitted when their corresponding forecast is empty or none of the implemented charts is enabled and valid. Temperature, feels-like, precipitation and hourly humidity/dew-point tabs follow the saved chart order. The feels-like tab requires at least one feels-like value and falls back to the actual temperature for individual missing values; its bounds exclude temperature normals. A selector is hidden when only one chart is available. Selected tab names live in `MainViewModel`, matching `MainAdapter`'s in-session state, and unavailable selections reset. Both modes show the app's own thin scrollbar, the implicit `ScrollBar` style in `app/Theme/Theme.xaml`. Breezy Weather's main list has none (`android:scrollbars="none"` in `fragment_home.xml`), so this is a deliberate difference. Both modes support vertical wheel scrolling, and Shift+wheel scrolls a forecast horizontally.

| File | Breezy Weather file |
|---|---|
| `app/Appearance/WeatherBackground.cs` | `WeatherImplementorFactory.getBackgroundId` in `ui-weather-view/src/main/kotlin/org/breezyweather/ui/theme/weatherView/materialWeatherView/`, with weather-code mapping from `WeatherViewController.kt`. |
| `app/Theme/WeatherBackgrounds.xaml` | The gradients in `ui-weather-view/src/main/res/drawable-night/weather_background_*.xml`, falling back to `drawable/` where there is no night-qualified resource. The application uses its existing dark theme. |
| `app/Icons/WeatherIcons.cs`, `assets/weather/weather_*.png` | Static weather artwork from `app/src/main/res/drawable/`, selected by weather code and daylight. |
| `app/Appearance/PrecipitationColors.cs` | `getHalfDayPrecipitationColor` and `getHourlyPrecipitationColor` in `app/src/main/kotlin/org/breezyweather/domain/weather/model/Precipitation.kt`, with `colorLevel_1` through `colorLevel_5` in `app/src/main/res/values/colors.xml`. Inclusive ranges keep exact threshold values in the preceding intensity level; null and negative totals have no color. |
| `app/ViewModels/Forecast/ForecastCardViewModel.cs` | `DailyTrendAdapter.kt`, `HourlyTrendAdapter.kt`, `daily/DailyTemperatureAdapter.kt`, `daily/DailyFeelsLikeAdapter.kt`, `hourly/HourlyTemperatureAdapter.kt`, `hourly/HourlyFeelsLikeAdapter.kt` and their base classes under `app/src/main/kotlin/org/breezyweather/ui/main/adapters/trend/`. Raw temperature values and interpolated midpoints retain 32-bit precision; normal temperatures participate only in actual-temperature bounds. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Precipitation.cs` | `daily/DailyPrecipitationAdapter.kt` and `hourly/HourlyPrecipitationAdapter.kt` in the same source folder. Daily scaling starts at 57.75 mm per half-day and hourly scaling at 15 mm; larger forecast totals extend it. Daily zero labels are hidden; hourly known zero totals show a zero label. Missing totals retain null labels. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Humidity.cs` | `hourly/HourlyHumidityAdapter.kt` in the same source folder. Availability requires a dew point, with raw dew-point bounds and interpolated midpoints. Humidity uses a separate 0–100 percent scale, and zero humidity is hidden while dew-point bounds exist. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Pressure.cs` | `hourly/HourlyPressureAdapter.kt` in the same source folder. Bounds start at standard atmospheric pressure, and the chart is unavailable when both remain at that value. Labels use narrow selected-unit precision. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Visibility.cs` | `hourly/HourlyVisibilityAdapter.kt` in the same source folder. Raw meter values and interpolated midpoints use a zero minimum and the forecast maximum; labels use short selected-unit precision. |
| `app/ViewModels/Forecast/ForecastCardViewModel.CloudCover.cs` | `hourly/HourlyCloudCoverAdapter.kt` in the same source folder. Bars use a 0–100 percent scale and no weather-icon row. |
| `app/ViewModels/Forecast/ForecastCardViewModel.UV.cs` | `daily/DailyUVAdapter.kt` and `hourly/HourlyUVAdapter.kt` in the same source folder. The minimum maximum is 8, bar heights use integer rounding, and the alert reference is 6. Missing indices have no label and reserve no icon rows. |
| `app/ViewModels/Forecast/ForecastCardViewModel.AirQuality.cs` | `daily/DailyAirQualityAdapter.kt` and `hourly/HourlyAirQualityAdapter.kt` in the same source folder. The minimum maximum is index 150, with reference lines at 20, 100 and 250, and availability requires an aggregate index. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Wind.cs` | `daily/DailyWindAdapter.kt` and `hourly/HourlyWindAdapter.kt` in the same source folder. Bounds include gusts and start at 15 m/s. Stronger gusts have a secondary bar and pill; empty labels reserve space across the series, separately for day and night. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Sunshine.cs` | `daily/DailySunshineAdapter.kt` in the same source folder. The maximum is the longest available solar duration, then forecast sunshine, then zero. Bars truncate to whole minutes and cap against that maximum; labels retain the uncapped duration in decimal hours and accessibility text uses hours and minutes. Availability requires a positive whole-minute duration. |
| `src/Localization/UVFormatting.cs`, `app/Appearance/ForecastColors.cs` | UV names, descriptions and colors in `app/src/main/kotlin/org/breezyweather/domain/weather/model/UV.kt`; wind colors in `domain/weather/model/Wind.kt`; AQI colors in `domain/weather/index/PollutantIndex.kt` and `values/arrays.xml`; cloud-cover colors in `common/extensions/UnitExtensions.kt`; and `values/colors.xml`. Cloud cover at exactly 62.5 percent retains level 2. |
| `app/Icons/WindIcons.cs` | `Wind.drawableArrow` in `app/src/main/kotlin/org/breezyweather/domain/weather/model/Wind.kt`, the eight `drawable/arrow_*.xml` vectors and `drawable/ic_replay.xml`. The original path data and viewports are retained in frozen WPF drawing images, tinted by wind strength. |
| `app/ViewModels/Forecast/ForecastDescriptions.cs` | The description builders in the temperature and feels-like adapters, and the date/hour prefixes in `daily/AbsDailyTrendAdapter.kt` and `hourly/AbsHourlyTrendAdapter.kt`. Preserves today/yesterday/tomorrow, full weekday/date text, temperature/condition/probability order and source separators. The same prefixes are used by precipitation and humidity descriptions and exposed through the columns' automation names. |
| `app/Controls/Forecast/ForecastChartView.cs` | Shared title-text appearance, `setShadowLayer(2f, 0f, 1f, ...)` and `dpToPx(...).toInt()` behavior used by the source chart renderers. A separate visual applies the 20-percent black shadow only to labels, with a two-device-pixel blur radius and one-device-pixel downward offset. Histogram half-width truncates in device pixels before returning to WPF coordinates. |
| `app/Controls/Forecast/PolylineAndHistogramView.cs` | Lines, shadow, time line and histograms from `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/chart/PolylineAndHistogramView.kt`. Wind uses opaque sustained-speed bars, 30-percent gust bars, 22 DIPs reserved for gust labels and the source pill dimensions. Other histogram colors and alpha follow their adapters. |
| `app/Controls/Forecast/DoubleHistogramView.cs` | Day and night precipitation and wind bars from `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/chart/DoubleHistogramView.kt`, with 24-DIP margins, 4-DIP center gap, 8-DIP bar width and day/night alpha of 255/127. Gust labels add 22 DIPs independently to each margin; secondary bars use 30-percent alpha. |
| `app/Controls/Forecast/TrendReferenceLinesView.cs` | `drawKeyLines` in `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/TrendRecyclerView.kt`. Normal-temperature, standard-pressure, UV-alert and AQI-level lines follow the horizontal-lines setting; labels stay at the viewport edges. WPF measures boundaries from the first rendered column and selects its polyline or histogram scale. |
| `src/Charts/TrendCoordinates.cs` | `computeSingleCoordinate` in `PolylineAndHistogramView.kt`, `computeCoordinates` in `DoubleHistogramView.kt` and the reference-line coordinate expression in `TrendRecyclerView.drawKeyLines`, preserving their different arithmetic order and saturating `Float.toInt()` conversion. |
| `app/Views/Weather/ForecastCardView.xaml`, `ForecastCardView.xaml.cs` | `DailyViewHolder.kt`, `HourlyViewHolder.kt`, their `container_main_*_trend_card.xml` layouts, `DailyTrendItemView.kt` and `HourlyTrendItemView.kt`. Columns are 67 pixels wide, icons 32, daily columns 318 high and hourly columns 206 high in WPF device-independent pixels. |
| `tests/Charts/TrendCoordinatesTests.cs` | No upstream test. Checks the C# conversion difference when equal plot/reference-line bounds or a zero paired-histogram maximum yield non-finite coordinates. There are no upstream tests for these forecast adapters, precipitation colors or chart renderers. |
| `tests/Localization/PercentFormattingTests.cs` | No upstream test. Checks .NET percent-format differences: half-even integer rounding, maximum fraction digits without zero padding, nonpositive clamping and the English/French percent spacing. |
| `tests/Localization/UnitFormattingTests.cs` | No upstream test. Checks the .NET conversion differences for Kotlin `roundToInt()`: upward half-rounding, saturation and NaN rejection. |

The static background gradients are included; the animated weather implementors are not. Daylight uses the forecast's first current-hour flag while solar calculations are unavailable. Temperature, dew-point and reference-line rendering uses the current theme colors and Windows font rasterization; Android and WPF font metrics and blur kernels cannot be assumed pixel-identical. Precipitation, UV, AQI, cloud-cover, wind and sunshine bars use the source palettes. All daily and hourly chart types are included. The tab selector wraps when space is limited, in place of Android's connected button group with an overflow menu. Detail navigation and the condition, sun and moon cards are implemented; nowcast, alerts, clock and footer remain unported. Without ICU, numeric daily dates use `MM-dd` and measured values use the source translated unit patterns.

## Current-weather cards

| C# file | Breezy Weather source |
|---|---|
| `app/ViewModels/WeatherCards/ConditionsCardViewModel.cs` | `MainAdapter.update` and `PrecipitationViewHolder.kt`, `WindViewHolder.kt`, `AirQualityViewHolder.kt`, `HumidityViewHolder.kt`, `UvViewHolder.kt`, `VisibilityViewHolder.kt`, `PressureViewHolder.kt` and `PollenViewHolder.kt` under `app/src/main/kotlin/org/breezyweather/ui/main/adapters/main/`. Preserves saved order and availability, precipitation's 05:00/17:00 half-day boundaries, current-then-today AQI fallback, humidity artwork thresholds, UV shapes, visibility categories, the 963–1063 hPa gauge, and two stable-sorted primary pollens excluding mold. Source-specific pollen index providers are not ported; Open-Meteo uses the standard concentration thresholds. |
| `src/Calculations/PollenIndex.cs` | `app/src/main/kotlin/org/breezyweather/domain/weather/index/PollenIndex.kt` and concentration lookup in `domain/weather/model/Pollen.kt`. Preserves all 21 pollen types, interpolation order, integer saturation and the zero index for negative concentrations. |
| `app/Controls/WeatherCards/ConditionsCardView.cs` | The seven `container_main_*.xml` condition layouts and `container_main_pollen.xml`, `ArcProgress.kt`, and `UnitUtils.formatUnitsHalfSize`. Uses the source square, circular and rounded shapes, percentage guidelines, 16-point titles, 45-point values, 48-point visibility autosizing, half-sized unit spans, 270-degree gauges and stronger-gust captions. WPF text metrics determine line fitting and the dew-point badge placement. Card elevation, enter animations and detail actions are not implemented. |
| `app/Controls/WeatherCards/WeatherCardsPanel.cs` | `HomeFragment` span-size selection, `MainLayoutManager.kt` and `DisplayExtensions.getBlocksPerRow`. Forecasts occupy a full row and condition cards fill one to five square columns. Widths use the 160-DIP unscaled block size and the 1.1 factor for more than two columns. The minimum block width follows the font-scale formula using Windows' text-size preference. Span widths use [GridLayoutManager.calculateItemBorders](https://github.com/androidx/androidx/blob/androidx-main/recyclerview/recyclerview/src/main/java/androidx/recyclerview/widget/GridLayoutManager.java) to distribute physical-pixel remainders, and compact blocks use the source's 1.1 threshold. The available weather-pane width stands in for the Android window width. |
| `app/Theme/WeatherCardArtwork.xaml`, `Theme.xaml` | `drawable/humidity_percent_*.xml`, `wind_arrow.xml`, `wind_variable.xml`, `visibility_shape.xml`, `uv_*.xml`, the corresponding title icons, and dark resource colors. Vector paths, viewports, clipping, layer order and fill alpha are retained as WPF drawings. Theme references use the app's brushes. Automatic artwork mirroring for RTL languages is not implemented. |
| `app/Views/WeatherView.xaml`, `app/ViewModels/WeatherViewModel.cs` | `MainAdapter` dispatch and card ordering. Typed data templates select forecast or condition content in one ordered collection. Cards are rebuilt from refreshed weather and the selected language and units. |

## Sun and moon cards

| C# file | Breezy Weather source |
|---|---|
| `app/ViewModels/WeatherCards/AstroCardViewModel.cs` | `AstroViewHolder.kt`, `SunViewHolder.kt`, `MoonViewHolder.kt` and the availability checks in `MainAdapter.kt`. Sun requires today's sun object, and moon requires valid rise and set times. Preserves the 24-hour-minus-one-millisecond polar-day sentinel, polar-night labels, location-specific times, yesterday's moon while it is still up, and today's phase description. |
| `app/Controls/WeatherCards/AstroCardView.cs` | `container_main_astro.xml`, `SunMoonView.kt`, `SunDrawable.kt` and `MoonDrawable.kt`. Keeps the 135-degree arc, 16-DIP inner margins, 5-DIP solid line, 1-DIP dotted line with 3/6 dashes, 24-DIP body, clear-sky colors, half-opacity dotted lines and the shadow blended into the source's card-gradient base. Path and rotation use the tension-1 overshoot interpolator and the holders' capped durations; the moon phase uses quadratic deceleration. Animations follow the element-animation switch, start once when the card intersects the scroll viewport, use the monitor refresh-rate helper and release their clocks on unload. |
| `app/Views/WeatherView.xaml`, `WeatherView.xaml.cs`, `app/ViewModels/WeatherViewModel.cs`, `app/Controls/WeatherCards/WeatherCardsPanel.cs` | `MainAdapter` dispatch and the astro holder's click handler. Sun and moon participate in the saved card order, use the shared square-card grid and open today's Sun & Moon detail page. |

| `app/Controls/WeatherCards/WeatherCardsPanel.cs`, `CardOvershootEase.cs` | `AbstractMainViewHolder.kt`, `HeaderViewHolder.kt`, `MainModuleUtils.kt` and `DisplayExtensions.getFloatingOvershotEnterAnimators`. Card entry combines alpha, 120-DIP vertical translation and 1.025-to-1 scaling. Waiting cards add 200 ms to the delay, reduce the duration from 500 ms to a 250-ms minimum, and increase overshoot tension from 0.4 by 0.2. The header's 300-ms fast-out-slow-in fade waits 100 ms in that same queue. The pending queue releases each card at the start of its movement, when its element animation begins. The list-animation preference is independent of the element-animation preference. Clocks and pending callbacks are released on removal or unload. |
| `app/Controls/WeatherCards/AstroCardText.cs`, `assets/fonts/Roboto-Medium.ttf`, `Roboto-Bold.ttf` | The astro layout's `TextAppearance.Material3.LabelLarge`, with its 16-sp bold title override. Bundled Roboto faces replace the system message font. WPF text shaping is retained; the 0.00714286-em letter spacing is applied at glyph-cluster boundaries, and truncation preserves combining characters. Labels use 20-sp line heights; their drawing origin adds the line baseline to the supplied top edge, keeping title and phase icons aligned with their text. Rise/set labels shrink and ellipsize instead of overlapping. The inherited type tokens are defined in [Material Components 1.14.0](https://github.com/material-components/material-components-android/tree/1.14.0/lib/java/com/google/android/material/typography/res). |
| `app/Display/TextScale.cs` | `DisplayExtensions.fontScale` and `minBlockWidth`. Windows' text-size preference supplies the font multiplier; per-monitor DPI supplies physical-pixel density. |

Sun/moon cards use the source's dark card surface, title, text and phase-outline colors. The inherited [Material card style](https://github.com/material-components/material-components-android/blob/1.14.0/lib/java/com/google/android/material/card/res/values/styles.xml) selects the outlined card's surface color (#1B1B1D); `SunMoonView` separately uses `colorMainCardBackground` (#1C1B1F) as its gradient base. Both are preserved. A separate background layer carries the 2-DIP elevation shadow so that text and icons do not cast shadows. Compact blocks move the title guideline to 5 percent and the moon description baseline to 95 percent. Arc widths, radii, heights, centers, icon bounds and phase padding follow the source's physical-pixel truncation before conversion back to WPF coordinates; drawable radius offsets remain two physical pixels. A zero-length interval draws the dotted arc without a path or body.

WPF supplies the shadow blur, glyph rasterization and animation scheduling. Their output needs comparison on screen against Android; the 2-DIP `DropShadowEffect` is an equivalent elevation treatment, not an implementation of Android's ambient and spot shadow renderer.

## Detail screens

| C# file | Breezy Weather source |
|---|---|
| `src/Settings/Options/DetailScreen.cs` | `app/src/main/kotlin/org/breezyweather/common/options/appearance/DetailScreen.kt`. The entries, their order, names, `CHART_MIN_COUNT` and the availability rules of `toDetailScreenList`. Icons are chosen in the application layer. |
| `src/Calculations/PollutantIndex.cs`, `src/Calculations/AirQualityIndex.cs` | `domain/weather/index/PollutantIndex.kt` and `domain/weather/model/AirQuality.kt`. Thresholds, chart minimums, string names, units, the full name with the particle size formatted as a micrometer length, per-pollutant index and level, and the index, name, description and harmless-exposure lookups. |
| `src/Charts/DetailsChartAxes.cs` | `roundUpToNearestMultiplier` and `roundDownToNearestMultiplier` in `common/extensions/NumberExtensions.kt`, `toTimezoneSpecificHour` in `common/extensions/DateOldExtensions.kt`, and `TimeHorizontalAxisItemPlacer` and `TimeTopAxisItemPlacer` in `ui/common/charts/compose/AxisItemPlacer.kt`. Top labels convert the label count with Kotlin's saturating `Float.toInt()`. A skipped local hour is read with the previous day's offset, as Java's lenient calendar uses the offset before the change; a repeated hour takes its first occurrence, which is not checked against Java. |
| `src/Localization/CloudCoverFormatting.cs`, `VisibilityFormatting.cs` | Cloud cover and visibility constants, scale thresholds and descriptions in `common/extensions/UnitExtensions.kt`. |
| `src/Localization/DailyRangeFormatting.cs` | `getRangeSummary`, `getRangeDescriptionSummary` and `getRangeContentDescriptionSummary` in `domain/weather/model/DailyRelativeHumidity.kt`, `DailyCloudCover.kt`, `DailyDewPoint.kt` and `DailyVisibility.kt`. |
| `src/Localization/DateFormatting.cs` | Adds `getFormattedDayOfTheMonth`, the `MMM` best pattern of the day tabs, and `Daily.getFullLabel` in `domain/weather/model/Daily.kt`. Without ICU the .NET patterns `dd`, `MMM` and `dddd d MMMM` are used. |
| `src/Localization/UnitFormatting.cs` | Adds signed temperatures through ICU's `sign-always`, plain percent and temperature values, precipitation values given as numbers, pollutant concentrations and pollen per cubic meter from the translated unit strings, as upstream formats units without a measure unit. |
| `src/Localization/WindFormatting.cs` | Adds `Speed.getBeaufortScaleStrength` in `common/extensions/UnitExtensions.kt`. |
| `app/ViewModels/Details/DetailsViewModel.cs` | `ui/details/DetailsActivity.kt`, `DetailsViewModel.kt`, `DetailsUiState.kt` and the screen state of `DetailsScreen.kt`. The location comes from the main window's in-memory list instead of being read again from the database. Deliberate difference: updates from `MainViewModel.UpdateLocation` refresh matching detail screens, including those retained in back and forward history. The selected date and page are retained when available, and the selected pollutant is kept. A missing date falls back to today, and a missing page to conditions. Upstream loads its detail snapshot once. |
| `app/ViewModels/Details/DetailsPage.cs` | `DailyPagerContent` in `ui/details/DetailsScreen.kt`, `ui/details/components/DetailsCommon.kt`, `UnitUtils.formatUnitsDifferentFontSize` and `formatPollutantName`, and the chart parameters of `ui/common/charts/compose/BreezyLineChart.kt` and `BreezyBarChart.kt`. An unset chart minimum is the lower of zero and the lowest value, Vico's default. Deliberate difference: current conditions belong to the snapshot's `Today`, rather than checking the clock again while rebuilding a page. |
| `app/ViewModels/Details/DetailsPage.Conditions.cs` | `ui/details/components/DetailsConditions.kt` and the precipitation probability parts of `DetailsPrecipitation.kt`. The month in the normals explanation uses .NET's genitive month names, the form of Java's `DateFormatSymbols.getMonths`. The cooling degree day reads the heating degree day aloud, as upstream does, and has no spoken text when the heating degree day is missing, where upstream would fail. |
| `app/ViewModels/Details/DetailsPage.Precipitation.cs` | `ui/details/components/DetailsPrecipitation.kt`. `formatTime` with its default smallest unit gives decimal hours (`FormatHours`), and with minutes the hours and minutes (`FormatTime`). |
| `app/ViewModels/Details/DetailsPage.Wind.cs` | `ui/details/components/DetailsWind.kt`, including the header that keeps the current wind after a press ends. The scale uses the region's default speed unit when Beaufort is selected. Deliberate difference: when daily summaries are missing, the initial header displays `defaultValue.Wind` alongside its timestamp. Upstream passes the null active item and omits the valid current wind. |
| `app/ViewModels/Details/DetailsPage.AirQuality.cs` | `ui/details/components/DetailsAirQuality.kt`. The pollutant selector wraps in narrow windows instead of using Android's connected buttons with an overflow menu. |
| `app/ViewModels/Details/DetailsPage.Pollen.cs` | `ui/details/components/DetailsPollen.kt`, `PollenGrid` in `ui/common/composables/AllergenComposables.kt` and `domain/weather/model/Pollen.kt`. Open-Meteo is not a pollen index source, so every pollen uses the standard thresholds. Names are sorted with the culture's string comparer in place of `Collator`. |
| `app/ViewModels/Details/DetailsPage.UV.cs`, `DetailsPage.Humidity.cs`, `DetailsPage.Pressure.cs`, `DetailsPage.CloudCover.cs`, `DetailsPage.Visibility.cs` | `ui/details/components/DetailsUV.kt`, `DetailsHumidity.kt`, `DetailsPressure.kt`, `DetailsCloudCover.kt` and `DetailsVisibility.kt`. The cloud cover scale's "%" header is upstream's literal. |
| `app/ViewModels/Details/DetailsPage.SunMoon.cs` | `ui/details/components/DetailsSunMoon.kt`, with `getMappedAstroValues`, the theme colors of `SunImplementor` and `MeteorShowerImplementor` brightened as `MaterialWeatherThemeDelegate.getBrighterColor` does (76, 87, 108), and the rise and set rows, including the full dates of debug builds. The Sun's altitude is Spp's upper limb, where upstream's `SunPosition` gives the refracted center, about 0.27° lower. |
| `app/Controls/Details/MoonPhaseView.cs` | `ui/common/widgets/astro/MoonPhaseView.kt`, shared by detail rows and the main moon card. The phase surfaces retain the dark theme's secondary text colors, while the outline uses the active body-text brush. Padding is truncated after conversion to physical pixels, and the outline scales with display density. |
| `src/Localization/MoonPhaseFormatting.cs` | `MoonPhase.getDescription` in `domain/weather/model/MoonPhase.kt`, with `MoonPhase.Phase.toPhase` of commons-suncalc 2.14. |
| `app/ViewModels/Details/DetailsBlocks.cs`, `DetailsChartData.cs` | The items of each page's `LazyColumn` and the data a Vico chart is given. |
| `app/Controls/Details/DetailsChartView.cs` | `BreezyLineChart.kt`, `BreezyBarChart.kt`, `EphemerisChart.kt` and `IconCartesianMarker.kt`, drawn in WPF in place of Vico 3.3.1, with Vico's defaults read from its source: straight lines unless a curvature is set, its `CubicInterpolator` formula for the ephemeris chart, color scales from the top of the layer to its bottom with stops beyond the range pinned to the edges as Skia pins them, areas down to zero, 4-unit label paddings, 4-unit ticks, guidelines dashed 4 and 2, and columns 500 thick with 16 between them. It reproduces the fixed day range, the segmented hour layout, lines 5 high, columns with 15 % corners, the top labels and 18-pixel icons, reference lines labelled below at the start, and a guideline while pressed. The animated entry is not drawn. |
| `app/Controls/Details/DetailsRunsText.cs`, `DetailsScaleView.cs`, `DetailsIcon.cs`, `DetailsItemSpacingConverter.cs`, `app/Icons/DetailsIcons.cs` | Text spans, the scale tables, and the vector drawables of the detail screen, with their viewports. |
| `app/Views/DetailsView.xaml`, `DetailsView.xaml.cs` | `DailyWeatherScreen`, `DailyPagerIndicator` and `DetailsDropdownMenu` in `ui/details/DetailsScreen.kt`. Days change with their tab; swiping between days is not ported. Explanations open on a click as a Windows tooltip in place of Material's plain tooltip. The page menu scrolls when it is taller than the window. `HelpOutline` uses upstream's `ic_help` drawable. |
| `app/ViewModels/WeatherViewModel.cs`, `app/ViewModels/Forecast/ForecastCardViewModel.cs`, `app/Views/WeatherView.xaml.cs`, `app/Views/Weather/ForecastCardView.xaml.cs` | The click handlers of the condition card holders, `AbsDailyTrendAdapter.onItemClicked` and `AbsHourlyTrendAdapter.onItemClicked`, each with the page of its card or chart. |

## Sun and moon

| C# file | Source |
|---|---|
| `src/Solar/SppNative.cs`, `src/Solar/SolarCalculator.cs` | The C interface of [Seasonal++ 1.1.0](https://github.com/Nouni2/Seasonal-plus-plus/releases/tag/v1.1.0) (`include/spp/spp_c.h`, commit `e916f2c`, library version `1.1.0+e916f2c`), with `DllImport` and `ref` structures. The interface version and the 72-byte layout are checked once; a missing, foreign-architecture or different library, or any status other than success, leaves results null. A local date takes the one solar day, among the UTC dates before, on and after it, whose transit falls on it in the location's time zone. Elevation is 0 m. No Breezy Weather counterpart. |
| `src/Astronomy/` | `MoonTimes`, `MoonIllumination`, `MoonPosition`, `MoonPhase.Phase` and the `util` classes `ExtendedMath`, `JulianDate`, `Matrix`, `Moon`, `Sun`, `QuadraticInterpolation` and `Vector` of commons-suncalc 2.14, which Breezy Weather uses. The observer is at sea level, and times are rounded to the minute, its default. Calendar fields come from the computer's time zone, as Java's default calendar does. `Math.signum` keeps NaN and signed zeros, and degrees use Java's multiplication constants. |
| `src/Refresh/AstroCalculation.cs` | `getCalculatedAstroSun`, `getCalculatedAstroMoon` and `getCalculatedMoonPhase` in `sources/CommonConverter.kt`. The Moon follows upstream. The Sun and civil twilight come from Spp instead of commons-suncalc, kept in the same shape: a polar day from midnight to a millisecond before the next, a polar night without times, midnight for a missing rise, and the first set of the following days for a missing set. Without civil dawn or dusk, the day counts as twilight all day when the Sun's center, taken as its upper limb less 0.2666°, stays above −6° at the transit plus 12 hours. When Spp is unavailable, the source's sun times are kept. |
| `src/Refresh/WeatherCompletion.cs` | `completeDailyListFromHourlyList` now sets the sun, twilight, moon and moon phase. Each hour is day or night by Spp's upper-limb altitude above 0°, a deliberate difference from upstream's comparison with sunrise and sunset, so polar days, polar nights and partial days need no special case; without Spp the source's flag is kept. Half days are still grouped by clock time, so the order of the completion steps is unchanged. |

## Experimental desktop interface

The Appearance setting `DesktopInterfaceEnabled`, stored as `desktop_interface` and off by default, applies desktop adjustments to the existing interface. Both modes use the same `WeatherView`, `SearchView`, `ManagementView` and `SettingsView`, with the same colors, weather header, icons, typography and page layouts.

The deliberate differences are smaller corner radii and button padding; compact location-list, refresh and settings actions in the weather screen's existing top bar; right-click location commands for opening, moving and deleting; keyboard access to preference cards and locations; and keyboard navigation. In both modes, the mouse's back and forward buttons move through the screens like a browser's: back closes an open dialog first and never leaves the main screen, and forward reopens a screen left with back until another screen is opened (`MainWindow.OnMouseUp`, `MainViewModel.BrowseBack` and `BrowseForward`). The middle button starts a browser-style autoscroll in both modes (`app/Input/AutoScroll.cs`): an anchor marks the click, the content scrolls towards the pointer faster the further it moves, a click without movement keeps it running until the next click, wheel or Escape, and a held drag stops on release. Each direction scrolls the nearest viewer that can move that way, so a forecast scrolls sideways inside the scrolling page. Deletion and reordering use the existing guards and undo behavior.

| File | Desktop adjustments |
|---|---|
| `app/Theme/Theme.xaml`, `DesktopTheme.xaml` | Theme resources parameterize corners and padding. Desktop overrides contain no palette or typography changes. `AppContextMenu` is the one style for context menus, used by the location actions and Mistray's menu, with check marks, submenus and separators. |
| `app/MainWindow.xaml`, `MainWindow.xaml.cs` | The same page template in both modes; application resources switch the shape overrides. Desktop dialogs keep keyboard focus until closed. |
| `app/Views/WeatherView.xaml` | The existing top bar keeps the place name and adds compact actions in desktop mode. The weather content is unchanged. |
| `app/Views/ManagementView.xaml`, `ManagementView.xaml.cs` | The existing cards gain desktop context menus and keyboard focus. Enter opens, Delete removes, and Ctrl+Up or Ctrl+Down reorders. |
| `app/Views/SettingsView.xaml` | Existing preference groups use smaller corners and padding in desktop mode; focused preferences respond to Enter or Space. |
| `app/Views/SearchView.xaml` | Enter chooses the selected search result. |
| `app/ViewModels/MainViewModel.cs` | Direct location selection and desktop commands. Ctrl+1 opens the weather, Ctrl+L locations, Ctrl+F search, Ctrl+, appearance, Alt+Left goes back and F5 refreshes. Shortcuts are enabled only in desktop mode and while no dialog is open. |
| `app/ViewModels/ManagementViewModel.cs` | Context-menu and keyboard commands call the existing deletion and reorder methods. |
| `resources/InterfaceStrings.resx`, `InterfaceStrings.fr.resx` | Application-specific English and French labels, loaded through `Strings` separately from generated translations. |

`SettingsPages.Appearance` adds the switch, saved by `SettingsManager`. The active page is recreated when the option or language changes.

## Display scaling

The app is per-monitor DPI aware (`PerMonitorV2`, with `PerMonitor` as fallback) through `packaging/Libreccio-Weather.manifest`, so each window is drawn at the scale of its monitor and redrawn when that scale changes. The main window and the tray popup round layout to whole device pixels (`UseLayoutRounding`), which Android does by drawing in physical pixels.

Animations run at the refresh rate of the monitor the window is on, read from Windows when each animation starts (`app/Display/RefreshRate.cs`), instead of WPF's default of 60 frames per second. Android animates at the display's rate. A window moved to another monitor uses that monitor's rate from its next animation. While autoscrolling, an animation that changes nothing visible keeps WPF drawing at that rate. The Windows-only setting `screen_refresh_rate` in Appearance, on by default, can turn this off for slower computers, which keeps WPF's 60 frames per second.

## Mistray

Mistray is Windows shell integration replacing the ongoing Android notification. Its visibility follows the notification enable switch, off by default. It follows the location on the main weather screen, including while another page is open; `WidgetNotificationIMP` uses the first saved location instead. The selected-location behavior is a Windows integration difference. The existing refresh command and location/settings navigation are reused.

| File | Responsibility and source relationship |
|---|---|
| `app/Tray/TrayIcon.cs` | `NotifyIcon` lifetime, condition or temperature icon, localized tooltip and context menu. Temperature fallback follows `WidgetNotificationIMP.buildBaseView`. With the temperature icon on, the Windows-only `tray_weather_icon` setting adds a second icon with the condition; Windows decides the order of the two icons. Both icons share the tooltip, popup and menu. The menu is a WPF context menu using the shared `AppContextMenu` style, opened on right-click and brought to the foreground so it closes on a click elsewhere. Persistent controls whether the menu offers Dismiss, replacing Android notification dismissal. Dismiss keeps the switch enabled and restores the tray on refresh or a settings change. A click opens the popup at once. The Windows-only `tray_double_click` setting, off by default, makes a double-click open the main window. The popup then opens after 75 ms, or after the double-click delay when that is shorter, and a second click within the delay still opens the window. The delay is `tray_double_click_delay`, from 1 to 5,000 ms, the most Windows accepts for its own double-click time, or Windows' double-click time while unset. Windows only reports double-clicks within its own time, so the clicks are paired from the button presses. Closing hides the window when an icon is visible and exits otherwise. Open restores the window's previous non-minimized state; Exit shuts down. Windows session ending bypasses close-to-tray. |
| `app/Tray/TrayIconImages.cs` | Weather artwork conversion and `ResourceHelper.createTempBitmap` in `app/src/main/kotlin/org/breezyweather/ui/theme/resource/ResourceHelper.kt`: narrow temperature text, the bundled Asap Condensed Bold face and 72-pixel drawing bounds. Windows needs an aspect-preserving fit into a square icon slot and explicit taskbar contrast colors. The owned icon is cloned before its temporary HICON is destroyed; replaced icons are disposed. |
| `app/Tray/TrayPopupContent.cs` | `WidgetNotificationIMP`, `NativeWidgetNotificationIMP`, `MultiCityWidgetNotificationIMP` and the daily range helpers in `domain/weather/model/Daily.kt`. Preserves the native style's conditional temperature and refresh caption, three secondary cities, five daily/hourly entries, actual/feels-like fallbacks and air-quality-or-wind caption. Secondary cities without a complete temperature range omit the range instead of appending Kotlin's literal null text. Widget icon mode uses its default automatic day/night selection; its separate widget preference and alternate calendars are not ported. |
| `app/Tray/TrayPopup.xaml`, `TrayPopup.xaml.cs` | The notification base, daily/hourly and multi-city layouts expressed as a WPF popup using the existing theme. The native style uses the same fields in a compact arrangement. Placement, focus-loss dismissal and the Open/Close actions are native window behavior. The window is created once and hidden instead of closed, so each click only refreshes its content. The window title, Open button and Close tooltip/accessibility label bind to translated properties in each new `TrayPopupContent`, keeping the retained popup in the selected app language when settings refresh its content or it opens again. |
| `app/ViewModels/Settings/SettingsPages.cs`, `src/Settings/SettingsManager.cs` | `settings_modules` notification settings, under a Mistray section. The section, the master switch and the temperature icon switch use the application's own labels (`tray_section`, `tray_icon`, `tray_temperature_icon`), because upstream's "Notification widget" and "status bar" describe Android; the other upstream labels are kept. The Windows-only weather icon switch follows the temperature icon switch and is available only while it is on. The double-click switch ends the section. While it is on, an Accessibility section title and the double-click delay follow it. |
| `app/ViewModels/Settings/PreferenceItems.cs`, `app/Views/SettingsView.xaml` | `SliderPreference` has no upstream counterpart: a whole number set with a slider or typed in a field, with a reset button labelled with upstream's `action_reset_to_default`. The field accepts a wider range than the slider; a value outside the slider's range puts the handle at the nearest end without changing the scale. For the double-click delay, the field takes 1 to 5,000 ms, the slider spans 100 to 900 ms like Windows' own double-click speed slider, and the recommended value and reset target is Windows' double-click time. |
| `app/Controls/MaterialSlider.cs`, `app/Controls/SliderStops.cs` | The Material 3 slider at its extra-small size: 16-pixel track with 8-pixel outer and 2-pixel inner corners, primary active and secondary-container inactive track, a 4 × 44 handle with 6-pixel gaps that narrows to 2 pixels while dragged or focused, stop indicators 4 pixels from the end of the track and at the recommended value, and a 48 × 44 inverse-surface value indicator in label-large text, 12 pixels above the handle, while dragging or focused. Breezy Weather has no slider, so Jetpack Compose's Material 3 `Slider` is the reference: its inner track corners, facing the handle, are `TrackInsideCornerSize`, 2 dp, which the 2-pixel inner corners follow. Styled by `AppSlider` in `app/Theme/Theme.xaml`. The value indicator's corner radius is not given in the specification and is fully rounded here. |
| `app/App.xaml.cs` | Creates Mistray after location loading, retains the process with explicit shutdown, and disposes shell resources on exit. |

The tooltip is limited to Windows' 127 UTF-16 code units and truncates without splitting a surrogate pair. It updates on weather, navigation, settings and language changes, and once per minute. Missing refresh timestamps are omitted; without relative-time ICU support, the last refresh uses a localized date/time in the location's time zone. Condition artwork uses hourly day/night flags while solar events are unavailable. Automatic startup is not implemented. Icon rendering, popup placement and focus, close-to-tray, dismissal, second-launch activation and Explorer restart require validation in the running app.

## Tests

Short paths are relative to `weather-unit/src/test/kotlin/org/breezyweather/unit/`.

| C# file | Breezy Weather file |
|---|---|
| `Calculations/PrecipitationCalculationsTests.cs` | `computing/PrecipitationComputingTest.kt` |
| `Calculations/TemperatureCalculationsTests.cs` | `computing/TemperatureComputingTest.kt` |
| `Units/TemperatureTests.cs` | `temperature/TemperatureTest.kt` |
| `Localization/NumberFormattingTests.cs` | `formatting/NumberFormattingTest.kt` |
| `Settings/CardDisplayTests.cs` | `app/src/test/kotlin/org/breezyweather/option/appearance/CardDisplayTest.kt` |
| `Settings/DailyTrendDisplayTests.cs` | `app/src/test/kotlin/org/breezyweather/option/appearance/DailyTrendDisplayTest.kt` |
| `Settings/OptionNamesTests.cs` | `app/src/test/kotlin/org/breezyweather/option/utils/UtilsTest.kt` |
| `Settings/ConfigStoreTests.cs` | No file. |
| `Units/SpeedTests.cs` | No file. |
| `Database/WeatherRepositoryTests.cs` | No upstream test. Covers weather storage and migration, plus formatted-ID changes with foreign keys enabled across all seven dependent tables, rollback after a name collision or failed child update, and full parameter replacement through both save paths. |
| `Calculations/AirQualityIndexTests.cs` | No upstream test. Checks integer saturation of the AQI result with a concentration that remains representable by the quantity constructor. |
| `Units/QuantityConversionTests.cs` | No upstream test. Checks C# floating-point conversion differences across the eight quantities: saturation, NaN rejection, exact large integers, half boundaries and overflow of converted long inputs. |
| `Localization/DurationFormattingTests.cs` | No upstream test. Checks truncation of negative TimeSpan hour components against Kotlin's duration components. |
| `Solar/SolarCalculatorTests.cs` | Seasonal++'s `capi/spp_capi_test_cases.inc`, read through the managed layer, without the day of 500 BC that .NET dates cannot hold, plus the selection of a local date ahead of UTC, the batch altitudes at sunrise and sunset, and .NET date-boundary fallbacks. |
| `Astronomy/MoonCalculatorTests.cs` | commons-suncalc 2.14's `MoonTimesTest`, `MoonIlluminationTest` and `MoonPositionTest`, with rise and set times rounded to the minute as the app uses them. |
| `Charts/DetailsChartAxesTests.cs` | No upstream test. Checks Kotlin's saturating `Float.toInt()` when no top label fits, and the lenient calendar's reading of a local hour skipped by a daylight saving change. |
