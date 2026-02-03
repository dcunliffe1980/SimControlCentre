# SimControlCentre v1.3.0

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

- Fixed LEDs staying lit after flag clears
- Fixed Clear Flag button not restoring profile
- Fixed telemetry glitches causing LED flickering  
- Fixed button capture delegate issues
- Fixed plugin settings type conversion

## ?? Installation

Download the installer below. Includes .NET 8.0 Runtime.

**Requirements:**
- Windows 10/11
- GoXLR Utility (for GoXLR features)
- iRacing (for telemetry features)

## ?? Documentation

- [Release Notes](RELEASE_NOTES_v1.3.0.md)
- [UI Design Guidelines](docs/UI_DESIGN_GUIDELINES.md)
- [Session Summary](docs/SESSION_GOXLR_BUTTON_CAPTURE.md)

**Full Changelog**: https://github.com/dcunliffe1980/SimControlCentre/compare/v1.2.0...v1.3.0
