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
/// A 3×3 rotation matrix, stored row by row.
/// </summary>
internal sealed class Matrix
{
    private readonly double[] _mx;

    private Matrix(params double[] values)
    {
        _mx = values;
    }

    public static Matrix RotateX(double angle)
    {
        double s = Math.Sin(angle);
        double c = Math.Cos(angle);
        return new Matrix(
            1.0, 0.0, 0.0,
            0.0, c, s,
            0.0, -s, c);
    }

    public static Matrix RotateY(double angle)
    {
        double s = Math.Sin(angle);
        double c = Math.Cos(angle);
        return new Matrix(
            c, 0.0, -s,
            0.0, 1.0, 0.0,
            s, 0.0, c);
    }

    public Matrix Transpose()
    {
        double[] result = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                result[i * 3 + j] = _mx[j * 3 + i];
            }
        }

        return new Matrix(result);
    }

    public Vector Multiply(Vector right)
    {
        double[] vec = [right.X, right.Y, right.Z];
        double[] result = new double[3];
        for (int i = 0; i < 3; i++)
        {
            double scalp = 0.0;
            for (int j = 0; j < 3; j++)
            {
                scalp += _mx[i * 3 + j] * vec[j];
            }

            result[i] = scalp;
        }

        return new Vector(result[0], result[1], result[2]);
    }
}
