# SimControlCentre v1.3.1 Release Notes

**Release Date:** January 2025

## ?? Version Notice

v1.3.1 is a complete release that includes all v1.3.0 features plus a critical hotfix. We skipped publishing v1.3.0 to ensure all users get the fix from day one.

## ?? Hotfix in v1.3.1

### Update Checker Fix
- **Issue:** Update checker was downloading standalone installer for all users, regardless of which version they installed
- **Fix:** New detection method checks for .NET Runtime DLLs to properly identify installer type
- **Impact:** Users now download the correct installer (framework-dependent or standalone) when updating

## ?? New Features (from v1.3.0)

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
- **Plugins included in installers** - GoXLR features work out of the box

## ?? Bug Fixes

### v1.3.1
- Fixed update checker downloading wrong installer type

### v1.3.0 Fixes
- Fixed LEDs staying lit after flag changes to None
- Fixed Clear Flag button not working
- Fixed telemetry glitches causing LED flickering
- Fixed GoXLR profile colors not restoring after race
- Fixed button capture delegate signature issues
- Fixed type conversion issues in plugin settings
- Fixed plugins not being included in installers

## ?? Technical Improvements

- **ILightingDevice interface** - added SaveStateAsync() and RestoreStateAsync()
- **Debouncing system** - 500ms threshold for None flag changes
- **Profile reload API** - GoXLRService.ReloadCurrentProfileAsync()
- **Bidirectional type conversion** - Dict<string,object> ? Dict<string,ChannelHotkeys>
- **Timeline rendering** - responsive canvas with markers that redraw on resize
- **Seek functionality** - TelemetryPlaybackProvider.Seek(position)
- **Time tracking** - CurrentTime and TotalDuration properties added
- **Update detection** - .NET Runtime DLL-based detection for installer type
- **Plugin support** - Standalone builds now support plugins properly

## ?? Documentation

- **UI Design Guidelines** - documented user preferences (inline feedback, no emojis, no unnecessary popups)
- **Build & Release Guide** - comprehensive build and release documentation
- **Session summaries** - detailed documentation of button capture implementation
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

**Which installer should I choose?**

### Framework-Dependent (2.4 MB)
- Requires .NET 8 Runtime installed on your PC
- Smaller download
- Choose this if you already have .NET 8 or don't mind installing it

### Standalone (46 MB)
- Includes .NET 8 Runtime
- No prerequisites needed
- Larger download but works on any Windows 10/11 PC
- Recommended for most users

**Both installers include:**
- SimControlCentre application
- GoXLR plugin (in Plugins folder)
- All dependencies
- Documentation

**Requirements:**
- Windows 10/11
- GoXLR Utility (for GoXLR features)
- iRacing (for telemetry features)

## ?? Known Issues

None identified for this release.

## ?? Upgrade Notes

### From v1.2.0
Simply download and run the installer. All settings will be preserved.

### From v1.3.0 (if you had early access)
This release fixes the update checker bug. Install v1.3.1 to get the fix.

## ?? Credits

Thanks to all users who tested and provided feedback on the flag lighting system!

---

**Full Changelog:** https://github.com/dcunliffe1980/SimControlCentre/compare/v1.2.0...v1.3.1
