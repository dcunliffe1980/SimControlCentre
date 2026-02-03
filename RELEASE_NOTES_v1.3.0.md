# SimControlCentre v1.3.0 Release Notes

**Release Date:** January 2025

## ?? What's New

### Timeline Scrubber for Telemetry Playback
- **Visual timeline** with scrubber slider for recorded telemetry sessions
- **Flag change markers** - see exactly when flags changed during your session
- **Color-coded markers** matching flag colors (Green, Yellow, White, Checkered, etc.)
- **Hover tooltips** showing flag transitions ("Green ? Yellow")
- **Time display** showing current position and total duration (MM:SS format)
- **Click to seek** - jump to any position instantly
- **Flag legend** showing which flags appear in the recording
- **Perfect for testing** - quickly verify flag lighting behavior

### Flag Lighting Improvements
- **Debounced None flags** - brief None flags (< 500ms) are now ignored to prevent flickering
- **Profile restore on flag clear** - LEDs now return to profile colors when flags clear
- **Profile restore on session end** - LEDs automatically restore when telemetry disconnects
- **Clear Flag button fixed** - now properly restores profile lighting
- **GoXLR profile reload** - uses API to reload current profile for accurate color restoration

### GoXLR Plugin Enhancements
- **Complete button capture** - keyboard and controller buttons fully working
- **All GoXLR channels** available in dropdown (Mic, LineIn, Console, System, Game, Chat, Music, Headphones, etc.)
- **Old horizontal grid layout** restored for better readability
- **Inline duplicate detection** - shows "Already assigned to: X" in red text (no popups)
- **Consistent UI spacing** - 5px between all buttons
- **Clean, plain text** - no emojis, professional appearance

## ?? Bug Fixes

- Fixed LEDs staying lit after flag changes to None
- Fixed Clear Flag button not working
- Fixed telemetry glitches causing LED flickering
- Fixed GoXLR profile colors not restoring after race
- Fixed button capture delegate signature issues
- Fixed type conversion issues in plugin settings

## ?? Technical Improvements

- **ILightingDevice interface** - added SaveStateAsync() and RestoreStateAsync()
- **Debouncing system** - 500ms threshold for None flag changes
- **Profile reload API** - GoXLRService.ReloadCurrentProfileAsync()
- **Bidirectional type conversion** - Dict<string,object> ? Dict<string,ChannelHotkeys>
- **Timeline rendering** - responsive canvas with markers that redraw on resize
- **Seek functionality** - TelemetryPlaybackProvider.Seek(position)
- **Time tracking** - CurrentTime and TotalDuration properties added

## ?? Documentation

- **UI Design Guidelines** - documented user preferences (inline feedback, no emojis, no unnecessary popups)
- **Session summaries** - comprehensive documentation of button capture implementation
- **Telemetry timeline** - usage instructions for scrubber and markers

## ?? Usage

### Timeline Scrubber:
1. Record telemetry during a race/practice session
2. Select recording from dropdown in Telemetry Debug tab
3. Timeline appears with markers showing flag changes
4. Click/drag slider to jump to any position
5. Hover over markers to see flag transitions
6. Legend shows which flags appear in recording

### Flag Lighting:
1. Enable Flag Lighting in Lighting tab
2. Configure which buttons to use
3. Flags now properly clear when returning to None
4. LEDs restore to profile colors automatically
5. Manual "Clear Flag" button works correctly

### GoXLR Device Control:
1. Add channels from dropdown (all channels available)
2. Click Capture to assign keyboard or button
3. Duplicate assignments show inline error message
4. Clear button removes assignments
5. Remove button deletes channel/profile

## ?? Installation

Download `SimControlCentre-v1.3.0-Setup.exe` from the releases page.

**Requirements:**
- Windows 10/11
- .NET 8.0 Runtime (included in installer)
- GoXLR Utility (for GoXLR features)
- iRacing (for telemetry features)

## ?? Known Issues

None reported for this release.

## ?? Credits

Thanks to all users who tested and provided feedback on the flag lighting system!

---

**Full Changelog:** https://github.com/dcunliffe1980/SimControlCentre/compare/v1.2.0...v1.3.0
