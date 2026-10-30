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

using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Units;
using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Database;

internal sealed class WeatherReader
{
    private readonly SqliteDataReader _reader;

    internal WeatherReader(SqliteDataReader reader)
    {
        _reader = reader;
    }

    internal Weather ReadWeather()
    {
        return new Weather
        {
            Base = new Base
            {
                RefreshTime = Date("refresh_time"),
                ForecastUpdateTime = Date("main_update_time"),
                CurrentUpdateTime = Date("current_update_time"),
                AirQualityUpdateTime = Date("air_quality_update_time"),
                PollenUpdateTime = Date("pollen_update_time"),
                MinutelyUpdateTime = Date("minutely_update_time"),
                AlertsUpdateTime = Date("alerts_update_time"),
                NormalsUpdateTime = Date("normals_update_time"),
                NormalsUpdateLatitude = _reader.GetDouble(_reader.GetOrdinal("normals_update_latitude")),
                NormalsUpdateLongitude = _reader.GetDouble(_reader.GetOrdinal("normals_update_longitude")),
            },
            Current = new Current
            {
                WeatherText = Text("weather_text"),
                WeatherCode = Code("weather_code"),
                Temperature = ReadTemperature(),
                Wind = ReadWind(),
                UV = new UV { Index = Real("uv_index") },
                AirQuality = ReadAirQuality(),
                RelativeHumidity = RatioValue("relative_humidity"),
                DewPoint = TemperatureValue("dew_point"),
                Pressure = PressureValue("pressure"),
                CloudCover = RatioValue("cloud_cover"),
                Visibility = DistanceValue("visibility"),
                Ceiling = DistanceValue("ceiling"),
                DailyForecast = Text("daily_forecast"),
                HourlyForecast = Text("hourly_forecast"),
            },
        };
    }

    internal Daily ReadDaily()
    {
        return new Daily
        {
            Date = DateTimeOffset.FromUnixTimeMilliseconds(_reader.GetInt64(_reader.GetOrdinal("date"))),
            Day = ReadHalfDay("daytime_"),
            Night = ReadHalfDay("nighttime_"),
            DegreeDay = new DegreeDay
            {
                Heating = TemperatureValue("degree_day_heating"),
                Cooling = TemperatureValue("degree_day_cooling"),
            },
            Sun = ReadAstro("sun_"),
            Twilight = ReadAstro("twilight_"),
            Moon = ReadAstro("moon_"),
            MoonPhase = new MoonPhase { Angle = Integer("moon_phase_angle") is { } angle ? (int)angle : null },
            AirQuality = ReadAirQuality(),
            Pollen = new Pollen
            {
                Alder = PollenValue("alder"),
                Ash = PollenValue("ash"),
                Birch = PollenValue("birch"),
                Chestnut = PollenValue("chestnut"),
                Cypress = PollenValue("cypress"),
                Grass = PollenValue("grass"),
                Hazel = PollenValue("hazel"),
                Hornbeam = PollenValue("hornbeam"),
                Linden = PollenValue("linden"),
                Mold = PollenValue("mold"),
                Mugwort = PollenValue("mugwort"),
                Oak = PollenValue("oak"),
                Olive = PollenValue("olive"),
                Plane = PollenValue("plane"),
                Plantain = PollenValue("plantain"),
                Poplar = PollenValue("poplar"),
                Ragweed = PollenValue("ragweed"),
                Sorrel = PollenValue("sorrel"),
                Tree = PollenValue("tree"),
                Urticaceae = PollenValue("urticaceae"),
                Willow = PollenValue("willow"),
            },
            UV = new UV { Index = Real("uv_index") },
            SunshineDuration = Duration("sunshine_duration"),
            RelativeHumidity = new DailyRelativeHumidity
            {
                Average = RatioValue("relative_humidity_average"),
                Min = RatioValue("relative_humidity_min"),
                Max = RatioValue("relative_humidity_max"),
            },
            DewPoint = new DailyDewPoint
            {
                Average = TemperatureValue("dewpoint_average"),
                Min = TemperatureValue("dewpoint_min"),
                Max = TemperatureValue("dewpoint_max"),
            },
            Pressure = new DailyPressure
            {
                Average = PressureValue("pressure_average"),
                Min = PressureValue("pressure_min"),
                Max = PressureValue("pressure_max"),
            },
            CloudCover = new DailyCloudCover
            {
                Average = RatioValue("cloud_cover_average"),
                Min = RatioValue("cloud_cover_min"),
                Max = RatioValue("cloud_cover_max"),
            },
            Visibility = new DailyVisibility
            {
                Average = DistanceValue("visibility_average"),
                Min = DistanceValue("visibility_min"),
                Max = DistanceValue("visibility_max"),
            },
        };
    }

    internal Hourly ReadHourly()
    {
        return new Hourly
        {
            Date = DateTimeOffset.FromUnixTimeMilliseconds(_reader.GetInt64(_reader.GetOrdinal("date"))),
            IsDaylight = _reader.GetInt64(_reader.GetOrdinal("daylight")) != 0,
            WeatherText = Text("weather_text"),
            WeatherCode = Code("weather_code"),
            Temperature = ReadTemperature(),
            Precipitation = ReadPrecipitation(),
            PrecipitationProbability = ReadProbability(),
            Wind = ReadWind(),
            AirQuality = ReadAirQuality(),
            UV = new UV { Index = Real("uvIndex") },
            RelativeHumidity = RatioValue("relative_humidity"),
            DewPoint = TemperatureValue("dew_point"),
            Pressure = PressureValue("pressure"),
            CloudCover = RatioValue("cloud_cover"),
            Visibility = DistanceValue("visibility"),
        };
    }

    internal Minutely ReadMinutely()
    {
        return new Minutely
        {
            Date = DateTimeOffset.FromUnixTimeMilliseconds(_reader.GetInt64(_reader.GetOrdinal("date"))),
            MinuteInterval = _reader.GetInt32(_reader.GetOrdinal("minute_interval")),
            PrecipitationIntensity = PrecipitationValue("precipitation_intensity"),
        };
    }

    internal Alert ReadAlert()
    {
        return new Alert
        {
            AlertId = _reader.GetString(_reader.GetOrdinal("alert_id")),
            StartDate = Date("start_date"),
            EndDate = Date("end_date"),
            Headline = Text("headline"),
            Description = Text("description"),
            Instruction = Text("instruction"),
            Source = Text("source"),
            Severity = AlertSeverity.GetInstance(_reader.GetInt32(_reader.GetOrdinal("severity"))),
            Color = _reader.GetInt32(_reader.GetOrdinal("color")),
        };
    }

    internal KeyValuePair<Month, Normals> ReadNormals()
    {
        return new KeyValuePair<Month, Normals>(
            Month.Of(_reader.GetInt32(_reader.GetOrdinal("month"))),
            new Normals
            {
                DaytimeTemperature = TemperatureValue("temperature_max_average"),
                NighttimeTemperature = TemperatureValue("temperature_min_average"),
            });
    }

    private HalfDay ReadHalfDay(string prefix)
    {
        return new HalfDay
        {
            WeatherText = Text(prefix + "weather_text"),
            WeatherSummary = Text(prefix + "weather_summary"),
            WeatherCode = Code(prefix + "weather_code"),
            Temperature = ReadTemperature(prefix),
            Precipitation = ReadPrecipitation(prefix),
            PrecipitationProbability = ReadProbability(prefix),
            PrecipitationDuration = new PrecipitationDuration
            {
                Total = Duration(prefix + "total_precipitation_duration"),
                Thunderstorm = Duration(prefix + "thunderstorm_precipitation_duration"),
                Rain = Duration(prefix + "rain_precipitation_duration"),
                Snow = Duration(prefix + "snow_precipitation_duration"),
                Ice = Duration(prefix + "ice_precipitation_duration"),
            },
            Wind = ReadWind(prefix),
        };
    }

    private Models.Temperature ReadTemperature(string prefix = "")
    {
        return new Models.Temperature
        {
            Value = TemperatureValue(prefix + "temperature"),
            SourceFeelsLike = TemperatureValue(prefix + "temperature_source_feels_like"),
            ComputedApparent = TemperatureValue(prefix + "temperature_apparent"),
            ComputedWindChill = TemperatureValue(prefix + "temperature_wind_chill"),
            ComputedHumidex = TemperatureValue(prefix + "humidex"),
        };
    }

    private Models.Precipitation ReadPrecipitation(string prefix = "")
    {
        return new Models.Precipitation
        {
            Total = PrecipitationValue(prefix + "total_precipitation"),
            Thunderstorm = PrecipitationValue(prefix + "thunderstorm_precipitation"),
            Rain = PrecipitationValue(prefix + "rain_precipitation"),
            Snow = PrecipitationValue(prefix + "snow_precipitation"),
            Ice = PrecipitationValue(prefix + "ice_precipitation"),
        };
    }

    private PrecipitationProbability ReadProbability(string prefix = "")
    {
        return new PrecipitationProbability
        {
            Total = RatioValue(prefix + "total_precipitation_probability"),
            Thunderstorm = RatioValue(prefix + "thunderstorm_precipitation_probability"),
            Rain = RatioValue(prefix + "rain_precipitation_probability"),
            Snow = RatioValue(prefix + "snow_precipitation_probability"),
            Ice = RatioValue(prefix + "ice_precipitation_probability"),
        };
    }

    private Wind ReadWind(string prefix = "")
    {
        return new Wind
        {
            Degree = Real(prefix + "wind_degree"),
            Speed = SpeedValue(prefix + "wind_speed"),
            Gusts = SpeedValue(prefix + "wind_gusts"),
        };
    }

    private AirQuality ReadAirQuality()
    {
        return new AirQuality
        {
            PM25 = PollutantValue("pm25"),
            PM10 = PollutantValue("pm10"),
            SO2 = PollutantValue("so2"),
            NO2 = PollutantValue("no2"),
            O3 = PollutantValue("o3"),
            CO = PollutantValue("co"),
        };
    }

    private Astro? ReadAstro(string prefix)
    {
        DateTimeOffset? rise = Date(prefix + "rise_date");
        DateTimeOffset? set = Date(prefix + "set_date");

        // Neither time is stored for a polar night, which therefore reads back as a day without times, as a day whose
        // calculation failed does.
        return rise is null && set is null ? null : new Astro { RiseDate = rise, SetDate = set };
    }

    private string? Text(string column)
    {
        int ordinal = _reader.GetOrdinal(column);
        return _reader.IsDBNull(ordinal) ? null : _reader.GetString(ordinal);
    }

    private long? Integer(string column)
    {
        int ordinal = _reader.GetOrdinal(column);
        return _reader.IsDBNull(ordinal) ? null : _reader.GetInt64(ordinal);
    }

    private double? Real(string column)
    {
        int ordinal = _reader.GetOrdinal(column);
        return _reader.IsDBNull(ordinal) ? null : _reader.GetDouble(ordinal);
    }

    private WeatherCode? Code(string column) =>
        Text(column) is { } value ? WeatherCode.GetInstance(value) ?? WeatherCode.Clear : null;

    private DateTimeOffset? Date(string column) =>
        Integer(column) is { } value ? DateTimeOffset.FromUnixTimeMilliseconds(value) : null;

    // Durations are stored in nanoseconds; one TimeSpan tick is 100 nanoseconds.
    private TimeSpan? Duration(string column) =>
        Integer(column) is { } value ? TimeSpan.FromTicks(value / 100) : null;

    private Units.Temperature? TemperatureValue(string column) =>
        Integer(column) is { } value ? Units.Temperature.FromDeciCelsius(value) : null;

    private Units.Precipitation? PrecipitationValue(string column) =>
        Integer(column) is { } value ? Units.Precipitation.FromMicrometers(value) : null;

    private Ratio? RatioValue(string column) =>
        Integer(column) is { } value ? Ratio.FromPermille(value) : null;

    private Speed? SpeedValue(string column) =>
        Integer(column) is { } value ? Speed.FromCentimetersPerSecond(value) : null;

    private Pressure? PressureValue(string column) =>
        Integer(column) is { } value ? Pressure.FromPascals(value) : null;

    private Distance? DistanceValue(string column) =>
        Integer(column) is { } value ? Distance.FromMeters(value) : null;

    private PollutantConcentration? PollutantValue(string column) =>
        Integer(column) is { } value ? PollutantConcentration.FromMicrogramsPerCubicMeter(value) : null;

    private PollenConcentration? PollenValue(string column) =>
        Integer(column) is { } value ? PollenConcentration.FromPerCubicMeter(value) : null;
}
