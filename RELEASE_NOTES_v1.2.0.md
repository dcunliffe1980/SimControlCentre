# SimControlCentre v1.2.0 - iRacing Flag Lighting Release

**Release Date**: February 2026  
**Status**: Production Ready

---

## ?? New Features

### iRacing Flag Lighting System

Transform your GoXLR into a visual race command center! Your GoXLR LEDs now automatically change colors based on iRacing track flags.

#### Supported Flags:
- **Green Flag** - Solid green (race start/clear track)
- **Yellow Flag** - Solid yellow (caution/no passing)
- **Yellow Waving** - Slow flash (danger ahead!)
- **Blue Flag** - Solid blue (being lapped, hold your line)
- **White Flag** - Solid white (final lap)
- **Red Flag** - Solid red (session stopped)
- **Black Flag** - Solid red (disqualification/penalty)
- **Checkered Flag** - Fast flash (race end celebration!)
- **Debris Flag** - Slow flash orange (surface warning)
- **One Lap to Green** - Medium flash green (restart soon)

#### Features:
? **Auto-Detection** - Automatically detects GoXLR Mini vs Full-size  
? **Customizable** - Choose which LEDs/buttons light up  
? **Persistent Settings** - Button selection saved across restarts  
? **SimHub Compatible** - Timing matches SimHub for familiar feel  
? **Instant Response** - Parallel LED updates for synchronized lighting  
? **Plugin System** - Ready for future devices (Philips Hue, Nanoleaf)  

### Plugin Architecture

New extensible plugin system for lighting devices:
- **Enable/Disable Plugins** - Don't have a GoXLR? Disable the plugin!
- **Future-Ready** - Philips Hue and Nanoleaf plugins coming soon
- **No Impact** - Disabled plugins don't affect performance

### Available GoXLR LEDs

**GoXLR Mini** (Auto-detected):
- Fader 1-4 Mute buttons
- Bleep and Cough buttons
- Global lighting (affects all LEDs)
- Accent color
- Fader scribble strips (A, B, C, D)

**Full-size GoXLR** (Auto-detected):
- All Mini options plus:
- Effect selection buttons (1-6)
- Effect type buttons (Fx, Megaphone, Robot, HardTune)

---

## ?? Bug Fixes

### Critical Fixes:
- **Fixed Global Color** - Now properly changes all LEDs simultaneously
- **Fixed First-Press Delay** - Added button color API warmup
- **Fixed Sequential Updates** - LEDs now update instantly in parallel
- **Fixed Button Selection** - Properly reinitializes when changing LED selection
- **Fixed Mini Detection** - Correctly identifies 4 faders on Mini (not 3!)

### Performance Improvements:
- **Reduced Log Spam** - iRacing polling now checks if process running first
- **Faster Initialization** - Removed redundant hardware availability checks
- **Optimized Warmup** - Single warmup sequence for all GoXLR APIs

---

## ?? Documentation

### New Documentation:
- `docs/SESSION_SUMMARY.md` - Complete implementation guide for future AI threads
- `docs/PLUGIN_SYSTEM.md` - Plugin architecture and how to create new plugins
- `docs/GOXLR_API_COMPLETE.md` - Comprehensive GoXLR API reference
- `docs/GOXLR_BUTTON_API.md` - Button color API specifics
- `docs/FLAG_LIGHTING.md` - Flag lighting system overview

### Updated Documentation:
- `README.md` - Added lighting features
- All API docs updated with tested examples

---

## ?? Technical Details

### Architecture Improvements:
- **Single Source of Truth** - GoXLRService handles all connection logic
- **Graceful Degradation** - Devices fail gracefully if hardware unavailable
- **Settings Persistence** - All lighting settings saved to `config.json`
- **Plugin System** - Extensible architecture for future devices

### Performance:
- **Parallel LED Updates** - `Task.WhenAll()` for instant synchronized changes
- **API Warmup** - Pre-warms volume and button color APIs on startup
- **Connection Pooling** - Single HttpClient for all API calls
- **Smart Caching** - Reduces unnecessary API calls

### Compatibility:
- **GoXLR Mini** - Fully supported with auto-detection
- **Full-size GoXLR** - Fully supported with all buttons
- **iRacing** - All session flags supported
- **.NET 8** - Latest framework for best performance

---

## ?? Usage

### Quick Start:

1. **Launch SimControlCentre**
2. **Go to Lighting Tab**
3. **Select which LEDs to use** (checkboxes)
4. **Start iRacing** - LEDs automatically change with flags!

### Testing Without iRacing:

Click the flag test buttons to see colors immediately:
- Green Flag, Yellow Flag, Blue Flag, etc.
- Perfect for setup without running the sim

### Configuration:

**Enable/Disable Lighting:**
- Lighting Tab ? "Enable flag lighting" checkbox

**Choose LEDs:**
- Lighting Tab ? "Active Buttons" section
- Check/uncheck any LED or button
- Changes save automatically

**Disable Plugin (if you don't have GoXLR):**
- Lighting Tab ? "Enabled Plugins" section
- Uncheck "GoXLR Lighting"
- Restart required

---

## ?? Upgrade Instructions

### From v1.1.x:

1. **Download** the new release
2. **Close** SimControlCentre
3. **Replace** `SimControlCentre.exe`
4. **Restart** the application

Your existing settings are preserved! The app will automatically add new lighting settings to your `config.json`.

### First Time Setup:

1. Go to **General Tab** ? Click "Detect Serial Number"
2. Go to **Lighting Tab** ? Select which LEDs to use
3. Test with flag buttons or start iRacing!

---

## ?? Future Plans

### Coming Soon:
- **Philips Hue Plugin** - Sync room lighting with flags
- **Nanoleaf Plugin** - Light panels react to racing
- **Custom Flag Colors** - Choose your own colors per flag
- **Animation Modes** - Control GoXLR animation modes from app
- **Profile-Based Lighting** - Different LED configs per GoXLR profile

---

## ?? Known Issues

None! All features tested and working on both GoXLR Mini and Full-size.

---

## ?? Acknowledgments

- **GoXLR-on-Linux** - For the amazing GoXLR Utility API
- **iRacing** - For telemetry SDK and shared memory access
- **Community** - For testing and feedback

---

## ?? Downloads

- **SimControlCentre v1.2.0** - [Download Latest Release](https://github.com/dcunliffe1980/SimControlCentre/releases/tag/v1.2.0)
- **Full Installer** - Includes .NET 8 Runtime
- **Portable** - Requires .NET 8 Runtime installed separately

---

## ?? Support

- **Issues**: https://github.com/dcunliffe1980/SimControlCentre/issues
- **Documentation**: See `docs/` folder
- **Quick Start**: See `QUICKSTART.md`

---

**Enjoy your new racing command center!** ??
