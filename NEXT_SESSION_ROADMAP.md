# ?? Next Session: Where We Are on the Roadmap

**Date:** January 2025  
**Current Version:** v1.3.1 (Published! ??)

---

## ? What We Accomplished Tonight (Not on Roadmap!)

Tonight was a **productive detour** - we didn't follow the roadmap but shipped critical improvements:

### 1. Timeline Scrubber (NEW FEATURE)
- Visual timeline for telemetry playback
- Color-coded flag change markers
- Seek functionality
- Perfect for testing flag lighting
- **Impact:** Major UX improvement for testing

### 2. Flag Lighting Fixes (CRITICAL)
- Debouncing (500ms) to prevent flickering
- Profile restore on flag clear
- Profile restore on session end
- Clear Flag button working
- **Impact:** Flag lighting now rock-solid

### 3. Plugin Inclusion Fix (CRITICAL)
- Plugins now included in installers
- Updated build script
- Updated installer configs
- **Impact:** GoXLR features work out of the box

### 4. Update Checker Fix (CRITICAL)
- Detects framework-dependent vs standalone correctly
- Uses .NET Runtime DLL detection
- **Impact:** Users get correct installer when updating

### 5. Release v1.3.1 (PUBLISHED)
- Skipped v1.3.0
- Complete release with all fixes
- Installers ready and published on GitHub
- **Impact:** Production-ready release!

---

## ?? Current Position on Roadmap

### Completed (?)
- ? **Phase 1:** Quick Wins & Foundation (100% complete)
  - Lighting Tab cleanup
  - Plugin Settings Architecture
  - Plugins tab created

- ? **Phase 2.1:** GoXLR Control ? Device Control Plugin (100% complete)
  - IDeviceControlPlugin interface
  - GoXLRDeviceControlPlugin implementation
  - Profile switching
  - Volume control
  - Channel mute functionality
  - HotkeyManager refactored
  - ControllerManager refactored

### Next Up (??)

**Phase 2.2: Telemetry Optimization** ? YOU ARE HERE

**Goal:** Only enable telemetry when actively needed

**Why:** Performance optimization - telemetry should only run when required

**Tasks:**
1. Add `bool RequiresTelemetry { get; }` to plugin interfaces
2. Track which components need telemetry
3. Auto-enable when lighting plugin enabled
4. Auto-disable when no components need it
5. Add telemetry status indicator to UI

**Estimated:** 1 session

**Files to Modify:**
- `SimControlCentre.Contracts\IPlugin.cs` - Add RequiresTelemetry property
- `SimControlCentre\Services\TelemetryService.cs` - Conditional start/stop
- `SimControlCentre\App.xaml.cs` - Check plugins before starting telemetry
- `SimControlCentre.Plugins.GoXLR\Services\GoXLRLightingPlugin.cs` - Return true for RequiresTelemetry

**Before:**
```csharp
// Always running
_telemetryService.StartAll();
```

**After:**
```csharp
// Only start if needed
if (_lightingService.Plugins.Any(p => p.RequiresTelemetry))
{
    _telemetryService.StartAll();
}
```

---

## ??? The Road Ahead

### After Phase 2.2 (Next 3-4 Sessions)

**Phase 3: New Features**

### 3.1 Controller Overhaul (2-3 sessions)
The big one! Major controller improvements:

#### 3.1.1 Remove Device List, Add Controller Management
- User explicitly adds controllers (not auto-detected)
- Press button to identify controller
- Clean controller management UI

#### 3.1.2 Button Type Support ?
- **Momentary** - Normal button (press/release)
- **Toggle** - Press to lock, press to unlock
- **RotaryEncoder** - Two buttons = CW/CCW
- **Switch** - Two positions, spring-return center
- **Latch** - Two positions, stays until moved
- **Joystick** - Analog axes
- **FanatecEncoder** - Three modes

**Why:** Your PXN CB1 has dials, switches, latches - need proper support!

#### 3.1.3 Toggle Mode Feature ??
**The killer feature!**

- One button changes what other buttons do
- Example: Toggle between "Game volume" and "Chat volume" modes
- Visual feedback
- Optional TTS for VR: "Chat volume mode active"

**Example Use Case:**
```
Normal Mode:
  - Dial 1 = Game volume
  - Dial 2 = Chat volume

Toggle pressed:
  - Dial 1 = Music volume  
  - Dial 2 = System volume

Toggle pressed again:
  - Back to normal mode
```

---

## ?? Recommendation for Next Session

### Option 1: Follow Roadmap (Recommended)
**Start Phase 2.2: Telemetry Optimization**

**Pros:**
- Quick win (1 session)
- Performance improvement
- Completes Phase 2
- Sets up for Phase 3

**What You'll Do:**
1. Add RequiresTelemetry to plugin interfaces
2. Modify telemetry service to start conditionally
3. Test with lighting plugin enabled/disabled
4. Add status indicator

**Estimated:** 1-2 hours

---

### Option 2: Jump to Controller Overhaul
**Start Phase 3.1: Button Types**

**Pros:**
- Super exciting feature
- Direct benefit for your PXN CB1
- Rotary encoders, switches, latches!

**Cons:**
- Bigger feature (2-3 sessions)
- Skips telemetry optimization

---

### Option 3: Polish Current Release
**Fix any v1.3.1 issues**

**If users report bugs:**
- Quick v1.3.2 hotfix
- Otherwise, proceed with roadmap

---

## ?? Roadmap Progress

```
Phase 1: Quick Wins          ???????????????????? 100%
Phase 2: Refactor Existing   ????????????????????  75% (2.1 done, 2.2 next)
Phase 3: New Features        ????????????????????   0%
```

**Overall:** ~58% through planned work

---

## ?? Next Session Checklist

### If Following Roadmap (Phase 2.2):

- [ ] Add `RequiresTelemetry` property to `IPlugin` interface
- [ ] Implement in GoXLR lighting plugin (return true)
- [ ] Modify `TelemetryService` for conditional start
- [ ] Update `App.xaml.cs` to check plugins before starting telemetry
- [ ] Test: Disable lighting ? telemetry stops
- [ ] Test: Enable lighting ? telemetry starts
- [ ] Add telemetry status to Telemetry Debug tab
- [ ] Document changes
- [ ] Commit: "Phase 2.2: Telemetry optimization complete"

**Estimated Time:** 1-2 hours

---

## ?? Notes for Next Time

### What Went Well Tonight:
- ? Caught critical bugs before users did
- ? Timeline scrubber is awesome
- ? Flag lighting is solid now
- ? Clean release process

### What to Remember:
- Always test update checker with both installer types
- Plugins MUST be in installers
- UI guidelines documented (no emojis, inline feedback)
- Build script now handles plugins automatically

### Technical Debt:
- None! Everything is clean after v1.3.1

---

## ?? Summary

**Tonight:** Detour was productive! Shipped v1.3.1 with critical fixes.

**Next Session:** Back to roadmap - Phase 2.2 (Telemetry Optimization)

**After That:** Phase 3 - Controller overhaul (button types, toggle modes!)

**Status:** On track, slightly ahead actually (caught bugs early)

---

**Ready to continue the roadmap next time!** ??
