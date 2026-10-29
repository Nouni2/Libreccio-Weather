/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Globalization;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Refresh;

/// <summary>
/// Merges data from sources with previous data and computes what the sources leave out:
/// half days and other daily values from hourly data, hourly values from daily data, and current conditions
/// from the current hour.
/// </summary>
/// <remarks>
/// Sun, twilight, moon and moon phase are not computed here, so they stay as the source gave them,
/// and an hour without sun times for its day keeps the day or night flag of the source.
/// </remarks>
public static class WeatherCompletion
{
    // Cloud cover thresholds in percent: 3 oktas for few clouds, 7 oktas for broken clouds.
    private const double CloudCoverFew = 37.5;
    private const double CloudCoverBroken = 87.5;

    // Degree days in tenths of a degree Celsius: 18 °C for heating, 21 °C for cooling, with a 3 °C margin around both.
    private const double HeatingDegreeDayBase = 180.0;
    private const double CoolingDegreeDayBase = 210.0;
    private const double DegreeDayMargin = 30.0;

    // Fewer hours than this give no meaningful daily average.
    private const int MinimumHoursForDailyValue = 18;

    /// <summary>
    /// Adds days and hours from the previous weather that the new data no longer has, back to the start date,
    /// so the forecast keeps its history.
    /// </summary>
    public static WeatherWrapper CompleteNewWeatherWithPreviousData(
        WeatherWrapper newWeather,
        Weather? oldWeather,
        DateTimeOffset startDate,
        string? airQualitySource,
        string? pollenSource)
    {
        if (oldWeather is null || (oldWeather.DailyForecast.Count == 0 && oldWeather.HourlyForecast.Count == 0))
        {
            return newWeather;
        }

        DateTimeOffset? firstNewDay = newWeather.DailyForecast is { Count: > 0 } newDaily ? newDaily[0].Date : null;
        DateTimeOffset? firstNewHour = newWeather.HourlyForecast is { Count: > 0 } newHourly ? newHourly[0].Date : null;
        bool IsMissingDay(DateTimeOffset date) => date >= startDate && (firstNewDay is null || date < firstNewDay);
        bool IsMissingHour(DateTimeOffset date) => date >= startDate && (firstNewHour is null || date < firstNewHour);

        List<DailyWrapper> missingDailyList = oldWeather.DailyForecast
            .Where(daily => IsMissingDay(daily.Date))
            .Select(daily => daily.ToDailyWrapper())
            .ToList();
        List<HourlyWrapper> missingHourlyList = oldWeather.HourlyForecast
            .Where(hourly => IsMissingHour(hourly.Date))
            .Select(hourly => hourly.ToHourlyWrapper())
            .ToList();

        if (missingDailyList.Count == 0 && missingHourlyList.Count == 0)
        {
            return newWeather;
        }

        Dictionary<DateTimeOffset, AirQuality> missingDailyAirQuality = ByDate(
            oldWeather.DailyForecast.Where(daily => IsMissingDay(daily.Date) && daily.AirQuality?.IsValid == true),
            daily => daily.Date,
            daily => daily.AirQuality!);
        Dictionary<DateTimeOffset, AirQuality> missingHourlyAirQuality = ByDate(
            oldWeather.HourlyForecast.Where(hourly => IsMissingHour(hourly.Date) && hourly.AirQuality?.IsValid == true),
            hourly => hourly.Date,
            hourly => hourly.AirQuality!);
        Dictionary<DateTimeOffset, Pollen> missingDailyPollen = ByDate(
            oldWeather.DailyForecast.Where(daily => IsMissingDay(daily.Date) && daily.Pollen?.IsValid == true),
            daily => daily.Date,
            daily => daily.Pollen!);

        return newWeather with
        {
            DailyForecast = missingDailyList.Concat(newWeather.DailyForecast ?? []).ToList(),
            HourlyForecast = missingHourlyList.Concat(newWeather.HourlyForecast ?? []).ToList(),
            AirQuality = !string.IsNullOrEmpty(airQualitySource) && newWeather.AirQuality is { } airQuality
                ? airQuality with
                {
                    DailyForecast = Valid(Merge(missingDailyAirQuality, airQuality.DailyForecast), item => item.ToValid()),
                    HourlyForecast = Valid(Merge(missingHourlyAirQuality, airQuality.HourlyForecast), item => item.ToValid()),
                }
                : null,
            Pollen = !string.IsNullOrEmpty(pollenSource) && newWeather.Pollen is { } pollen
                ? pollen with
                {
                    DailyForecast = Valid(Merge(missingDailyPollen, pollen.DailyForecast), item => item.ToValid()),
                }
                : null,
        };
    }

    /// <summary>
    /// The daily forecast with the daily air quality and pollen of the sources added.
    /// </summary>
    public static IReadOnlyList<Daily> ConvertDailyWrapperToDailyList(WeatherWrapper weatherWrapper)
    {
        IReadOnlyDictionary<DateTimeOffset, AirQuality>? airQuality = weatherWrapper.AirQuality?.DailyForecast;
        IReadOnlyDictionary<DateTimeOffset, Pollen>? pollen = weatherWrapper.Pollen?.DailyForecast;
        if ((airQuality is null || airQuality.Count == 0) && (pollen is null || pollen.Count == 0))
        {
            return weatherWrapper.DailyForecast?.Select(daily => daily.ToDaily()).ToList() ?? [];
        }

        return (weatherWrapper.DailyForecast ?? [])
            .Select(daily => daily.ToDaily(
                airQuality?.GetValueOrDefault(daily.Date),
                pollen?.GetValueOrDefault(daily.Date)))
            .ToList();
    }

    /// <summary>
    /// Hours with invalid values removed and the computable values filled in: weather code and text,
    /// relative humidity, dew point, feels-like temperatures, and total precipitation and probability.
    /// </summary>
    public static IReadOnlyList<Hourly>? ComputeMissingHourlyData(IReadOnlyList<HourlyWrapper>? hourlyList)
    {
        return hourlyList?.Select(ComputeMissingHourlyData).ToList();
    }

    /// <summary>
    /// Converts a cardinal direction of up to three letters to degrees, in English (N, E, S, W)
    /// or French and Spanish (N, E, S, O). VR and VAR mean a variable direction.
    /// </summary>
    public static double? GetWindDegree(string? direction)
    {
        return direction switch
        {
            "N" => 0.0,
            "NNE" => 22.5,
            "NE" => 45.0,
            "ENE" => 67.5,
            "E" => 90.0,
            "ESE" => 112.5,
            "SE" => 135.0,
            "SSE" => 157.5,
            "S" => 180.0,
            "SSW" or "SSO" => 202.5,
            "SW" or "SO" => 225.0,
            "WSW" or "OSO" => 247.5,
            "W" or "O" => 270.0,
            "WNW" or "ONO" => 292.5,
            "NW" or "NO" => 315.0,
            "NNW" or "NNO" => 337.5,
            "VR" or "VAR" => -1.0,
            _ => null,
        };
    }

    /// <summary>
    /// Completes each day from the hours of that day: day and night halves, degree day, air quality, pollen, UV,
    /// sunshine duration, and daily humidity, dew point, pressure, cloud cover and visibility.
    /// </summary>
    /// <param name="currentPollen">Used as today's pollen when there is no hourly pollen.</param>
    public static IReadOnlyList<Daily> CompleteDailyListFromHourlyList(
        IReadOnlyList<Daily> dailyList,
        IReadOnlyList<Hourly> hourlyList,
        IReadOnlyDictionary<DateTimeOffset, AirQuality> hourlyAirQuality,
        IReadOnlyDictionary<DateTimeOffset, Pollen> hourlyPollen,
        IReadOnlyDictionary<DateTimeOffset, TimeSpan?> hourlySunshine,
        Pollen? currentPollen,
        Location location)
    {
        if (dailyList.Count == 0)
        {
            return dailyList;
        }

        TimeZoneInfo timeZone = location.TimeZone;
        Dictionary<string, (List<Hourly> Day, List<Hourly> Night)> hourlyByHalfDay = GetHourlyListByHalfDay(hourlyList, timeZone);
        Dictionary<string, List<Hourly>> hourlyByDay = hourlyList
            .GroupBy(hourly => IsoDate(hourly.Date, timeZone))
            .ToDictionary(group => group.Key, group => group.ToList());
        string today = IsoDate(DateTimeOffset.UtcNow, timeZone);

        return dailyList.Select(daily =>
        {
            string day = IsoDate(daily.Date, timeZone);
            hourlyByHalfDay.TryGetValue(day, out (List<Hourly> Day, List<Hourly> Night) halves);
            List<Hourly>? hoursOfDay = hourlyByDay.GetValueOrDefault(day);

            HalfDay? newDay = CompleteHalfDayFromHourlyList(daily.Day, halves.Day, isDay: true);
            HalfDay? newNight = CompleteHalfDayFromHourlyList(daily.Night, halves.Night, isDay: false);

            Pollen? pollen = daily.Pollen;
            if (pollen is null)
            {
                if (hourlyPollen.Count == 0 && currentPollen is not null && today == day)
                {
                    pollen = GetDailyPollenFromHourlyList([currentPollen], bypassSizeCheck: true);
                }
                else if (hourlyPollen.Count > 0)
                {
                    pollen = GetDailyPollenFromHourlyList(
                        hourlyPollen.Where(pair => IsoDate(pair.Key, timeZone) == day).Select(pair => pair.Value.ToValid()).ToList());
                }
            }

            return daily with
            {
                Day = newDay,
                Night = newNight,
                DegreeDay = daily.DegreeDay?.Cooling is null || daily.DegreeDay.Heating is null
                    ? GetDegreeDay(newDay?.Temperature?.Value, newNight?.Temperature?.Value)
                    : daily.DegreeDay,
                AirQuality = daily.AirQuality ?? GetDailyAirQualityFromHourlyList(
                    hourlyAirQuality.Where(pair => IsoDate(pair.Key, timeZone) == day).Select(pair => pair.Value.ToValid()).ToList()),
                Pollen = pollen,
                UV = daily.UV?.Index is >= 0.0 ? daily.UV : GetDailyUVFromHourlyList(hoursOfDay),
                SunshineDuration = daily.SunshineDuration is { } sunshine && sunshine.TotalHours <= 24.0
                    ? sunshine
                    : GetSunshineDuration(hourlySunshine.Where(pair => IsoDate(pair.Key, timeZone) == day).Select(pair => pair.Value)),
                RelativeHumidity = GetDailyRelativeHumidity(daily.RelativeHumidity, Values(hoursOfDay, hourly => hourly.RelativeHumidity)),
                DewPoint = GetDailyDewPoint(daily.DewPoint, Values(hoursOfDay, hourly => hourly.DewPoint)),
                Pressure = GetDailyPressure(daily.Pressure, Values(hoursOfDay, hourly => hourly.Pressure)),
                CloudCover = GetDailyCloudCover(daily.CloudCover, Values(hoursOfDay, hourly => hourly.CloudCover)),
                Visibility = GetDailyVisibility(daily.Visibility, Values(hoursOfDay, hourly => hourly.Visibility)),
            };
        }).ToList();
    }

    public static DailyRelativeHumidity? GetDailyRelativeHumidity(DailyRelativeHumidity? initial, IReadOnlyList<Ratio>? values)
    {
        if (values is null || values.Count == 0)
        {
            return initial;
        }

        return new DailyRelativeHumidity
        {
            Average = initial?.Average?.ToValidRangeOrNull() ?? Ratio.FromPermille(values.Average(value => (double)value.Value)),
            Min = initial?.Min?.ToValidRangeOrNull() ?? values.Min(),
            Max = initial?.Max?.ToValidRangeOrNull() ?? values.Max(),
        };
    }

    public static DailyDewPoint? GetDailyDewPoint(DailyDewPoint? initial, IReadOnlyList<Units.Temperature>? values)
    {
        if (values is null || values.Count == 0)
        {
            return initial;
        }

        return new DailyDewPoint
        {
            Average = initial?.Average?.ToValidOrNull()
                ?? Units.Temperature.FromDeciCelsius(values.Average(value => (double)value.Value)),
            Min = initial?.Min?.ToValidOrNull() ?? values.Min(),
            Max = initial?.Max?.ToValidOrNull() ?? values.Max(),
        };
    }

    public static DailyPressure? GetDailyPressure(DailyPressure? initial, IReadOnlyList<Pressure>? values)
    {
        if (values is null || values.Count == 0)
        {
            return initial;
        }

        return new DailyPressure
        {
            Average = initial?.Average?.ToValidOrNull() ?? Pressure.FromPascals(values.Average(value => (double)value.Value)),
            Min = initial?.Min?.ToValidOrNull() ?? Pressure.FromPascals(values.Min(value => value.Value)),
            Max = initial?.Max?.ToValidOrNull() ?? Pressure.FromPascals(values.Max(value => value.Value)),
        };
    }

    public static DailyCloudCover? GetDailyCloudCover(DailyCloudCover? initial, IReadOnlyList<Ratio>? values)
    {
        if (values is null || values.Count == 0)
        {
            return initial;
        }

        return new DailyCloudCover
        {
            Average = initial?.Average?.ToValidRangeOrNull() ?? Ratio.FromPermille(values.Average(value => (double)value.Value)),
            Min = initial?.Min?.ToValidRangeOrNull() ?? values.Min(),
            Max = initial?.Max?.ToValidRangeOrNull() ?? values.Max(),
        };
    }

    public static DailyVisibility? GetDailyVisibility(DailyVisibility? initial, IReadOnlyList<Distance>? values)
    {
        if (values is null || values.Count == 0)
        {
            return initial;
        }

        return new DailyVisibility
        {
            Average = initial?.Average?.ToValidOrNull() ?? Distance.FromMeters(values.Average(value => (double)value.Value)),
            Min = initial?.Min?.ToValidOrNull() ?? Distance.FromMeters(values.Min(value => value.Value)),
            Max = initial?.Max?.ToValidOrNull() ?? Distance.FromMeters(values.Max(value => value.Value)),
        };
    }

    /// <summary>
    /// Completes each hour from its day: day or night, UV estimated from the daily maximum, and hourly air quality.
    /// </summary>
    public static IReadOnlyList<Hourly> CompleteHourlyListFromDailyList(
        IReadOnlyList<Hourly> hourlyList,
        IReadOnlyList<Daily> dailyList,
        IReadOnlyDictionary<DateTimeOffset, AirQuality> hourlyAirQuality,
        Location location)
    {
        TimeZoneInfo timeZone = location.TimeZone;
        Dictionary<string, Daily> dailyByDate = dailyList
            .GroupBy(daily => IsoDate(daily.Date, timeZone))
            .ToDictionary(group => group.Key, group => group.First());

        return hourlyList.Select(hourly =>
        {
            if (!dailyByDate.TryGetValue(IsoDate(hourly.Date, timeZone), out Daily? daily))
            {
                return hourly;
            }

            Astro? sun = daily.Sun;
            return hourly with
            {
                AirQuality = hourlyAirQuality.GetValueOrDefault(hourly.Date),
                // Without sun times, the flag given by the source is kept.
                IsDaylight = sun is null ? hourly.IsDaylight : IsDaylight(sun.RiseDate, sun.SetDate, hourly.Date),
                UV = hourly.UV?.Index is >= 0.0
                    ? hourly.UV
                    : GetCurrentUVFromDayMax(daily.UV?.Index, hourly.Date, sun?.RiseDate, sun?.SetDate, timeZone),
            };
        }).ToList();
    }

    /// <summary>
    /// Current conditions, completed from the current hour and today: weather text and code, temperature, wind,
    /// UV, air quality, humidity, dew point, pressure, cloud cover and visibility.
    /// </summary>
    public static Current CompleteCurrentFromHourlyData(
        CurrentWrapper? initialCurrent,
        Hourly? hourly,
        Daily? todayDaily,
        AirQuality? currentAirQuality,
        Location location)
    {
        CurrentWrapper newCurrent = initialCurrent ?? new CurrentWrapper();
        UV? uv = newCurrent.UV?.Index is not >= 0.0 && todayDaily is not null
            ? GetCurrentUVFromDayMax(
                todayDaily.UV?.Index,
                DateTimeOffset.UtcNow,
                todayDaily.Sun?.RiseDate,
                todayDaily.Sun?.SetDate,
                location.TimeZone)
            : newCurrent.UV;

        if (hourly is null)
        {
            return newCurrent.ToCurrent(uv);
        }

        Units.Temperature? initialTemperature = newCurrent.Temperature?.Temperature?.ToValidOrNull();
        Units.Temperature? initialFeelsLike = newCurrent.Temperature?.FeelsLike?.ToValidOrNull();
        Wind? initialWind = Validate(newCurrent.Wind);
        Wind? newWind = initialWind?.Speed is not null || hourly.Wind?.Speed is null ? initialWind : hourly.Wind;
        Ratio? newRelativeHumidity = newCurrent.RelativeHumidity?.ToValidRangeOrNull() ?? hourly.RelativeHumidity;

        // With current data, the dew point is computed from it rather than taken from the hour.
        Units.Temperature? newDewPoint = newCurrent.DewPoint?.ToValidOrNull()
            ?? (newRelativeHumidity is not null || initialTemperature is not null
                ? HumidityCalculations.ComputeDewPoint(initialTemperature ?? hourly.Temperature?.Value, newRelativeHumidity)
                : hourly.DewPoint);

        Models.Temperature? newTemperature = initialTemperature is { } temperature
            ? new Models.Temperature
            {
                Value = temperature,
                SourceFeelsLike = initialFeelsLike ?? hourly.Temperature?.SourceFeelsLike,
                ComputedApparent = TemperatureCalculations.ComputeApparentTemperature(temperature, newRelativeHumidity, newWind?.Speed),
                ComputedWindChill = TemperatureCalculations.ComputeWindChillTemperature(temperature, newWind?.Speed),
                ComputedHumidex = TemperatureCalculations.ComputeHumidex(temperature, newDewPoint),
            }
            : hourly.Temperature;

        string? weatherText = string.IsNullOrEmpty(newCurrent.WeatherText)
            ? (newCurrent.WeatherCode is { } currentCode ? GetWeatherText(currentCode) : null)
                ?? (hourly.WeatherCode is { } hourlyCode ? GetWeatherText(hourlyCode) : null)
                ?? hourly.WeatherText
            : newCurrent.WeatherText;

        return newCurrent.ToCurrent(uv) with
        {
            WeatherText = weatherText,
            WeatherCode = newCurrent.WeatherCode ?? hourly.WeatherCode,
            Temperature = newTemperature,
            Wind = newWind,
            AirQuality = currentAirQuality ?? hourly.AirQuality,
            RelativeHumidity = newRelativeHumidity,
            DewPoint = newDewPoint,
            Pressure = newCurrent.Pressure?.ToValidOrNull() ?? hourly.Pressure,
            CloudCover = newCurrent.CloudCover?.ToValidRangeOrNull() ?? hourly.CloudCover,
            Visibility = newCurrent.Visibility?.ToValidOrNull() ?? hourly.Visibility,
            Ceiling = newCurrent.Ceiling?.ToValidOrNull(),
        };
    }

    /// <summary>
    /// The generic text for a weather code, such as "Partly cloudy".
    /// </summary>
    public static string GetWeatherText(WeatherCode code)
    {
        string key = code switch
        {
            WeatherCode.PartlyCloudy => "weather_kind_partly_cloudy",
            WeatherCode.Cloudy => "weather_kind_cloudy",
            WeatherCode.Rain => "weather_kind_rain",
            WeatherCode.Snow => "weather_kind_snow",
            WeatherCode.Sleet => "weather_kind_sleet",
            WeatherCode.Hail => "weather_kind_hail",
            WeatherCode.Fog => "weather_kind_fog",
            WeatherCode.Haze => "weather_kind_haze",
            WeatherCode.Thunder => "weather_kind_thunder",
            WeatherCode.Thunderstorm => "weather_kind_thunderstorm",
            WeatherCode.Wind => "weather_kind_wind",
            _ => "weather_kind_clear",
        };
        return Strings.Get(key);
    }

    public static Wind? Validate(Wind? wind)
    {
        return wind is null
            ? null
            : wind with
            {
                Degree = Wind.ValidateDegree(wind.Degree),
                Speed = wind.Speed?.ToValidOrNull(),
                Gusts = wind.Gusts?.ToValidOrNull(),
            };
    }

    private static Hourly ComputeMissingHourlyData(HourlyWrapper hourly)
    {
        Units.Temperature? temperature = hourly.Temperature?.Temperature?.ToValidOrNull();
        Units.Temperature? feelsLike = hourly.Temperature?.FeelsLike?.ToValidOrNull();
        Wind? wind = Validate(hourly.Wind);
        Models.Precipitation? precipitation = hourly.Precipitation is { } p
            ? p with
            {
                Total = p.Total?.ToValidHourlyOrNull(),
                Thunderstorm = p.Thunderstorm?.ToValidHourlyOrNull(),
                Rain = p.Rain?.ToValidHourlyOrNull(),
                Snow = p.Snow?.ToValidHourlyOrNull(),
                Ice = p.Ice?.ToValidHourlyOrNull(),
            }
            : null;
        PrecipitationProbability? probability = hourly.PrecipitationProbability is { } pp
            ? pp with
            {
                Total = pp.Total?.ToValidRangeOrNull(),
                Thunderstorm = pp.Thunderstorm?.ToValidRangeOrNull(),
                Rain = pp.Rain?.ToValidRangeOrNull(),
                Snow = pp.Snow?.ToValidRangeOrNull(),
                Ice = pp.Ice?.ToValidRangeOrNull(),
            }
            : null;
        Ratio? cloudCover = hourly.CloudCover?.ToValidRangeOrNull();
        Distance? visibility = hourly.Visibility?.ToValidOrNull();
        WeatherCode? weatherCode = hourly.WeatherCode
            ?? GetHalfDayWeatherCodeFromHourlyList([hourly.ToHourly()], precipitation, probability, wind, cloudCover, visibility);
        Ratio? relativeHumidity = hourly.RelativeHumidity?.ToValidRangeOrNull()
            ?? HumidityCalculations.ComputeRelativeHumidity(temperature, hourly.DewPoint?.ToValidOrNull());
        Units.Temperature? dewPoint = hourly.DewPoint?.ToValidOrNull()
            ?? HumidityCalculations.ComputeDewPoint(temperature, relativeHumidity);

        return hourly.ToHourly() with
        {
            WeatherText = string.IsNullOrEmpty(hourly.WeatherText)
                ? (weatherCode is { } code ? GetWeatherText(code) : null)
                : hourly.WeatherText,
            WeatherCode = weatherCode,
            Temperature = new Models.Temperature
            {
                Value = temperature,
                SourceFeelsLike = feelsLike,
                ComputedApparent = TemperatureCalculations.ComputeApparentTemperature(temperature, relativeHumidity, wind?.Speed),
                ComputedWindChill = TemperatureCalculations.ComputeWindChillTemperature(temperature, wind?.Speed),
                ComputedHumidex = TemperatureCalculations.ComputeHumidex(temperature, dewPoint),
            },
            Precipitation = new Models.Precipitation
            {
                Total = precipitation?.Total
                    ?? PrecipitationCalculations.ComputeTotalPrecipitation(temperature, precipitation?.Rain, precipitation?.Snow, precipitation?.Ice),
                Thunderstorm = precipitation?.Thunderstorm,
                Rain = precipitation?.Rain,
                Snow = precipitation?.Snow,
                Ice = precipitation?.Ice,
            },
            PrecipitationProbability = new PrecipitationProbability
            {
                Total = probability?.Total
                    ?? PrecipitationCalculations.ComputeTotalProbabilityOfPrecipitation(
                        probability?.Thunderstorm, probability?.Rain, probability?.Ice, probability?.Snow),
                Thunderstorm = probability?.Thunderstorm,
                Rain = probability?.Rain,
                Snow = probability?.Snow,
                Ice = probability?.Ice,
            },
            Wind = wind,
            RelativeHumidity = relativeHumidity,
            DewPoint = dewPoint,
            Pressure = hourly.Pressure?.ToValidOrNull(),
            CloudCover = cloudCover,
            Visibility = visibility,
        };
    }

    private static DegreeDay? GetDegreeDay(Units.Temperature? minTemperature, Units.Temperature? maxTemperature)
    {
        if (minTemperature is not { } min || maxTemperature is not { } max)
        {
            return null;
        }

        double mean = (min.Value + max.Value) / 2.0;
        Units.Temperature zero = Units.Temperature.FromDeciCelsius(0L);
        if (mean is >= HeatingDegreeDayBase - DegreeDayMargin and <= CoolingDegreeDayBase + DegreeDayMargin)
        {
            return new DegreeDay { Heating = zero, Cooling = zero };
        }

        return mean < HeatingDegreeDayBase
            ? new DegreeDay { Heating = Units.Temperature.FromDeciCelsius(HeatingDegreeDayBase - mean), Cooling = zero }
            : new DegreeDay { Heating = zero, Cooling = Units.Temperature.FromDeciCelsius(mean - CoolingDegreeDayBase) };
    }

    // The day runs from 06:00 to 17:59 and the night from 18:00 to 05:59 the next morning. Hours are shifted back
    // by 6 hours so that the night after midnight belongs to the day it started on.
    private static Dictionary<string, (List<Hourly> Day, List<Hourly> Night)> GetHourlyListByHalfDay(
        IReadOnlyList<Hourly> hourlyList,
        TimeZoneInfo timeZone)
    {
        Dictionary<string, (List<Hourly> Day, List<Hourly> Night)> byHalfDay = [];
        foreach (Hourly hourly in hourlyList)
        {
            DateTimeOffset shifted = TimeZoneInfo.ConvertTime(hourly.Date.AddHours(-6), timeZone);
            string day = shifted.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!byHalfDay.TryGetValue(day, out (List<Hourly> Day, List<Hourly> Night) halves))
            {
                halves = ([], []);
                byHalfDay[day] = halves;
            }

            (shifted.Hour < 12 ? halves.Day : halves.Night).Add(hourly);
        }

        return byHalfDay;
    }

    // Only what the source left out is computed from the hours of the half day.
    private static HalfDay? CompleteHalfDayFromHourlyList(HalfDay? initialHalfDay, List<Hourly>? hours, bool isDay)
    {
        if (hours is null || hours.Count == 0)
        {
            return initialHalfDay;
        }

        HalfDay halfDay = initialHalfDay ?? new HalfDay();

        Models.Temperature initialTemperature = new()
        {
            Value = halfDay.Temperature?.Value?.ToValidOrNull(),
            SourceFeelsLike = halfDay.Temperature?.SourceFeelsLike?.ToValidOrNull(),
        };
        Models.Temperature temperature = initialTemperature.Value is null || initialTemperature.SourceFeelsLike is null
            ? GetHalfDayTemperatureFromHourlyList(initialTemperature, hours, isDay)
            : initialTemperature;

        Models.Precipitation? initialPrecipitation = halfDay.Precipitation is { } p
            ? p with
            {
                Total = p.Total?.ToValidHalfDayOrNull(),
                Thunderstorm = p.Thunderstorm?.ToValidHalfDayOrNull(),
                Rain = p.Rain?.ToValidHalfDayOrNull(),
                Snow = p.Snow?.ToValidHalfDayOrNull(),
                Ice = p.Ice?.ToValidHalfDayOrNull(),
            }
            : null;
        Models.Precipitation totalPrecipitation = initialPrecipitation?.Total is null
            ? GetHalfDayPrecipitationFromHourlyList(hours)
            : initialPrecipitation;

        PrecipitationProbability? initialProbability = halfDay.PrecipitationProbability is { } pp
            ? pp with
            {
                Total = pp.Total?.ToValidRangeOrNull(),
                Thunderstorm = pp.Thunderstorm?.ToValidRangeOrNull(),
                Rain = pp.Rain?.ToValidRangeOrNull(),
                Snow = pp.Snow?.ToValidRangeOrNull(),
                Ice = pp.Ice?.ToValidRangeOrNull(),
            }
            : null;
        PrecipitationProbability maxProbability = initialProbability?.Total is null
            ? GetHalfDayPrecipitationProbabilityFromHourlyList(hours)
            : initialProbability;

        PrecipitationDuration? precipitationDuration = halfDay.PrecipitationDuration is { } pd
            ? pd with
            {
                Total = ValidHalfDay(pd.Total),
                Thunderstorm = ValidHalfDay(pd.Thunderstorm),
                Rain = ValidHalfDay(pd.Rain),
                Snow = ValidHalfDay(pd.Snow),
                Ice = ValidHalfDay(pd.Ice),
            }
            : null;

        Wind? initialWind = Validate(halfDay.Wind);
        Wind? maxWind = initialWind?.Speed is null ? GetHalfDayWindFromHourlyList(hours) : initialWind;

        WeatherCode? weatherCode = halfDay.WeatherCode ?? GetHalfDayWeatherCodeFromHourlyList(
            hours,
            totalPrecipitation,
            maxProbability,
            maxWind,
            GetHalfDayCloudCoverFromHourlyList(hours),
            GetHalfDayAvgVisibilityFromHourlyList(hours));

        string? textFromCode = weatherCode is { } code ? GetWeatherText(code) : null;
        return halfDay with
        {
            WeatherText = string.IsNullOrEmpty(halfDay.WeatherText) ? textFromCode : halfDay.WeatherText,
            WeatherSummary = string.IsNullOrEmpty(halfDay.WeatherSummary)
                ? (string.IsNullOrEmpty(halfDay.WeatherText) ? textFromCode : halfDay.WeatherText)
                : halfDay.WeatherSummary,
            WeatherCode = weatherCode,
            Temperature = temperature,
            Precipitation = totalPrecipitation,
            PrecipitationProbability = maxProbability,
            PrecipitationDuration = precipitationDuration,
            Wind = maxWind,
        };
    }

    /// <summary>
    /// Guesses a weather code from the conditions of a period, following NOAA's approach.
    /// </summary>
    /// <param name="totalPrecipitation">Precipitation summed over the period.</param>
    /// <param name="maxProbability">Highest precipitation probability over the period.</param>
    /// <param name="maxWind">Strongest wind over the period.</param>
    /// <param name="averageCloudCover">Average cloud cover over the period.</param>
    /// <param name="averageVisibility">Average visibility over the period.</param>
    private static WeatherCode? GetHalfDayWeatherCodeFromHourlyList(
        IReadOnlyList<Hourly> hours,
        Models.Precipitation? totalPrecipitation,
        PrecipitationProbability? maxProbability,
        Wind? maxWind,
        Ratio? averageCloudCover,
        Distance? averageVisibility)
    {
        Units.Precipitation minPrecipitation = Units.Precipitation.FromMillimeters(1.0);
        Ratio minProbability = Ratio.FromPercent(30L);
        Distance maxVisibilityHaze = Distance.FromMeters(5000L);
        Distance maxVisibilityFog = Distance.FromMeters(1000L);
        Speed maxWindSpeedCalm = Speed.FromMetersPerSecond(10L);

        // At least 1 mm in total with a probability above 30 %, assuming 1 mm and 100 % when they are not reported.
        if ((totalPrecipitation?.Total ?? minPrecipitation) >= minPrecipitation
            && (maxProbability?.Total ?? Ratio.FromPercent(100L)) > minProbability)
        {
            bool isRain = IsLikely(maxProbability?.Rain, totalPrecipitation?.Rain, minProbability);
            bool isSnow = IsLikely(maxProbability?.Snow, totalPrecipitation?.Snow, minProbability);
            bool isIce = IsLikely(maxProbability?.Ice, totalPrecipitation?.Ice, minProbability);
            bool isThunder = IsLikely(maxProbability?.Thunderstorm, totalPrecipitation?.Thunderstorm, minProbability);

            if (isRain || isSnow || isIce || isThunder)
            {
                if (isThunder)
                {
                    return isRain || isSnow || isIce ? WeatherCode.Thunderstorm : WeatherCode.Thunder;
                }

                if (isIce)
                {
                    return WeatherCode.Hail;
                }

                if (isSnow)
                {
                    return isRain ? WeatherCode.Sleet : WeatherCode.Snow;
                }

                return WeatherCode.Rain;
            }

            // Otherwise the weather codes of the hours decide.
            if (hours.Count(hourly => hourly.WeatherCode == WeatherCode.Thunderstorm) > 1)
            {
                return WeatherCode.Thunderstorm;
            }

            int rain = hours.Count(hourly => hourly.WeatherCode == WeatherCode.Rain);
            int sleet = hours.Count(hourly => hourly.WeatherCode == WeatherCode.Sleet);
            int snow = hours.Count(hourly => hourly.WeatherCode == WeatherCode.Snow);
            int hail = hours.Count(hourly => hourly.WeatherCode == WeatherCode.Hail);
            if (Math.Max(Math.Max(rain, sleet), Math.Max(snow, hail)) == 0)
            {
                return WeatherCode.Rain;
            }

            if (hours.Count(hourly => hourly.WeatherCode == WeatherCode.Thunder) > 1)
            {
                return WeatherCode.Thunderstorm;
            }

            if (hail > 1)
            {
                return WeatherCode.Hail;
            }

            if ((rain > 1 && snow > 1) || sleet > 1)
            {
                return WeatherCode.Sleet;
            }

            return snow > 1 ? WeatherCode.Snow : WeatherCode.Rain;
        }

        if (averageVisibility is { } visibility && visibility < maxVisibilityHaze)
        {
            return visibility < maxVisibilityFog ? WeatherCode.Fog : WeatherCode.Haze;
        }

        if (maxWind?.Speed is { } speed && speed > maxWindSpeedCalm)
        {
            return WeatherCode.Wind;
        }

        if (averageCloudCover is { } cloudCover)
        {
            if (cloudCover > Ratio.FromPercent(CloudCoverBroken))
            {
                return WeatherCode.Cloudy;
            }

            return cloudCover > Ratio.FromPercent(CloudCoverFew) ? WeatherCode.PartlyCloudy : WeatherCode.Clear;
        }

        // Without precipitation, visibility, wind or cloud cover, the median weather code of the hours is used.
        List<Hourly> withCode = hours.Where(hourly => hourly.WeatherCode is not null).ToList();
        if (withCode.Count == 0)
        {
            return WeatherCode.PartlyCloudy;
        }

        return withCode[withCode.Count % 2 == 0 ? withCode.Count / 2 : (withCode.Count - 1) / 2].WeatherCode;
    }

    // The probability decides when reported, then the amount.
    private static bool IsLikely(Ratio? probability, Units.Precipitation? amount, Ratio minProbability)
    {
        if (probability is { } p)
        {
            return p > minProbability;
        }

        return amount is { } a && a.Value > 0;
    }

    // The day keeps the highest values and the night the lowest. A feels-like temperature from the source
    // is only kept when every hour has one.
    private static Models.Temperature GetHalfDayTemperatureFromHourlyList(
        Models.Temperature initial,
        List<Hourly> hours,
        bool isDay)
    {
        Units.Temperature? Extreme(IEnumerable<Units.Temperature?> values)
        {
            List<Units.Temperature> present = values.OfType<Units.Temperature>().ToList();
            if (present.Count == 0)
            {
                return null;
            }

            return isDay ? present.Max() : present.Min();
        }

        Units.Temperature? sourceFeelsLike = initial.SourceFeelsLike?.ToValidOrNull();
        if (sourceFeelsLike is null)
        {
            List<Units.Temperature> hourlyFeelsLike =
                hours.Select(hourly => hourly.Temperature?.SourceFeelsLike).OfType<Units.Temperature>().ToList();
            sourceFeelsLike = hourlyFeelsLike.Count == hours.Count ? Extreme(hourlyFeelsLike.Cast<Units.Temperature?>()) : null;
        }

        return initial with
        {
            Value = initial.Value?.ToValidOrNull() ?? Extreme(hours.Select(hourly => hourly.Temperature?.Value)),
            SourceFeelsLike = sourceFeelsLike,
            ComputedApparent = initial.ComputedApparent ?? Extreme(hours.Select(hourly => hourly.Temperature?.ComputedApparent)),
            ComputedWindChill = initial.ComputedWindChill ?? Extreme(hours.Select(hourly => hourly.Temperature?.ComputedWindChill)),
            ComputedHumidex = initial.ComputedHumidex ?? Extreme(hours.Select(hourly => hourly.Temperature?.ComputedHumidex)),
        };
    }

    private static Models.Precipitation GetHalfDayPrecipitationFromHourlyList(List<Hourly> hours)
    {
        Units.Precipitation? Sum(Func<Models.Precipitation, Units.Precipitation?> amount)
        {
            List<Units.Precipitation> present =
                hours.Select(hourly => hourly.Precipitation is { } p ? amount(p) : null).OfType<Units.Precipitation>().ToList();
            return present.Count == 0 ? null : Units.Precipitation.FromMicrometers(present.Sum(value => value.Value));
        }

        return new Models.Precipitation
        {
            Total = Sum(p => p.Total),
            Thunderstorm = Sum(p => p.Thunderstorm),
            Rain = Sum(p => p.Rain),
            Snow = Sum(p => p.Snow),
            Ice = Sum(p => p.Ice),
        };
    }

    private static PrecipitationProbability GetHalfDayPrecipitationProbabilityFromHourlyList(List<Hourly> hours)
    {
        Ratio? Highest(Func<PrecipitationProbability, Ratio?> probability)
        {
            List<Ratio> present = hours
                .Select(hourly => hourly.PrecipitationProbability is { } p ? probability(p) : null)
                .OfType<Ratio>()
                .ToList();
            return present.Count == 0 ? null : present.Max();
        }

        return new PrecipitationProbability
        {
            Total = Highest(p => p.Total),
            Thunderstorm = Highest(p => p.Thunderstorm),
            Rain = Highest(p => p.Rain),
            Snow = Highest(p => p.Snow),
            Ice = Highest(p => p.Ice),
        };
    }

    // The wind of the windiest hour, with the strongest gust of the period when it came at another hour.
    private static Wind? GetHalfDayWindFromHourlyList(List<Hourly> hours)
    {
        Wind? maxWind = hours
            .Where(hourly => hourly.Wind?.Speed is not null)
            .MaxBy(hourly => hourly.Wind!.Speed!.Value)
            ?.Wind;
        Speed? maxGusts = hours
            .Where(hourly => hourly.Wind?.Gusts is not null)
            .MaxBy(hourly => hourly.Wind!.Gusts!.Value)
            ?.Wind?.Gusts;

        return maxGusts is null || maxGusts == maxWind?.Gusts ? maxWind : maxWind is null ? null : maxWind with { Gusts = maxGusts };
    }

    private static Ratio? GetHalfDayCloudCoverFromHourlyList(List<Hourly> hours)
    {
        List<long> values = hours.Select(hourly => hourly.CloudCover?.Value).OfType<long>().ToList();
        return values.Count == 0 ? null : Ratio.FromPermille(values.Average());
    }

    private static Distance? GetHalfDayAvgVisibilityFromHourlyList(List<Hourly> hours)
    {
        List<double> values = hours.Select(hourly => hourly.Visibility?.InMeters).OfType<double>().ToList();
        return values.Count == 0 ? null : Distance.FromMeters(values.Average());
    }

    private static AirQuality? GetDailyAirQualityFromHourlyList(IReadOnlyCollection<AirQuality>? hours)
    {
        if (hours is null || hours.Count < MinimumHoursForDailyValue)
        {
            return null;
        }

        PollutantConcentration? Average(Func<AirQuality, PollutantConcentration?> pollutant)
        {
            List<double> values = hours.Select(pollutant).OfType<PollutantConcentration>()
                .Select(value => value.InMicrogramsPerCubicMeter).ToList();
            return values.Count == 0 ? null : PollutantConcentration.FromMicrogramsPerCubicMeter(values.Average());
        }

        return new AirQuality
        {
            PM25 = Average(airQuality => airQuality.PM25),
            PM10 = Average(airQuality => airQuality.PM10),
            SO2 = Average(airQuality => airQuality.SO2),
            NO2 = Average(airQuality => airQuality.NO2),
            O3 = Average(airQuality => airQuality.O3),
            CO = Average(airQuality => airQuality.CO),
        };
    }

    private static Pollen? GetDailyPollenFromHourlyList(IReadOnlyCollection<Pollen>? hours, bool bypassSizeCheck = false)
    {
        if (hours is null || hours.Count == 0 || (hours.Count < MinimumHoursForDailyValue && !bypassSizeCheck))
        {
            return null;
        }

        PollenConcentration? Highest(Func<Pollen, PollenConcentration?> pollen)
        {
            List<PollenConcentration> values = hours.Select(pollen).OfType<PollenConcentration>().ToList();
            return values.Count == 0 ? null : values.Max();
        }

        return new Pollen
        {
            Alder = Highest(pollen => pollen.Alder),
            Ash = Highest(pollen => pollen.Ash),
            Birch = Highest(pollen => pollen.Birch),
            Chestnut = Highest(pollen => pollen.Chestnut),
            Cypress = Highest(pollen => pollen.Cypress),
            Grass = Highest(pollen => pollen.Grass),
            Hazel = Highest(pollen => pollen.Hazel),
            Hornbeam = Highest(pollen => pollen.Hornbeam),
            Linden = Highest(pollen => pollen.Linden),
            Mold = Highest(pollen => pollen.Mold),
            Mugwort = Highest(pollen => pollen.Mugwort),
            Oak = Highest(pollen => pollen.Oak),
            Olive = Highest(pollen => pollen.Olive),
            Plane = Highest(pollen => pollen.Plane),
            Plantain = Highest(pollen => pollen.Plantain),
            Poplar = Highest(pollen => pollen.Poplar),
            Ragweed = Highest(pollen => pollen.Ragweed),
            Sorrel = Highest(pollen => pollen.Sorrel),
            Tree = Highest(pollen => pollen.Tree),
            Urticaceae = Highest(pollen => pollen.Urticaceae),
        };
    }

    private static UV? GetDailyUVFromHourlyList(List<Hourly>? hours)
    {
        List<double> values = hours?.Select(hourly => hourly.UV?.Index).OfType<double>().ToList() ?? [];
        return values.Count == 0 ? null : new UV { Index = values.Max() };
    }

    private static TimeSpan? GetSunshineDuration(IEnumerable<TimeSpan?> hours)
    {
        List<TimeSpan> values = hours.OfType<TimeSpan>().ToList();
        return values.Count == 0 ? null : values.Aggregate(TimeSpan.Zero, (total, value) => total + value);
    }

    // No sunrise or sunset means polar night. A sunrise after the sunset counts as daylight.
    private static bool IsDaylight(DateTimeOffset? sunrise, DateTimeOffset? sunset, DateTimeOffset current)
    {
        if (sunrise is not { } rise || sunset is not { } set)
        {
            return false;
        }

        return rise > set || (current >= rise && current < set);
    }

    // The UV follows a cosine from zero at sunrise to the daily maximum at solar noon and back to zero at sunset.
    private static UV? GetCurrentUVFromDayMax(
        double? dayMaxUV,
        DateTimeOffset? currentDate,
        DateTimeOffset? sunriseDate,
        DateTimeOffset? sunsetDate,
        TimeZoneInfo timeZone)
    {
        if (dayMaxUV is not { } maxUV
            || currentDate is not { } current
            || sunriseDate is not { } sunrise
            || sunsetDate is not { } sunset
            || sunrise > sunset
            || current < sunrise
            || current > sunset)
        {
            return null;
        }

        static double HourOfDay(DateTimeOffset date, TimeZoneInfo zone)
        {
            DateTimeOffset local = TimeZoneInfo.ConvertTime(date, zone);
            return local.Hour + local.Minute / 60.0;
        }

        double sunriseTime = HourOfDay(sunrise, timeZone);
        double sunlightDuration = HourOfDay(sunset, timeZone) - sunriseTime;
        double currentOffset = HourOfDay(current, timeZone) - sunriseTime;
        double theta = currentOffset / sunlightDuration * 2.0 * Math.PI - Math.PI;
        return new UV { Index = maxUV * (Math.Cos(theta) + 1) / 2 };
    }

    private static TimeSpan? ValidHalfDay(TimeSpan? duration)
    {
        return duration is { TotalHours: <= 12.0 } ? duration : null;
    }

    /// <summary>
    /// The date of an instant in the time zone, as yyyy-MM-dd.
    /// </summary>
    public static string IsoDate(DateTimeOffset date, TimeZoneInfo timeZone)
    {
        return TimeZoneInfo.ConvertTime(date, timeZone).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static List<T> Values<T>(List<Hourly>? hours, Func<Hourly, T?> value)
        where T : struct
    {
        return hours?.Select(value).OfType<T>().ToList() ?? [];
    }

    // The second map wins when both have the same date.
    private static Dictionary<DateTimeOffset, T> Merge<T>(
        IReadOnlyDictionary<DateTimeOffset, T> first,
        IReadOnlyDictionary<DateTimeOffset, T>? second)
    {
        Dictionary<DateTimeOffset, T> merged = new(first);
        foreach ((DateTimeOffset date, T value) in second ?? new Dictionary<DateTimeOffset, T>())
        {
            merged[date] = value;
        }

        return merged;
    }

    private static Dictionary<DateTimeOffset, T> Valid<T>(Dictionary<DateTimeOffset, T> items, Func<T, T> toValid)
    {
        return items.ToDictionary(pair => pair.Key, pair => toValid(pair.Value));
    }

    private static Dictionary<DateTimeOffset, TValue> ByDate<TItem, TValue>(
        IEnumerable<TItem> items,
        Func<TItem, DateTimeOffset> date,
        Func<TItem, TValue> value)
    {
        Dictionary<DateTimeOffset, TValue> result = [];
        foreach (TItem item in items)
        {
            result[date(item)] = value(item);
        }

        return result;
    }
}
