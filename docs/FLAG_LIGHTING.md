# Flag-Based Lighting System

## Overview

The flag-based lighting system provides visual indicators for racing flags using connected lighting devices. The system is modular and extensible, currently supporting GoXLR LEDs with easy integration for future devices like Philips Hue.

## Architecture

### Modular Design

```
ILightingDevice (interface)
??? GoXLRLightingDevice (GoXLR button LEDs)
??? PhilipsHueLightingDevice (future)

LightingService (coordinator)
??? Manages multiple devices
??? Handles flag changes from telemetry
??? Controls flashing animations
??? Restores previous states

TelemetryService
??? FlagChanged event ? LightingService
```

### Components

#### **ILightingDevice** (`Services\ILightingDevice.cs`)
Abstract interface for any lighting device:
- `SetColorAsync(color)` - Set solid color
- `StartFlashingAsync(color1, color2, intervalMs)` - Start flashing animation
- `StopFlashingAsync()` - Stop flashing
- `SaveStateAsync()` - Save current state before flag
- `RestoreStateAsync()` - Restore saved state after flag

#### **GoXLRLightingDevice** (`Services\GoXLRLightingDevice.cs`)
GoXLR implementation using fader mute button LEDs:
- Uses Fader 1-4 mute buttons (most visible)
- Supports all standard colors
- Handles flashing with timer
- Saves/restores previous button states

#### **LightingService** (`Services\LightingService.cs`)
Coordinates all lighting devices:
- Registers multiple devices
- Responds to `TelemetryService.FlagChanged` event
- Maps flags to colors and animations
- Manages state saving/restoration

## Flag Mappings

| Flag | Lighting Behavior | Color | Flashing |
|------|------------------|-------|----------|
| Green | Solid green | Green | No |
| Yellow | Solid yellow | Yellow | No |
| Yellow Waving | Flashing yellow | Yellow/Off | 500ms |
| Red | Solid red | Red | No |
| Blue | Solid blue | Blue | No |
| White | Solid white | White | No |
| Checkered | Flashing white | White/Off | 300ms |
| Black | Flashing red (warning) | Red/Off | 500ms |
| Debris | Solid orange | Orange | No |
| One Lap To Green | Flashing green | Green/Off | 700ms |
| None | Restore previous state | - | - |

## Real-Life Behavior

The system mimics real racing flag behavior:

### **Flashing Flags**
- **Yellow Waving**: 500ms flash cycle (waving caution)
- **Checkered**: 300ms flash cycle (rapid victory flash)
- **Black Flag**: 500ms flash cycle (warning to driver)
- **One Lap to Green**: 700ms flash cycle (slow warning)

### **State Management**
1. **Flag appears**: Current lighting state is saved
2. **Flag active**: Lighting shows flag color/animation
3. **Flag clears**: Previous state is restored automatically

## GoXLR Button Mapping

**Used Buttons:**
- Fader 1 Mute
- Fader 2 Mute
- Fader 3 Mute
- Fader 4 Mute

*These buttons are chosen because they're the most visible on the GoXLR*

**Color Support:**
- Red
- Green
- Blue
- Yellow
- White
- Orange
- Purple
- Off

## Usage

### Automatic Operation

The system works automatically once initialized:

```csharp
// In App.xaml.cs (already configured)

// Initialize lighting service
_lightingService = new LightingService();

// Register GoXLR as lighting device
var goxlrLighting = new GoXLRLightingDevice(_goXLRService, Settings);
_lightingService.RegisterDevice(goxlrLighting);

// Connect to telemetry flag changes
_telemetryService.FlagChanged += async (s, e) =>
{
    await _lightingService.UpdateForFlagAsync(e.NewFlag);
};
```

### Adding New Devices

To add a new lighting device (e.g., Philips Hue):

```csharp
// 1. Create device implementation
public class PhilipsHueLightingDevice : ILightingDevice
{
    public string DeviceName => "Philips Hue";
    public bool IsAvailable => /* check if bridge is connected */;
    
    public async Task SetColorAsync(LightingColor color)
    {
        // Call Philips Hue API to set light color
    }
    
    public async Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs)
    {
        // Implement flashing for Hue lights
    }
    
    // ... implement other methods
}

// 2. Register with lighting service
var hueLighting = new PhilipsHueLightingDevice(hueConfig);
_lightingService.RegisterDevice(hueLighting);

// That's it! The device will now respond to flags automatically
```

## Configuration

### Enable/Disable (Future Enhancement)

Add to `AppSettings`:
```csharp
public class GeneralSettings
{
    // Existing settings...
    public bool EnableFlagLighting { get; set; } = true;
}
```

Conditional registration:
```csharp
if (Settings.General.EnableFlagLighting)
{
    var goxlrLighting = new GoXLRLightingDevice(_goXLRService, Settings);
    _lightingService.RegisterDevice(goxlrLighting);
}
```

### Device-Specific Settings (Future Enhancement)

```csharp
public class LightingSettings
{
    public bool UseGoXLR { get; set; } = true;
    public bool UsePhilipsHue { get; set; } = false;
    public string PhilipsHueBridgeIP { get; set; } = "";
    public List<string> HueLightIds { get; set; } = new();
}
```

## Technical Details

### Flashing Implementation

Flashing uses a `Timer` that alternates between two colors:

```csharp
_flashTimer = new Timer(async _ => await FlashUpdate(), null, 0, intervalMs);

private async Task FlashUpdate()
{
    _flashState = !_flashState;
    var color = _flashState ? _flashColor1 : _flashColor2;
    await SetColorAsync(color);
}
```

### State Management

States are saved/restored to prevent disrupting user's lighting setup:

```csharp
// When flag appears (first time)
await device.SaveStateAsync(); // Store current colors

// While flag is active
await device.SetColorAsync(flagColor); // Show flag

// When flag clears
await device.RestoreStateAsync(); // Return to previous state
```

### Multiple Device Coordination

All registered devices update simultaneously:

```csharp
foreach (var device in _devices.Where(d => d.IsAvailable))
{
    await ApplyFlagToDeviceAsync(device, flag);
}
```

## Limitations

### Current
- GoXLR API button color control not yet implemented (TODO)
- Button state save/restore is placeholder
- No UI settings to enable/disable

### Future Enhancements
- Full GoXLR API integration for button colors
- Query and restore actual button states
- Philips Hue integration
- Nanoleaf integration
- LIFX integration
- UI settings for device selection
- Brightness control
- Custom color mapping
- Per-flag device selection

## Testing

### Without Hardware
- Logs show all lighting commands
- Can verify flag detection and mapping
- Timer behavior confirmed in logs

### With GoXLR
- Visual confirmation of LED changes
- Flashing animations visible
- State restoration verified

### With iRacing
1. Start iRacing and join a session
2. Observe LEDs during:
   - Race start (green flag)
   - Caution (yellow/yellow waving)
   - Final lap (white flag)
   - Race finish (checkered flag)
3. Exit session - LEDs should restore previous state

## Benefits

### Modularity
- Easy to add new devices
- No coupling between devices
- Single service manages all

### Real-World Feel
- Authentic flag flashing patterns
- Non-intrusive state restoration
- Automatic operation

### Extensibility
- Interface-based design
- Multiple devices simultaneously
- Easy configuration

## Files

### Created
- `SimControlCentre\Services\ILightingDevice.cs` - Interface
- `SimControlCentre\Services\GoXLRLightingDevice.cs` - GoXLR implementation
- `SimControlCentre\Services\LightingService.cs` - Coordinator

### Modified
- `SimControlCentre\App.xaml.cs` - Initialization and wiring
- `SimControlCentre\Services\GoXLRService.cs` - Added `SetButtonColorAsync`

---

*Feature Added: February 2026*
*Architecture: Modular lighting system for racing flags*
