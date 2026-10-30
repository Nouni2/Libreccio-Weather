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

using LibreccioWeather.Appearance;
using LibreccioWeather.Core.Calculations;
using LibreccioWeather.Core.Localization;
using LibreccioWeather.Core.Locations;
using LibreccioWeather.Core.Models;
using LibreccioWeather.Core.Settings;
using LibreccioWeather.Core.Settings.Options;
using LibreccioWeather.Core.Units;
using Color = System.Windows.Media.Color;
using Precipitation = LibreccioWeather.Core.Models.Precipitation;

namespace LibreccioWeather.ViewModels.WeatherCards;

public enum ConditionsCardKind
{
    Precipitation,
    Wind,
    AirQuality,
    Humidity,
    UV,
    Visibility,
    Pressure,
    Pollen,
}

public sealed record ConditionsCardViewModel(
    ConditionsCardKind Kind, string Title, string ValueText, string? SupportingText, string Description,
    string IconKey, string? ArtworkKey = null, float Rotation = 0, string? DewPointText = null,
    float? GaugeProgress = null, float GaugeMaximum = 100, Color? GaugeColor = null, bool HalfSizeUnits = false,
    IReadOnlyList<PollenCardItem>? PollenItems = null)
{
    public static ConditionsCardViewModel? Create(
        CardDisplay card, Core.Models.Weather weather, Location location, SettingsManager settings, string regionCode)
    {
        Current? current = weather.Current;
        if (card == CardDisplay.Precipitation)
        {
            return CreatePrecipitation(weather, location, settings.GetPrecipitationUnit(regionCode));
        }

        if (card == CardDisplay.Wind && current?.Wind is { IsValid: true } wind)
        {
            SpeedUnit unit = settings.GetSpeedUnit(regionCode);
            string? detail = wind.Speed is { } speed && wind.Gusts is { } gusts && gusts > speed
                ? Strings.Get("wind_gusts_short") + Strings.Get("colon_separator")
                    + UnitFormatting.Format(gusts, unit, UnitWidth.Narrow)
                : WindFormatting.GetDirection(wind) is { } direction
                    ? wind.Degree is >= 0 and <= 360 ? Strings.Format("wind_origin", direction) : direction
                    : null;
            return new ConditionsCardViewModel(ConditionsCardKind.Wind, Strings.Get("wind"),
                wind.Speed is { } value ? UnitFormatting.Format(value, unit, UnitWidth.Narrow) : "",
                detail, Strings.Get("wind") + Strings.Get("colon_separator") + WindFormatting.GetContentDescription(wind, unit),
                "CardIconWind", wind.Degree is null ? null : wind.Degree == -1 ? "CardWindVariable" : "CardWindArrow",
                wind.Degree is { } degree && degree != -1 ? (float)degree : 0, HalfSizeUnits: true);
        }

        if (card == CardDisplay.AirQuality)
        {
            AirQuality? airQuality = current?.AirQuality is { IsIndexValid: true } currentAirQuality
                ? currentAirQuality : weather.Today?.AirQuality is { IsIndexValid: true } dailyAirQuality ? dailyAirQuality : null;
            if (airQuality is null)
            {
                return null;
            }

            int? index = AirQualityIndex.GetIndex(airQuality);
            string value = NumberFormatting.Format(index ?? 0, 0, Strings.Culture);
            string? level = AirQualityIndex.GetAqiToLevel(index) is { } levelIndex
                ? Strings.GetArray("air_quality_levels").ElementAtOrDefault(levelIndex) : null;
            return new ConditionsCardViewModel(ConditionsCardKind.AirQuality, Strings.Get("air_quality_short"), value, level,
                Strings.Get("air_quality_index") + Strings.Get("colon_separator") + value
                    + Strings.GetUnit("locale_separator") + level, "CardIconAirQuality",
                GaugeProgress: Math.Min(index ?? 0, settings.IsElementsAnimationEnabled ? AirQualityIndex.AqiThresholds[^1] : 400),
                GaugeMaximum: AirQualityIndex.AqiThresholds[^1],
                GaugeColor: ForecastColors.GetAirQualityColor(index));
        }

        if (card == CardDisplay.Humidity && current?.RelativeHumidity is { } humidity)
        {
            string value = UnitFormatting.FormatPercent(humidity, UnitWidth.Narrow);
            string? dewPoint = current.DewPoint is { } dew
                ? UnitFormatting.Format(dew, settings.GetTemperatureUnit(regionCode), UnitWidth.Narrow, UnitWidth.Narrow) : null;
            string? artwork = humidity.InPercent switch
            {
                >= 0 and <= 20 => "CardHumidity7",
                >= 20 and <= 40 => "CardHumidity30",
                >= 60 and <= 80 => "CardHumidity75",
                >= 80 and <= 100 => "CardHumidity90",
                >= 0 and <= 100 => "CardHumidity50",
                _ => null,
            };
            return new ConditionsCardViewModel(ConditionsCardKind.Humidity, Strings.Get("humidity"), value, Strings.Get("dew_point"),
                Strings.Get("humidity") + Strings.Get("colon_separator") + value
                    + (dewPoint is not null ? Strings.GetUnit("locale_separator") + Strings.Get("dew_point")
                        + Strings.Get("colon_separator") + dewPoint : ""),
                "CardIconHumidity", artwork, DewPointText: dewPoint);
        }

        if (card == CardDisplay.UV && current?.UV is { Index: { } uvIndex } uv)
        {
            string artwork = uvIndex switch
            {
                >= 0 and < UV.UVIndexLow => "CardUVLow",
                >= UV.UVIndexLow and < UV.UVIndexMiddle => "CardUVModerate",
                >= UV.UVIndexMiddle and < UV.UVIndexHigh => "CardUVHigh",
                >= UV.UVIndexHigh and < UV.UVIndexExcessive => "CardUVVeryHigh",
                >= UV.UVIndexExcessive and <= double.MaxValue => "CardUVExtreme",
                _ => "CardUVUnknown",
            };
            return new ConditionsCardViewModel(ConditionsCardKind.UV, Strings.Get("uv_index_short"),
                NumberFormatting.Format(UnitFormatting.RoundToInt(uvIndex), 0, Strings.Culture), UVFormatting.GetLevel(uv),
                Strings.Get("uv_index") + Strings.Get("colon_separator") + UVFormatting.GetContentDescription(uv),
                "CardIconUV", artwork);
        }

        if (card == CardDisplay.Visibility && current?.Visibility is { } visibility)
        {
            DistanceUnit unit = settings.GetDistanceUnit(regionCode);
            string value = UnitFormatting.Format(visibility, unit);
            string? description = visibility.InMeters switch
            {
                >= 0 and < 1000 => Strings.Get("visibility_very_poor"),
                >= 1000 and < 4000 => Strings.Get("visibility_poor"),
                >= 4000 and < 10000 => Strings.Get("visibility_moderate"),
                >= 10000 and < 20000 => Strings.Get("visibility_good"),
                >= 20000 and < 40000 => Strings.Get("visibility_clear"),
                >= 40000 and <= double.MaxValue => Strings.Get("visibility_perfectly_clear"),
                _ => null,
            };
            return new ConditionsCardViewModel(ConditionsCardKind.Visibility, Strings.Get("visibility"), value, description,
                Strings.Get("visibility") + Strings.Get("colon_separator")
                    + UnitFormatting.Format(visibility, unit, unitWidth: UnitWidth.Long)
                    + Strings.GetUnit("locale_separator") + value,
                "CardIconVisibility", "CardVisibility", HalfSizeUnits: true);
        }

        if (card == CardDisplay.Pressure && current?.Pressure is { } pressure)
        {
            PressureUnit unit = settings.GetPressureUnit(regionCode);
            return new ConditionsCardViewModel(ConditionsCardKind.Pressure, Strings.Get("pressure"),
                UnitFormatting.FormatValue(pressure, unit), UnitFormatting.GetNominativeUnit(unit),
                Strings.Get("pressure") + Strings.Get("colon_separator") + UnitFormatting.Format(pressure, unit, unitWidth: UnitWidth.Long),
                "CardIconPressure", GaugeProgress: Math.Min((float)pressure.InHectopascals - 963f, 100f));
        }

        if (card == CardDisplay.Pollen && weather.Today?.Pollen is { IsValid: true } pollen)
        {
            List<PollenCardItem> items = PollenIndex.Entries.Where(type => type != PollenIndex.Mold
                    && type.GetConcentration(pollen) is { Value: > 0 })
                .Select(type => (Type: type, Index: type.GetIndex(type.GetConcentration(pollen)?.InPerCubicMeter)))
                .OrderByDescending(item => item.Index)
                .Take(2)
                .Select(item => new PollenCardItem(Strings.Get(item.Type.NameKey), GetPollenName(item.Index), GetPollenColor(item.Index)))
                .ToList();
            string description;
            if (items.Count == 0)
            {
                items.Add(new PollenCardItem(null, GetPollenName(0), GetPollenColor(0)));
                description = Strings.Get("pollen") + Strings.Get("colon_separator") + items[0].Level;
            }
            else
            {
                description = Strings.Get("pollen_primary") + Strings.Get("colon_separator")
                    + string.Join(Strings.GetUnit("locale_separator"), items.Select(item =>
                        item.Name + Strings.Get("colon_separator") + item.Level));
            }

            return new ConditionsCardViewModel(ConditionsCardKind.Pollen, Strings.Get("pollen"), "", null,
                description, "CardIconPollen", PollenItems: items);
        }

        return null;
    }

    private static string? GetPollenName(int? index)
    {
        return index == 0 ? Strings.Get("pollen_level_0") : PollenIndex.GetPollenIndexToLevel(index) is { } level
            ? Strings.GetArray("pollen_levels").ElementAtOrDefault(level) : null;
    }

    private static Color GetPollenColor(int? index)
    {
        if (index == 0)
        {
            return Color.FromRgb(0xBF, 0xBF, 0xBF);
        }

        return PollenIndex.GetPollenIndexToLevel(index) switch
        {
            0 => Color.FromRgb(0x08, 0xC2, 0x86),
            1 => Color.FromRgb(0x6A, 0xD5, 0x55),
            2 => Color.FromRgb(0xFF, 0xD7, 0x41),
            3 => Color.FromRgb(0xFF, 0xAB, 0x40),
            4 => Color.FromRgb(0xFF, 0x3B, 0x30),
            _ => System.Windows.Media.Colors.Transparent,
        };
    }

    private static ConditionsCardViewModel? CreatePrecipitation(Core.Models.Weather weather, Location location, PrecipitationUnit unit)
    {
        int hour = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, location.TimeZone).Hour;
        Precipitation? precipitation = hour < 5
            ? weather.TodayIndex is > 0 and { } today ? weather.DailyForecast[today - 1].Night?.Precipitation : null
            : hour < 17 ? weather.Today?.Day?.Precipitation : weather.Today?.Night?.Precipitation;
        if (precipitation?.Total is not { } total)
        {
            return null;
        }

        string type = precipitation.Rain is { Value: > 0 } && precipitation.Snow is { Value: > 0 } ? ""
            : precipitation.Snow is { Value: > 0 } snow && snow == total ? "snow_"
            : precipitation.Rain is { Value: > 0 } rain && rain == total ? "rain_" : "";
        string amount = Strings.Get("precipitation_" + type + "total_" + (hour is >= 5 and <= 16 ? "day" : "night"));
        return new ConditionsCardViewModel(ConditionsCardKind.Precipitation, Strings.Get("precipitation"),
            UnitFormatting.Format(total, unit), amount,
            Strings.Get("precipitation") + Strings.Get("colon_separator") + amount + Strings.Get("colon_separator")
                + UnitFormatting.Format(total, unit, unitWidth: UnitWidth.Long),
            "CardIconPrecipitation", HalfSizeUnits: true);
    }
}

public sealed record PollenCardItem(string? Name, string? Level, Color Color);
