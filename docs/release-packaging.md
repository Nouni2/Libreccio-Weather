# Release packaging

`0.1.0-alpha1` has two Windows x64 package configurations: a standalone executable and an MSI installer. Both include the .NET runtime. The MSI installs the same standalone executable for the current user and creates a Start menu shortcut.

## Standalone executable

From the repository root, publish with the checked-in profile:

```powershell
dotnet publish .\Libreccio-Weather.csproj --configuration Release "-p:PublishProfileFullPath=$PWD\packaging\portable-win-x64.pubxml"
```

The profile compresses the bundled assemblies and includes the native libraries, translations and licence documents. It rejects a publish that leaves additional files beside the executable. Trimming and ReadyToRun are disabled.

The release executable is `build/release/v0.1.0-alpha1/Libreccio-Weather-v0.1.0-alpha1-win-x64.exe`. The installer input is `build/publish/v0.1.0-alpha1/win-x64/Libreccio-Weather.exe`. Paths derive from `Version` in `Directory.Build.props`.

The download is one executable. On launch, .NET extracts its bundled files into its per-user `%TEMP%\.net` cache. Settings and saved weather remain in `%LOCALAPPDATA%\Libreccio Weather\`. The executable does not keep user data beside itself.

## MSI installer

The installer project uses WiX Toolset 7.0.1. Review its [EULA and maintenance-fee terms](https://docs.firegiant.com/wix/osmf/) before accepting them. After accepting those terms, build the installer from the repository root:

```powershell
dotnet build .\Libreccio-Weather.Installer.wixproj --configuration Release -p:AcceptEula=wix7
```

The MSI is `build/release/v0.1.0-alpha1/en-US/Libreccio-Weather-v0.1.0-alpha1-win-x64.msi`. Only the MSI is distributed; the WiX debugging files are build output. The installer embeds its cabinet, so it needs no separate payload files.

Installation uses `%LOCALAPPDATA%\Programs\Libreccio Weather\` and the current user's Start menu. Uninstall removes the installed program and shortcut without removing saved weather or settings. Exit the app through its tray menu before installing an update.

The MSI derives its numeric version from the release version without its prerelease suffix. Prereleases sharing that numeric version replace each other; MSI does not order `alpha1` and `alpha2`. A larger numeric version blocks installation of a smaller one.

The installer is built separately from the application solution because it consumes the published executable.

## Validation and release assets

Copy the standalone executable alone into another directory, launch it, and check fonts, French and English text, forecasts, Mistray and the sun and moon cards. On a clean Windows x64 system, verify that neither package needs a separate .NET installation. Check MSI install, Start menu launch, uninstall and reinstall; confirm that saved locations survive uninstall.

Attach the versioned EXE and MSI to the existing `v0.1.0-alpha1` release tag after validation. Keep the tag's target consistent with the committed release source.

Single-file extraction and compression follow [Microsoft's deployment documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview). Installer authoring follows the [WiX MSBuild documentation](https://docs.firegiant.com/wix/tools/msbuild/).
