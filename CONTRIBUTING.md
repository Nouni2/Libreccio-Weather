# Contributing

Thank you for your interest in helping with Libreccio Weather. This guide explains how to report problems, suggest changes and get code accepted.

Libreccio Weather is a port of [Breezy Weather](https://github.com/breezy-weather/breezy-weather). Its interface, features and behaviour follow Breezy Weather, so changes that make the app behave differently from it are not accepted. If you want the app to work differently, feel free to fork it.

## Ways to help

- Report a bug.
- Suggest a feature or point out where the app differs from Breezy Weather.
- Improve the code, tests or documentation.

## Reporting a bug

Open an [issue](https://github.com/Nouni2/Libreccio-Weather/issues) and include:

- What you did, step by step.
- What you expected to happen, and what happened instead.
- Your Windows version and the Libreccio Weather version.
- Screenshots or error messages, if any.

Search existing issues first, in case it has already been reported.

## Discuss before large changes

For anything beyond a small fix, open an issue describing the change before writing code. This confirms the change fits the project before you spend time on it.

## Development setup

Building requires Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
git clone https://github.com/Nouni2/Libreccio-Weather.git
cd Libreccio-Weather
dotnet build Libreccio-Weather.slnx
dotnet test --solution Libreccio-Weather.slnx
dotnet run --project Libreccio-Weather.csproj
```

The repository layout and the rules for comments, licence headers and naming are in [AGENTS.md](AGENTS.md).

## Pull requests

Before opening a pull request:

- The solution builds without warnings. Warnings are treated as errors.
- All tests pass, and new engine code comes with tests.
- The code follows the code style below and the rules in [AGENTS.md](AGENTS.md).
- Every new source file starts with the licence header.
- `CHANGELOG.md` describes the change under `Unreleased`.

When opening a pull request:

- Link the issue it resolves.
- Describe what changed and why.
- Keep it to one change. Split unrelated changes into separate pull requests.

## Code style

The code follows the standard .NET conventions. `.editorconfig` defines them and the editor applies them; `.gitattributes` keeps line endings consistent.

- UTF-8, CRLF line endings, a final newline, no trailing whitespace. Bash scripts use LF.
- Indentation is 4 spaces, and 2 spaces in project files, `.props`, `.slnx` and JSON.
- One file-scoped namespace per file (`namespace LibreccioWeather.Core;`), with `using` directives above it and `System` first.
- Braces always, each on its own line, including single-line `if` bodies.
- `var` only when the type is visible on the same line, such as `var list = new List<int>();`.
- Language keywords over type names (`int`, `string`), no `this.`, and explicit access modifiers.
- Naming: `PascalCase` for types, members, constants and non-private fields; `_camelCase` for private fields; `camelCase` for parameters and locals; interfaces start with `I`.
- Test methods are named `Method_Condition_Result`, such as `ToFahrenheit_ZeroCelsius_Returns32`.

## Licence

By contributing, you agree that your contributions are licensed under the [GNU General Public License v3.0](LICENCE.md).
