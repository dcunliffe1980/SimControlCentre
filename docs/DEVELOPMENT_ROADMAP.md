# Development Roadmap

**Version**: Post-v1.2.0  
**Status**: Planning & Implementation  
**Last Updated**: February 2026

---

## Overview

This roadmap outlines the evolution of SimControlCentre from a GoXLR-focused app to a comprehensive device control system with extensible plugin architecture.

---

## Phase 1: Quick Wins & Foundation (1-2 sessions) ? IN PROGRESS

### 1.1 Lighting Tab Cleanup ? COMPLETE (Commit: b8829d4)
**Goal**: Make the lighting tab more professional and user-friendly

**Completed Changes**:
- ? Removed "coming soon" devices (Hue, Nanoleaf)
- ? Removed "Connected Devices" section (unnecessary)
- ? Improved button layout and formatting
- ? Added friendly display names (Fader1Mute ? Fader 1 Mute)
- ? Reordered buttons logically: Global, Accent, Faders 1-4, Fader Mutes 1-4, Bleep, Cough
- ? Better test flags layout (UniformGrid, 4 columns, consistent sizing)
- ? Renamed "Flag Lighting" ? "Lighting"

**Files Modified**:
- `SimControlCentre\Services\GoXLRLightingPlugin.cs` - Display name mapping
- `SimControlCentre\Views\Tabs\LightingTab.xaml` - Layout improvements
- `SimControlCentre\Views\Tabs\LightingTab.xaml.cs` - Tag-based selection
- `SimControlCentre\MainWindow.xaml` - Tab rename

### 1.2 Plugin Settings Architecture ?? NEXT
**Goal**: Establish consistent plugin management

**To Do**:
- [ ] Create centralized "Plugins" tab
- [ ] Move GoXLR plugin enable/disable from Lighting tab
- [ ] Structure for plugin components (Device Control + Lighting)
- [ ] Save settings properly
- [ ] Test enable/disable functionality

**Decision**: Centralized "Plugins" tab ? APPROVED

**Structure**:
```
Settings Window
??? General
??? Channels & Profiles
??? Hotkeys
??? Controllers
??? External Apps
??? Lighting Settings (flag enable/disable, colors)
??? Plugins ? NEW
    ??? GoXLR Plugin (enable/disable)
    ?   ??? Device Control (channels/profiles)
    ?   ?   ??? Settings: Serial, API endpoint
    ?   ??? Lighting (flag-based)
    ?       ??? Settings: Button selection
    ??? Philips Hue (future)
    ??? Stream Deck (future)
```

**Benefits**:
- Consistent UX across all plugins
- Easy to add new plugins
- Clear enable/disable for hardware not owned
- Sub-components can be toggled independently

---

## Phase 2: Refactor Existing (2-3 sessions)

### 2.1 GoXLR Control ? Device Control Plugin
**Goal**: Convert existing GoXLR control to plugin architecture

**Changes**:
- Create `IDeviceControlPlugin` interface
- Implement `GoXLRDeviceControlPlugin`
- Convert profile switching to plugin action
- Convert volume control to plugin action
- **Add**: Channel mute functionality

**Architecture**:
```csharp
public interface IDeviceControlPlugin
{
    string Id { get; }
    string DisplayName { get; }
    bool IsEnabled { get; }
    
    // Actions the plugin can perform
    List<DeviceAction> GetAvailableActions();
    
    // Execute an action
    Task ExecuteActionAsync(string actionId, Dictionary<string, object> parameters);
}

public class DeviceAction
{
    public string Id { get; set; }              // "switch_profile", "adjust_volume"
    public string DisplayName { get; set; }     // "Switch Profile", "Adjust Volume"
    public List<ActionParameter> Parameters { get; set; }
}
```

**New Feature: Channel Mute**:
- Mute/Unmute individual channels
- Toggle mute state
- Assign to hotkey or controller button

### 2.2 Telemetry Optimization
**Goal**: Only enable telemetry when actively needed

**Changes**:
- Track which components need telemetry
- Auto-enable when lighting plugin enabled
- Auto-disable when no components need it
- Add to plugin interface: `bool RequiresTelemetry { get; }`

**Before**:
```csharp
// Always running
_telemetryService.StartAll();
```

**After**:
```csharp
// Only start if needed
if (_lightingService.Plugins.Any(p => p.RequiresTelemetry))
{
    _telemetryService.StartAll();
}
```

---

## Phase 3: New Features (3-4 sessions)

### 3.1 Controller Overhaul
**Goal**: Better controller configuration and button type support

#### 3.1.1 Remove Device List, Add Controller Management
**Current**: Shows all DirectInput devices automatically

**New**: User explicitly adds controllers they want to use

**UI Flow**:
1. Click "Add Controller"
2. Press any button on the controller to identify it
3. App detects controller and creates config
4. User maps buttons with types

#### 3.1.2 Button Type Support

**Button Types**:
```csharp
public enum ButtonType
{
    Momentary,      // Press/release (keyboard-like)
    Toggle,         // Press to lock, press to unlock
    RotaryEncoder,  // Two directions (CW/CCW)
    Switch,         // Two positions, spring-return center
    Latch,          // Two positions, stays until moved
    Joystick,       // Analog X/Y axis
    
    // Fanatec-specific
    FanatecEncoder, // Three modes: Encoder/Pulse/Constant
}
```

**Button Configuration**:
```csharp
public class ButtonConfig
{
    public int ButtonId { get; set; }
    public string DisplayName { get; set; }     // User-friendly name
    public ButtonType Type { get; set; }
    
    // For RotaryEncoder, Switch, Latch
    public int? SecondaryButtonId { get; set; } // CW/CCW, Up/Down
    
    // For FanatecEncoder
    public FanatecMode? FanatecMode { get; set; }
}
```

**Example: PXN CB1 Configuration**:
```json
{
  "controllerId": "VID_0483&PID_5750",
  "displayName": "PXN CB1",
  "buttons": [
    { "id": 0, "name": "Button 1", "type": "Momentary" },
    { "id": 1, "name": "Button 2", "type": "Toggle" },
    { "id": 4, "name": "Dial 1", "type": "RotaryEncoder", "secondary": 5 },
    { "id": 6, "name": "Switch 1", "type": "Switch", "secondary": 7 },
    { "id": 8, "name": "Latch 1", "type": "Latch", "secondary": 9 }
  ]
}
```

#### 3.1.3 Toggle Mode Feature
**Goal**: One button changes what other inputs do

**Example**:
- Dial normally controls Game volume
- Press toggle button
- Dial now controls Chat volume
- Press toggle again ? back to Game

**Implementation**:
```csharp
public class ToggleMode
{
    public string Id { get; set; }              // "volume_mode_toggle"
    public string DisplayName { get; set; }     // "Volume Mode"
    public int TriggerButtonId { get; set; }    // Button that toggles
    public List<ModeState> States { get; set; } // What changes per state
}

public class ModeState
{
    public string Name { get; set; }            // "Game Volume", "Chat Volume"
    public Dictionary<int, ActionMapping> ButtonMappings { get; set; }
}
```

**UI Feedback**:
- Visual indicator (LED color change on button visual)
- TTS announcement: "Chat volume mode active"
- Status bar indicator

**TTS for VR**:
- Optional per-button
- Configurable message
- Only for specific buttons (like toggle)

---

## Technical Specifications

### Plugin System Enhancements

#### Unified Plugin Interface
```csharp
public interface IPlugin
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    bool IsEnabled { get; set; }
    bool RequiresTelemetry { get; }
    
    // Sub-components
    List<IPluginComponent> Components { get; }
    
    // Configuration
    List<ConfigOption> GetConfigOptions();
    void ApplyConfiguration(Dictionary<string, object> config);
    
    // Lifecycle
    Task InitializeAsync();
    Task ShutdownAsync();
}

public interface IPluginComponent
{
    string Id { get; }
    string DisplayName { get; }
    bool IsEnabled { get; set; }
}
```

#### GoXLR Plugin Structure
```csharp
public class GoXLRPlugin : IPlugin
{
    public string Id => "goxlr";
    public string DisplayName => "GoXLR";
    
    public List<IPluginComponent> Components => new()
    {
        new DeviceControlComponent(),  // Profile/volume control
        new LightingComponent()         // Flag lighting
    };
}
```

### Controller System

#### Controller Configuration Model
```csharp
public class ControllerConfig
{
    public string Id { get; set; }              // Device GUID or VID/PID
    public string DisplayName { get; set; }     // User-friendly name
    public List<ButtonConfig> Buttons { get; set; }
    public List<ToggleMode> ToggleModes { get; set; }
}

public class ButtonConfig
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public ButtonType Type { get; set; }
    public int? SecondaryId { get; set; }       // For dual-button types
    public FanatecMode? FanatecMode { get; set; }
    
    // Action mapping
    public ActionMapping Action { get; set; }
}

public class ActionMapping
{
    public string PluginId { get; set; }        // "goxlr"
    public string ActionId { get; set; }        // "adjust_volume"
    public Dictionary<string, object> Parameters { get; set; }
}
```

---

## Implementation Order Summary

### ? Phase 1: Foundation (Start Here)
1. **Lighting Tab Cleanup** (1 session)
   - Visual improvements
   - Better UX
   - Remove clutter

2. **Plugin Settings Architecture** (1 session)
   - Create Plugins tab
   - Move settings
   - Establish patterns

### Phase 2: Refactor (Next)
3. **GoXLR Device Control Plugin** (1-2 sessions)
   - Convert to plugin
   - Add channel mute
   - Test architecture

4. **Telemetry Optimization** (1 session)
   - Conditional enabling
   - Performance improvement

### Phase 3: New Features (Future)
5. **Controller Overhaul** (3-4 sessions)
   - Add controller management
   - Button type support
   - Toggle modes
   - TTS integration

---

## Design Decisions

### Why Centralized Plugins Tab?
**Pros**:
- ? Consistent UX
- ? Easy discoverability
- ? Clear enable/disable
- ? Scales well

**Cons**:
- ? One more tab
- ? Settings separated from feature

**Decision**: Centralized wins for consistency and scalability

### Why Display Name Mapping?
**Alternative**: Change internal IDs everywhere

**Chosen**: Mapping layer
- Keeps API calls clean
- No breaking changes
- Easy to update UI
- Supports localization later

### Why Plugin Components?
**Alternative**: Separate plugins for each feature

**Chosen**: Sub-components
- Less clutter in plugin list
- Logical grouping
- Shared configuration (serial number)
- Can still enable/disable independently

---

## Future Considerations

### Multi-Device Support
- Multiple GoXLRs (streamers)
- Multiple controllers
- Device profiles per sim/game

### Advanced Controller Features
- Macro recording
- Button combos (Ctrl+button)
- Sequences
- Context-aware mappings (per-app)

### Plugin Marketplace
- Community plugins
- Auto-update
- Dependency management

### Cloud Sync
- Settings backup
- Multi-PC sync
- Profile sharing

---

## Testing Strategy

### Phase 1
- Visual inspection
- Button click tests
- Checkbox functionality

### Phase 2
- Plugin enable/disable
- Settings migration
- Existing features still work

### Phase 3
- Controller detection
- Button type recognition
- Toggle mode switching
- TTS audio

---

## Migration Plan

### For Existing Users

**Lighting Settings**:
- Auto-migrate button selection
- Preserve enabled state
- No action required

**GoXLR Control**:
- Automatically convert to plugin
- Preserve hotkeys
- Preserve controller mappings

**Controllers**:
- Keep existing mappings
- Add button type detection
- User reviews and confirms

---

## Success Criteria

### Phase 1
- ? Lighting tab looks professional
- ? No functional regressions
- ? User can enable/disable plugins easily

### Phase 2
- ? GoXLR control works as plugin
- ? Channel mute functional
- ? Telemetry only when needed

### Phase 3
- ? Can add multiple controllers
- ? Button types work correctly
- ? Toggle mode functional
- ? TTS clear and useful

---

**Next Steps**: Start Phase 1.1 - Lighting Tab Cleanup

**ETA**: 
- Phase 1: 1-2 sessions
- Phase 2: 2-3 sessions  
- Phase 3: 3-4 sessions

**Total**: ~6-9 sessions for complete implementation
