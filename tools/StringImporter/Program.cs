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

using LibreccioWeather.Core.Localization;
using LibreccioWeather.StringImporter;

// Run from the repository root: dotnet run --project Libreccio-Weather.StringImporter.csproj
// Paths are relative to the current directory, so the upstream clone must be in upstream/.
(string Name, string ResourceFolder, string[] Files)[] sets =
[
    ("Strings", Path.Combine("upstream", "app", "src", "main", "res"), ["strings.xml", "plurals.xml"]),
    ("UnitStrings", Path.Combine("upstream", "weather-unit", "src", "main", "res"), ["strings.xml"]),
];

foreach ((string name, string resourceFolder, string[] files) in sets)
{
    if (!Directory.Exists(resourceFolder))
    {
        Console.Error.WriteLine($"Missing folder: {resourceFolder}. Run from the repository root with upstream/ cloned.");
        return 1;
    }

    Dictionary<string, string> defaults = AndroidStrings.Read(files.Select(file => Path.Combine(resourceFolder, "values", file)));
    AndroidStrings.ResolveReferences(defaults, defaults);
    ResxFile.Write(Path.Combine("translations", name + ".resx"), defaults);
    Console.WriteLine($"{name}: {defaults.Count} strings in {AppLanguages.Default}");

    foreach (string language in AppLanguages.Codes.Where(code => code != AppLanguages.Default))
    {
        string folder = Path.Combine(resourceFolder, AndroidFolderName(language));
        Dictionary<string, string> translated = AndroidStrings.Read(files.Select(file => Path.Combine(folder, file)));
        AndroidStrings.ResolveReferences(translated, defaults);
        ResxFile.Write(Path.Combine("translations", $"{name}.{language}.resx"), translated);
        Console.WriteLine($"{name}: {translated.Count} strings in {language}");
    }
}

return 0;

// Android writes regional variants with an "r" before the region, such as values-pt-rBR for pt-BR.
static string AndroidFolderName(string languageCode)
{
    string[] parts = languageCode.Split('-');
    return parts.Length == 2 ? $"values-{parts[0]}-r{parts[1]}" : $"values-{languageCode}";
}
