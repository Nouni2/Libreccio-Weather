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

namespace LibreccioWeather.Core.Charts;

public static class TrendCoordinates
{
    public static int ComputeSingleCoordinate(
        int measuredHeight, int marginTop, int marginBottom, float value, float max, float min)
    {
        float canvasHeight = measuredHeight - marginTop - marginBottom;
        float coordinate = measuredHeight - marginBottom - canvasHeight * (value - min) / (max - min);
        if (float.IsNaN(coordinate))
        {
            return 0;
        }

        if (coordinate >= int.MaxValue)
        {
            return int.MaxValue;
        }

        if (coordinate <= int.MinValue)
        {
            return int.MinValue;
        }

        return (int)coordinate;
    }
}
