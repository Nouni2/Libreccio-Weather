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

/*
 * Copyright 2008, 2013 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace LibreccioWeather.Core.Sources.Geography;

internal readonly record struct SourceCoordinate
{
    public SourceCoordinate(double latitude, double longitude)
    {
        Latitude = Math.Max(-90.0, Math.Min(90.0, latitude));
        Longitude = Math.Max(-180.0, Math.Min(180.0, longitude));
    }

    public double Latitude { get; }
    public double Longitude { get; }
}

internal static class SourceGeometry
{
    private const double EarthRadius = 6371009.0;

    public static double ComputeDistanceBetween(SourceCoordinate from, SourceCoordinate to)
    {
        double lat1 = Radians(from.Latitude);
        double lat2 = Radians(to.Latitude);
        double distance = Hav(lat1 - lat2) + Hav(Radians(from.Longitude) - Radians(to.Longitude)) * Math.Cos(lat1) * Math.Cos(lat2);
        return 2 * Math.Asin(Math.Sqrt(distance)) * EarthRadius;
    }

    public static bool ContainsLocation(double latitude, double longitude, IReadOnlyList<SourceCoordinate> polygon, bool geodesic)
    {
        if (polygon.Count == 0)
        {
            return false;
        }

        double lat3 = Radians(latitude);
        double lng3 = Radians(longitude);
        SourceCoordinate previous = polygon[^1];
        double lat1 = Radians(previous.Latitude);
        double lng1 = Radians(previous.Longitude);
        int intersections = 0;
        foreach (SourceCoordinate point in polygon)
        {
            double dLng3 = Wrap(lng3 - lng1, -Math.PI, Math.PI);
            if (lat3 == lat1 && dLng3 == 0.0)
            {
                return true;
            }

            double lat2 = Radians(point.Latitude);
            double lng2 = Radians(point.Longitude);
            if (Intersects(lat1, lat2, Wrap(lng2 - lng1, -Math.PI, Math.PI), lat3, dLng3, geodesic))
            {
                intersections++;
            }

            lat1 = lat2;
            lng1 = lng2;
        }

        return (intersections & 1) != 0;
    }

    private static bool Intersects(double lat1, double lat2, double lng2, double lat3, double lng3, bool geodesic)
    {
        if ((lng3 >= 0 && lng3 >= lng2) || (lng3 < 0 && lng3 < lng2))
        {
            return false;
        }

        if (lat3 <= -Math.PI / 2)
        {
            return false;
        }

        if (lat1 <= -Math.PI / 2 || lat2 <= -Math.PI / 2 || lat1 >= Math.PI / 2 || lat2 >= Math.PI / 2)
        {
            return false;
        }

        if (lng2 <= -Math.PI)
        {
            return false;
        }

        double linearLat = (lat1 * (lng2 - lng3) + lat2 * lng3) / lng2;
        if (lat1 >= 0 && lat2 >= 0 && lat3 < linearLat)
        {
            return false;
        }

        if (lat1 <= 0 && lat2 <= 0 && lat3 >= linearLat)
        {
            return true;
        }

        if (lat3 >= Math.PI / 2)
        {
            return true;
        }

        return geodesic
            ? Math.Tan(lat3) >= (Math.Tan(lat1) * Math.Sin(lng2 - lng3) + Math.Tan(lat2) * Math.Sin(lng3)) / Math.Sin(lng2)
            : Mercator(lat3) >= (Mercator(lat1) * (lng2 - lng3) + Mercator(lat2) * lng3) / lng2;
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180.0;
    private static double Hav(double angle) => Math.Sin(angle * 0.5) * Math.Sin(angle * 0.5);
    private static double Mercator(double latitude) => Math.Log(Math.Tan(latitude * 0.5 + Math.PI / 4));
    private static double Wrap(double value, double minimum, double maximum)
    {
        double width = maximum - minimum;
        return value >= minimum && value < maximum ? value : (((value - minimum) % width + width) % width) + minimum;
    }
}
