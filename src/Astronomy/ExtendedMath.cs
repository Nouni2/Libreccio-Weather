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

namespace LibreccioWeather.Core.Astronomy;

internal static class ExtendedMath
{
    public const double PI2 = Math.PI * 2.0;

    // Arc seconds per radian.
    public static readonly double Arcs = ToDegrees(3600.0);

    public const double EarthMeanRadius = 6371.0;

    private const double RadiansToDegrees = 57.29577951308232;
    private const double DegreesToRadians = 0.017453292519943295;

    // Multiplies by the constant rather than dividing by π, which rounds differently.
    public static double ToDegrees(double radians) => radians * RadiansToDegrees;

    public static double ToRadians(double degrees) => degrees * DegreesToRadians;

    // Keeps the sign of the value, as the remainder of a division does.
    public static double Frac(double a) => a % 1.0;

    // Zero and NaN are returned as they are, where Math.Sign would throw on NaN.
    public static double Signum(double d) => d > 0.0 ? 1.0 : d < 0.0 ? -1.0 : d;

    public static bool IsZero(double d) => !double.IsNaN(d) && Signum(d) == 0.0;

    public static Vector EquatorialToHorizontal(double tau, double dec, double dist, double lat)
    {
        return Matrix.RotateY(Math.PI / 2.0 - lat).Multiply(Vector.OfPolar(tau, dec, dist));
    }

    public static Matrix EquatorialToEcliptical(JulianDate t)
    {
        double jc = t.JulianCentury;
        double eps = ToRadians(23.43929111 - (46.8150 + (0.00059 - 0.001813 * jc) * jc) * jc / 3600.0);
        return Matrix.RotateX(eps);
    }

    /// <param name="height">Height of the observer above sea level, in meters.</param>
    /// <param name="distance">Distance of the body, in kilometers.</param>
    public static double Parallax(double height, double distance)
    {
        return Math.Asin(EarthMeanRadius / distance)
            - Math.Acos(EarthMeanRadius / (EarthMeanRadius + (height / 1000.0)));
    }

    /// <summary>
    /// Atmospheric refraction of an apparent altitude in degrees, in radians. Below the horizon it is zero.
    /// </summary>
    public static double ApparentRefraction(double ha)
    {
        if (ha < 0.0)
        {
            return 0.0;
        }

        return Math.PI / (Math.Tan(ToRadians(ha + (7.31 / (ha + 4.4)))) * 10800.0);
    }

    /// <summary>
    /// Atmospheric refraction of a true altitude in radians, in radians. Below the horizon it is zero.
    /// </summary>
    public static double Refraction(double h)
    {
        if (h < 0.0)
        {
            return 0.0;
        }

        return 0.000296706 / Math.Tan(h + 0.00312537 / (h + 0.0890118));
    }
}
