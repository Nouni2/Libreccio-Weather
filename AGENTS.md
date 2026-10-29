# AGENTS

Libreccio Weather is a native Windows weather app written in C# with .NET 10 and WPF.

## Fidelity to upstream

Libreccio Weather ports Breezy Weather, whose source is cloned into `upstream/`. Ported code behaves exactly like the code it ports. Compiling, passing the tests and following the rules in this file do not show that it does.

- Read the upstream code before porting it. Keep its logic, its order and its names, so the two can be compared side by side.
- Port upstream's tests for the code being ported along with it.
- When a Kotlin, Java or Android call becomes a .NET call, check that both behave the same on edge cases: halves, negative numbers, zero, `NaN`, `null`, empty collections, time zones and culture. [docs/porting-pitfalls.md](docs/porting-pitfalls.md) lists the calls known to differ, and a newly found one is added to it.
- Where .NET behaves differently, reproduce upstream's behaviour and pin it with a test of the edge case.
- Record in [docs/port-map.md](docs/port-map.md) which upstream file each C# file ports, with any deliberate difference from upstream. Comments still never mention origin.
- Report a difference that cannot be reproduced instead of choosing a behaviour silently.

## Comments

A comment explains the code next to it and nothing else. The code must not reveal who or what wrote it.

- Explain why the code does something it cannot show by itself: a constraint, an assumption, a workaround, non-obvious behaviour, or what breaks if it changes.
- Do not restate what the code plainly does. Ordinary code needs no comment.
- Never write history, origin, versions, tags, dates, task notes, text addressed to a person, or the name of any tool, AI or agent.
- Keep each comment next to the code it explains and update it when that code changes.
- These rules apply to every file: C#, XAML, configuration, project files, `.gitignore`.

```csharp
// Good: explains a non-obvious constraint.
// Open-Meteo returns 15-minute precipitation in local time, so it is converted to UTC before merging.

// Bad: history and origin.
// Ported from RefreshHelper.kt, previously used a fixed interval.

// Bad: restates the code.
// Increment the counter.
```

## Licence header

Every source file starts with this header, written in the file's comment syntax:

```csharp
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
```

## Code style

Follow the code style in `CONTRIBUTING.md`.

## Text and translations

The code and its default text are in English. The app is translated through resource files.

- Never write user-visible text in code or XAML. Read it with `Strings` in code and `{l:Translate key}` in XAML.
- `translations/Strings.resx` holds the English default, and each other language has its own `Strings.xx.resx`, such as `Strings.fr.resx`. Unit text is in `UnitStrings.resx` the same way.
- The files in `translations/` are generated from upstream by the string importer, so never edit them by hand. Keys keep upstream's names.
- A language ships once its code is in `AppLanguages.Codes`; the importer then generates its files.
- A string missing from a language falls back to English.
- Plurals follow the CLDR plural rules of each language.
- Format numbers and dates with the culture of the selected language.
- Storage identifiers, logs and exception messages are not translated and stay in English.

## Changelog

Every commit updates `CHANGELOG.md`, which follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

- Add each change under `## [Unreleased]`, in the matching group: Added, Changed, Deprecated, Removed, Fixed or Security.
- Describe what changed for someone using or building the project, in more detail than the commit message.
- At a release, move the `Unreleased` entries under a new version heading with the release date.

## Naming

- The project is `Libreccio-Weather` everywhere: solution, projects, assemblies. Never use placeholder names.
- Single-word Markdown files are all caps with a lowercase extension: `README.md`, `AGENTS.md`, `LICENCE.md`.
- Multi-word Markdown files use hyphens.

## Layout

| Location | Holds |
|---|---|
| Root | Project-wide files only: README, `.gitignore`, solution file, project files (`.csproj`), `global.json`, `.editorconfig`, `.gitattributes`, `Directory.Build.props`, `Directory.Packages.props`. Never implementation code. Except for an optional entry point file (main / index etc.) |
| `src/` | The machinery: models, weather sources, merging, calculations, units, persistence, settings. Code files only. |
| `app/` | The application layer: windows, tray icon and popup, view models, startup composition. Code files only. |
| `tests/` | Automated tests of the machinery. Code files only. |
| `build/` | Build output from every project. Generated and git-ignored. |
| `translations/` | The `.resx` files with the app's text in each shipped language, generated by the string importer in `tools/`. |
| `tools/` | Programs that work on the project itself, such as generators and converters. |
| `scripts/` | Reserved for Bash programs that perform operating-system actions. Never general helpers. |
| `docs/` | Long-form explanation that outgrows the README. |

- `src/` owns the machinery. `app/` builds on it and never the other way around.
- `src/`, `app/` and `tests/` hold code only, in folders named after their purpose. Project files stay in the root and build output goes to `build/`.
- Name any other folder after its responsibility.
- Do not create a folder that would stay empty once the project is populated.

## Code organization

- Give every responsibility a named home. Instead of `Utils`, `Helpers` or `Misc`, name the folder or class after what it does.
- Move code to a shared location only once two places actually use it.
- Entry points assemble, configure and dispatch. Put the behaviour they start elsewhere.
- Create an interface, layer or abstraction only for a real boundary, a contract something depends on, or duplication that already exists. Write the concrete class directly otherwise.
