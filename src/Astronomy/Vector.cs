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
/// A three-dimensional vector. A vector made from polar coordinates returns them exactly as given, without
/// normalizing the angle; one made from Cartesian coordinates computes them, with φ in [0, 2π).
/// </summary>
internal sealed class Vector
{
    private double? _phi;
    private double? _theta;
    private double? _r;

    public Vector(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }

    public static Vector OfPolar(double phi, double theta, double r)
    {
        double cosTheta = Math.Cos(theta);
        return new Vector(r * Math.Cos(phi) * cosTheta, r * Math.Sin(phi) * cosTheta, r * Math.Sin(theta))
        {
            _phi = phi,
            _theta = theta,
            _r = r,
        };
    }

    public double Phi
    {
        get
        {
            if (_phi is null)
            {
                double phi = ExtendedMath.IsZero(X) && ExtendedMath.IsZero(Y) ? 0.0 : Math.Atan2(Y, X);
                _phi = phi < 0.0 ? phi + ExtendedMath.PI2 : phi;
            }

            return _phi.Value;
        }
    }

    public double Theta
    {
        get
        {
            if (_theta is null)
            {
                double rhoSqr = X * X + Y * Y;
                _theta = ExtendedMath.IsZero(Z) && ExtendedMath.IsZero(rhoSqr) ? 0.0 : Math.Atan2(Z, Math.Sqrt(rhoSqr));
            }

            return _theta.Value;
        }
    }

    public double R => _r ??= Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector Cross(Vector right)
    {
        return new Vector(Y * right.Z - Z * right.Y, Z * right.X - X * right.Z, X * right.Y - Y * right.X);
    }

    public double Dot(Vector right) => X * right.X + Y * right.Y + Z * right.Z;
}
