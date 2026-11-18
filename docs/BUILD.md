# Build Class Alerts

## Application

On Windows with .NET Framework 4.8 and Windows PowerShell:

```powershell
.\Build.ps1
```

Output: `App\ClassAlerts.exe`. No external NuGet dependencies are required. The script uses the .NET Framework compiler installed with Windows. Alternatively, build `Source\ClassAlerts.csproj` with an appropriate .NET SDK and .NET Framework 4.8 targeting pack.

Run automated checks with:

```powershell
Start-Process .\App\ClassAlerts.exe -ArgumentList '--self-test' -Wait
```

Read `App\test-results.txt`, `customisation-tests.txt`, `appearance-sort-tests.txt` and `warning-bounds-tests.txt`. A failed check returns exit code 1 and writes `test-error.txt`. `--render-test` generates PNG previews. Test-only data lives under `build\tests` (override with CLASSALERTS_TEST_ROOT).

## Per-user MSI

Download and extract the [official WiX Toolset 3.14.1 binaries](https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm). This project pins that build tool for its .NET Framework-based packaging scripts; it does not install WiX or add it to PATH.

```powershell
.\Build-Installer.ps1 -WixDirectory C:\Tools\wix314
.\Installer\Verify-Msi.ps1 -Path .\dist\ClassAlerts-1.3.1-User.msi
```

Output: `dist\ClassAlerts-1.3.1-User.msi`. Intermediate files: `build\installer`. Optional BuildDirectory and OutputDirectory parameters override these locations.

The MSI has perUser scope and limited privileges. It uses LocalAppDataFolder, HKCU registry entries and non-advertised per-user shortcuts. The runtime selects a separate user-data directory when installed.flag is present. Startup is opt-in, and an uninstall action clears only a startup value pointing to this executable.

Windows Installer ICE validation needs access to the Windows Installer service. Expected ICE91 notices state that LocalAppData files do not support a per-machine installation; that is intentional. Do not switch scope to per-machine to silence those notices.

Update Product.wxs Product/@Version and AssemblyInfo.cs together for a new release. Preserve the UpgradeCode and component GUID for compatible upgrades. No signing certificate is configured in this repository.

## Do not publish personal data

Do not commit App/UserData, actual timetables, custom sounds, build logs with user paths, or local test output. Only the generic CSV template and empty source config belong in the repository. Upload installers as release assets, not source files.

Optional audio integration tests: set CLASSALERTS_AUDIO_FIXTURES to a folder containing tone.wav, tone.mp3, tone.wma, tone.m4a and tone.aac before running --self-test. These should be short audio-only clips; they are copied and decoded without playing. Tests use a scratch data folder and do not change installed preferences.
