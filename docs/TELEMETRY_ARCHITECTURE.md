# Telemetry Architecture Refactoring

## Overview

The telemetry system has been refactored to be completely self-contained and independent from the iRacing process monitor. This creates better separation of concerns and makes the system more modular.

## Previous Architecture (Problematic)

```
iRacingMonitorService
??? Monitors: iRacing.exe process
??? Events: iRacingStateChanged (running/stopped)
??? Used by: External apps + Telemetry recording

iRacingTelemetryProvider
??? Reads: Shared memory telemetry data
??? Events: TelemetryUpdated

TelemetryDebugTab
??? Depends on: BOTH services
??? Problem: Mixed concerns (process vs data)
```

**Issues:**
- ? Process running ? Valid telemetry data
- ? iRacing at menu = No telemetry
- ? iRacing in replay = Limited telemetry
- ? Tight coupling between unrelated concerns
- ? Can't use telemetry without process monitor

## New Architecture (Clean & Modular)

```
iRacingMonitorService
??? Purpose: Process monitoring ONLY
??? Monitors: iRacing.exe process lifecycle
??? Events: iRacingStateChanged
??? Used by: External application manager

iRacingTelemetryProvider (Self-Contained)
??? Purpose: Telemetry reading + Session detection
??? Reads: Shared memory
??? Detects: Valid session data availability
??? Events: ConnectionChanged, TelemetryUpdated
??? Independent: Doesn't need process monitor

TelemetryService (Coordinator)
??? Manages: All telemetry providers
??? Events: ConnectionChanged (when telemetry available)
??? Used by: Recording, playback, UI

TelemetryRecorder
??? Listens to: TelemetryService.ConnectionChanged
??? Records when: Valid telemetry data exists
??? Independent: Self-contained logic

TelemetryDebugTab
??? Depends on: TelemetryService ONLY
??? Completely decoupled from process monitoring
```

## Benefits

### ? **Separation of Concerns**
- **iRacingMonitorService**: Process lifecycle management
- **Telemetry System**: Data availability detection

### ? **Sim-Agnostic**
- Works with any sim that provides telemetry
- Not tied to iRacing-specific process monitoring
- Can add other sims without touching process monitor

### ? **Better Detection**
- Records when telemetry is *actually available*
- Not when process is just *running*
- Handles menu, replay, and session states correctly

### ? **Modularity**
- Telemetry can be used without iRacingMonitorService
- Process monitor can work without telemetry
- Each component has single responsibility

### ? **Testability**
- Can test telemetry in isolation
- Can mock telemetry service for UI tests
- No cross-dependencies

## Key Changes

### 1. TelemetryDebugTab Constructor
**Before:**
```csharp
public TelemetryDebugTab(TelemetryService telemetryService, iRacingMonitorService iRacingMonitor)
```

**After:**
```csharp
public TelemetryDebugTab(TelemetryService telemetryService)
```

### 2. Recording Trigger
**Before:**
```csharp
// Waited for iRacing process to start
_iRacingMonitor.iRacingStateChanged += OniRacingStateChanged;
```

**After:**
```csharp
// Waits for telemetry connection (valid data)
_telemetryService.ConnectionChanged += OnConnectionChanged;
```

### 3. Session Detection
**Before:**
- Mixed: Process state + Telemetry data
- Two sources of truth

**After:**
- Single source: TelemetryService.IsConnected
- True = Valid telemetry data available

## How It Works

### Connection Flow

1. **iRacing Starts**
   - iRacingMonitorService detects process
   - External apps are launched
   - *Telemetry system is independent*

2. **Telemetry Connects**
   - iRacingTelemetryProvider finds shared memory
   - Validates data (status bit, variable scan)
   - Fires ConnectionChanged(true)

3. **Recording Starts**
   - TelemetryDebugTab receives ConnectionChanged
   - If recording was pending, starts now
   - Records valid telemetry data

4. **Session Ends**
   - Telemetry data becomes invalid
   - iRacingTelemetryProvider fires ConnectionChanged(false)
   - Recording auto-stops

### Recording Workflow

```
User clicks "Start Recording"
??> Check TelemetryService.IsConnected
    ??> TRUE: Start recording immediately
    ??> FALSE: Set _pendingRecording = true
            ??> Wait for ConnectionChanged(true)
                ??> Start recording automatically
```

## Files Changed

### Modified
- `SimControlCentre\Views\Tabs\TelemetryDebugTab.xaml.cs` - Removed iRacingMonitor dependency
- `SimControlCentre\Views\Tabs\SettingsTab.xaml.cs` - Updated constructor call
- `SimControlCentre\App.xaml.cs` - Removed GetiRacingMonitor method

### Unchanged (Proof of Modularity)
- `SimControlCentre\Services\iRacingMonitorService.cs` - Still works independently
- `SimControlCentre\Services\TelemetryService.cs` - Already was a coordinator
- `SimControlCentre\Services\iRacingTelemetryProvider.cs` - Already self-detecting

## Future Extensibility

### Adding Other Sims
```csharp
// Add Assetto Corsa telemetry
var acProvider = new AssettoCorsTelemetryProvider();
_telemetryService.RegisterProvider(acProvider);

// Recording works automatically - no changes needed!
// TelemetryDebugTab works - no changes needed!
```

### Multiple Sims Running
```csharp
// TelemetryService automatically switches to active provider
// Recording captures whichever sim has valid telemetry
// No process monitoring logic needed
```

## Testing Strategy

### Unit Tests (Now Possible)
```csharp
// Test recording without real iRacing
var mockTelemetry = new MockTelemetryService();
var debugTab = new TelemetryDebugTab(mockTelemetry);

// Simulate connection
mockTelemetry.SimulateConnection();

// Assert recording started
Assert.IsTrue(mockTelemetry.Recorder.IsRecording);
```

### Integration Tests
- Test telemetry reading independently
- Test process monitoring independently
- Test UI with mock services

## Documentation Updates

- ? Architecture diagram shows clear separation
- ? Component responsibilities defined
- ? Dependencies documented
- ? Extension points identified

## Migration Notes

### For Developers
- Remove iRacingMonitorService from telemetry-related code
- Use TelemetryService.ConnectionChanged for session detection
- Use TelemetryService.IsConnected to check telemetry availability

### For Users
- No changes - system works the same
- Better reliability - records actual telemetry
- Improved session detection

## Conclusion

This refactoring follows the **Single Responsibility Principle** and creates a more maintainable, testable, and extensible system. Each component now does one thing well:

- **iRacingMonitorService**: Process lifecycle management
- **TelemetryService**: Telemetry coordination
- **TelemetryRecorder**: Data recording
- **UI**: User interaction

The system is now properly modular and ready for future enhancements.

---

*Refactored: February 2026*
*Architecture: Self-contained telemetry system*
