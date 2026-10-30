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

using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Astronomy;

/// <summary>
/// An instant with its modified Julian date. The calendar fields it reads, the day of the year and the seconds of a
/// minute, are those of the computer's time zone.
/// </summary>
internal sealed class JulianDate
{
    public JulianDate(DateTimeOffset instant)
    {
        Instant = instant;
        ModifiedJulianDate = instant.ToUnixTimeMilliseconds() / 86400000.0 + 40587.0;
    }

    public DateTimeOffset Instant { get; }

    public double ModifiedJulianDate { get; }

    public double JulianCentury => (ModifiedJulianDate - 51544.5) / 36525.0;

    /// <summary>
    /// The instant some hours later, to the nearest second.
    /// </summary>
    public JulianDate AtHour(double hour)
    {
        return new JulianDate(Instant.AddSeconds((int)UnitRounding.RoundToLong(hour * 60.0 * 60.0)));
    }

    /// <summary>
    /// The instant rounded to the nearest minute, as the computer's clock shows minutes.
    /// </summary>
    public DateTimeOffset GetDateTruncatedToMinutes()
    {
        DateTimeOffset truncated = Instant.AddMilliseconds(-Instant.Millisecond).AddSeconds(30);
        int second = TimeZoneInfo.ConvertTime(truncated, TimeZoneInfo.Local).Second;
        return truncated.AddSeconds(-second);
    }

    public double GreenwichMeanSiderealTime
    {
        get
        {
            const double Secs = 86400.0;
            double mjd0 = Math.Floor(ModifiedJulianDate);
            double ut = (ModifiedJulianDate - mjd0) * Secs;
            double t0 = (mjd0 - 51544.5) / 36525.0;
            double t = (ModifiedJulianDate - 51544.5) / 36525.0;
            double gmst = 24110.54841
                + 8640184.812866 * t0
                + 1.0027379093 * ut
                + (0.093104 - 6.2e-6 * t) * t * t;
            return (ExtendedMath.PI2 / Secs) * (gmst % Secs);
        }
    }

    public double TrueAnomaly
    {
        get
        {
            int dayOfYear = TimeZoneInfo.ConvertTime(Instant, TimeZoneInfo.Local).DayOfYear - 1;
            return ExtendedMath.PI2 * ExtendedMath.Frac((dayOfYear - 4.0) / 365.256363);
        }
    }
}
