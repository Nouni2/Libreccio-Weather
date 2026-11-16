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
 * Copyright 2021 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.MaterialColors;

internal static class ColorMath
{
    /// <summary>
    /// 1 if the number is positive, -1 if it is negative, and 0 otherwise.
    /// </summary>
    public static int Signum(double num)
    {
        if (num < 0)
        {
            return -1;
        }
        else if (num == 0)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    /// <summary>
    /// The start at an amount of 0 and the stop at an amount of 1.
    /// </summary>
    public static double Lerp(double start, double stop, double amount)
    {
        return (1.0 - amount) * start + amount * stop;
    }

    public static int ClampInt(int min, int max, int input)
    {
        if (input < min)
        {
            return min;
        }
        else if (input > max)
        {
            return max;
        }

        return input;
    }

    public static double ClampDouble(double min, double max, double input)
    {
        if (input < min)
        {
            return min;
        }
        else if (input > max)
        {
            return max;
        }

        return input;
    }

    /// <summary>
    /// A degree measure between 0 inclusive and 360 exclusive.
    /// </summary>
    public static int SanitizeDegreesInt(int degrees)
    {
        degrees %= 360;
        if (degrees < 0)
        {
            degrees += 360;
        }

        return degrees;
    }

    /// <summary>
    /// A degree measure between 0.0 inclusive and 360.0 exclusive.
    /// </summary>
    public static double SanitizeDegreesDouble(double degrees)
    {
        degrees %= 360.0;
        if (degrees < 0)
        {
            degrees += 360.0;
        }

        return degrees;
    }

    /// <summary>
    /// -1 when decreasing the first angle reaches the second one by the shorter way, 1 otherwise, including for
    /// angles 180 degrees apart.
    /// </summary>
    public static double RotationDirection(double from, double to)
    {
        double increasingDifference = SanitizeDegreesDouble(to - from);
        return increasingDifference <= 180.0 ? 1.0 : -1.0;
    }

    /// <summary>
    /// The distance of two points on a circle, in degrees.
    /// </summary>
    public static double DifferenceDegrees(double a, double b)
    {
        return 180.0 - Math.Abs(Math.Abs(a - b) - 180.0);
    }

    /// <summary>
    /// A 1x3 row vector multiplied by a 3x3 matrix.
    /// </summary>
    public static double[] MatrixMultiply(double[] row, double[][] matrix)
    {
        double a = row[0] * matrix[0][0] + row[1] * matrix[0][1] + row[2] * matrix[0][2];
        double b = row[0] * matrix[1][0] + row[1] * matrix[1][1] + row[2] * matrix[1][2];
        double c = row[0] * matrix[2][0] + row[1] * matrix[2][1] + row[2] * matrix[2][2];
        return [a, b, c];
    }

    /// <summary>
    /// Rounds as Java's Math.round: halves towards positive infinity, NaN to 0 and out-of-range values to the
    /// nearest bound. Math.Round would round halves to even and change the tones and thresholds compared.
    /// </summary>
    public static long JavaRound(double value)
    {
        return double.IsNaN(value) ? 0 : UnitRounding.RoundToLong(value);
    }

    /// <summary>
    /// Java's Math.signum: ±1.0, or the value itself for ±0.0 and NaN. Math.Sign returns an int and throws on NaN.
    /// </summary>
    public static double JavaSignum(double value)
    {
        return double.IsNaN(value) || value == 0.0 ? value : value > 0.0 ? 1.0 : -1.0;
    }

    // Java's Math.toDegrees and Math.toRadians multiply by these constants; dividing by π, as .NET does, can differ
    // in the last bit.
    public static double JavaToDegrees(double radians) => radians * 57.29577951308232;

    public static double JavaToRadians(double degrees) => degrees * 0.017453292519943295;

    /// <summary>
    /// ln(1 + x) without the loss of precision of Math.Log(1 + x) for small x, as Java's Math.log1p.
    /// </summary>
    public static double Log1p(double x)
    {
        double u = 1.0 + x;
        return u == 1.0 ? x : Math.Log(u) * x / (u - 1.0);
    }

    /// <summary>
    /// e^x - 1 without the loss of precision of Math.Exp(x) - 1 for small x, as Java's Math.expm1.
    /// </summary>
    public static double Expm1(double x)
    {
        double u = Math.Exp(x);
        if (u == 1.0)
        {
            return x;
        }

        double um1 = u - 1.0;
        return um1 == -1.0 ? -1.0 : um1 * x / Math.Log(u);
    }
}
