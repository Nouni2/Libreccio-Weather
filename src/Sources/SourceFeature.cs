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

namespace LibreccioWeather.Core.Sources;

/// <summary>
/// A kind of data that each location can take from a different source.
/// </summary>
public enum SourceFeature
{
    Forecast,
    Current,
    AirQuality,
    Pollen,
    Minutely,
    Alert,
    Normals,
    ReverseGeocoding,
}

public static class SourceFeatureExtensions
{
    extension(SourceFeature feature)
    {
        /// <summary>
        /// Identifier used when the feature is stored.
        /// </summary>
        public string Id => feature switch
        {
            SourceFeature.Forecast => "forecast",
            SourceFeature.Current => "current",
            SourceFeature.AirQuality => "airQuality",
            SourceFeature.Pollen => "pollen",
            SourceFeature.Minutely => "minutely",
            SourceFeature.Alert => "alert",
            SourceFeature.Normals => "normals",
            SourceFeature.ReverseGeocoding => "reverseGeocoding",
            _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null),
        };
    }

    extension(SourceFeature)
    {
        public static SourceFeature? GetInstance(string value)
        {
            foreach (SourceFeature feature in Enum.GetValues<SourceFeature>())
            {
                if (feature.Id == value)
                {
                    return feature;
                }
            }

            return null;
        }
    }
}
