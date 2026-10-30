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
using LibreccioWeather.Core.Units;

namespace LibreccioWeather.Core.Localization;

/// <summary>
/// Unit precision and unit-name width are independent, so a short value can use a full unit name.
/// </summary>
public static class UnitFormatting
{
    public static string FormatValue(Speed speed, SpeedUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return NumberFormatting.Format(speed.ToDouble(unit), unit.GetPrecision(width), Strings.Culture);
    }

    public static string Format(
        Speed speed, SpeedUnit unit,
        UnitWidth valueWidth = UnitWidth.Short, UnitWidth unitWidth = UnitWidth.Short)
    {
        return FormatMeasure(speed.ToDouble(unit), unit, valueWidth, unitWidth);
    }

    public static string FormatValue(Distance distance, DistanceUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return NumberFormatting.Format(distance.ToDouble(unit), unit.GetPrecision(width), Strings.Culture);
    }

    public static string FormatValue(Pressure pressure, PressureUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return NumberFormatting.Format(pressure.ToDouble(unit), unit.GetPrecision(width), Strings.Culture);
    }

    public static string Format(
        Distance distance, DistanceUnit unit,
        UnitWidth valueWidth = UnitWidth.Short, UnitWidth unitWidth = UnitWidth.Short)
    {
        return FormatMeasure(distance.ToDouble(unit), unit, valueWidth, unitWidth);
    }

    public static string Format(
        Pressure pressure, PressureUnit unit,
        UnitWidth valueWidth = UnitWidth.Short, UnitWidth unitWidth = UnitWidth.Short)
    {
        return FormatMeasure(pressure.ToDouble(unit), unit, valueWidth, unitWidth);
    }

    private static string FormatMeasure(double value, WeatherUnit unit, UnitWidth valueWidth, UnitWidth unitWidth)
    {
        UnitNames names = GetUnitNames(unit);
        CultureInfo culture = Strings.Culture;
        string language = culture.TwoLetterISOLanguageName;
        bool useIcu = !(unit is PressureUnit && language == "eo")
            && !(unit == SpeedUnit.Knot && language == "eo")
            && !(unit == SpeedUnit.BeaufortScale && language is "eo" or "es" or "eu" or "ga" or "ja" or "zh");
        string region = culture.Name.Split('-')[^1];
        if (unit == SpeedUnit.BeaufortScale && language is "fr" or "de")
        {
            culture = CultureInfo.GetCultureInfo(language == "fr" ? "fr-CA" : "de-CH");
        }
        else if (language == "zh" && region is "TW" or "HK" or "MO" && unitWidth != UnitWidth.Long)
        {
            culture = CultureInfo.GetCultureInfo("en-001");
        }

        if (useIcu && names.IcuUnit is { } icuUnit
            && FormatIcuMeasure(value, unit.GetPrecision(valueWidth), $"measure-unit/{icuUnit}"
                + (names.IcuPer is not null ? " per-measure-unit/duration-second" : ""), unitWidth, culture) is { } formatted)
        {
            return formatted;
        }

        string suffix = unitWidth == UnitWidth.Long ? "long" : "short";
        string text = string.Format(Strings.Culture, Strings.GetUnit($"{names.Name}_nominative_{suffix}"),
            NumberFormatting.Format(value, unit.GetPrecision(valueWidth), culture));
        return names.Per is null ? text : string.Format(Strings.Culture, Strings.GetUnit($"{names.Per}_per_{suffix}"), text);
    }

    public static string FormatValue(Precipitation precipitation, PrecipitationUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return NumberFormatting.Format(precipitation.ToDouble(unit), unit.GetPrecision(width), Strings.Culture);
    }

    public static string Format(
        Precipitation precipitation, PrecipitationUnit unit,
        UnitWidth valueWidth = UnitWidth.Short, UnitWidth unitWidth = UnitWidth.Short)
    {
        UnitNames names = GetUnitNames(unit);
        if (names.IcuUnit is { } icuUnit && FormatIcuMeasure(precipitation.ToDouble(unit), unit.GetPrecision(valueWidth),
            $"measure-unit/{icuUnit}" + (unit == PrecipitationUnit.LiterPerSquareMeter ? " per-measure-unit/area-square-meter" : ""),
            unitWidth) is { } formatted)
        {
            return formatted;
        }

        string suffix = unitWidth == UnitWidth.Long ? "long" : "short";
        string text = string.Format(Strings.Culture, Strings.GetUnit($"{names.Name}_nominative_{suffix}"),
            FormatValue(precipitation, unit, valueWidth));
        return names.Per is null ? text : string.Format(Strings.Culture, Strings.GetUnit($"{names.Per}_per_{suffix}"), text);
    }

    public static string FormatPercent(Ratio ratio, UnitWidth valueWidth = UnitWidth.Short)
    {
        return FormatIcuMeasure(ratio.InPercent, RatioUnit.Percent.GetPrecision(valueWidth), "percent", UnitWidth.Short)
            ?? PercentFormatting.Format(ratio.InPercent, RatioUnit.Percent.GetPrecision(valueWidth), Strings.Culture);
    }

    private static string? FormatIcuMeasure(double value, int precision, string measure, UnitWidth width, CultureInfo? culture = null)
    {
        if (!Icu.SupportsUnitNames)
        {
            return null;
        }

        string precisionSkeleton = precision == 0 ? "precision-integer" : "." + new string('#', precision);
        string unitWidth = width switch
        {
            UnitWidth.Long => "unit-width-full-name",
            UnitWidth.Narrow => "unit-width-narrow",
            _ => "unit-width-short",
        };
        double number = precision == 0 ? RoundToInt(value) : value;
        return Icu.FormatNumber($"{measure} {unitWidth} {precisionSkeleton}", number, culture ?? Strings.Culture)?.Text;
    }

    public static string Format(
        Temperature temperature,
        TemperatureUnit unit,
        UnitWidth valueWidth = UnitWidth.Short,
        UnitWidth unitWidth = UnitWidth.Short)
    {
        string language = Strings.Culture.TwoLetterISOLanguageName;
        UnitNames names = GetUnitNames(unit);
        if (unitWidth != UnitWidth.Narrow && language != "eo" && !(language == "pl" && unitWidth == UnitWidth.Short)
            && names.IcuUnit is { } icuUnit
            && FormatIcuMeasure(temperature.ToDouble(unit), unit.GetPrecision(valueWidth), $"measure-unit/{icuUnit}", unitWidth) is { } formatted)
        {
            return formatted;
        }

        string value = NumberFormatting.Format(temperature.ToDouble(unit), unit.GetPrecision(valueWidth), Strings.Culture);
        return string.Format(Strings.Culture, Nominative("temperature", unit, unitWidth), value);
    }

    /// <summary>
    /// The name of a unit, such as "kilometers per hour". It comes from CLDR through ICU when Windows has it, and from
    /// the translated unit strings otherwise, such as "km/h". A unit measured per another one without a CLDR name of
    /// its own, such as centimeters per second, joins the name of the first with the "per" pattern of the second.
    /// </summary>
    public static string GetDisplayName(WeatherUnit unit, UnitWidth width = UnitWidth.Short)
    {
        UnitNames names = GetUnitNames(unit);
        string suffix = width == UnitWidth.Long ? "long" : "short";

        if (names.IcuUnit is not null && Icu.SupportsUnitNames && GetIcuDisplayName(names.IcuUnit, width) is { } icuName)
        {
            return names.IcuPer is null
                ? icuName
                : string.Format(Strings.Culture, Strings.GetUnit($"{names.IcuPer}_per_{suffix}"), icuName);
        }

        string name = Strings.GetUnit($"{names.Name}_display_name_{suffix}");
        return names.Per is null ? name : string.Format(Strings.Culture, Strings.GetUnit($"{names.Per}_per_{suffix}"), name);
    }

    // The unit part of a value formatted in the plural form "other", which matches CLDR's display name for the
    // shipped languages. ICU's C API has no call for the display name itself.
    private static string? GetIcuDisplayName(string icuUnit, UnitWidth width)
    {
        string unitWidth = width switch
        {
            UnitWidth.Long => "unit-width-full-name",
            UnitWidth.Narrow => "unit-width-narrow",
            _ => "unit-width-short",
        };
        const double OtherPluralValue = 2;
        return Icu.FormatNumber($"measure-unit/{icuUnit} {unitWidth}", OtherPluralValue, Strings.Culture)?.Unit?.Trim();
    }

    /// <summary>
    /// The string keys of a unit's name and "per" pattern, and its CLDR unit with the "per" pattern used alongside it.
    /// A unit without a CLDR unit is only named from the strings.
    /// </summary>
    private sealed record UnitNames(string Name, string? Per, string? IcuUnit, string? IcuPer);

    private static UnitNames GetUnitNames(WeatherUnit unit)
    {
        return unit switch
        {
            _ when unit == TemperatureUnit.DeciCelsius => new("temperature_dc", null, null, null),
            _ when unit == TemperatureUnit.Celsius => new("temperature_c", null, "temperature-celsius", null),
            _ when unit == TemperatureUnit.Fahrenheit => new("temperature_f", null, "temperature-fahrenheit", null),
            _ when unit == TemperatureUnit.Kelvin => new("temperature_k", null, "temperature-kelvin", null),
            _ when unit == DistanceUnit.Meter => new("length_m", null, "length-meter", null),
            _ when unit == DistanceUnit.Kilometer => new("length_km", null, "length-kilometer", null),
            _ when unit == DistanceUnit.Mile => new("length_mi", null, "length-mile", null),
            _ when unit == DistanceUnit.NauticalMile => new("length_nmi", null, "length-nautical-mile", null),
            _ when unit == DistanceUnit.Foot => new("length_ft", null, "length-foot", null),
            _ when unit == PrecipitationUnit.Micrometer => new("length_microm", null, "length-micrometer", null),
            _ when unit == PrecipitationUnit.Millimeter => new("length_mm", null, "length-millimeter", null),
            _ when unit == PrecipitationUnit.Centimeter => new("length_cm", null, "length-centimeter", null),
            _ when unit == PrecipitationUnit.Inch => new("length_in", null, "length-inch", null),
            _ when unit == PrecipitationUnit.LiterPerSquareMeter => new("volume_l", "area_m2", "volume-liter", "area_m2"),
            _ when unit == PressureUnit.Pascal => new("pressure_pa", null, null, null),
            _ when unit == PressureUnit.Hectopascal => new("pressure_hpa", null, "pressure-hectopascal", null),
            _ when unit == PressureUnit.Kilopascal => new("pressure_kpa", null, null, null),
            _ when unit == PressureUnit.Millibar => new("pressure_mbar", null, "pressure-millibar", null),
            _ when unit == PressureUnit.Atmosphere => new("pressure_atm", null, "pressure-atmosphere", null),
            _ when unit == PressureUnit.MillimeterOfMercury => new("pressure_mmhg", null, "pressure-millimeter-ofhg", null),
            _ when unit == PressureUnit.InchOfMercury => new("pressure_inhg", null, "pressure-inch-ofhg", null),
            _ when unit == SpeedUnit.CentimeterPerSecond => new("length_cm", "duration_sec", "length-centimeter", "duration_sec"),
            _ when unit == SpeedUnit.MeterPerSecond => new("length_m", "duration_sec", "speed-meter-per-second", null),
            _ when unit == SpeedUnit.KilometerPerHour => new("length_km", "duration_hr", "speed-kilometer-per-hour", null),
            _ when unit == SpeedUnit.MilePerHour => new("length_mi", "duration_hr", "speed-mile-per-hour", null),
            _ when unit == SpeedUnit.Knot => new("speed_kn", null, "speed-knot", null),
            _ when unit == SpeedUnit.FootPerSecond => new("length_ft", "duration_sec", "length-foot", "duration_sec"),
            _ when unit == SpeedUnit.BeaufortScale => new("speed_bf", null, "speed-beaufort", null),
            _ => throw new ArgumentException($"No display name for the unit '{unit.Id}'.", nameof(unit)),
        };
    }

    /// <summary>
    /// The unit symbol alone, such as "°C".
    /// </summary>
    public static string GetNominativeUnit(TemperatureUnit unit, UnitWidth width = UnitWidth.Short)
    {
        return string.Format(Strings.Culture, Nominative("temperature", unit, width), "").Trim();
    }

    public static string GetNominativeUnit(PressureUnit unit, UnitWidth width = UnitWidth.Short)
    {
        UnitNames names = GetUnitNames(unit);
        string suffix = width == UnitWidth.Long ? "long" : "short";
        return string.Format(Strings.Culture, Strings.GetUnit($"{names.Name}_nominative_{suffix}"), "").Trim();
    }

    /// <summary>
    /// The value rounded to a whole number, half towards positive infinity, without its unit.
    /// </summary>
    public static string FormatRounded(Temperature temperature, TemperatureUnit unit)
    {
        return NumberFormatting.Format(temperature.ToDouble(unit), 0, Strings.Culture);
    }

    public static int RoundToInt(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("Cannot round NaN to an integer.", nameof(value));
        }

        return (int)Math.Clamp(UnitRounding.RoundToLong(value), int.MinValue, int.MaxValue);
    }

    // Keys follow the upstream names, such as temperature_c_nominative_short.
    private static string Nominative(string quantity, WeatherUnit unit, UnitWidth width)
    {
        string suffix = width switch
        {
            UnitWidth.Narrow => "narrow",
            UnitWidth.Long => "long",
            _ => "short",
        };
        return Strings.GetUnit($"{quantity}_{unit.Id}_nominative_{suffix}");
    }
}
