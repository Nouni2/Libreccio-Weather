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

using System.Text.RegularExpressions;
using LibreccioWeather.Core.Models;

namespace LibreccioWeather.Core.Sources.Nws;

public sealed partial class NwsService
{
    private static readonly Regex WrappedLines = new(@"([0-9A-Za-z.,]) *\n([0-9A-Za-z])", RegexOptions.CultureInvariant);

    private static IReadOnlyList<Alert>? GetAlerts(NwsAlert[]? alerts)
    {
        if (alerts is not { Length: > 0 })
        {
            return null;
        }

        List<Alert> result = [];
        foreach (NwsAlert alert in alerts)
        {
            if (alert.Properties is not { } properties || properties.Status?.Contains("test", StringComparison.OrdinalIgnoreCase) == true)
            {
                continue;
            }

            AlertSeverity severity = properties.Severity?.ToLowerInvariant() switch
            {
                "extreme" => AlertSeverity.Extreme,
                "severe" => AlertSeverity.Severe,
                "moderate" => AlertSeverity.Moderate,
                "minor" => AlertSeverity.Minor,
                _ => AlertSeverity.Unknown,
            };
            result.Add(new Alert
            {
                AlertId = properties.Id,
                StartDate = properties.Onset,
                EndDate = properties.Ends ?? properties.Expires,
                Headline = properties.Event ?? properties.Headline,
                Description = properties.Description is { } description ? WrappedLines.Replace(description, "$1 $2") : null,
                Instruction = properties.Instruction is { } instruction ? WrappedLines.Replace(instruction, "$1 $2") : null,
                Source = !string.IsNullOrEmpty(properties.SenderName) ? properties.SenderName : properties.Sender ?? "NWS",
                Severity = severity,
                Color = GetAlertColor(properties.Event) ?? Alert.ColorFromSeverity(severity),
            });
        }

        return result;
    }

    private static int? GetAlertColor(string? weatherEvent)
    {
        return weatherEvent switch
        {
            "Tsunami Warning" => Rgb(253, 99, 71),
            "Tornado Warning" => Rgb(255, 0, 0),
            "Extreme Wind Warning" => Rgb(255, 140, 0),
            "Severe Thunderstorm Warning" => Rgb(255, 165, 0),
            "Flash Flood Warning" => Rgb(139, 0, 0),
            "Flash Flood Statement" => Rgb(139, 0, 0),
            "Severe Weather Statement" => Rgb(0, 255, 255),
            "Shelter In Place Warning" => Rgb(250, 128, 114),
            "Evacuation Immediate" => Rgb(127, 255, 0),
            "Civil Danger Warning" => Rgb(255, 182, 193),
            "Nuclear Power Plant Warning" => Rgb(75, 0, 130),
            "Radiological Hazard Warning" => Rgb(75, 0, 130),
            "Hazardous Materials Warning" => Rgb(75, 0, 130),
            "Fire Warning" => Rgb(160, 82, 45),
            "Civil Emergency Message" => Rgb(255, 182, 193),
            "Law Enforcement Warning" => Rgb(192, 192, 192),
            "Storm Surge Warning" => Rgb(181, 36, 247),
            "Hurricane Force Wind Warning" => Rgb(205, 92, 92),
            "Hurricane Warning" => Rgb(220, 20, 60),
            "Typhoon Warning" => Rgb(220, 20, 60),
            "Special Marine Warning" => Rgb(255, 165, 0),
            "Blizzard Warning" => Rgb(255, 69, 0),
            "Snow Squall Warning" => Rgb(199, 21, 133),
            "Ice Storm Warning" => Rgb(139, 0, 139),
            "Heavy Freezing Spray Warning" => Rgb(0, 191, 255),
            "Winter Storm Warning" => Rgb(255, 105, 180),
            "Lake Effect Snow Warning" => Rgb(0, 139, 139),
            "Dust Storm Warning" => Rgb(255, 228, 196),
            "Blowing Dust Warning" => Rgb(255, 228, 196),
            "High Wind Warning" => Rgb(218, 165, 32),
            "Tropical Storm Warning" => Rgb(178, 34, 34),
            "Storm Warning" => Rgb(148, 0, 211),
            "Tsunami Advisory" => Rgb(210, 105, 30),
            "Tsunami Watch" => Rgb(255, 0, 255),
            "Avalanche Warning" => Rgb(30, 144, 255),
            "Earthquake Warning" => Rgb(139, 69, 19),
            "Volcano Warning" => Rgb(47, 79, 79),
            "Ashfall Warning" => Rgb(169, 169, 169),
            "Flood Warning" => Rgb(0, 255, 0),
            "Coastal Flood Warning" => Rgb(34, 139, 34),
            "Lakeshore Flood Warning" => Rgb(34, 139, 34),
            "Ashfall Advisory" => Rgb(105, 105, 105),
            "High Surf Warning" => Rgb(34, 139, 34),
            "Extreme Heat Warning" => Rgb(199, 21, 133),
            "Tornado Watch" => Rgb(255, 255, 0),
            "Severe Thunderstorm Watch" => Rgb(219, 112, 147),
            "Flash Flood Watch" => Rgb(46, 139, 87),
            "Gale Warning" => Rgb(221, 160, 221),
            "Flood Statement" => Rgb(0, 255, 0),
            "Extreme Cold Warning" => Rgb(0, 0, 255),
            "Freeze Warning" => Rgb(72, 61, 139),
            "Red Flag Warning" => Rgb(255, 20, 147),
            "Storm Surge Watch" => Rgb(219, 127, 247),
            "Hurricane Watch" => Rgb(255, 0, 255),
            "Hurricane Force Wind Watch" => Rgb(153, 50, 204),
            "Typhoon Watch" => Rgb(255, 0, 255),
            "Tropical Storm Watch" => Rgb(240, 128, 128),
            "Storm Watch" => Rgb(255, 228, 181),
            "Tropical Cyclone Local Statement" => Rgb(255, 228, 181),
            "Winter Weather Advisory" => Rgb(123, 104, 238),
            "Avalanche Advisory" => Rgb(205, 133, 63),
            "Cold Weather Advisory" => Rgb(175, 238, 238),
            "Heat Advisory" => Rgb(255, 127, 80),
            "Flood Advisory" => Rgb(0, 255, 127),
            "Coastal Flood Advisory" => Rgb(124, 252, 0),
            "Lakeshore Flood Advisory" => Rgb(124, 252, 0),
            "High Surf Advisory" => Rgb(186, 85, 211),
            "Dense Fog Advisory" => Rgb(112, 128, 144),
            "Dense Smoke Advisory" => Rgb(240, 230, 140),
            "Small Craft Advisory" => Rgb(216, 191, 216),
            "Brisk Wind Advisory" => Rgb(216, 191, 216),
            "Hazardous Seas Warning" => Rgb(216, 191, 216),
            "Dust Advisory" => Rgb(189, 183, 107),
            "Blowing Dust Advisory" => Rgb(189, 183, 107),
            "Lake Wind Advisory" => Rgb(210, 180, 140),
            "Wind Advisory" => Rgb(210, 180, 140),
            "Frost Advisory" => Rgb(100, 149, 237),
            "Freezing Fog Advisory" => Rgb(0, 128, 128),
            "Freezing Spray Advisory" => Rgb(0, 191, 255),
            "Low Water Advisory" => Rgb(165, 42, 42),
            "Local Area Emergency" => Rgb(192, 192, 192),
            "Winter Storm Watch" => Rgb(70, 130, 180),
            "Rip Current Statement" => Rgb(64, 224, 208),
            "Beach Hazards Statement" => Rgb(64, 224, 208),
            "Gale Watch" => Rgb(255, 192, 203),
            "Avalanche Watch" => Rgb(244, 164, 96),
            "Hazardous Seas Watch" => Rgb(72, 61, 139),
            "Heavy Freezing Spray Watch" => Rgb(188, 143, 143),
            "Flood Watch" => Rgb(46, 139, 87),
            "Coastal Flood Watch" => Rgb(102, 205, 170),
            "Lakeshore Flood Watch" => Rgb(102, 205, 170),
            "High Wind Watch" => Rgb(184, 134, 11),
            "Extreme Heat Watch" => Rgb(128, 0, 0),
            "Extreme Cold Watch" => Rgb(0, 0, 255),
            "Freeze Watch" => Rgb(0, 255, 255),
            "Fire Weather Watch" => Rgb(255, 222, 173),
            "Extreme Fire Danger" => Rgb(233, 150, 122),
            "911 Telephone Outage" => Rgb(192, 192, 192),
            "Coastal Flood Statement" => Rgb(107, 142, 35),
            "Lakeshore Flood Statement" => Rgb(107, 142, 35),
            "Special Weather Statement" => Rgb(255, 228, 181),
            "Marine Weather Statement" => Rgb(255, 239, 213),
            "Air Quality Alert" => Rgb(128, 128, 128),
            "Air Stagnation Advisory" => Rgb(128, 128, 128),
            "Hazardous Weather Outlook" => Rgb(238, 232, 170),
            "Hydrologic Outlook" => Rgb(144, 238, 144),
            "Short Term Forecast" => Rgb(152, 251, 152),
            "Administrative Message" => Rgb(192, 192, 192),
            "Test" => Rgb(240, 255, 255),
            "Child Abduction Emergency" => Rgb(255, 255, 255),
            "Blue Alert" => Rgb(255, 255, 255),
            _ => null,
        };
    }

    private static int Rgb(int red, int green, int blue)
    {
        return unchecked((int)0xFF000000) | (red << 16) | (green << 8) | blue;
    }
}
