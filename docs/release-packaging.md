# Release packaging

`0.1.0-alpha1` is distributed as a standalone Windows x64 executable with the .NET runtime included.

## Standalone executable

From the repository root, publish with the checked-in profile:

```powershell
dotnet publish .\Libreccio-Weather.csproj --configuration Release "-p:PublishProfileFullPath=$PWD\packaging\portable-win-x64.pubxml"
```

The profile compresses the bundled assemblies and includes the native libraries, translations and licence documents. It rejects a publish that leaves additional files beside the executable. Trimming and ReadyToRun are disabled.

The release executable is `build/release/v0.1.0-alpha1/Libreccio-Weather-v0.1.0-alpha1-win-x64.exe`. The publish output is `build/publish/v0.1.0-alpha1/win-x64/Libreccio-Weather.exe`. Paths derive from `Version` in `Directory.Build.props`.

The download is one executable. On launch, .NET extracts its bundled files into its per-user `%TEMP%\.net` cache. Settings and saved weather remain in `%LOCALAPPDATA%\Libreccio Weather\`. The executable does not keep user data beside itself.

## Validation and release asset

Copy the standalone executable alone into another directory, launch it, and check fonts, French and English text, forecasts, Mistray and the sun and moon cards. On a clean Windows x64 system, verify that it needs no separate .NET installation.

Attach `Libreccio-Weather-v0.1.0-alpha1-win-x64.exe` to the GitHub prerelease tagged `v0.1.0-alpha1`. Keep the tag's target consistent with the committed release source.

Single-file extraction and compression follow [Microsoft's deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).
