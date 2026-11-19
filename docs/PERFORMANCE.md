# Resource-use investigation — 2026-11-19

The installed 1.3.1 process was observed at approximately 935 MiB working set at the first read. This is an observation of an already-running process, not the controlled baseline below.

## Changes

- Software rendering avoids the large graphics-device memory overhead observed on this computer for a tiny transparent WPF overlay. It does not disable Windows security or change the topmost window styles.
- Reuse the typeface and cap retained layouts to one per drawing slot. Cache frozen text drawings/outline geometry and a bounded set of frozen brushes. Recompute wrapping only when its text/style/layout changes.
- Replace display-refresh-rate render callbacks with a 30 fps scrolling timer that stops when hidden, unloaded, or unnecessary.
- Parse the weekly timetable once per day or settings change. Cache event times while keeping the same clock-rewind, 90-second stale-alert, boundary-priority and warning rules.
- Skip unchanged repaints and tooltip writes. Idle/paused timers poll more slowly, with a scheduled start shortening the next interval. Avoid redundant topmost calls, while retaining foreground-change checks and a periodic fallback.

## Matched rendering measurements

Same machine, 14 logical processors, transparent WPF window, generic long English label, label size 22, and countdown/progress updated every 200 ms. Each process runs independently of the installed timer and uses no personal data. Baseline uses the saved 1.3.1 executable and default graphics rendering. Optimized uses the 1.4 rendering implementation with software rendering.

CPU percentages below are process CPU seconds / wall-clock seconds / 14, from the approximately 10-second to 40-second samples (startup excluded). They are not instantaneous Task Manager readings. Working set is the 40-second sample. Measurements were made on an active workstation, not a laboratory-isolated machine.

| View | Old RAM | New RAM | Old CPU | New CPU | CPU reduction |
|---|---:|---:|---:|---:|---:|
| Scrolling round | 433.9 MiB | 60.9 MiB | 1.61% | 0.55% | 66% |
| Wrapped numeric | 444.1 MiB | 61.8 MiB | 1.23% | 0.22% | 83% |

The optimized scrolling test continued for two minutes: working set was 60.9 MiB at 40 seconds and 61.1 MiB at 120 seconds; private memory was 73.6 MiB at 120 seconds. This is a short stability test, not proof of zero leaks over a full school day. Opening settings, loading fonts/audio, graphics drivers and display configuration can change memory usage. The standalone rendering harness forces a 200 ms redraw; it does not measure the additional savings from the controller's schedule cache and skipped unchanged redraws.

Raw samples are in Tests/Measurements. Tests/PerfProbe.cs is a standalone .NET Framework WPF harness. Compile it with the Framework64 csc.exe and references to WindowsBase.dll, PresentationCore.dll, PresentationFramework.dll and System.Xaml.dll. Arguments: path-to-app-exe Round-or-Numeric output.csv duration-seconds [software]. It creates a temporary visible test window and exits by itself; it does not install or change user settings.

## Verification and remaining checks

Application regression tests pass, including warnings, pause/resume rules, sound-format imports, CSV validation, Unicode captions, save/close, sound radio selections, screen fitting and cleanup tests. New tests check bounded/reused text layouts, font-change invalidation, animation shutdown on hide/close, and cached-schedule equivalence. Installer validation passes with the expected per-user ICE91 notices. Rendered output was inspected.

Recheck full-screen PowerPoint/browser behaviour and monitor memory during a classroom session after upgrading. The old installed app was left running and was not replaced during this investigation. Quit it, then run the new MSI to upgrade; do not uninstall first, since uninstall deletes your saved data.

## Why no Windows service?

The countdown and sounds must run in the signed-in user's session. A service would require a separate desktop process, would not remove rendering costs, and would complicate a per-user installation. Task Scheduler can launch an app, but does not make a visible live countdown cheaper. The existing sign-in option remains sufficient.

Microsoft references: [WPF text performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-text), [freezing drawing resources](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-object-behavior), [interactive service restrictions](https://learn.microsoft.com/en-us/windows/win32/services/interactive-services).
