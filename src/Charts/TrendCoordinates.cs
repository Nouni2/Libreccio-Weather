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
        return ToInt(coordinate);
    }

    public static int ComputeKeyLineCoordinate(int boundaryTop, int boundaryBottom, float value, float max, float min)
    {
        float dataRange = max - min;
        float boundaryRange = boundaryBottom - boundaryTop;
        return ToInt(boundaryBottom - (value - min) / dataRange * boundaryRange);
    }

    public static int ComputeDoubleHistogramCoordinate(
        int measuredHeight, int marginTop, int marginBottom, int marginCenter, float value, float max, bool low)
    {
        float cy = measuredHeight / 2f;
        if (low)
        {
            float bottomCanvasHeight = measuredHeight - marginBottom - (cy + marginCenter / 2f);
            return ToInt(cy + marginCenter / 2f + bottomCanvasHeight * value / max);
        }

        float topCanvasHeight = cy - marginCenter / 2f - marginTop;
        return ToInt(cy - marginCenter / 2f - topCanvasHeight * value / max);
    }

    private static int ToInt(float coordinate)
    {
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
