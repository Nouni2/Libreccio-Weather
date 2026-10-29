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

using LibreccioWeather.Core.Models.Wrappers;

namespace LibreccioWeather.Core.Models;

/// <summary>
/// Everything known about the weather at a location.
/// </summary>
public sealed record Weather
{
    public Base Base { get; init; } = new();

    public Current? Current { get; init; }

    public IReadOnlyList<Daily> DailyForecast { get; init; } = [];

    public IReadOnlyList<Hourly> HourlyForecast { get; init; } = [];

    public IReadOnlyList<Minutely> MinutelyForecast { get; init; } = [];

    public IReadOnlyList<Alert> AlertList { get; init; } = [];

    public IReadOnlyDictionary<Month, Normals> Normals { get; init; } = new Dictionary<Month, Normals>();

    // The next hours, today and the current alerts are measured from when the weather was built,
    // so they stay the same while it is shown, even across midnight.
    private readonly DateTimeOffset _builtAt = DateTimeOffset.UtcNow;

    public Weather()
    {
    }

    // A copy made with "with" is a new weather, so it takes a new build time.
    private Weather(Weather original)
    {
        Base = original.Base;
        Current = original.Current;
        DailyForecast = original.DailyForecast;
        HourlyForecast = original.HourlyForecast;
        MinutelyForecast = original.MinutelyForecast;
        AlertList = original.AlertList;
        Normals = original.Normals;
        _builtAt = DateTimeOffset.UtcNow;
    }

    // The build time is left out, so two weathers with the same data are equal.
    public bool Equals(Weather? other)
    {
        return ReferenceEquals(this, other)
            || other is not null
            && Equals(Base, other.Base)
            && Equals(Current, other.Current)
            && Equals(DailyForecast, other.DailyForecast)
            && Equals(HourlyForecast, other.HourlyForecast)
            && Equals(MinutelyForecast, other.MinutelyForecast)
            && Equals(AlertList, other.AlertList)
            && Equals(Normals, other.Normals);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Base, Current, DailyForecast, HourlyForecast, MinutelyForecast, AlertList, Normals);
    }

    /// <summary>
    /// Hours from the one in progress to 24 hours ahead. At 15:59, the first hour is the one starting at 15:00.
    /// </summary>
    public IReadOnlyList<Hourly> NextHourlyForecast
    {
        get
        {
            return HourlyForecast.Where(hourly => hourly.Date >= _builtAt.AddHours(-1) && hourly.Date < _builtAt.AddHours(24)).ToList();
        }
    }

    /// <summary>
    /// Hours from the one in progress to the end of the forecast.
    /// </summary>
    public IReadOnlyList<Hourly> FullNextHourlyForecast
    {
        get
        {
            return HourlyForecast.Where(hourly => hourly.Date >= _builtAt.AddHours(-1)).ToList();
        }
    }

    // A day starts at midnight, so today is the first day that started less than 24 hours ago.
    public int? TodayIndex
    {
        get
        {
            DateTimeOffset dayAgo = _builtAt.AddDays(-1);
            for (int i = 0; i < DailyForecast.Count; i++)
            {
                if (DailyForecast[i].Date > dayAgo)
                {
                    return i;
                }
            }

            return null;
        }
    }

    public Daily? Today => TodayIndex is { } index ? DailyForecast[index] : null;

    public Daily? Tomorrow
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return DailyForecast.FirstOrDefault(daily => daily.Date > now);
        }
    }

    public IReadOnlyList<Daily> DailyForecastStartingToday =>
        TodayIndex is { } index ? DailyForecast.Skip(index).ToList() : [];

    /// <summary>
    /// Alerts in effect now. An alert without a start date started in the past, and one without an end date has no end.
    /// </summary>
    public IReadOnlyList<Alert> CurrentAlertList
    {
        get
        {
            return AlertList.Where(alert => IsInEffect(alert, _builtAt)).ToList();
        }
    }

    /// <summary>
    /// The minutely forecast with every interval at 5 minutes.
    /// One-minute intervals are averaged in groups of five, and longer intervals are split.
    /// </summary>
    public IReadOnlyList<Minutely> MinutelyForecastBy5Minutes
    {
        get
        {
            if (MinutelyForecast.All(minutely => minutely.MinuteInterval == 5))
            {
                return MinutelyForecast;
            }

            List<Minutely> result = [];

            // A forecast with any one-minute interval is assumed to use one-minute intervals throughout.
            if (MinutelyForecast.Any(minutely => minutely.MinuteInterval == 1))
            {
                for (int i = 0; i < MinutelyForecast.Count; i += 5)
                {
                    double average = Enumerable.Range(i, 5)
                        .Select(j => j < MinutelyForecast.Count
                            ? MinutelyForecast[j].PrecipitationIntensity?.InMicrometers ?? 0.0
                            : 0.0)
                        .Average();
                    result.Add(MinutelyForecast[i] with
                    {
                        PrecipitationIntensity = Units.Precipitation.FromMicrometers(average),
                        MinuteInterval = 5,
                    });
                }

                return result;
            }

            // Other intervals are assumed to be multiples of 5 minutes.
            foreach (Minutely minutely in MinutelyForecast)
            {
                if (minutely.MinuteInterval == 5)
                {
                    result.Add(minutely);
                    continue;
                }

                for (int j = 0; j < minutely.MinuteInterval / 5; j++)
                {
                    result.Add(minutely with { Date = minutely.Date.AddMinutes(j * 5), MinuteInterval = 5 });
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Whether the weather was refreshed within the polling interval. Always true without an interval.
    /// </summary>
    public bool IsValid(TimeSpan? pollingInterval)
    {
        if (pollingInterval is not { } interval)
        {
            return true;
        }

        DateTimeOffset updateTime = Base.RefreshTime ?? DateTimeOffset.UnixEpoch;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return now >= updateTime && now - updateTime < interval;
    }

    public WeatherWrapper ToWeatherWrapper()
    {
        return new WeatherWrapper
        {
            Current = Current?.ToCurrentWrapper(),
            DailyForecast = DailyForecast.Select(daily => daily.ToDailyWrapper()).ToList(),
            HourlyForecast = HourlyForecast.Select(hourly => hourly.ToHourlyWrapper()).ToList(),
            MinutelyForecast = MinutelyForecast,
            AlertList = AlertList,
            Normals = Normals,
        };
    }

    public IReadOnlyList<DailyWrapper> ToDailyWrapperList(DateTimeOffset startDate)
    {
        return DailyForecast
            .Where(daily => daily.Date >= startDate)
            .Select(daily => daily.ToDailyWrapper())
            .ToList();
    }

    public IReadOnlyList<HourlyWrapper> ToHourlyWrapperList(DateTimeOffset startDate)
    {
        return HourlyForecast
            .Where(hourly => hourly.Date >= startDate)
            .Select(hourly => hourly.ToHourlyWrapper())
            .ToList();
    }

    public AirQualityWrapper? ToAirQualityWrapperList(DateTimeOffset startDate)
    {
        List<Hourly> hourlyAirQuality = HourlyForecast
            .Where(hourly => hourly.Date >= startDate && hourly.AirQuality?.IsValid == true)
            .ToList();
        List<Daily> dailyAirQuality = DailyForecast
            .Where(daily => daily.Date >= startDate && daily.AirQuality?.IsValid == true)
            .ToList();
        if (hourlyAirQuality.Count == 0 && dailyAirQuality.Count == 0)
        {
            return null;
        }

        return new AirQualityWrapper
        {
            Current = Current?.AirQuality,
            DailyForecast = ByDate(dailyAirQuality, daily => daily.Date, daily => daily.AirQuality!),
            HourlyForecast = ByDate(hourlyAirQuality, hourly => hourly.Date, hourly => hourly.AirQuality!),
        };
    }

    public PollenWrapper? ToPollenWrapperList(DateTimeOffset startDate)
    {
        List<Daily> dailyPollen = DailyForecast
            .Where(daily => daily.Date >= startDate && daily.Pollen?.IsValid == true)
            .ToList();
        if (dailyPollen.Count == 0)
        {
            return null;
        }

        return new PollenWrapper
        {
            DailyForecast = ByDate(dailyPollen, daily => daily.Date, daily => daily.Pollen!),
        };
    }

    /// <summary>
    /// The minutely forecast from now on, or null when it has three intervals or fewer.
    /// </summary>
    public IReadOnlyList<Minutely>? ToMinutelyWrapper()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<Minutely> upcoming = MinutelyForecast.Where(minutely => minutely.Date >= now).ToList();
        return upcoming.Count > 3 ? upcoming : null;
    }

    /// <summary>
    /// Alerts that have not ended yet.
    /// </summary>
    public IReadOnlyList<Alert> ToAlertsWrapper()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return AlertList.Where(alert => alert.EndDate is null || alert.EndDate > now).ToList();
    }

    private static bool IsInEffect(Alert alert, DateTimeOffset now)
    {
        if (alert.StartDate is { } start && alert.EndDate is { } end)
        {
            return now >= start && now <= end;
        }

        if (alert.EndDate is { } endOnly)
        {
            return now < endOnly;
        }

        if (alert.StartDate is { } startOnly)
        {
            return now > startOnly;
        }

        return true;
    }

    // When two items share a date, the later one is kept.
    private static Dictionary<DateTimeOffset, TValue> ByDate<TItem, TValue>(
        IEnumerable<TItem> items,
        Func<TItem, DateTimeOffset> date,
        Func<TItem, TValue> value)
    {
        Dictionary<DateTimeOffset, TValue> result = [];
        foreach (TItem item in items)
        {
            result[date(item)] = value(item);
        }

        return result;
    }
}
