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
(string Name, string ResourceFolder, string[] Files, string[] ArrayFiles)[] sets =
[
    ("Strings", Path.Combine("upstream", "app", "src", "main", "res"), ["strings.xml", "plurals.xml"], ["arrays.xml"]),
    ("UnitStrings", Path.Combine("upstream", "weather-unit", "src", "main", "res"), ["strings.xml"], []),
];

foreach ((string name, string resourceFolder, string[] files, string[] arrayFiles) in sets)
{
    if (!Directory.Exists(resourceFolder))
    {
        Console.Error.WriteLine($"Missing folder: {resourceFolder}. Run from the repository root with upstream/ cloned.");
        return 1;
    }

    string defaultFolder = Path.Combine(resourceFolder, "values");
    Dictionary<string, string> defaults = AndroidStrings.Read(files.Select(file => Path.Combine(defaultFolder, file)));
    Dictionary<string, List<string>> defaultArrays = AndroidStrings.ReadArrays(arrayFiles.Select(file => Path.Combine(defaultFolder, file)));
    AndroidStrings.AddArrays(defaults, defaultArrays);

    // Strings that only point to another string, such as the untranslated "openmeteo_weather_text_clear_sky", are kept
    // as written so each language can resolve them against its own strings.
    Dictionary<string, string> defaultReferences = defaults
        .Where(pair => pair.Value.StartsWith("@string/", StringComparison.Ordinal))
        .ToDictionary(pair => pair.Key, pair => pair.Value);
    AndroidStrings.ResolveReferences(defaults, defaults);
    ResxFile.Write(Path.Combine("translations", name + ".resx"), defaults);
    Console.WriteLine($"{name}: {defaults.Count} strings in {AppLanguages.Default}");

    foreach (string language in AppLanguages.Codes.Where(code => code != AppLanguages.Default))
    {
        string folder = Path.Combine(resourceFolder, AndroidFolderName(language));
        Dictionary<string, string> translated = AndroidStrings.Read(files.Select(file => Path.Combine(folder, file)));

        // Android resolves the "@string/" items of an array in the current language, so an array the language does not
        // translate takes the default items and resolves them against this language's strings.
        Dictionary<string, List<string>> arrays = AndroidStrings.ReadArrays(arrayFiles.Select(file => Path.Combine(folder, file)));
        foreach ((string arrayName, List<string> items) in defaultArrays)
        {
            arrays.TryAdd(arrayName, items);
        }

        AndroidStrings.AddArrays(translated, arrays);

        // Android resolves a reference in the current language, so a reference the language does not define itself
        // points to this language's text, such as "Ciel dégagé", and falls back to English only when that is missing.
        foreach ((string key, string reference) in defaultReferences)
        {
            translated.TryAdd(key, reference);
        }

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
