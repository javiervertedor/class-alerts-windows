# User guide

## First run

The installer adds Class Alerts to the Start menu and desktop. Launch it and right-click the floating timer → Settings. An empty timetable displays No lessons. Settings do not open automatically.

## Appearance

Choose Round, Bar or Numeric and adjust the timer scale. Select an installed font, Bold, countdown size and class/end-time size. Chinese text uses available CJK fallback fonts. Larger text is fitted to the compact layout; long names are shortened, with the full name available in the tooltip.

Choose text, fill and border colours with the colour picker or enter #RRGGBB. The border colour also controls progress and the numeric outline. Numeric view stays transparent. During a lesson, Warning colour applies to the countdown and progress once remaining time reaches the lesson's WarningMinutes setting, provided that setting is greater than zero. The warning preview checkbox only previews this appearance.

Switching views or changing size moves the timer inward if necessary to keep it inside its current screen's work area. Drag it to another monitor before locking it.

## Timetable

Double-click cells to edit day, class name, start/end times and warning minutes. Use 24-hour HH:mm times. Add, duplicate or remove lessons with the toolbar. Start/end checkboxes control those sound alerts; WarningMinutes = 0 disables the pre-end warning.

The Day header toggles Monday–Sunday or Sunday–Monday, keeping start times earliest-first within each day. Missing names, invalid times, overlaps and warnings outside a lesson are rejected when saving. Adjacent lessons are allowed; at a shared end/start boundary the start sound takes priority. Stale sounds more than 90 seconds old are not replayed after sleep.

Between lessons, a dash and the next lesson/start time are displayed. The schedule uses the computer's local time.

## CSV import

Use Save CSV template or download Templates/schedule-template.csv from the repository. Keep these headers:

`Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert`

Names may contain Chinese, commas and quotes using normal CSV quoting. Weekdays can be Monday–Sunday, 星期一–星期日 or 周一–周日. Alert values accept true/false, yes/no, 1/0 or 是/否. WarningMinutes is an integer. Excel-style 9:00 and 9:00:00 are normalized to 09:00. Save as CSV UTF-8 to preserve Chinese text.

Select Replace timetable or Append to timetable, then Import CSV. The entire candidate is validated before the draft changes. Review and Save & close; Cancel keeps the previously saved schedule.

## Sounds

Select Chime, Soft bell, Double bell, Silent or custom audio separately for start, warning and end. Preview and Stop preview let you check the selected sound. Save before testing a new sound choice.

Import WAV copies audio locally. Supported: uncompressed PCM WAV, mono/stereo, 8-bit or 16-bit, 8–192 kHz, up to 20 MB. MP3 is not supported. No online upload occurs. Imported audio remains in the library if settings are cancelled.

## Quick timer

Enter an activity name and duration in minutes, then Start timer. 0.1 minutes is a six-second test. Manual timers play the start/end sounds and support pause/resume. They have no scheduled pre-end warning threshold. Use lesson timetable returns to the weekly schedule.

## Presentation controls

- Drag the unlocked timer to position it.
- Right-click the timer or tray icon for settings, mode selection and quit.
- Lock / click through passes pointer input to the app underneath.
- Ctrl+Alt+T: show or hide the timer.
- Ctrl+Alt+O: recover it on the primary display and unlock it.

If another app owns a shortcut, use the tray menu. Closing settings leaves the timer running; Quit app exits.

## Start with Windows

Select the optional checkbox on the installer's final screen, or use Settings → About → Windows startup and Save & close. This runs the app when this user signs in. It is not a system service and does not require administrator access.

## Saving and uninstalling

Save & close applies all settings. Cancel discards unsaved edits. Settings are stored with a previous-version backup. Click-through resets on restart so the timer remains recoverable.

Installed files are under %LOCALAPPDATA%\Programs\ClassAlerts. Your data is under %LOCALAPPDATA%\ClassAlerts\UserData. Uninstall via Windows Apps & features; your data is retained. Portable builds store UserData beside the executable and do not automatically migrate that data into an installed copy.
