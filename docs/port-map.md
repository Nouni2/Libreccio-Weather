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
| `src/Localization/UnitFormatting.cs` | `weather-unit/src/main/kotlin/org/breezyweather/unit/formatting/MeasureUnitFormatting.kt` |
| `src/Localization/Strings.cs` | No file. It replaces Android's string resources. |
| `src/Localization/PluralRules.cs` | No file. It replaces Android's plural resources. |
| `src/Localization/AppLanguages.cs` | No file. |

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
| `App.xaml.cs`, `Localization/TranslateExtension.cs` | No file. |

## Tests

`tests/` ports `weather-unit/src/test/kotlin/org/breezyweather/unit/`.

| C# file | Breezy Weather file |
|---|---|
| `Calculations/PrecipitationCalculationsTests.cs` | `computing/PrecipitationComputingTest.kt` |
| `Calculations/TemperatureCalculationsTests.cs` | `computing/TemperatureComputingTest.kt` |
| `Units/TemperatureTests.cs` | `temperature/TemperatureTest.kt` |
| `Localization/NumberFormattingTests.cs` | `formatting/NumberFormattingTest.kt` |
| `Units/SpeedTests.cs` | No file. |
| `Database/WeatherRepositoryTests.cs` | No file. |
