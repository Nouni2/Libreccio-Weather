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

## Localization

| C# file | Breezy Weather file |
|---|---|
| `src/Localization/NumberFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/NumberFormatting.kt` |
| `src/Localization/UnitFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/MeasureUnitFormatting.kt`, and `getDisplayName` in `weather-unit/src/main/kotlin/org/breezyweather/unit/WeatherUnit.kt` through its translated strings, without Android's CLDR names. |
| `src/Localization/Icu.cs` | No file. It calls Windows' ICU, which Android's `android.icu` classes are built on. |
| `src/Localization/DateFormatting.cs` | `getFormattedShortDayAndMonth` in `app/src/main/kotlin/org/breezyweather/common/extensions/DateExtensions.kt`. It keeps the ICU boundary inside Core and uses `MM-dd` when ICU cannot format the date. |
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
| `ViewModels/Settings/SettingsPages.cs` | `settings/compose/RootSettingsScreen.kt`, `BackgroundUpdatesSettingsScreen.kt`, `AppearanceSettingsScreen.kt`, `UnitSettingsScreen.kt`. Categories not ported are shown disabled. Without ICU, unit and interval names come from strings, as in Breezy Weather with "use measure format" off. |
| `ViewModels/Settings/PreferenceItems.cs`, `ListDialogViewModel.cs`, `SettingsPageViewModel.cs` | `settings/preference/` |
| `ViewModels/Settings/NeverRefreshDialogViewModel.cs` | The dialog in `settings/compose/BackgroundUpdatesSettingsScreen.kt` |
| `Localization/SystemStrings.cs` | No file. It takes the Cancel text from Windows, as Breezy Weather takes it from Android. |
| `App.xaml.cs`, `Localization/TranslateExtension.cs` | No file. |
| `Icons/SeasonalWindowIcon.cs` | No file. It gives the main window Libreccio Weather's icon for the current season, which Breezy Weather does not have. |

## Main-screen scrolling and forecasts

Both interface modes use the scrolling weather screen in `app/Views/WeatherView.xaml`. Its header wraps its content, as `container_main_header.xml` does, and shares the vertical scroll viewer with the forecast cards. Daily and hourly cards follow their order in `SettingsManager.CardDisplayList`, and are omitted when their corresponding forecast is empty or their temperature display is disabled. Desktop mode shows native scrollbars; touch mode keeps them hidden. Both modes support vertical wheel scrolling, and Shift+wheel scrolls a forecast horizontally.

| File | Breezy Weather file |
|---|---|
| `app/Appearance/WeatherBackground.cs` | `WeatherImplementorFactory.getBackgroundId` in `ui-weather-view/src/main/kotlin/org/breezyweather/ui/theme/weatherView/materialWeatherView/`, with weather-code mapping from `WeatherViewController.kt`. |
| `app/Theme/WeatherBackgrounds.xaml` | The gradients in `ui-weather-view/src/main/res/drawable-night/weather_background_*.xml`, falling back to `drawable/` where there is no night-qualified resource. The application uses its existing dark theme. |
| `app/Icons/WeatherIcons.cs`, `assets/weather/weather_*.png` | Static weather artwork from `app/src/main/res/drawable/`, selected by weather code and daylight. |
| `app/ViewModels/Forecast/ForecastCardViewModel.cs` | `DailyTemperatureAdapter.kt`, `HourlyTemperatureAdapter.kt` and their base classes under `app/src/main/kotlin/org/breezyweather/ui/main/adapters/trend/`. Raw temperature values and interpolated midpoints retain 32-bit precision; normal temperatures participate in the bounds. |
| `app/Controls/Forecast/PolylineAndHistogramView.cs` | Temperature lines, shadow, time line and precipitation histogram from `app/src/main/kotlin/org/breezyweather/ui/common/widgets/trend/chart/PolylineAndHistogramView.kt`. |
| `src/Charts/TrendCoordinates.cs` | `computeSingleCoordinate` in the same file, including the saturating `Float.toInt()` conversion. |
| `app/Views/Weather/ForecastCardView.xaml`, `ForecastCardView.xaml.cs` | `DailyViewHolder.kt`, `HourlyViewHolder.kt`, their `container_main_*_trend_card.xml` layouts, `DailyTrendItemView.kt` and `HourlyTrendItemView.kt`. Columns are 67 pixels wide, icons 32, daily columns 318 high and hourly columns 206 high in WPF device-independent pixels. |
| `tests/Charts/TrendCoordinatesTests.cs` | No upstream test. Checks the C# conversion difference when equal chart bounds yield non-finite coordinates. |

The static background gradients are included; the animated weather implementors are not. Daylight uses the forecast's first current-hour flag while solar calculations are unavailable. The temperature renderer uses the current theme's primary color, rather than weather-derived theme colors, and WPF text metrics. Normal-temperature reference lines, other trend tabs, detail navigation and the other main-screen card types are not implemented. Without ICU date formatting, numeric daily dates use `MM-dd`; 12-hour or 24-hour labels follow the selected language's time pattern rather than Android's system preference.

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
