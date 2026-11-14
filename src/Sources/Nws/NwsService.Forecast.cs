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

using System.Text.Json;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Models.Wrappers;
using LibreccioWeather.Core.Sources.Serialization;
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Sources.Nws;

public sealed partial class NwsService
{
    private static IReadOnlyList<HourlyWrapper>? GetHourlyForecast(NwsGridPointProperties? properties)
    {
        if (properties is null)
        {
            return null;
        }

        Dictionary<DateTimeOffset, NwsValueWeatherValue> weather = GetWeatherForecast(properties.Weather);
        Dictionary<DateTimeOffset, double> temperature = GetDoubleForecast(properties.Temperature);
        Dictionary<DateTimeOffset, double> apparentTemperature = GetDoubleForecast(properties.ApparentTemperature);
        Dictionary<DateTimeOffset, double> wetBulbGlobeTemperature = GetDoubleForecast(properties.WetBulbGlobeTemperature);
        Dictionary<DateTimeOffset, double> windChill = GetDoubleForecast(properties.WindChill);
        Dictionary<DateTimeOffset, double> dewpoint = GetDoubleForecast(properties.Dewpoint);
        Dictionary<DateTimeOffset, int> relativeHumidity = GetIntForecast(properties.RelativeHumidity);
        Dictionary<DateTimeOffset, double> quantitativePrecipitation = GetDoubleForecast(properties.QuantitativePrecipitation, true);
        Dictionary<DateTimeOffset, double> snowfallAmount = GetDoubleForecast(properties.SnowfallAmount, true);
        Dictionary<DateTimeOffset, double> iceAccumulation = GetDoubleForecast(properties.IceAccumulation, true);
        Dictionary<DateTimeOffset, int> probabilityOfPrecipitation = GetIntForecast(properties.ProbabilityOfPrecipitation);
        Dictionary<DateTimeOffset, double> probabilityOfThunder = GetDoubleForecast(properties.ProbabilityOfThunder);
        Dictionary<DateTimeOffset, int> windDirection = GetIntForecast(properties.WindDirection);
        Dictionary<DateTimeOffset, double> windSpeed = GetDoubleForecast(properties.WindSpeed);
        Dictionary<DateTimeOffset, double> windGust = GetDoubleForecast(properties.WindGust);
        Dictionary<DateTimeOffset, double> pressure = GetDoubleForecast(properties.Pressure);
        Dictionary<DateTimeOffset, int> skyCover = GetIntForecast(properties.SkyCover);
        Dictionary<DateTimeOffset, double> visibility = GetDoubleForecast(properties.Visibility);
        SortedSet<DateTimeOffset> uniqueDates = [];
        IEnumerable<DateTimeOffset>[] dates =
        [
            temperature.Keys, dewpoint.Keys, relativeHumidity.Keys, apparentTemperature.Keys, wetBulbGlobeTemperature.Keys,
            windChill.Keys, skyCover.Keys, windDirection.Keys, windSpeed.Keys, windGust.Keys, weather.Keys,
            probabilityOfPrecipitation.Keys, quantitativePrecipitation.Keys, iceAccumulation.Keys, snowfallAmount.Keys,
            visibility.Keys, pressure.Keys, probabilityOfThunder.Keys,
        ];
        foreach (IEnumerable<DateTimeOffset> values in dates)
        {
            uniqueDates.UnionWith(values);
        }

        List<HourlyWrapper> hourly = [];
        foreach (DateTimeOffset date in uniqueDates)
        {
            NwsValueWeatherValue? weatherValue = weather.GetValueOrDefault(date);
            Speed? speed = KilometersPerHour(Value(windSpeed, date));
            int? cloudCover = Value(skyCover, date);
            hourly.Add(new HourlyWrapper
            {
                Date = date,
                WeatherText = GetWeatherText(weatherValue, speed, cloudCover),
                WeatherCode = GetWeatherCode(weatherValue, speed, cloudCover),
                Temperature = new TemperatureWrapper
                {
                    Temperature = Celsius(Value(temperature, date)),
                    FeelsLike = Celsius(Value(apparentTemperature, date)),
                },
                Precipitation = new Models.Precipitation
                {
                    Total = Millimeters(Value(quantitativePrecipitation, date)),
                    Snow = Millimeters(Value(snowfallAmount, date)),
                    Ice = Millimeters(Value(iceAccumulation, date)),
                },
                PrecipitationProbability = new PrecipitationProbability
                {
                    Total = Percent(Value(probabilityOfPrecipitation, date)),
                    Thunderstorm = Percent(Value(probabilityOfThunder, date)),
                },
                Wind = new Wind
                {
                    Degree = Value(windDirection, date),
                    Speed = speed,
                    Gusts = KilometersPerHour(Value(windGust, date)),
                },
                RelativeHumidity = Percent(Value(relativeHumidity, date)),
                DewPoint = Celsius(Value(dewpoint, date)),
                Pressure = Value(pressure, date) is { } pressureValue ? Pressure.FromInchesOfMercury(pressureValue) : null,
                CloudCover = Percent(cloudCover),
                Visibility = Meters(Value(visibility, date)),
            });
        }

        return hourly;
    }

    private static Dictionary<DateTimeOffset, double> GetDoubleForecast(NwsValueDoubleContainer? properties,
        bool multipleHoursAreDivided = false)
    {
        Dictionary<DateTimeOffset, double> forecast = [];
        foreach (NwsValueDouble item in properties?.Values ?? [])
        {
            if (item.Value is not { } value)
            {
                continue;
            }

            (DateTimeOffset date, int durationInHours) = ReadInterval(item.ValidTime);
            for (int i = 1; i <= durationInHours; i++)
            {
                forecast[date.AddHours(i - 1)] = multipleHoursAreDivided ? value / durationInHours : value;
            }
        }

        return forecast;
    }

    private static Dictionary<DateTimeOffset, int> GetIntForecast(NwsValueIntContainer? properties)
    {
        Dictionary<DateTimeOffset, int> forecast = [];
        foreach (NwsValueInt item in properties?.Values ?? [])
        {
            if (item.Value is not { } value)
            {
                continue;
            }

            (DateTimeOffset date, int durationInHours) = ReadInterval(item.ValidTime);
            for (int i = 1; i <= durationInHours; i++)
            {
                forecast[date.AddHours(i - 1)] = value;
            }
        }

        return forecast;
    }

    private static Dictionary<DateTimeOffset, NwsValueWeatherValue> GetWeatherForecast(NwsValueWeatherContainer? properties)
    {
        Dictionary<DateTimeOffset, NwsValueWeatherValue> forecast = [];
        foreach (NwsValueWeather item in properties?.Values ?? [])
        {
            if (item.Value?.FirstOrDefault() is not { } value || string.IsNullOrEmpty(value.Weather))
            {
                continue;
            }

            (DateTimeOffset date, int durationInHours) = ReadInterval(item.ValidTime);
            for (int i = 1; i <= durationInHours; i++)
            {
                forecast[date.AddHours(i - 1)] = value;
            }
        }

        return forecast;
    }

    private static (DateTimeOffset Date, int DurationInHours) ReadInterval(string validTime)
    {
        string[] interval = validTime.Split('/');
        DateTimeOffset date = SourceDateConverter.Parse(interval[0]);
        int durationInHours = NwsDuration.ParseWholeHours(interval[1]);
        if (durationInHours > 360)
        {
            throw new JsonException("The forecast interval is longer than 15 days.");
        }

        return (date, durationInHours);
    }

    private static T? Value<T>(Dictionary<DateTimeOffset, T> values, DateTimeOffset date) where T : struct
        => values.TryGetValue(date, out T value) ? value : null;

    private static Units.Precipitation? Millimeters(double? value)
        => value is { } number ? Units.Precipitation.FromMillimeters(number) : null;
}
