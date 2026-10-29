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
using Microsoft.Data.Sqlite;

namespace LibreccioWeather.Core.Database;

internal sealed class WeatherWriter
{
    private readonly SqliteCommand _command;

    internal WeatherWriter(SqliteCommand command)
    {
        _command = command;
    }

    internal void WriteWeather(Weather weather)
    {
        Base weatherBase = weather.Base;
        Add("refresh_time", weatherBase.RefreshTime?.ToUnixTimeMilliseconds());
        Add("main_update_time", weatherBase.ForecastUpdateTime?.ToUnixTimeMilliseconds());
        Add("current_update_time", weatherBase.CurrentUpdateTime?.ToUnixTimeMilliseconds());
        Add("air_quality_update_time", weatherBase.AirQualityUpdateTime?.ToUnixTimeMilliseconds());
        Add("pollen_update_time", weatherBase.PollenUpdateTime?.ToUnixTimeMilliseconds());
        Add("minutely_update_time", weatherBase.MinutelyUpdateTime?.ToUnixTimeMilliseconds());
        Add("alerts_update_time", weatherBase.AlertsUpdateTime?.ToUnixTimeMilliseconds());
        Add("normals_update_time", weatherBase.NormalsUpdateTime?.ToUnixTimeMilliseconds());
        Add("normals_update_latitude", weatherBase.NormalsUpdateLatitude);
        Add("normals_update_longitude", weatherBase.NormalsUpdateLongitude);

        Current? current = weather.Current;
        Add("weather_text", current?.WeatherText);
        Add("weather_code", current?.WeatherCode?.Id);
        WriteTemperature(current?.Temperature);
        WriteWind(current?.Wind);
        Add("uv_index", current?.UV?.Index);
        WriteAirQuality(current?.AirQuality);
        Add("relative_humidity", current?.RelativeHumidity?.Value);
        Add("dew_point", current?.DewPoint?.Value);
        Add("pressure", current?.Pressure?.Value);
        Add("cloud_cover", current?.CloudCover?.Value);
        Add("visibility", current?.Visibility?.Value);
        Add("ceiling", current?.Ceiling?.Value);
        Add("daily_forecast", current?.DailyForecast);
        Add("hourly_forecast", current?.HourlyForecast);
    }

    internal void WriteDaily(Daily daily)
    {
        Add("date", daily.Date.ToUnixTimeMilliseconds());
        WriteHalfDay(daily.Day, "daytime_");
        WriteHalfDay(daily.Night, "nighttime_");
        Add("degree_day_heating", daily.DegreeDay?.Heating?.Value);
        Add("degree_day_cooling", daily.DegreeDay?.Cooling?.Value);
        Add("sun_rise_date", daily.Sun?.RiseDate?.ToUnixTimeMilliseconds());
        Add("sun_set_date", daily.Sun?.SetDate?.ToUnixTimeMilliseconds());
        Add("twilight_rise_date", daily.Twilight?.RiseDate?.ToUnixTimeMilliseconds());
        Add("twilight_set_date", daily.Twilight?.SetDate?.ToUnixTimeMilliseconds());
        Add("moon_rise_date", daily.Moon?.RiseDate?.ToUnixTimeMilliseconds());
        Add("moon_set_date", daily.Moon?.SetDate?.ToUnixTimeMilliseconds());
        Add("moon_phase_angle", daily.MoonPhase?.Angle);
        WriteAirQuality(daily.AirQuality);

        Pollen? pollen = daily.Pollen;
        Add("alder", pollen?.Alder?.Value);
        Add("ash", pollen?.Ash?.Value);
        Add("birch", pollen?.Birch?.Value);
        Add("chestnut", pollen?.Chestnut?.Value);
        Add("cypress", pollen?.Cypress?.Value);
        Add("grass", pollen?.Grass?.Value);
        Add("hazel", pollen?.Hazel?.Value);
        Add("hornbeam", pollen?.Hornbeam?.Value);
        Add("linden", pollen?.Linden?.Value);
        Add("mold", pollen?.Mold?.Value);
        Add("mugwort", pollen?.Mugwort?.Value);
        Add("oak", pollen?.Oak?.Value);
        Add("olive", pollen?.Olive?.Value);
        Add("plane", pollen?.Plane?.Value);
        Add("plantain", pollen?.Plantain?.Value);
        Add("poplar", pollen?.Poplar?.Value);
        Add("ragweed", pollen?.Ragweed?.Value);
        Add("sorrel", pollen?.Sorrel?.Value);
        Add("tree", pollen?.Tree?.Value);
        Add("urticaceae", pollen?.Urticaceae?.Value);
        Add("willow", pollen?.Willow?.Value);

        Add("uv_index", daily.UV?.Index);
        Add("sunshine_duration", Nanoseconds(daily.SunshineDuration));
        Add("relative_humidity_average", daily.RelativeHumidity?.Average?.Value);
        Add("relative_humidity_min", daily.RelativeHumidity?.Min?.Value);
        Add("relative_humidity_max", daily.RelativeHumidity?.Max?.Value);
        Add("dewpoint_average", daily.DewPoint?.Average?.Value);
        Add("dewpoint_min", daily.DewPoint?.Min?.Value);
        Add("dewpoint_max", daily.DewPoint?.Max?.Value);
        Add("pressure_average", daily.Pressure?.Average?.Value);
        Add("pressure_min", daily.Pressure?.Min?.Value);
        Add("pressure_max", daily.Pressure?.Max?.Value);
        Add("cloud_cover_average", daily.CloudCover?.Average?.Value);
        Add("cloud_cover_min", daily.CloudCover?.Min?.Value);
        Add("cloud_cover_max", daily.CloudCover?.Max?.Value);
        Add("visibility_average", daily.Visibility?.Average?.Value);
        Add("visibility_min", daily.Visibility?.Min?.Value);
        Add("visibility_max", daily.Visibility?.Max?.Value);
    }

    internal void WriteHourly(Hourly hourly)
    {
        Add("date", hourly.Date.ToUnixTimeMilliseconds());
        Add("daylight", hourly.IsDaylight ? 1L : 0L);
        Add("weather_text", hourly.WeatherText);
        Add("weather_code", hourly.WeatherCode?.Id);
        WriteTemperature(hourly.Temperature);
        WritePrecipitation(hourly.Precipitation);
        WriteProbability(hourly.PrecipitationProbability);
        WriteWind(hourly.Wind);
        WriteAirQuality(hourly.AirQuality);
        Add("uvIndex", hourly.UV?.Index);
        Add("relative_humidity", hourly.RelativeHumidity?.Value);
        Add("dew_point", hourly.DewPoint?.Value);
        Add("pressure", hourly.Pressure?.Value);
        Add("cloud_cover", hourly.CloudCover?.Value);
        Add("visibility", hourly.Visibility?.Value);
    }

    internal void WriteMinutely(Minutely minutely)
    {
        Add("date", minutely.Date.ToUnixTimeMilliseconds());
        Add("minute_interval", minutely.MinuteInterval);
        Add("precipitation_intensity", minutely.PrecipitationIntensity?.Value);
    }

    internal void WriteAlert(Alert alert)
    {
        Add("alert_id", alert.AlertId);
        Add("start_date", alert.StartDate?.ToUnixTimeMilliseconds());
        Add("end_date", alert.EndDate?.ToUnixTimeMilliseconds());
        Add("headline", alert.Headline);
        Add("description", alert.Description);
        Add("instruction", alert.Instruction);
        Add("source", alert.Source);
        Add("severity", (int)alert.Severity);
        Add("color", alert.Color);
    }

    internal void WriteNormals(KeyValuePair<Month, Normals> normals)
    {
        Add("month", (int)normals.Key);
        Add("temperature_max_average", normals.Value.DaytimeTemperature?.Value);
        Add("temperature_min_average", normals.Value.NighttimeTemperature?.Value);
    }

    private void WriteHalfDay(HalfDay? halfDay, string prefix)
    {
        Add(prefix + "weather_text", halfDay?.WeatherText);
        Add(prefix + "weather_summary", halfDay?.WeatherSummary);
        Add(prefix + "weather_code", halfDay?.WeatherCode?.Id);
        WriteTemperature(halfDay?.Temperature, prefix);
        WritePrecipitation(halfDay?.Precipitation, prefix);
        WriteProbability(halfDay?.PrecipitationProbability, prefix);
        Add(prefix + "total_precipitation_duration", Nanoseconds(halfDay?.PrecipitationDuration?.Total));
        Add(prefix + "thunderstorm_precipitation_duration", Nanoseconds(halfDay?.PrecipitationDuration?.Thunderstorm));
        Add(prefix + "rain_precipitation_duration", Nanoseconds(halfDay?.PrecipitationDuration?.Rain));
        Add(prefix + "snow_precipitation_duration", Nanoseconds(halfDay?.PrecipitationDuration?.Snow));
        Add(prefix + "ice_precipitation_duration", Nanoseconds(halfDay?.PrecipitationDuration?.Ice));
        WriteWind(halfDay?.Wind, prefix);
    }

    private void WriteTemperature(Temperature? temperature, string prefix = "")
    {
        Add(prefix + "temperature", temperature?.Value?.Value);
        Add(prefix + "temperature_source_feels_like", temperature?.SourceFeelsLike?.Value);
        Add(prefix + "temperature_apparent", temperature?.ComputedApparent?.Value);
        Add(prefix + "temperature_wind_chill", temperature?.ComputedWindChill?.Value);
        Add(prefix + "humidex", temperature?.ComputedHumidex?.Value);
    }

    private void WritePrecipitation(Precipitation? precipitation, string prefix = "")
    {
        Add(prefix + "total_precipitation", precipitation?.Total?.Value);
        Add(prefix + "thunderstorm_precipitation", precipitation?.Thunderstorm?.Value);
        Add(prefix + "rain_precipitation", precipitation?.Rain?.Value);
        Add(prefix + "snow_precipitation", precipitation?.Snow?.Value);
        Add(prefix + "ice_precipitation", precipitation?.Ice?.Value);
    }

    private void WriteProbability(PrecipitationProbability? probability, string prefix = "")
    {
        Add(prefix + "total_precipitation_probability", probability?.Total?.Value);
        Add(prefix + "thunderstorm_precipitation_probability", probability?.Thunderstorm?.Value);
        Add(prefix + "rain_precipitation_probability", probability?.Rain?.Value);
        Add(prefix + "snow_precipitation_probability", probability?.Snow?.Value);
        Add(prefix + "ice_precipitation_probability", probability?.Ice?.Value);
    }

    private void WriteWind(Wind? wind, string prefix = "")
    {
        Add(prefix + "wind_degree", wind?.Degree);
        Add(prefix + "wind_speed", wind?.Speed?.Value);
        Add(prefix + "wind_gusts", wind?.Gusts?.Value);
    }

    private void WriteAirQuality(AirQuality? airQuality)
    {
        Add("pm25", airQuality?.PM25?.Value);
        Add("pm10", airQuality?.PM10?.Value);
        Add("so2", airQuality?.SO2?.Value);
        Add("no2", airQuality?.NO2?.Value);
        Add("o3", airQuality?.O3?.Value);
        Add("co", airQuality?.CO?.Value);
    }

    private void Add(string column, object? value)
    {
        _command.Parameters.AddWithValue("$" + column, value ?? DBNull.Value);
    }

    // Checked arithmetic prevents a duration from silently wrapping in the stored nanosecond value.
    private static long? Nanoseconds(TimeSpan? duration)
    {
        return duration is { } value ? checked(value.Ticks * 100) : null;
    }
}
