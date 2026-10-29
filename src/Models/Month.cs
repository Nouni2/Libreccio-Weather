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

using System.Globalization;

namespace LibreccioWeather.Core.Models;

/// <summary>
/// A month of the year, numbered from 1 for January to 12 for December as in ISO 8601.
/// </summary>
public enum Month
{
    January = 1,
    February,
    March,
    April,
    May,
    June,
    July,
    August,
    September,
    October,
    November,
    December,
}

public static class MonthExtensions
{
    extension(Month month)
    {
        public int Value => (int)month;

        public string GetDisplayName(CultureInfo culture)
        {
            return culture.DateTimeFormat.MonthNames[(int)month - 1];
        }

        /// <summary>
        /// The month the given number of months later, wrapping around the year. The number may be negative.
        /// </summary>
        public Month Plus(int months)
        {
            int amount = months % 12;
            return (Month)(((int)month - 1 + amount + 12) % 12 + 1);
        }

        /// <summary>
        /// The month the given number of months earlier, wrapping around the year. The number may be negative.
        /// </summary>
        public Month Minus(int months)
        {
            return month.Plus(-(months % 12));
        }

        /// <summary>
        /// The longest the month can be, so 29 days for February.
        /// </summary>
        public int MaxLength()
        {
            return month switch
            {
                Month.February => 29,
                Month.April or Month.June or Month.September or Month.November => 30,
                _ => 31,
            };
        }
    }

    extension(Month)
    {
        /// <summary>
        /// The month for a number from 1 for January to 12 for December.
        /// </summary>
        public static Month Of(int month)
        {
            if (month is < 1 or > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Invalid value for MonthOfYear.");
            }

            return (Month)month;
        }

        /// <summary>
        /// The month for a zero-based number from 0 for January to 11 for December.
        /// </summary>
        public static Month FromCalendarMonth(int month)
        {
            if (month is < 0 or > 11)
            {
                throw new ArgumentOutOfRangeException(nameof(month), month, "Invalid value for MonthOfYear.");
            }

            return (Month)(month + 1);
        }
    }
}
