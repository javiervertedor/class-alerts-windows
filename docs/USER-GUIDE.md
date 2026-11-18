# User guide

## First run

The installer adds Class Alerts to the Start menu and desktop. Click Finish to launch it, then right-click the floating timer → Settings. An empty timetable displays No lessons. Settings do not open automatically.

## Appearance

Choose Round, Bar or Numeric and adjust the timer scale. Select an installed font, Bold, countdown size and class/end-time size. Chinese text uses available CJK fallback fonts. Larger text is fitted to the compact layout; long names are shortened, with the full name available in the tooltip.

Choose text, fill and border colours with the colour picker or enter #RRGGBB. The border colour also controls progress and the numeric outline. Numeric view stays transparent. During a lesson, Warning colour applies to the countdown and progress once remaining time reaches the lesson's WarningMinutes setting, provided that setting is greater than zero. The warning preview checkbox only previews this appearance.

Switching views or changing size moves the timer inward if necessary to keep it inside its current screen's work area. Drag it to another monitor before locking it.

## Timetable

Double-click cells to edit day, class name, start/end times and warning minutes. Use 24-hour HH:mm times. Typed H:mm values are normalized (9:05 → 09:05). Invalid time cells stay in edit mode with a correction message. Conflicts are reported when a row is committed and block saving; adjacent lessons remain valid. Add, duplicate or remove lessons with the toolbar. Start/end checkboxes control those sound alerts; WarningMinutes = 0 disables the pre-end warning.

The Day header toggles Monday–Sunday or Sunday–Monday, keeping start times earliest-first within each day. Missing names, invalid times, overlaps and warnings outside a lesson are rejected when saving. Adjacent lessons are allowed; at a shared end/start boundary the start sound takes priority. Stale sounds more than 90 seconds old are not replayed after sleep.

Between lessons, a dash and the next lesson/start time are displayed. The schedule uses the computer's local time.

## CSV import

Use Save CSV template or download Templates/schedule-template.csv from the repository. Keep these headers:

`Day,Name,Start,End,WarningMinutes,StartAlert,EndAlert`

Names may contain Chinese, commas and quotes using normal CSV quoting. Weekdays can be Monday–Sunday, 星期一–星期日 or 周一–周日. Alert values accept true/false, yes/no, 1/0 or 是/否. WarningMinutes is an integer. Excel-style 9:00 and 9:00:00 are normalized to 09:00. Save as CSV UTF-8 to preserve Chinese text.

Select Replace timetable or Append to timetable, then Import CSV. The entire candidate is validated before the draft changes. Review and Save & close; Cancel keeps the previously saved schedule.

## Sounds

The Sounds tab lists built-in and imported audio. Select one radio button in each of the Start, Warning and End columns. The same sound may be assigned to all three. Select Silent to disable a sound category. Preview plays that row; Stop preview ends playback.

Import audio accepts WAV, MP3, WMA, M4A and AAC files up to 20 MB. The app copies the file into its local library, checks that Windows can decode the copy, and gives it a readable name based on the original filename. Duplicate names receive a number. The original file can then be moved or removed. Codec support depends on Windows; unsupported/corrupt files are rejected and their temporary copies removed. PCM WAV remains the most portable choice.

Select an imported row to rename or delete it. Deleting an assigned sound switches those alerts to built-in defaults. Rename, deletion and assignments apply on Save & close; Cancel discards them. New imports stay in the library even if settings are cancelled. Built-in sounds cannot be renamed or deleted. Playback failures are reported in settings and through the tray notification.

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
