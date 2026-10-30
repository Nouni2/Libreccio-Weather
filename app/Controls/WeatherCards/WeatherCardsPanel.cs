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

using System.Windows;
using System.Windows.Controls;
using LibreccioWeather.ViewModels.WeatherCards;
using Size = System.Windows.Size;

namespace LibreccioWeather.Controls.WeatherCards;

public sealed class WeatherCardsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        return Layout(availableSize.Width, false);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize.Width, true);
        return finalSize;
    }

    private Size Layout(double width, bool arrange)
    {
        float minBlockWidth = 160f;
        int blocks = Math.Clamp((int)Math.Floor((float)width / minBlockWidth), 1, 5);
        if (blocks > 2)
        {
            blocks = Math.Clamp((int)Math.Floor((float)width / (minBlockWidth * 1.1f)), 1, 5);
        }

        double blockWidth = width / blocks;
        double y = 0;
        int column = 0;
        foreach (UIElement child in InternalChildren)
        {
            bool square = child is ContentPresenter { Content: ConditionsCardViewModel };
            if (!square && column > 0)
            {
                y += blockWidth;
                column = 0;
            }

            if (square)
            {
                if (arrange)
                {
                    child.Arrange(new Rect(column * blockWidth, y, blockWidth, blockWidth));
                }
                else
                {
                    child.Measure(new Size(blockWidth, blockWidth));
                }

                column++;
                if (column == blocks)
                {
                    column = 0;
                    y += blockWidth;
                }
            }
            else
            {
                if (arrange)
                {
                    child.Arrange(new Rect(0, y, width, child.DesiredSize.Height));
                }
                else
                {
                    child.Measure(new Size(width, double.PositiveInfinity));
                }

                y += child.DesiredSize.Height;
            }
        }

        return new Size(width, y + (column > 0 ? blockWidth : 0));
    }
}
