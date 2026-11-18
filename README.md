# Class Alerts

An always-on-top classroom countdown timer for Windows, by **Francisco Javier Vertedor Postigo**.

## Download and install

[Download Class Alerts 1.3.1 (per-user MSI)](https://github.com/javiervertedor/class-alerts-windows/raw/refs/heads/main/downloads/ClassAlerts-1.3.1-User.msi). [SHA-256 checksum](downloads/SHA256SUMS.txt). Run it using your normal Windows account. It installs for the current user, creates Start menu and desktop shortcuts, launches the app when you click Finish, and offers an optional **Start with Windows** checkbox on the final screen.

Requires Windows with **.NET Framework 4.8**. The installer does not request administrator privileges; organisational policy can still restrict MSI installation. This release is unsigned.

The timetable starts empty. Right-click the timer → **Settings** to enter your own lessons or import a CSV. Download the [CSV template](Templates/schedule-template.csv) and save edited spreadsheets as **CSV UTF-8**.

[Download ZIP with instructions, AI timetable-conversion prompt and sample CSV](https://github.com/javiervertedor/class-alerts-windows/raw/refs/heads/main/downloads/ClassAlerts-1.3.1-Package.zip).

## Features

- Round, bar and numeric views with transparent surroundings.
- Topmost overlay tested by the author over full-screen PowerPoint and browsers.
- Drag-to-position, adjustable size, click-through and off-screen recovery.
- Font family, bold, text sizes, text/fill/border colours and warning colour.
- Chinese and English lesson names, with CJK font fallback.
- Weekly timetable editor; chronological weekday and start-time sorting.
- CSV import with replace/append modes and overlap validation.
- Start, pre-end and end alerts; built-in sounds and a sound library with WAV/MP3/WMA/M4A/AAC import, rename/delete, and separate Start/Warning/End assignments.
- Named manual countdowns with pause/resume.
- Per-user installation, shortcuts and optional Windows sign-in startup.

Audio is checked using Windows media support; codec availability can vary. Files are copied into the local library, and rejected imports leave no library copy.

## Quick controls

Right-click the timer or tray icon to open settings. **Ctrl+Alt+T** shows/hides the timer. **Ctrl+Alt+O** recovers it on the primary monitor and disables click-through. Settings only open through the right-click menus.

[User guide](docs/USER-GUIDE.md) · [Build instructions](docs/BUILD.md) · [Installation notes](docs/INSTALL.txt)

## Data and privacy

Installed app: `%LOCALAPPDATA%\Programs\ClassAlerts`.
Settings and copied sounds: `%LOCALAPPDATA%\ClassAlerts\UserData`.
The portable app uses `UserData` beside its executable. Uninstall removes personal settings, backups and imported sounds; upgrades preserve them. This repository and its installer contain no personal timetable, preferences or custom audio. The app does not upload schedules or audio.

## Validation and limitations

The application test suite covers timetable validation/recurrence, alert boundaries, settings persistence, CSV/Chinese text, audio import, weekday sorting, warning colours and screen fitting. The MSI passes Windows Installer validation with expected ICE91 notices for an intentionally per-user-only package; package structure and cabinet extraction are also checked. The author confirmed the 1.0 installer installs successfully. The 1.2 upgrade, launch-on-Finish and uninstall still need an interactive installation check.

The overlay targets ordinary desktop/full-screen PowerPoint and browsers, not exclusive-rendering applications or secure Windows screens. Mixed-DPI projector setups should be checked on the actual classroom equipment.

## License and attribution

**Creative Commons Attribution–NonCommercial 4.0 International (CC BY-NC 4.0).** See [LICENSE](LICENSE) and [the license summary](https://creativecommons.org/licenses/by-nc/4.0/).

Attribution: Francisco Javier Vertedor Postigo · Class Alerts.

This C# WPF implementation develops the classroom alert concept from [javiervertedor/class_alerts](https://github.com/javiervertedor/class_alerts). The original personal schedule is not distributed. WiX is a separate build tool, not bundled with the application.

Uninstall removes app-owned files, shortcuts, startup registration and saved user data. Windows-managed installer caches/history and downloaded installer/ZIP files are not erased. Close old versions before uninstalling. Upgrades keep user data.
