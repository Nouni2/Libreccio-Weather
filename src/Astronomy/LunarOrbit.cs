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

/// <summary>
/// Low-precision positions of the Moon, good to a few arc minutes. Distances are in kilometers.
/// </summary>
internal static class LunarOrbit
{
    private const double MoonMeanRadius = 1737.1;

    public static Vector PositionEquatorial(JulianDate date)
    {
        double t = date.JulianCentury;
        double l0 = ExtendedMath.Frac(0.606433 + 1336.855225 * t);
        double l = ExtendedMath.PI2 * ExtendedMath.Frac(0.374897 + 1325.552410 * t);
        double ls = ExtendedMath.PI2 * ExtendedMath.Frac(0.993133 + 99.997361 * t);
        double d = ExtendedMath.PI2 * ExtendedMath.Frac(0.827361 + 1236.853086 * t);
        double f = ExtendedMath.PI2 * ExtendedMath.Frac(0.259086 + 1342.227825 * t);
        double d2 = 2.0 * d;
        double l2 = 2.0 * l;
        double f2 = 2.0 * f;

        double dL = 22640.0 * Math.Sin(l)
            - 4586.0 * Math.Sin(l - d2)
            + 2370.0 * Math.Sin(d2)
            + 769.0 * Math.Sin(l2)
            - 668.0 * Math.Sin(ls)
            - 412.0 * Math.Sin(f2)
            - 212.0 * Math.Sin(l2 - d2)
            - 206.0 * Math.Sin(l + ls - d2)
            + 192.0 * Math.Sin(l + d2)
            - 165.0 * Math.Sin(ls - d2)
            - 125.0 * Math.Sin(d)
            - 110.0 * Math.Sin(l + ls)
            + 148.0 * Math.Sin(l - ls)
            - 55.0 * Math.Sin(f2 - d2);

        double s = f + (dL + 412.0 * Math.Sin(f2) + 541.0 * Math.Sin(ls)) / ExtendedMath.Arcs;
        double h = f - d2;
        double n = -526.0 * Math.Sin(h)
            + 44.0 * Math.Sin(l + h)
            - 31.0 * Math.Sin(-l + h)
            - 23.0 * Math.Sin(ls + h)
            + 11.0 * Math.Sin(-ls + h)
            - 25.0 * Math.Sin(-l2 + f)
            + 21.0 * Math.Sin(-l + f);

        double lMoon = ExtendedMath.PI2 * ExtendedMath.Frac(l0 + dL / 1296.0e3);
        double bMoon = (18520.0 * Math.Sin(s) + n) / ExtendedMath.Arcs;

        double dt = 385000.5584
            - 20905.3550 * Math.Cos(l)
            - 3699.1109 * Math.Cos(d2 - l)
            - 2955.9676 * Math.Cos(d2)
            - 569.9251 * Math.Cos(l2);

        return Vector.OfPolar(lMoon, bMoon, dt);
    }

    public static Vector Position(JulianDate date)
    {
        Matrix rotateMatrix = ExtendedMath.EquatorialToEcliptical(date).Transpose();
        return rotateMatrix.Multiply(PositionEquatorial(date));
    }

    public static Vector PositionHorizontal(JulianDate date, double lat, double lng)
    {
        Vector mc = Position(date);
        double h = date.GreenwichMeanSiderealTime + lng - mc.Phi;
        return ExtendedMath.EquatorialToHorizontal(h, mc.Theta, mc.R, lat);
    }

    public static double AngularRadius(double distance) => Math.Asin(MoonMeanRadius / distance);
}

/// <summary>
/// A low-precision position of the Sun, used for the Moon's phase. Distances are in kilometers.
/// </summary>
internal static class SolarOrbit
{
    private const double SunDistance = 149598000.0;

    public static Vector PositionEquatorial(JulianDate date)
    {
        double t = date.JulianCentury;
        double m = ExtendedMath.PI2 * ExtendedMath.Frac(0.993133 + 99.997361 * t);
        double l = ExtendedMath.PI2 * ExtendedMath.Frac(0.7859453 + m / ExtendedMath.PI2
            + (6893.0 * Math.Sin(m) + 72.0 * Math.Sin(2.0 * m) + 6191.2 * t) / 1296.0e3);
        double d = SunDistance * (1 - 0.016718 * Math.Cos(date.TrueAnomaly));
        return Vector.OfPolar(l, 0.0, d);
    }

    public static Vector Position(JulianDate date)
    {
        Matrix rotateMatrix = ExtendedMath.EquatorialToEcliptical(date).Transpose();
        return rotateMatrix.Multiply(PositionEquatorial(date));
    }
}

/// <summary>
/// The parabola through three values at x = −1, 0 and 1, with its extremum and its roots within [−1, 1].
/// </summary>
internal sealed class QuadraticInterpolation
{
    private readonly double _root1;

    public QuadraticInterpolation(double yMinus, double y0, double yPlus)
    {
        double a = 0.5 * (yPlus + yMinus) - y0;
        double b = 0.5 * (yPlus - yMinus);
        double c = y0;

        double xe = -b / (2.0 * a);
        Ye = (a * xe + b) * xe + c;

        double dis = b * b - 4.0 * a * c;
        int rootCount = 0;
        if (dis >= 0.0)
        {
            double dx = 0.5 * Math.Sqrt(dis) / Math.Abs(a);
            _root1 = xe - dx;
            Root2 = xe + dx;
            if (Math.Abs(_root1) <= 1.0)
            {
                rootCount++;
            }

            if (Math.Abs(Root2) <= 1.0)
            {
                rootCount++;
            }
        }
        else
        {
            _root1 = double.NaN;
            Root2 = double.NaN;
        }

        NumberOfRoots = rootCount;
    }

    public double Ye { get; }

    // The second root when the first is outside the range.
    public double Root1 => _root1 < -1.0 ? Root2 : _root1;

    public double Root2 { get; }

    public int NumberOfRoots { get; }
}
