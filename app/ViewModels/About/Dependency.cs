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

namespace LibreccioWeather.ViewModels.About;

/// <summary>
/// A library, translation, font or set of artwork the app ships, as listed in the third-party notices and the package
/// versions. Names, authors and licence names are proper names and stay untranslated.
/// </summary>
public sealed record Dependency(string Name, string? Version, string Author, string Licence, string Link)
{
    public static IReadOnlyList<Dependency> All { get; } =
    [
        new("Breezy Weather", null, "Breezy Weather contributors", "LGPL-3.0", "https://github.com/breezy-weather/breezy-weather"),
        new("CommunityToolkit.Mvvm", "8.4.0", ".NET Foundation and Contributors", "MIT", "https://github.com/CommunityToolkit/dotnet"),
        new("commons-suncalc", "2.14", "Richard Körber", "Apache-2.0", "https://github.com/shred/commons-suncalc"),
        new("Geometric Weather", null, "Geometric Weather contributors", "LGPL-3.0", "https://github.com/WangDaYeeeeee/GeometricWeather"),
        new("Asap Condensed", null, "The Asap Project Authors", "OFL-1.1", "https://github.com/Omnibus-Type/Asap"),
        new("Microsoft.Data.Sqlite", "10.0.0", "Microsoft", "MIT", "https://github.com/dotnet/efcore"),
        new("Roboto", null, "Google", "Apache-2.0", "https://github.com/googlefonts/roboto-2"),
        new("Seasonal++", "1.1.0", "Nouni2", "GPL-3.0", "https://github.com/Nouni2/Seasonal-plus-plus"),
        new("SQLitePCLRaw.bundle_e_sqlite3", "2.1.12", "Eric Sink", "Apache-2.0", "https://github.com/ericsink/SQLitePCL.raw"),
        new("Unicode CLDR", null, "Unicode, Inc.", "Unicode-3.0", "https://cldr.unicode.org"),
    ];
}
