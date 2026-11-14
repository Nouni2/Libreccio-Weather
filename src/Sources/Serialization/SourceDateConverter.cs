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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LibreccioWeather.Core.Sources.Serialization;

internal sealed class SourceDateConverter : JsonConverter<DateTimeOffset>
{
    private static readonly Regex DatePattern = new(
        "^(?<year>[0-9]{4})-?(?<month>[0-9]{2})-?(?<day>[0-9]{2})(?:T(?<hour>[0-9]{2}):?(?<minute>[0-9]{2}):?(?:(?<second>[0-9]{2})(?:[.](?<fraction>[0-9]+))?)?)?(?:(?<zone>Z).*|(?<zone>[+-][0-9]{2}(?::?[0-9]{2})?))?\\z",
        RegexOptions.CultureInvariant | RegexOptions.Singleline);

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || reader.GetString() is not { } text)
        {
            throw new JsonException("Expected an ISO 8601 date string.");
        }

        return Parse(text);
    }

    internal static DateTimeOffset Parse(string text)
    {
        Match match = DatePattern.Match(text);
        if (!match.Success)
        {
            throw new JsonException("Invalid ISO 8601 date.");
        }

        int ReadNumber(string name) => match.Groups[name].Success
            ? int.Parse(match.Groups[name].Value, NumberStyles.None, CultureInfo.InvariantCulture) : 0;
        try
        {
            int seconds = ReadNumber("second");
            if (seconds is >= 60 and <= 62)
            {
                seconds = 59;
            }

            string fraction = match.Groups["fraction"].Value;
            int milliseconds = fraction.Length == 0 ? 0
                : int.Parse(fraction[..Math.Min(3, fraction.Length)].PadRight(3, '0'), NumberStyles.None, CultureInfo.InvariantCulture);
            DateTime local = new(ReadNumber("year"), ReadNumber("month"), ReadNumber("day"),
                ReadNumber("hour"), ReadNumber("minute"), seconds, milliseconds, DateTimeKind.Unspecified);
            string zone = match.Groups["zone"].Value;
            if (zone.Length == 0)
            {
                if (match.Groups["hour"].Success)
                {
                    throw new JsonException("A date with a time requires a time zone.");
                }

                return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local));
            }

            int offset = 0;
            if (zone != "Z")
            {
                string digits = zone[1..].Replace(":", "", StringComparison.Ordinal);
                int hours = int.Parse(digits[..2], NumberStyles.None, CultureInfo.InvariantCulture);
                int minutes = digits.Length == 2 ? 0 : int.Parse(digits[2..], NumberStyles.None, CultureInfo.InvariantCulture);
                if (hours > 23 || minutes > 59)
                {
                    throw new JsonException("Invalid time zone offset.");
                }

                offset = (hours * 60 + minutes) * (zone[0] == '-' ? -1 : 1);
            }

            return new DateTimeOffset(DateTime.SpecifyKind(local.AddMinutes(-offset), DateTimeKind.Utc));
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Invalid ISO 8601 date.", exception);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
    }
}
