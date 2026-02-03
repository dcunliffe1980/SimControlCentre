# SimControlCentre v1.3.1

## ?? Major Features

### Timeline Scrubber for Telemetry Playback
Jump to any point in your recorded sessions with visual flag change markers!

- **Visual timeline** with scrubber slider
- **Color-coded flag markers** showing exactly when flags changed
- **Hover tooltips** displaying flag transitions
- **Click to seek** to any position instantly
- **Flag legend** showing which flags appear

Perfect for testing flag lighting without running full race sessions.

### Flag Lighting Improvements
No more sticky LEDs or flickering!

- **Debounced None flags** (500ms threshold) prevents flickering from telemetry glitches
- **Profile restore** - LEDs return to your GoXLR profile colors automatically
- **Session end restore** - LEDs reset when telemetry disconnects
- **Clear Flag button** now works correctly

### GoXLR Plugin Complete
Full button capture and UI improvements:

- **All GoXLR channels** available (Mic, LineIn, Console, System, Game, Chat, Music, Headphones, etc.)
- **Button capture working** - keyboard and controller buttons
- **Inline duplicate detection** - no popups, just red text showing conflicts
- **Classic horizontal layout** restored
- **Clean UI** - consistent spacing, plain text (no emojis)

## ?? Bug Fixes

### v1.3.1 Hotfix
- **Fixed update checker** - Now correctly detects framework-dependent vs standalone installations
  - Uses .NET Runtime DLL detection instead of single-file bundle check
  - Ensures users download the correct installer type when updating

### v1.3.0 Fixes
- Fixed LEDs staying lit after flag clears
- Fixed Clear Flag button not restoring profile
- Fixed telemetry glitches causing LED flickering  
- Fixed button capture delegate issues
- Fixed plugin settings type conversion
- Fixed plugins not included in installers

## ?? Installation

Download the installer below. Includes .NET 8.0 Runtime (standalone version).

**Requirements:**
- Windows 10/11
- GoXLR Utility (for GoXLR features)
- iRacing (for telemetry features)

### Which Installer to Choose?

- **SimControlCentre-Setup-v1.3.1.exe** (~2.4 MB) - Requires .NET 8 Runtime
- **SimControlCentre-Setup-Standalone-v1.3.1.exe** (~46 MB) - Includes everything, no prerequisites

Both installers include the GoXLR plugin!

## ?? Documentation

- [Release Notes](RELEASE_NOTES_v1.3.1.md)
- [Build & Release Guide](docs/BUILD_AND_RELEASE.md)
- [UI Design Guidelines](docs/UI_DESIGN_GUIDELINES.md)

## ?? What's New in v1.3.1

This release includes all features from v1.3.0 plus a critical hotfix:

**Hotfix:** Update checker now properly detects which installer type you have installed and downloads the matching version for updates.

## ?? Upgrading from v1.2.0

Simply download and run the installer. Your settings will be preserved.

**Note:** If you have v1.3.0 installed (from early access), this v1.3.1 update fixes the update checker bug.

**Full Changelog**: https://github.com/dcunliffe1980/SimControlCentre/compare/v1.2.0...v1.3.1
