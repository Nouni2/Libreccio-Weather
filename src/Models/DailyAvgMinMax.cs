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

namespace LibreccioWeather.Core.Models;

/// <summary>
/// Average, highest and lowest values of a quantity over a day.
/// </summary>
public interface IDailyAvgMinMax<T>
    where T : struct
{
    T? Average { get; }

    T? Max { get; }

    T? Min { get; }

    string? Summary { get; }
}

public sealed record DailyCloudCover : IDailyAvgMinMax<Ratio>
{
    public Ratio? Average { get; init; }

    public Ratio? Max { get; init; }

    public Ratio? Min { get; init; }

    public string? Summary { get; init; }
}

public sealed record DailyDewPoint : IDailyAvgMinMax<Units.Temperature>
{
    public Units.Temperature? Average { get; init; }

    public Units.Temperature? Max { get; init; }

    public Units.Temperature? Min { get; init; }

    public string? Summary { get; init; }
}

public sealed record DailyPressure : IDailyAvgMinMax<Pressure>
{
    public Pressure? Average { get; init; }

    public Pressure? Max { get; init; }

    public Pressure? Min { get; init; }

    public string? Summary { get; init; }
}

public sealed record DailyRelativeHumidity : IDailyAvgMinMax<Ratio>
{
    public Ratio? Average { get; init; }

    public Ratio? Max { get; init; }

    public Ratio? Min { get; init; }

    public string? Summary { get; init; }
}

public sealed record DailyVisibility : IDailyAvgMinMax<Distance>
{
    public Distance? Average { get; init; }

    public Distance? Max { get; init; }

    public Distance? Min { get; init; }

    public string? Summary { get; init; }
}
