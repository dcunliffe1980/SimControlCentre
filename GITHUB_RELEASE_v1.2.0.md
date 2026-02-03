# ?? SimControlCentre v1.2.0 - iRacing Flag Lighting

Transform your GoXLR into a visual race command center! This release adds comprehensive iRacing flag-based lighting with an extensible plugin system.

## ? What's New

### iRacing Flag Lighting
Your GoXLR LEDs now automatically change colors based on track flags:
- ?? **Green** - Race start/clear track (solid)
- ?? **Yellow** - Caution (solid)
- ?? **Yellow Waving** - Danger ahead (slow flash)
- ?? **Blue** - Being lapped (solid)
- ? **White** - Final lap (solid)
- ?? **Red** - Session stopped (solid)
- ? **Black** - Disqualification (solid red for visibility)
- ?? **Checkered** - Race end (fast flash)
- ?? **Debris** - Surface warning (slow flash)
- ?? **One Lap to Green** - Restart soon (medium flash)

### Features
? Auto-detects GoXLR Mini vs Full-size  
? Choose which LEDs light up (buttons, faders, global)  
? Settings persist across restarts  
? Test flags without running iRacing  
? Plugin system ready for Philips Hue, Nanoleaf  
? Instant parallel LED updates  

### Available LEDs
**GoXLR Mini**: Fader 1-4 mute, Bleep, Cough, Global, Accent, Faders A-D  
**Full-size**: All Mini LEDs + Effect buttons (Select 1-6, Fx, Megaphone, Robot, HardTune)

## ?? Bug Fixes
- Fixed Global color (JSON structure)
- Fixed first-press delay (added warmup)
- Fixed sequential updates (now parallel)
- Fixed button selection persistence
- Fixed Mini detection (4 faders not 3)
- Reduced iRacing log spam

## ?? Quick Start
1. Download and extract
2. Run `SimControlCentre.exe`
3. Go to Lighting tab
4. Select LEDs to use
5. Start iRacing - LEDs change automatically!

## ?? Documentation
- Complete API documentation in `docs/` folder
- Plugin system guide for future extensions
- Session summary for developers

## ?? Installation

### Option 1: Self-Contained (Recommended)
Download `SimControlCentre-v1.2.0.zip` - includes everything needed, no installation required.

### Option 2: Framework-Dependent
Download `SimControlCentre-v1.2.0-framework.zip` - requires .NET 8 Runtime installed separately.

## ?? Requirements
- Windows 10/11
- .NET 8 Runtime (if using framework-dependent build)
- GoXLR with GoXLR Utility running
- iRacing (for flag lighting)

## ?? Coming Soon
- Philips Hue plugin
- Nanoleaf plugin
- Custom flag colors
- Animation mode control

## ?? Full Release Notes
See `RELEASE_NOTES_v1.2.0.md` for complete details.

---

**Enjoy your racing command center!** ??
