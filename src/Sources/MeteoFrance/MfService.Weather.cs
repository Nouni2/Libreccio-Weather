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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.MeteoFrance;

public sealed partial class MfService
{
    private static CurrentWrapper? GetCurrent(MfCurrentResult result)
    {
        return result.Properties?.Gridded is not { } current ? null : new CurrentWrapper
        {
            WeatherText = current.WeatherDescription,
            WeatherCode = GetWeatherCode(current.WeatherIcon),
            Temperature = new TemperatureWrapper { Temperature = Celsius(current.Temperature) },
            Wind = new Wind { Degree = current.WindDirection, Speed = MetersPerSecond(current.WindSpeed) },
        };
    }

    private static IReadOnlyList<DailyWrapper> GetDailyList(Location location, MfForecastDaily[]? dailyForecasts)
    {
        List<DailyWrapper> daily = [];
        for (int i = 0; i < (dailyForecasts?.Length ?? 0) - 1; i++)
        {
            MfForecastDaily item = dailyForecasts![i];
            DateTime local = DateTime.SpecifyKind(item.Time.UtcDateTime.Date, DateTimeKind.Unspecified);
            while (location.TimeZone.IsInvalidTime(local))
            {
                local = local.AddMinutes(1);
            }

            daily.Add(new DailyWrapper
            {
                Date = new DateTimeOffset(local, location.TimeZone.GetUtcOffset(local)),
                Day = new HalfDayWrapper
                {
                    WeatherText = item.DailyWeatherDescription,
                    WeatherCode = GetWeatherCode(item.DailyWeatherIcon),
                    Temperature = new TemperatureWrapper { Temperature = Celsius(item.TMax) },
                },
                Night = new HalfDayWrapper
                {
                    WeatherText = item.DailyWeatherDescription,
                    WeatherCode = GetWeatherCode(item.DailyWeatherIcon),
                    Temperature = new TemperatureWrapper { Temperature = Celsius(dailyForecasts[i + 1].TMin) },
                },
                UV = new UV { Index = item.UVIndex },
                RelativeHumidity = new DailyRelativeHumidity { Min = Percent(item.RelativeHumidityMin), Max = Percent(item.RelativeHumidityMax) },
            });
        }

        return daily;
    }

    private static IReadOnlyList<HourlyWrapper>? GetHourlyList(MfForecastHourly[]? hours, MfForecastProbability[]? probabilities)
    {
        return hours?.Select(hour => new HourlyWrapper
        {
            Date = hour.Time,
            WeatherText = hour.WeatherDescription,
            WeatherCode = GetWeatherCode(hour.WeatherIcon),
            Temperature = new TemperatureWrapper { Temperature = Celsius(hour.T), FeelsLike = Celsius(hour.TWindchill) },
            Precipitation = new Models.Precipitation
            {
                Rain = Millimeters(hour.Rain1h ?? hour.Rain3h ?? hour.Rain6h ?? hour.Rain12h ?? hour.Rain24h),
                Snow = Millimeters(hour.Snow1h ?? hour.Snow3h ?? hour.Snow6h ?? hour.Snow12h ?? hour.Snow24h),
            },
            PrecipitationProbability = GetHourlyPrecipitationProbability(probabilities, hour.Time),
            Wind = new Wind { Degree = hour.WindDirection, Speed = MetersPerSecond(hour.WindSpeed), Gusts = MetersPerSecond(hour.WindSpeedGust) },
            RelativeHumidity = Percent(hour.RelativeHumidity),
            Pressure = hour.PSea is { } pressure ? Pressure.FromHectopascals(pressure) : null,
            CloudCover = Percent(hour.TotalCloudCover),
        }).ToList();
    }

    private static PrecipitationProbability? GetHourlyPrecipitationProbability(MfForecastProbability[]? probabilities, DateTimeOffset date)
    {
        if (probabilities is not { Length: > 0 })
        {
            return null;
        }

        int? rain = null;
        int? snow = null;
        int? ice = null;
        foreach (MfForecastProbability item in probabilities)
        {
            long offset = date.ToUnixTimeMilliseconds() - item.Time.ToUnixTimeMilliseconds();
            if (offset is 0 or 3_600_000 or 7_200_000)
            {
                rain = item.RainHazard3h ?? item.RainHazard6h ?? rain;
                snow = item.SnowHazard3h ?? item.SnowHazard6h ?? snow;
                ice = item.FreezingHazard ?? ice;
            }

            if (offset is 10_800_000 or 14_400_000 or 18_000_000)
            {
                rain = item.RainHazard6h ?? rain;
                snow = item.SnowHazard6h ?? snow;
                ice = item.FreezingHazard ?? ice;
            }
        }

        return new PrecipitationProbability
        {
            Total = rain is not null || snow is not null || ice is not null ? Percent(Math.Max(rain ?? 0, Math.Max(snow ?? 0, ice ?? 0))) : null,
            Rain = Percent(rain),
            Snow = Percent(snow),
            Ice = Percent(ice),
        };
    }

    private static IReadOnlyList<Minutely> GetMinutelyList(MfRainResult result)
    {
        List<Minutely> minutely = [];
        MfRainForecast[] forecasts = result.Properties?.RainForecasts ?? [];
        for (int i = 0; i < forecasts.Length; i++)
        {
            MfRainForecast item = forecasts[i];
            long interval = i < forecasts.Length - 1
                ? forecasts[i + 1].Time.ToUnixTimeMilliseconds() - item.Time.ToUnixTimeMilliseconds()
                : item.Time.ToUnixTimeMilliseconds() - forecasts[i - 1].Time.ToUnixTimeMilliseconds();
            minutely.Add(new Minutely
            {
                Date = item.Time,
                MinuteInterval = UnitFormatting.RoundToInt((double)(interval / 60_000)),
                PrecipitationIntensity = item.RainIntensity is { } intensity ? Units.Precipitation.FromMillimeters(intensity switch
                {
                    4 => Models.Precipitation.PrecipitationHourlyHeavy,
                    3 => Models.Precipitation.PrecipitationHourlyMedium,
                    2 => Models.Precipitation.PrecipitationHourlyLight,
                    _ => 0.0,
                }) : null,
            });
        }

        return minutely;
    }

    private static IReadOnlyDictionary<Month, Normals>? GetNormals(MfNormalsResult result)
    {
        if (result.Properties?.Stats is not { Length: > 0 } stats)
        {
            return null;
        }

        Dictionary<Month, Normals> normals = [];
        foreach (Month month in Enum.GetValues<Month>())
        {
            if ((int)month <= stats.Length)
            {
                MfNormalsStats item = stats[(int)month - 1];
                normals[month] = new Normals { DaytimeTemperature = Celsius(item.TMax), NighttimeTemperature = Celsius(item.TMin) };
            }
        }

        return normals;
    }

    private static WeatherCode? GetWeatherCode(string? icon)
    {
        return icon switch
        {
            null => null,
            _ when StartsWith(icon, "p32", "p33", "p34") => WeatherCode.Wind,
            _ when icon.StartsWith("p31", StringComparison.Ordinal) => null,
            _ when StartsWith(icon, "p26", "p27", "p28", "p29") => WeatherCode.Thunder,
            _ when StartsWith(icon, "p21", "p22", "p23") => WeatherCode.Snow,
            _ when StartsWith(icon, "p19", "p20") => WeatherCode.Hail,
            _ when StartsWith(icon, "p17", "p18") => WeatherCode.Sleet,
            _ when StartsWith(icon, "p16", "p24", "p25", "p30") => WeatherCode.Thunderstorm,
            _ when StartsWith(icon, "p9", "p10", "p11", "p12", "p13", "p14", "p15") => WeatherCode.Rain,
            _ when StartsWith(icon, "p6", "p7", "p8") => WeatherCode.Fog,
            _ when StartsWith(icon, "p4", "p5") => WeatherCode.Haze,
            _ when icon.StartsWith("p3", StringComparison.Ordinal) => WeatherCode.Cloudy,
            _ when icon.StartsWith("p2", StringComparison.Ordinal) => WeatherCode.PartlyCloudy,
            _ when icon.StartsWith("p1", StringComparison.Ordinal) => WeatherCode.Clear,
            _ => null,
        };
    }

    private static bool StartsWith(string value, params string[] prefixes) => prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
    private static Units.Temperature? Celsius(double? value) => value is { } number ? Units.Temperature.FromCelsius(number) : null;
    private static Speed? MetersPerSecond(double? value) => value is { } number ? Speed.FromMetersPerSecond(number) : null;
    private static Units.Precipitation? Millimeters(double? value) => value is { } number ? Units.Precipitation.FromMillimeters(number) : null;
    private static Ratio? Percent(int? value) => value is { } number ? Ratio.FromPercent((long)number) : null;
}
