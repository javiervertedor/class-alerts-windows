# Changelog

## 1.4.0 — 2026-11-19

- Use software rendering for the small transparent timer to avoid the graphics-device memory overhead observed on the test machine.
- Reuse a typeface, bounded text layouts, frozen text drawings and frozen brushes instead of recreating them on each animation frame.
- Animate scrolling at up to 30 fps only while visible; stop its timer when hidden or unloaded.
- Cache the parsed weekly schedule and alerts per day; invalidate on settings changes.
- Skip unchanged repaints and tooltip assignments; slow idle/paused polling and reduce redundant topmost calls while still reacting to foreground changes.
- Preserve warning thresholds, sound choices, pause/resume, countdown accuracy, Chinese text, screen fitting and full-screen overlay behavior in the code.
- Add cache-lifetime and alert-parity tests, plus before/after resource measurements. Full-screen acceptance and long-running classroom monitoring remain user checks.


## 1.3.1 — 2026-11-18

- Add Warning minutes to Quick timer. Zero disables it; values must be non-negative and strictly smaller than the duration.
- Play the selected warning sound once and apply the warning colour at the threshold; support decimals, pause/resume and timer restart.
- Validate invalid values and test threshold, disabled, paused, expired and repeated-alert cases.


## 1.3.0 — 2026-11-18

- Scroll overflowing round/bar text horizontally with pauses at each end.
- Wrap numeric-view labels below the countdown; increase caption space and fit long labels.
- Limit new lesson/activity names and CSV imports to 50 characters, with Unicode-safe display clipping for older names.
- Validate long English/Chinese captions, character limits and scrolling bounds.


## 1.2.0 — 2026-11-18

- Separate Save (persist/apply immediately, keep settings open) and Close (discard edits since the last save).
- Synchronize radio selections explicitly after sound import, rename and list rebuilds.
- Full uninstall stops the current timer and removes app data, backups, imported sounds and app registry/startup entries. Upgrades skip data cleanup.
- Include an installer ZIP with concise instructions, a timetable-conversion AI prompt and a sample CSV.
- Regression checks cover repeated Save, Close, regenerated sound rows and scoped data deletion. Installer checks verify the cleanup action excludes upgrades. Windows-managed history/caches and downloaded packages are outside the cleanup scope.


## 1.1.0 — 2026-11-18

- Launch Class Alerts when Finish is clicked after installation or upgrade.
- Add a sound library with independent Start, Warning and End radio-button columns.
- Import WAV, MP3, WMA, M4A and AAC using locally available Windows decoding support.
- Verify local copies before adding sounds; reject broken audio and clean up unsuccessful imports.
- Give imported sounds readable, unique names; support renaming and deleting imported entries.
- Keep PCM WAV playback available without codec initialization; report asynchronous playback failures.
- Validate time edits immediately, normalize H:mm to HH:mm, and report lesson conflicts after row edits.
- Preserve CSV validation for duplicates, overlaps, invalid times and invalid warning minutes, including append conflicts.

Validation: application regression tests, actual files in all five audio formats, copied-file persistence, rename/delete, independent radio selections, rendered settings screens, and MSI ICE/package checks passed. The interactive upgrade and launch-on-Finish still require a user test. Version 1.0 installation was confirmed by the author.
