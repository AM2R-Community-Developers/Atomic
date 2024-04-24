# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)


## [Unreleased] - 202?-??-??


## [2.2.0] - 2024-03-17

- Added: Show error messages if Atomic crashes
- Added: Mod names will now have leading/trailing whitespace trimmed
- Added: Localization Support. Atomic should be *mostly* localized into English, German, Japanese, Italian, Spanish, Portuguese, Chinese and Russian.
- Added: Option to remember the last used field content
- Changed: MessageBoxes will now make the main window inactive.
- Changed: Atomic now requires dotnet8 instead of dotnet6
- Fixed: macOS builds should now work
- Fixed: Using custom save location now works when you have a non-C drive on Windows as your system drive
- Fixed: Launching Atomic from a flatpak should now work.

## [2.1.0] - 2022-12-24

- Added: A warning is shown if the mod zip contains any subfolders
- Added: A warning is shown if the mod zip contains a `profile.xml` file.
- Added: Better warnings are shown if an invalid AM2R 1.1.zip is provided.
- Changed: The project has been rebranded from AM2RModPacker to Atomic. This includes having a custom icon.
- Changed: The project has been rewritten in Eto.Forms instead of WinForms. This means that this project now also supports running on Linux and macOS.
- Changed: The UI is now completely resizable.
- Changed: The space between the left side and the right side in the UI is now adjustable via a splitter.
- Changed: Don't allow the user to use invalid characters in their mod name.
- Changed: The option to make Mods compatible with the experimental Mac Am2RLauncher was added.
- Changed: Making a Mod compatible with the Windows AM2RLauncher is now not required anymore
- Changed: Remember the last chosen folder for file dialogs.
- Changed: If the save location of a user's mod is inside a subfolder of the vanilla save location, and that mod save location is selected, then the save location will be automatically corrected to use lowercase subfolders to mimic what Game Maker does.
- Changed: When having a mod zip, that contains non-lowercase subfolders, then during mod creation these will now be lowercased to make the mods work correctly on Unix systems.

## [2.0.3] - 2021-05-2

- Fixed: Small inconsistencies with custom save locations.

## [2.0.2] - 2021-04-17

- Fixed: Issues when selecting VM builds from GameMaker.


## [2.0.1] - 2021-04-14

- Fixed: Fixes a crash if AM2R.ini does not exist in the input APK for some reason (likely due to VM exports).

## [2.0.0] - 2021-03-26

- Added: Better error handling if creating temporary directories fail.
- Added: Supports creating Linux mods.
- Added: Supports creating Android mods.
- Added: Added new fields to the UI: A version field and a mod notes field
- Added: Support for AM2RLauncher 2.0.0
- Changed: Relicensed the project from MIT to GPLv3.
- Changed: Improved the save directory field in the UI.
- Changed: Generated mod info is now saved as XML instead of JSON.
- Fixed: Fix rare cases where UI was still resizable.
- Removed: Support for AM2RLauncher 1.X.X.

## [1.0.1]

- Changed: Improved UI performance.
- Changed: Limit Mod name and Author name to 30 characters.
- Fixed: Disable resizing the UI.
- Fixed: Fixed the original file picker dialog referring to the modded am2r zip, and vice versa.

## [1.0.0] - 2021-01-12

- Added: Initial Release
