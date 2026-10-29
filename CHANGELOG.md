# Changelog

All notable changes to Libreccio Weather are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- README describing the project, its planned features, building from source, credits and licence.
- GNU General Public License v3.0 in `LICENCE.md`.
- `AGENTS.md` with the rules for contributors: comments, licence header, changelog, naming, folder layout and code organization, pointing to `CONTRIBUTING.md` for code style.
- `CONTRIBUTING.md` explaining how to report bugs, when to discuss changes first, how to set up, build and test, the pull request checklist, the code style, and the licence of contributions.
- `Libreccio-Weather.slnx` solution with three projects, each defined by a project file in the root:
  - `Libreccio-Weather.Core`, the engine library, built from `src/`.
  - `Libreccio-Weather`, the WPF application, built from `app/`, with an empty main window.
  - `Libreccio-Weather.Core.Tests`, the xUnit test project, built from `tests/`.
- `global.json` pinning the .NET 10 SDK and selecting Microsoft.Testing.Platform for `dotnet test`.
- `Directory.Build.props` with shared build settings: .NET 10 for Windows, nullable reference types, implicit usings, warnings treated as errors, and all build output sent to `build/`.
- `Directory.Packages.props` with central NuGet package versions.
- `.editorconfig` with the standard .NET code style: encoding, CRLF line endings, indentation, file-scoped namespaces, braces, `var` usage and naming rules.
- `.gitattributes` normalizing line endings, with LF kept for Bash scripts.
- `.gitignore`, which also ignores the `build/` output folder.
- This changelog.
