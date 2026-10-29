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
