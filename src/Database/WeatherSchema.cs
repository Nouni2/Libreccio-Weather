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

namespace LibreccioWeather.Core.Database;

internal static class WeatherSchema
{
    internal const string Create = """
        CREATE TABLE weathers(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,
            refresh_time INTEGER,
            main_update_time INTEGER,
            current_update_time INTEGER,
            air_quality_update_time INTEGER,
            pollen_update_time INTEGER,
            minutely_update_time INTEGER,
            alerts_update_time INTEGER,
            normals_update_time INTEGER,
            normals_update_latitude REAL NOT NULL DEFAULT 0.0,
            normals_update_longitude REAL NOT NULL DEFAULT 0.0,
            weather_text TEXT,
            weather_code TEXT,

            temperature INTEGER,
            temperature_source_feels_like INTEGER,
            temperature_apparent INTEGER,
            temperature_wind_chill INTEGER,
            humidex INTEGER,

            wind_degree REAL,
            wind_speed INTEGER,
            wind_gusts INTEGER,

            uv_index REAL,

            pm25 INTEGER,
            pm10 INTEGER,
            so2 INTEGER,
            no2 INTEGER,
            o3 INTEGER,
            co INTEGER,

            relative_humidity INTEGER,
            dew_point INTEGER,
            pressure INTEGER,
            visibility INTEGER,
            cloud_cover INTEGER,
            ceiling INTEGER,
            daily_forecast TEXT,
            hourly_forecast TEXT,

            UNIQUE(location_formatted_id) ON CONFLICT REPLACE,
            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX weathers_location_formatted_id_index ON weathers(location_formatted_id);

        CREATE TABLE dailys(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,

            date INTEGER NOT NULL,
            daytime_weather_text TEXT,
            daytime_weather_summary TEXT,
            daytime_weather_code TEXT,

            daytime_temperature INTEGER,
            daytime_temperature_source_feels_like INTEGER,
            daytime_temperature_apparent INTEGER,
            daytime_temperature_wind_chill INTEGER,
            daytime_humidex INTEGER,

            daytime_total_precipitation INTEGER,
            daytime_thunderstorm_precipitation INTEGER,
            daytime_rain_precipitation INTEGER,
            daytime_snow_precipitation INTEGER,
            daytime_ice_precipitation INTEGER,

            daytime_total_precipitation_probability INTEGER,
            daytime_thunderstorm_precipitation_probability INTEGER,
            daytime_rain_precipitation_probability INTEGER,
            daytime_snow_precipitation_probability INTEGER,
            daytime_ice_precipitation_probability INTEGER,

            daytime_total_precipitation_duration INTEGER,
            daytime_thunderstorm_precipitation_duration INTEGER,
            daytime_rain_precipitation_duration INTEGER,
            daytime_snow_precipitation_duration INTEGER,
            daytime_ice_precipitation_duration INTEGER,

            daytime_wind_degree REAL,
            daytime_wind_speed INTEGER,
            daytime_wind_gusts INTEGER,
            nighttime_weather_text TEXT,
            nighttime_weather_summary TEXT,
            nighttime_weather_code TEXT,

            nighttime_temperature INTEGER,
            nighttime_temperature_source_feels_like INTEGER,
            nighttime_temperature_apparent INTEGER,
            nighttime_temperature_wind_chill INTEGER,
            nighttime_humidex INTEGER,

            nighttime_total_precipitation INTEGER,
            nighttime_thunderstorm_precipitation INTEGER,
            nighttime_rain_precipitation INTEGER,
            nighttime_snow_precipitation INTEGER,
            nighttime_ice_precipitation INTEGER,

            nighttime_total_precipitation_probability INTEGER,
            nighttime_thunderstorm_precipitation_probability INTEGER,
            nighttime_rain_precipitation_probability INTEGER,
            nighttime_snow_precipitation_probability INTEGER,
            nighttime_ice_precipitation_probability INTEGER,

            nighttime_total_precipitation_duration INTEGER,
            nighttime_thunderstorm_precipitation_duration INTEGER,
            nighttime_rain_precipitation_duration INTEGER,
            nighttime_snow_precipitation_duration INTEGER,
            nighttime_ice_precipitation_duration INTEGER,

            nighttime_wind_degree REAL,
            nighttime_wind_speed INTEGER,
            nighttime_wind_gusts INTEGER,

            degree_day_heating INTEGER,
            degree_day_cooling INTEGER,
            sun_rise_date INTEGER,
            sun_set_date INTEGER,
            twilight_rise_date INTEGER,
            twilight_set_date INTEGER,
            moon_rise_date INTEGER,
            moon_set_date INTEGER,
            moon_phase_angle INTEGER,
            pm25 INTEGER,
            pm10 INTEGER,
            so2 INTEGER,
            no2 INTEGER,
            o3 INTEGER,
            co INTEGER,
            alder INTEGER,
            ash INTEGER,
            birch INTEGER,
            chestnut INTEGER,
            cypress INTEGER,
            grass INTEGER,
            hazel INTEGER,
            hornbeam INTEGER,
            linden INTEGER,
            mold INTEGER,
            mugwort INTEGER,
            oak INTEGER,
            olive INTEGER,
            plane INTEGER,
            plantain INTEGER,
            poplar INTEGER,
            ragweed INTEGER,
            sorrel INTEGER,
            tree INTEGER,
            urticaceae INTEGER,
            willow INTEGER,
            uv_index REAL,

            sunshine_duration INTEGER,
            relative_humidity_average INTEGER,
            relative_humidity_min INTEGER,
            relative_humidity_max INTEGER,
            dewpoint_average INTEGER,
            dewpoint_min INTEGER,
            dewpoint_max INTEGER,
            pressure_average INTEGER,
            pressure_min INTEGER,
            pressure_max INTEGER,
            cloud_cover_average INTEGER,
            cloud_cover_min INTEGER,
            cloud_cover_max INTEGER,
            visibility_average INTEGER,
            visibility_min INTEGER,
            visibility_max INTEGER,

            UNIQUE(location_formatted_id, date) ON CONFLICT REPLACE,
            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX dailys_location_formatted_id_index ON dailys(location_formatted_id);

        CREATE TABLE hourlys(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,

            date INTEGER NOT NULL,

            daylight INTEGER NOT NULL DEFAULT 1,
            weather_text TEXT,
            weather_code TEXT,

            temperature INTEGER,
            temperature_source_feels_like INTEGER,
            temperature_apparent INTEGER,
            temperature_wind_chill INTEGER,
            humidex INTEGER,

            total_precipitation INTEGER,
            thunderstorm_precipitation INTEGER,
            rain_precipitation INTEGER,
            snow_precipitation INTEGER,
            ice_precipitation INTEGER,

            total_precipitation_probability INTEGER,
            thunderstorm_precipitation_probability INTEGER,
            rain_precipitation_probability INTEGER,
            snow_precipitation_probability INTEGER,
            ice_precipitation_probability INTEGER,

            wind_degree REAL,
            wind_speed INTEGER,
            wind_gusts INTEGER,

            pm25 INTEGER,
            pm10 INTEGER,
            so2 INTEGER,
            no2 INTEGER,
            o3 INTEGER,
            co INTEGER,
            uvIndex REAL,
            relative_humidity INTEGER,
            dew_point INTEGER,
            pressure INTEGER,
            cloud_cover INTEGER,
            visibility INTEGER,

            UNIQUE(location_formatted_id, date) ON CONFLICT REPLACE,
            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX hourlys_location_formatted_id_index ON hourlys(location_formatted_id);

        CREATE TABLE minutelys(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,

            date INTEGER NOT NULL,
            minute_interval INTEGER NOT NULL,
            precipitation_intensity INTEGER,

            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX minutelys_location_formatted_id_index ON minutelys(location_formatted_id);

        CREATE TABLE alerts(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,
            alert_id TEXT NOT NULL,
            start_date INTEGER,
            end_date INTEGER,
            headline TEXT,
            description TEXT,
            instruction TEXT,
            source TEXT,
            severity INTEGER NOT NULL,
            color INTEGER NOT NULL,

            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX alerts_location_formatted_id_index ON alerts(location_formatted_id);

        CREATE TABLE normals(
            _id INTEGER NOT NULL PRIMARY KEY,
            location_formatted_id TEXT NOT NULL,

            month INTEGER NOT NULL,
            temperature_max_average INTEGER,
            temperature_min_average INTEGER,

            UNIQUE(location_formatted_id, month) ON CONFLICT REPLACE,
            FOREIGN KEY(location_formatted_id) REFERENCES locations (formatted_id)
            ON DELETE CASCADE
        );

        CREATE INDEX normals_location_formatted_id_index ON normals(location_formatted_id);
        """;
}
