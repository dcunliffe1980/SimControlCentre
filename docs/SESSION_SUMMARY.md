# Session Summary - Flag Lighting & GoXLR Integration

**Date**: February 2026  
**Session Focus**: Implementing iRacing flag-based lighting system with GoXLR integration  
**Status**: ? Mostly Complete (Global color debugging in progress)

---

## What Was Accomplished This Session

### 1. ? Plugin System for Lighting Devices

**Created**: Modular plugin architecture for extensible lighting device support

**Files Created**:
- `SimControlCentre\Services\ILightingDevicePlugin.cs` - Plugin interface
- `SimControlCentre\Services\GoXLRLightingPlugin.cs` - GoXLR implementation
- `docs\PLUGIN_SYSTEM.md` - Complete plugin documentation

**Key Features**:
- Device discovery and initialization
- Per-device configuration (e.g., button selection)
- Dynamic UI generation based on device capabilities
- Easy to add new devices (Philips Hue, Nanoleaf, etc.)

**Architecture Decision**:
- **Core services** (GoXLRService) handle connection - single source of truth
- **Plugins** extend functionality - optional features
- Devices always created, fail gracefully if hardware unavailable

---

### 2. ? GoXLR Device Type Detection

**Implementation**: Auto-detect Mini vs Full-size GoXLR

**Method Used**: 
```csharp
var deviceType = status.Hardware.DeviceType; // "Mini" or "Full"
```

**Key Findings**:
- ? GoXLR Mini HAS 4 faders (Fader1-4), NOT 3!
- ? Mini DOES support animation modes (Rainbow Retro/Bright/Dark)
- ? Mini lacks effect buttons (EffectSelect1-6, EffectFx, etc.)
- ? Both have Global/Accent color controls

**Button Lists**:
- **Mini**: Fader1-4Mute, Bleep, Cough, Global, Accent, FaderA-D
- **Full**: Mini buttons + EffectSelect1-6, EffectFx, EffectMegaphone, EffectRobot, EffectHardTune

---

### 3. ? GoXLR Button Color API - WORKING

**Commands Implemented**:
```csharp
// Regular buttons
{"Command":["SERIAL",{"SetButtonColours":["Fader1Mute","FF0000","FF0000"]}]}

// Fader scribble strips
{"Command":["SERIAL",{"SetFaderColours":["A","FF0000","FF0000"]}]}

// Accent color
{"Command":["SERIAL",{"SetSimpleColour":["Accent","FF0000"]}]}

// Global color
{"Command":["SERIAL",{"SetGlobalColour":"FF0000"}]}
```

**Important Notes**:
- Uses British spelling: `SetButtonColours`, `SetFaderColours`, `SetSimpleColour`
- Colors are 6-char hex WITHOUT `#` prefix
- `SetButtonColours` takes (ButtonId, String, Optional<String>) - NOT an object!

**Parallel Updates**:
- All buttons update simultaneously using `Task.WhenAll()`
- No sequential delays - instant synchronized lighting

---

### 4. ? iRacing Flag Support

**Flags Implemented** (from iRacing telemetry):
1. Green - Solid green (race start/clear)
2. Yellow - Solid yellow (caution)
3. Yellow Waving - Slow flash 500ms (danger)
4. Blue - Solid blue (being lapped)
5. White - Solid white (final lap)
6. Red - Solid red (session stopped)
7. Black - Solid red (disqualification - uses red for visibility)
8. Checkered - Fast flash 250ms (race end)
9. Debris - Slow flash 500ms (surface warning)
10. One Lap to Green - Medium flash 700ms (restart soon)

**Flashing Implementation**:
- Timer-based software flashing (GoXLR has no hardware flash)
- SimHub-compatible timing (250ms/500ms/700ms)
- Proper state save/restore when flags clear

**Flag Detection**:
```csharp
private FlagStatus ParseFlagStatus(uint sessionFlags)
{
    const uint CheckeredFlag = 0x00000001;
    const uint WhiteFlag = 0x00000002;
    const uint GreenFlag = 0x00000004;
    const uint YellowFlag = 0x00000008;
    const uint RedFlag = 0x00000010;
    const uint BlueFlag = 0x00000020;
    const uint DebrisFlag = 0x00000040;
    const uint CrossedFlag = 0x00000080; // Not shown in UI (rare)
    const uint YellowWaving = 0x00000100;
    const uint OneLapToGreen = 0x00000200;
    const uint BlackFlag = 0x00010000;
    // Priority order determines which flag shows if multiple are set
}
```

---

### 5. ? Lighting Tab UI

**Features**:
- **Device Detection**: Auto-loads available buttons after 1-second detection delay
- **Button Selection**: Checkboxes for each available LED/button
- **Flag Test Buttons**: All iRacing flags with tooltips
- **Real-time Updates**: Changes apply immediately
- **Device Status**: Shows connected devices and active button count

**Layout**:
```
???????????????????????????????????????
? Settings                            ?
? ? Enable flag lighting             ?
???????????????????????????????????????

???????????????????????????????????????
? Active Buttons (GoXLR)              ?
? ? Fader1Mute  ? Fader2Mute         ?
? ? Fader3Mute  ? Fader4Mute         ?
? ? Bleep       ? Cough              ?
? ? Global      ? Accent             ?
???????????????????????????????????????

???????????????????????????????????????
? Test Flag Lighting                   ?
? [Green] [Yellow] [Yellow Waving]    ?
? [Blue]  [White]  [Debris]           ?
? [Red]   [Black]  [Checkered]        ?
? [One Lap to Green] [Clear]          ?
???????????????????????????????????????
```

---

### 6. ? Architecture Optimizations

**Single Source of Truth**:
- GoXLRService initializes once, shared across all features
- No duplicate connection checks
- Plugins receive service instance, don't check hardware independently

**Device Reinitialization**:
- Clears old devices before creating new ones
- Prevents duplicate device instances when config changes
- Unchecked buttons properly stop receiving updates

**Graceful Failure**:
- Devices created even if GoXLR not connected
- API calls fail silently with logging
- No cascading failures

---

## ?? Known Issues / In Progress

### 1. Global Color Not Visually Applying

**Status**: Commands work (return "Ok") but may not affect hardware LEDs

**Testing Commands**:
```powershell
# Test 1: Global to Red
$body = '{"Command":["S220202153DI7",{"SetGlobalColour":"FF0000"}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body

# Test 2: Global to Green
$body = '{"Command":["S220202153DI7",{"SetGlobalColour":"00FF00"}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body

# Test 3: Accent to Orange
$body = '{"Command":["S220202153DI7",{"SetSimpleColour":["Accent","FF8800"]}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body
```

**What's Known**:
- ? Command structure is correct
- ? API accepts commands (returns "Ok")
- ? LEDs may or may not change (user testing in progress)
- ? May require animation mode to be active

**Debugging Added**:
- Extensive logging in `SetGlobalColourAsync`
- Shows serial, color, JSON payload, HTTP response
- Check console output for details

**Possible Causes**:
1. Global color only applies with certain animation modes active
2. Hardware limitation on how Global color works
3. Get-devices doesn't reflect Global changes (but hardware does)
4. Requires additional API call to apply changes

**Next Steps for Future Thread**:
1. Get user feedback on which test commands actually change LEDs
2. If none work, investigate animation mode requirements
3. Check if `SetAnimationMode("None")` helps
4. Consider whether Global is just not supported for this use case

---

## ?? Documentation Created

### Complete API References
1. **`docs/GOXLR_API_COMPLETE.md`** - Start here for API overview
   - Connection methods (HTTP, WebSocket, Unix Socket)
   - Command structure (exact format with examples)
   - Common commands with working code
   - Testing & debugging guide
   - Common pitfalls (6 mistakes documented)
   - Complete examples (C#, PowerShell, curl)

2. **`docs/GOXLR_BUTTON_API.md`** - Button color specifics
   - All button IDs for Mini and Full
   - Color format (6-char hex)
   - Command examples
   - Mini capabilities (4 faders, animations supported)
   - Rate limiting guidelines

3. **`docs/PLUGIN_SYSTEM.md`** - Plugin architecture
   - How to create new plugins
   - Configuration system
   - When to use plugins vs direct usage
   - Single source of truth pattern
   - Future extension points

4. **`docs/FLAG_LIGHTING.md`** - Flag system overview
   - Flag meanings and behaviors
   - Lighting patterns
   - Device integration
   - User guide

---

## ?? Technical Implementation Details

### Key Files Modified/Created

**Models**:
- `GoXLRCommand.cs` - Command factory methods (SetButtonColours, SetFaderColours, SetGlobalColour)
- `GoXLRDeviceStatus.cs` - Added ButtonDown, DeviceType, ColourWay properties
- `FlagStatus.cs` - Flag enum matching iRacing telemetry

**Services**:
- `GoXLRService.cs` - Added GetDeviceTypeAsync(), routes button/fader/global commands
- `GoXLRApiClient.cs` - HTTP client with SetButtonColourAsync, SetGlobalColourAsync, SetFaderColorsAsync
- `GoXLRLightingDevice.cs` - ILightingDevice implementation, parallel updates, timer-based flashing
- `GoXLRLightingPlugin.cs` - Plugin with device detection, button filtering
- `LightingService.cs` - Plugin manager, flag mapping to colors, device coordination
- `ILightingDevice.cs` - Interface for lighting devices
- `ILightingDevicePlugin.cs` - Plugin interface with configuration system

**UI**:
- `LightingTab.xaml` - Flag test buttons, device selection UI
- `LightingTab.xaml.cs` - Dynamic button loading, configuration handling

### Important Code Patterns

**Command Structure** (CRITICAL - don't change!):
```csharp
new GoXLRCommandRequest
{
    Command = new object[]
    {
        serialNumber,
        new Dictionary<string, object[]>
        {
            { "SetButtonColours", new object[] { "Fader1Mute", "FF0000", "FF0000" } }
        }
    }
};
```

**Parallel LED Updates**:
```csharp
var tasks = _activeButtons.Select(button => SetButtonColorAsync(button, color)).ToList();
await Task.WhenAll(tasks); // All at once!
```

**Device Type Detection**:
```csharp
var deviceType = status.Hardware.DeviceType; // "Mini" or "Full"
```

---

## ?? Testing Checklist

### Working Features ?
- [x] Button color changes (Fader mutes, Bleep, Cough)
- [x] Fader scribble strip colors
- [x] Accent color
- [x] Flashing flags (Yellow Waving, Checkered, etc.)
- [x] Flag test buttons
- [x] Device type detection
- [x] Button selection (unchecking works)
- [x] Parallel LED updates (simultaneous)
- [x] Flag priority (Checkered > Red > Black > etc.)

### Needs Testing ??
- [ ] Global color (commands work, visual effect unclear)
- [ ] Animation mode interaction with Global
- [ ] Multiple devices (if user adds Philips Hue later)
- [ ] Connection recovery after GoXLR Utility restart

### Not Implemented Yet ?
- [ ] Save button selection to settings
- [ ] Enable/disable lighting toggle
- [ ] Per-flag color customization
- [ ] Animation mode control from app
- [ ] Other lighting devices (Hue, Nanoleaf)

---

## ?? Quick Start for Next Thread

### If Continuing Global Color Work:

1. **Read**: `docs/GOXLR_API_COMPLETE.md` and `docs/GOXLR_BUTTON_API.md`
2. **Test**: Run the three PowerShell commands above, get user feedback on which works
3. **Check Logs**: Look for `[GoXLR] SetGlobalColour` messages in console
4. **If Still Not Working**: 
   - Check animation mode: `curl http://localhost:14564/api/get-devices | jq '.mixers.S220202153DI7.lighting.animation.mode'`
   - Try setting animation: `{"Command":["SERIAL",{"SetAnimationMode":"None"}]}`
   - Consider if Global just doesn't work for this use case

### If Adding New Features:

**Adding a New Lighting Device**:
1. Implement `ILightingDevice` (colors, flashing, state save/restore)
2. Implement `ILightingDevicePlugin` (discovery, config, device creation)
3. Register in `App.xaml.cs`: `_lightingService.RegisterPlugin(new MyPlugin())`
4. Done! UI auto-generates, system coordinates

**Adding More GoXLR Controls**:
- Check `docs/lib.rs` for available commands
- Add factory method to `GoXLRCommand.cs`
- Add API client method to `GoXLRApiClient.cs`
- Use from appropriate service

---

## ?? Common Mistakes to Avoid

1. **Don't** use objects for button colors - just strings!
   ```csharp
   // WRONG: new ButtonColours { ColourOne = "FF0000", ColourTwo = "FF0000" }
   // RIGHT: new object[] { "Fader1Mute", "FF0000", "FF0000" }
   ```

2. **Don't** check hardware availability in plugins - let devices fail gracefully

3. **Don't** forget British spelling: `SetButtonColours`, `SetFaderColours`

4. **Don't** include `#` in hex colors: `FF0000` not `#FF0000`

5. **Don't** use static properties for device-specific lists (breaks detection)

6. **Don't** forget to clear devices before reinitializing

---

## ?? Important Resources

- **GoXLR Utility API**: https://github.com/GoXLR-on-Linux/goxlr-utility/wiki/The-GoXLR-Utility-API
- **Rust Source (commands)**: `docs/lib.rs` (saved in project)
- **Get Devices**: `http://localhost:14564/api/get-devices` (current state)
- **Command Endpoint**: `http://localhost:14564/api/command` (POST)

---

## ?? Key Learnings from This Session

1. **Mini has more features than expected**: 4 faders, animation support
2. **Global color is different**: Uses `SetGlobalColour` not `SetSimpleColour`
3. **Parallel updates work**: Much faster than sequential
4. **Device detection is reliable**: Hardware.DeviceType field is accurate
5. **Plugins are powerful**: Easy to extend with new devices
6. **Documentation matters**: Comprehensive docs prevent future confusion

---

**Status**: Ready for next session  
**Next Priority**: Resolve Global color issue  
**Blockers**: None (can proceed with other features)  

**Last Updated**: February 2026  
**Session Duration**: ~3 hours  
**Commits**: 15+ with all features working except Global color visual feedback
