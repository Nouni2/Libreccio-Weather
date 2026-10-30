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
| `UnitRounding.cs` | No file. It reproduces Kotlin's `roundToLong()`, which .NET does not have. |

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
| `src/Localization/NumberFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/NumberFormatting.kt` |
| `src/Localization/UnitFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/MeasureUnitFormatting.kt`, `WeatherValue.formatValue`, `WeatherUnit.getDisplayName`, `WeatherUnit.format`, `precipitation/PrecipitationUnit.kt`, `temperature/TemperatureUnit.kt` and `ratio/RatioUnit.kt`. Precipitation chart numbers follow selected-unit precision. Measured values use Windows' ICU; narrow temperatures retain the translated degree-only pattern. Percentages use the percent formatter when ICU number formatting is unavailable. |
| `src/Localization/PercentFormatting.cs` | The `NumberFormat.getPercentInstance` fallback in `ratio/RatioUnit.kt`. Preserves fraction conversion, half-even rounding, maximum fraction digits, nonpositive clamping, grouping and locale-specific percent placement. French CLDR's non-breaking space is restored because .NET's `PercentPositivePattern` retains placement but discards the spacing character. |
| `src/Localization/Icu.cs` | No file. It calls Windows' ICU, which Android's `android.icu` classes are built on. |
| `src/Localization/DateFormatting.cs` | `getFormattedShortDayAndMonth`, `getFormattedFullDayAndMonth`, `getWeek`, `getHour` and `getFormattedTime` in `app/src/main/kotlin/org/breezyweather/common/extensions/DateExtensions.kt`. Keeps the ICU boundary inside Core, location time zones and first-letter capitalization. Hour format follows Windows' 12/24-hour preference while text follows the selected language. Without ICU, full dates use the culture's month/day pattern and numeric dates use `MM-dd`. |
| `src/Localization/DurationFormatting.cs` | `format` and `formatWithAndroidTranslations` in `weather-unit/src/main/kotlin/org/breezyweather/unit/duration/Duration.kt`, and `formatTime` in `app/src/main/kotlin/org/breezyweather/common/extensions/UnitExtensions.kt`, for hours with minutes or seconds. |
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
| `WeatherCompletion.cs` | `sources/CommonConverter.kt`. Sun and moon times are not computed; Breezy Weather computes them with commons-suncalc. |
| `WeatherRefresher.cs` | `sources/RefreshHelper.kt` |

## Database

`src/Database/` ports `data/src/main/`.

| C# file | Breezy Weather file |
|---|---|
| `LocationRepository.cs` | `kotlin/breezyweather/data/location/LocationRepository.kt`, `sqldelight/breezyweather/data/locations.sq`, `location_parameters.sq` |
| `WeatherRepository.cs`, `WeatherWriter.cs` | `kotlin/breezyweather/data/weather/WeatherRepository.kt` |
| `WeatherReader.cs` | `kotlin/breezyweather/data/weather/WeatherMapper.kt` |
| `WeatherSchema.cs`, `WeatherDatabase.cs` | `sqldelight/breezyweather/data/weathers.sq`, `dailys.sq`, `hourlys.sq`, `minutelys.sq`, `alerts.sq`, `normals.sq` |

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
| `ViewModels/MainViewModel.cs` | `main/MainActivity.kt`, `main/MainActivityViewModel.kt`, for the screens, the location list and the messages. |
| `ViewModels/ManagementViewModel.cs`, `Views/ManagementView.xaml.cs` | `main/fragments/ManagementFragment.kt`, `main/widgets/LocationItemTouchCallback.kt`. A mouse drag stands for a finger swipe; sliding towards the start only slides back, as source settings are not ported. |
| `ViewModels/LocationItemViewModel.cs` | `main/adapters/location/LocationModel.kt`, `LocationHolder.kt`. Without ICU, outdated weather does not show "Last updated". |
| `ViewModels/SnackbarViewModel.cs` | `app/src/main/kotlin/org/breezyweather/common/utils/helpers/SnackbarHelper.kt` |
| `ViewModels/Settings/SettingsPages.cs` | `settings/compose/RootSettingsScreen.kt`, `BackgroundUpdatesSettingsScreen.kt`, `AppearanceSettingsScreen.kt`, `UnitSettingsScreen.kt` and the notification-widget section of `ModulesSettingsScreen.kt`. The notification section preserves its five controls, order, source labels, options, saved keys, defaults and dependent enabled states. Windows tray icons need no Android notification permission request. Categories not ported are shown disabled. Without ICU, unit and interval names come from strings, as in Breezy Weather with "use measure format" off. |
| `ViewModels/Settings/PreferenceItems.cs`, `ListDialogViewModel.cs`, `SettingsPageViewModel.cs` | `settings/preference/` |
| `ViewModels/Settings/NeverRefreshDialogViewModel.cs` | The dialog in `settings/compose/BackgroundUpdatesSettingsScreen.kt` |
| `Localization/SystemStrings.cs` | No file. It takes the Cancel text from Windows, as Breezy Weather takes it from Android. |
| `App.xaml.cs`, `Localization/TranslateExtension.cs` | No file. |
| `Icons/SeasonalWindowIcon.cs` | No file. It gives the main window Libreccio Weather's icon for the current season, which Breezy Weather does not have. |

## Main-screen scrolling and forecasts

Both interface modes use the scrolling weather screen in `app/Views/WeatherView.xaml`. Its header wraps its content, as `container_main_header.xml` does, and shares the vertical scroll viewer with the forecast cards. Daily and hourly cards follow their order in `SettingsManager.CardDisplayList`, and are omitted when their corresponding forecast is empty or none of the implemented charts is enabled and valid. Temperature, feels-like, precipitation and hourly humidity/dew-point tabs follow the saved chart order. The feels-like tab requires at least one feels-like value and falls back to the actual temperature for individual missing values; its bounds exclude temperature normals. A selector is hidden when only one chart is available. Selected tab names live in `MainViewModel`, matching `MainAdapter`'s in-session state, and unavailable selections reset. Desktop mode shows native scrollbars; touch mode keeps them hidden. Both modes support vertical wheel scrolling, and Shift+wheel scrolls a forecast horizontally.

| File | Breezy Weather file |
|---|---|
| `app/Appearance/WeatherBackground.cs` | `WeatherImplementorFactory.getBackgroundId` in `ui-weather-view/src/main/kotlin/org/breezyweather/ui/theme/weatherView/materialWeatherView/`, with weather-code mapping from `WeatherViewController.kt`. |
| `app/Theme/WeatherBackgrounds.xaml` | The gradients in `ui-weather-view/src/main/res/drawable-night/weather_background_*.xml`, falling back to `drawable/` where there is no night-qualified resource. The application uses its existing dark theme. |
| `app/Icons/WeatherIcons.cs`, `assets/weather/weather_*.png` | Static weather artwork from `app/src/main/res/drawable/`, selected by weather code and daylight. |
| `app/Appearance/PrecipitationColors.cs` | `getHalfDayPrecipitationColor` and `getHourlyPrecipitationColor` in `app/src/main/kotlin/org/breezyweather/domain/weather/model/Precipitation.kt`, with `colorLevel_1` through `colorLevel_5` in `app/src/main/res/values/colors.xml`. Inclusive ranges keep exact threshold values in the preceding intensity level; null and negative totals have no color. |
| `app/ViewModels/Forecast/ForecastCardViewModel.cs` | `DailyTrendAdapter.kt`, `HourlyTrendAdapter.kt`, `daily/DailyTemperatureAdapter.kt`, `daily/DailyFeelsLikeAdapter.kt`, `hourly/HourlyTemperatureAdapter.kt`, `hourly/HourlyFeelsLikeAdapter.kt` and their base classes under `app/src/main/kotlin/org/breezyweather/ui/main/adapters/trend/`. Raw temperature values and interpolated midpoints retain 32-bit precision; normal temperatures participate only in actual-temperature bounds. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Precipitation.cs` | `daily/DailyPrecipitationAdapter.kt` and `hourly/HourlyPrecipitationAdapter.kt` in the same source folder. Daily scaling starts at 57.75 mm per half-day and hourly scaling at 15 mm; larger forecast totals extend it. Daily zero labels are hidden; hourly known zero totals show a zero label. Missing totals retain null labels. |
| `app/ViewModels/Forecast/ForecastCardViewModel.Humidity.cs` | `hourly/HourlyHumidityAdapter.kt` in the same source folder. Availability requires a dew point, with raw dew-point bounds and interpolated midpoints. Humidity uses a separate 0–100 percent scale, and zero humidity is hidden while dew-point bounds exist. |
| `app/ViewModels/Forecast/ForecastDescriptions.cs` | The description builders in the temperature and feels-like adapters, and the date/hour prefixes in `daily/AbsDailyTrendAdapter.kt` and `hourly/AbsHourlyTrendAdapter.kt`. Preserves today/yesterday/tomorrow, full weekday/date text, temperature/condition/probability order and source separators. The same prefixes are used by precipitation and humidity descriptions and exposed through the columns' automation names. |
| `app/Controls/Forecast/ForecastChartView.cs` | Shared title-text appearance, `setShadowLayer(2f, 0f, 1f, ...)` and `dpToPx(...).toInt()` behavior used by the source chart renderers. A separate visual applies the 20-percent black shadow only to labels, with a two-device-pixel blur radius and one-device-pixel downward offset. Histogram half-width truncates in device pixels before returning to WPF coordinates. |
| `app/Controls/Forecast/PolylineAndHistogramView.cs` | Temperature lines, shadow, time line and precipitation histogram from `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/chart/PolylineAndHistogramView.kt`. |
| `app/Controls/Forecast/DoubleHistogramView.cs` | Day and night precipitation bars from `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/chart/DoubleHistogramView.kt`, with 24-DIP top and bottom margins, 4-DIP center gap, 8-DIP bar width and day/night alpha of 255/127. Gust bars and pills belong to the wind charts and are not included. |
| `app/Controls/Forecast/TrendReferenceLinesView.cs` | `drawKeyLines` in `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/TrendRecyclerView.kt`. Lines require both current-month normal temperatures and the horizontal-lines setting; labels stay at the viewport edges above the daytime line and below the nighttime line. WPF measures the chart boundaries from the rendered first column. |
| `src/Charts/TrendCoordinates.cs` | `computeSingleCoordinate` in `PolylineAndHistogramView.kt`, `computeCoordinates` in `DoubleHistogramView.kt` and the reference-line coordinate expression in `TrendRecyclerView.drawKeyLines`, preserving their different arithmetic order and saturating `Float.toInt()` conversion. |
| `app/Views/Weather/ForecastCardView.xaml`, `ForecastCardView.xaml.cs` | `DailyViewHolder.kt`, `HourlyViewHolder.kt`, their `container_main_*_trend_card.xml` layouts, `DailyTrendItemView.kt` and `HourlyTrendItemView.kt`. Columns are 67 pixels wide, icons 32, daily columns 318 high and hourly columns 206 high in WPF device-independent pixels. |
| `tests/Charts/TrendCoordinatesTests.cs` | No upstream test. Checks the C# conversion difference when equal plot/reference-line bounds or a zero paired-histogram maximum yield non-finite coordinates. There are no upstream tests for these forecast adapters, precipitation colors or chart renderers. |
| `tests/Localization/PercentFormattingTests.cs` | No upstream test. Checks .NET percent-format differences: half-even integer rounding, maximum fraction digits without zero padding, nonpositive clamping and the English/French percent spacing. |
| `tests/Localization/UnitFormattingTests.cs` | No upstream test. Checks the .NET conversion differences for Kotlin `roundToInt()`: upward half-rounding, saturation and NaN rejection. |

The static background gradients are included; the animated weather implementors are not. Daylight uses the forecast's first current-hour flag while solar calculations are unavailable. Temperature, dew-point and reference-line rendering uses the current theme colors and Windows font rasterization; Android and WPF font metrics and blur kernels cannot be assumed pixel-identical. Precipitation bars use the source intensity palette. The tab selector wraps when space is limited, in place of Android's connected button group with an overflow menu. Chart types other than temperature, feels-like, precipitation and hourly humidity/dew point, detail navigation and the other main-screen card types are not implemented. Without ICU, numeric daily dates use `MM-dd` and measured values use the source translated unit patterns.

## Experimental desktop interface

The Appearance setting `DesktopInterfaceEnabled`, stored as `desktop_interface` and off by default, applies desktop adjustments to the existing interface. Both modes use the same `WeatherView`, `SearchView`, `ManagementView` and `SettingsView`, with the same colors, weather header, icons, typography and page layouts.

The deliberate differences are smaller corner radii and button padding; a saved-location dropdown and compact location-list, refresh and settings actions in the weather screen's existing top bar; right-click location commands for opening, moving and deleting; keyboard access to preference cards and locations; and keyboard navigation. Deletion and reordering use the existing guards and undo behavior.

| File | Desktop adjustments |
|---|---|
| `app/Theme/Theme.xaml`, `DesktopTheme.xaml` | Theme resources parameterize corners and padding. Desktop overrides contain no palette or typography changes. The location dropdown uses the existing colors. |
| `app/MainWindow.xaml`, `MainWindow.xaml.cs` | The same page template in both modes; application resources switch the shape overrides. Desktop dialogs keep keyboard focus until closed. |
| `app/Views/WeatherView.xaml` | The existing top bar offers a location dropdown and compact actions in desktop mode. The weather content is unchanged. |
| `app/Views/ManagementView.xaml`, `ManagementView.xaml.cs` | The existing cards gain desktop context menus and keyboard focus. Enter opens, Delete removes, and Ctrl+Up or Ctrl+Down reorders. |
| `app/Views/SettingsView.xaml` | Existing preference groups use smaller corners and padding in desktop mode; focused preferences respond to Enter or Space. |
| `app/Views/SearchView.xaml` | Enter chooses the selected search result. |
| `app/ViewModels/MainViewModel.cs` | Direct location selection and desktop commands. Ctrl+1 opens the weather, Ctrl+L locations, Ctrl+F search, Ctrl+, appearance, Alt+Left goes back and F5 refreshes. Shortcuts are enabled only in desktop mode and while no dialog is open. |
| `app/ViewModels/ManagementViewModel.cs` | Context-menu and keyboard commands call the existing deletion and reorder methods. |
| `resources/InterfaceStrings.resx`, `InterfaceStrings.fr.resx` | Application-specific English and French labels, loaded through `Strings` separately from generated translations. |

`SettingsPages.Appearance` adds the switch, saved by `SettingsManager`. The active page is recreated when the option or language changes.

## Mistray

Mistray is Windows shell integration replacing the ongoing Android notification. Its visibility follows the notification enable switch, off by default. It follows the location on the main weather screen, including while another page is open; `WidgetNotificationIMP` uses the first saved location instead. The selected-location behavior is a Windows integration difference. The existing refresh command and location/settings navigation are reused.

| File | Responsibility and source relationship |
|---|---|
| `app/Tray/TrayIcon.cs` | `NotifyIcon` lifetime, condition or temperature icon, localized tooltip and context menu. Temperature fallback follows `WidgetNotificationIMP.buildBaseView`. Persistent controls whether the menu offers Dismiss, replacing Android notification dismissal. Dismiss keeps the switch enabled and restores the tray on refresh or a settings change. Closing hides the window when the icon is visible and exits otherwise. Double-click or Open restores its previous non-minimized state; Exit shuts down. Windows session ending bypasses close-to-tray. |
| `app/Tray/TrayIconImages.cs` | Weather artwork conversion and `ResourceHelper.createTempBitmap` in `app/src/main/kotlin/org/breezyweather/ui/theme/resource/ResourceHelper.kt`: narrow temperature text, the bundled Asap Condensed Bold face and 72-pixel drawing bounds. Windows needs an aspect-preserving fit into a square icon slot and explicit taskbar contrast colors. The owned icon is cloned before its temporary HICON is destroyed; replaced icons are disposed. |
| `app/Tray/TrayPopupContent.cs` | `WidgetNotificationIMP`, `NativeWidgetNotificationIMP`, `MultiCityWidgetNotificationIMP` and the daily range helpers in `domain/weather/model/Daily.kt`. Preserves the native style's conditional temperature and refresh caption, three secondary cities, five daily/hourly entries, actual/feels-like fallbacks and air-quality-or-wind caption. Secondary cities without a complete temperature range omit the range instead of appending Kotlin's literal null text. Widget icon mode uses its default automatic day/night selection; its separate widget preference and alternate calendars are not ported. |
| `app/Tray/TrayPopup.xaml`, `TrayPopup.xaml.cs` | The notification base, daily/hourly and multi-city layouts expressed as a WPF popup using the existing theme. The native style uses the same fields in a compact arrangement. Placement, focus-loss dismissal and the Open/Close actions are native window behavior. |
| `app/App.xaml.cs` | Creates Mistray after location loading, retains the process with explicit shutdown, and disposes shell resources on exit. |

The tooltip is limited to Windows' 127 UTF-16 code units and truncates without splitting a surrogate pair. It updates on weather, navigation, settings and language changes, and once per minute. Missing refresh timestamps are omitted; without relative-time ICU support, the last refresh uses a localized date/time in the location's time zone. Condition artwork uses hourly day/night flags while solar events are unavailable. Automatic startup and single-instance activation are not implemented. Icon rendering, popup placement and focus, close-to-tray, dismissal and Explorer restart require validation in the running app.

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
| `Database/WeatherRepositoryTests.cs` | No file. |
| `Calculations/AirQualityIndexTests.cs` | No upstream test. Checks integer saturation of the AQI result with a concentration that remains representable by the quantity constructor. |
