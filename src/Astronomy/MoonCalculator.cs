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
/// Moonrise and moonset found from an instant: the first rise and the first set in the next 24 hours, or in the next
/// year with a full cycle. Without either in a day, the Moon is always up or always down that day.
/// </summary>
public sealed record MoonTimes(DateTimeOffset? Rise, DateTimeOffset? Set, bool IsAlwaysUp, bool IsAlwaysDown);

/// <summary>
/// The lit part of the Moon as an angle: −180° at new moon, 0° at full moon, negative while waxing.
/// </summary>
public sealed record MoonIllumination(double Fraction, double Phase);

/// <summary>
/// The Moon's rise, set, phase and altitude, for an observer at sea level. Times are rounded to the minute.
/// </summary>
public static class MoonCalculator
{
    // Refraction at the horizon, which the Moon's height is corrected by when it rises and sets.
    private static readonly double HorizonRefraction = ExtendedMath.ApparentRefraction(0.0);

    public static MoonTimes GetTimes(DateTimeOffset start, double latitude, double longitude, bool fullCycle = false)
    {
        var jd = new JulianDate(start);
        double latitudeRad = ExtendedMath.ToRadians(latitude);
        double longitudeRad = ExtendedMath.ToRadians(longitude);

        double? rise = null;
        double? set = null;
        bool alwaysUp = false;
        bool alwaysDown = false;

        int hour = 0;
        int maxHours = fullCycle ? 365 * 24 : 24;

        double yMinus = CorrectedMoonHeight(jd.AtHour(hour - 1.0), latitudeRad, longitudeRad);
        double y0 = CorrectedMoonHeight(jd.AtHour(hour), latitudeRad, longitudeRad);
        double yPlus = CorrectedMoonHeight(jd.AtHour(hour + 1.0), latitudeRad, longitudeRad);

        while (hour <= maxHours)
        {
            var qi = new QuadraticInterpolation(yMinus, y0, yPlus);
            double ye = qi.Ye;

            if (qi.NumberOfRoots == 1)
            {
                double rt = qi.Root1 + hour;
                if (yMinus < 0.0)
                {
                    if (rise is null && rt >= 0.0)
                    {
                        rise = rt;
                    }
                }
                else
                {
                    if (set is null && rt >= 0.0)
                    {
                        set = rt;
                    }
                }
            }
            else if (qi.NumberOfRoots == 2)
            {
                if (rise is null)
                {
                    double rt = hour + (ye < 0.0 ? qi.Root2 : qi.Root1);
                    if (rt >= 0.0)
                    {
                        rise = rt;
                    }
                }

                if (set is null)
                {
                    double rt = hour + (ye < 0.0 ? qi.Root1 : qi.Root2);
                    if (rt >= 0.0)
                    {
                        set = rt;
                    }
                }
            }

            if (hour == 23 && rise is null && set is null)
            {
                alwaysUp = ye >= 0.0;
                alwaysDown = ye < 0.0;
            }

            if (rise is not null && set is not null)
            {
                break;
            }

            hour++;
            yMinus = y0;
            y0 = yPlus;
            yPlus = CorrectedMoonHeight(jd.AtHour(hour + 1.0), latitudeRad, longitudeRad);
        }

        if (!fullCycle)
        {
            if (rise >= 24.0)
            {
                rise = null;
            }

            if (set >= 24.0)
            {
                set = null;
            }
        }

        return new MoonTimes(
            rise is { } riseHour ? jd.AtHour(riseHour).GetDateTruncatedToMinutes() : null,
            set is { } setHour ? jd.AtHour(setHour).GetDateTruncatedToMinutes() : null,
            alwaysUp,
            alwaysDown);
    }

    public static MoonIllumination GetIllumination(DateTimeOffset instant)
    {
        var t = new JulianDate(instant);
        Vector s = SolarOrbit.Position(t);
        Vector m = LunarOrbit.Position(t);

        double phi = Math.PI - Math.Acos(m.Dot(s) / (m.R * s.R));
        Vector sunMoon = m.Cross(s);

        return new MoonIllumination(
            (1 + Math.Cos(phi)) / 2,
            ExtendedMath.ToDegrees(phi * ExtendedMath.Signum(sunMoon.Theta)));
    }

    /// <summary>
    /// The Moon's altitude above the horizon, in degrees, corrected for refraction.
    /// </summary>
    public static double GetAltitude(DateTimeOffset instant, double latitude, double longitude)
    {
        var t = new JulianDate(instant);
        double phi = ExtendedMath.ToRadians(latitude);
        double lambda = ExtendedMath.ToRadians(longitude);

        Vector mc = LunarOrbit.Position(t);
        double h = t.GreenwichMeanSiderealTime + lambda - mc.Phi;
        Vector horizontal = ExtendedMath.EquatorialToHorizontal(h, mc.Theta, mc.R, phi);
        double hRef = ExtendedMath.Refraction(horizontal.Theta);
        return ExtendedMath.ToDegrees(horizontal.Theta + hRef);
    }

    private static double CorrectedMoonHeight(JulianDate jd, double latitudeRad, double longitudeRad)
    {
        Vector pos = LunarOrbit.PositionHorizontal(jd, latitudeRad, longitudeRad);
        double hc = ExtendedMath.Parallax(0.0, pos.R) - HorizonRefraction - LunarOrbit.AngularRadius(pos.R);
        return pos.Theta - hc;
    }
}
