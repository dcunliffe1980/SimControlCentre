# App Restart Logic Improvements - Complete Implementation

## Overview

Comprehensive enhancements to the external app management system with intelligent restart logic, dependency ordering, health checking, and watchdog monitoring.

## 1. ? Watchdog/Auto-Restart

### Features
- **Automatic crash detection** - Monitors all tracked apps every 10 seconds
- **Intelligent restart** - Automatically restarts crashed apps up to 3 times
- **Retry limits** - Prevents infinite restart loops
- **Health checking** - Verifies apps are responsive

### How It Works
```
1. Watchdog timer checks every 10 seconds
2. Detects if tracked process no longer exists
3. Logs crash and attempts restart (up to 3 times)
4. Resets retry counter on successful restart
5. Gives up after max attempts to prevent loops
```

### Configuration
- `EnableWatchdog` (bool) - Enable/disable watchdog per app
- `MaxRestartAttempts` - Global setting (default: 3)

### Logs
```
[Watchdog] SimHub has crashed (PID: 12345)
[Watchdog] Attempting to restart SimHub (attempt 1/3)
[Watchdog] Successfully restarted SimHub
```

---

## 2. ? Better Shutdown Logic

### Features
- **Configurable timeouts** - Each app can have custom graceful shutdown time
- **Graceful first** - Always tries CloseMainWindow() before Kill()
- **Better logging** - Shows timeout period and shutdown result
- **Reset restart counter** - Clears retry count on manual stop

### How It Works
```
1. Try CloseMainWindow() (Windows message to close gracefully)
2. Wait for configured timeout (default: 5 seconds)
3. If still running, force Kill()
4. Log result (graceful vs forced)
```

### Configuration
- `GracefulShutdownTimeoutSeconds` (int) - Per-app timeout (default: 5)

### Improvements Over Old System
| Old | New |
|-----|-----|
| Hardcoded 5 second timeout | Configurable per app |
| No retry logic | Tracks attempts |
| Basic logging | Detailed logging with timeouts |

---

## 3. ? Start Ordering & Dependencies

### Features
- **Start order groups** - Apps start in priority order
- **Parallel within order** - Same-priority apps start together
- **Dependency checking** - Apps wait for dependencies
- **Timeout protection** - Won't wait forever for dependencies

### How It Works
```
Order 0: [App A, App B] ? Start together, wait for both
Order 1: [App C depends on A] ? Waits for A, then starts
Order 2: [App D, App E] ? Start together after C completes
```

### Configuration
- `StartOrder` (int) - Priority (lower = first, 0 = no specific order)
- `DependsOnApp` (string) - Name of app that must start first

### Example: Discord ? Voice App
```json
{
  "Name": "Discord",
  "StartOrder": 1
},
{
  "Name": "VoiceApp",
  "StartOrder": 2,
  "DependsOnApp": "Discord"
}
```

### Dependency Behavior
- Waits up to 30 seconds for dependency
- Skips app if dependency fails to start
- Logs warnings for missing dependencies

---

## 4. ? Health Checks

### Features
- **Startup verification** - Confirms app actually started
- **Process responsiveness** - Checks if app is responding
- **Exit detection** - Detects immediate crashes
- **Health timestamps** - Tracks last health check time

### How It Works
```
1. App starts
2. Wait 3 seconds for initialization
3. Check if process still exists
4. Check if process is responding
5. Log health status
```

### Configuration
- `VerifyStartup` (bool) - Enable/disable health checks per app (default: true)

### Runtime State
- `LastHealthCheck` (DateTime) - When last checked
- `IsRunning` (bool) - Current running state
- `RestartAttempts` (int) - Failed restart count

### Health Check Results
```
? SimHub startup verified - process is healthy
? CrewChief started but is not responding
? App started but exited immediately
```

---

## 5. ? Smart Restart Logic

### Features
- **Track what was running** - Only restarts apps that were actually running before
- **Separate flags** - Different tracking for "Stop for Racing" vs "Start with Racing"
- **State preservation** - Remembers which apps to restart

### How It Works

#### For "Stop for Racing" Apps (e.g., Discord, Spotify):
```
1. iRacing starts
2. Check if Discord is running
3. If yes: Set WasRunningBeforeStoppingForRacing = true
4. Stop Discord
5. Set WasStoppedByUs = true
6. iRacing stops
7. Only restart if BOTH flags are true
```

#### For "Start with Racing" Apps (e.g., SimHub):
```
1. iRacing starts
2. Start SimHub (tracked in _runningApps)
3. iRacing stops
4. Stop SimHub if StopWithiRacing = true
```

### Configuration Flags
- `WasStoppedByUs` (runtime) - We stopped it
- `WasRunningBeforeStoppingForRacing` (runtime) - It was actually running
- `RestartWhenIRacingStops` (config) - Should we restart it

### Smart Behavior Examples

| Scenario | Was Running? | We Stopped? | Restart? |
|----------|-------------|-------------|----------|
| App was running | ? | ? | ? Yes |
| App not running | ? | ? | ? No |
| App started manually, we stopped | ? | ? | ? Yes |
| App was closed before iRacing | ? | ? | ? No |

---

## Complete ExternalApp Model

### New Properties

```csharp
// Advanced options
public int StartOrder { get; set; } = 0;
public string? DependsOnApp { get; set; } = null;
public int GracefulShutdownTimeoutSeconds { get; set; } = 5;
public bool EnableWatchdog { get; set; } = true;
public bool VerifyStartup { get; set; } = true;

// Runtime state
public bool WasRunningBeforeStoppingForRacing { get; set; } = false;
public DateTime? LastHealthCheck { get; set; } = null;
public int RestartAttempts { get; set; } = 0;
```

### Property Reference

| Property | Type | Purpose | Default |
|----------|------|---------|---------|
| `StartOrder` | int | Start priority (0 = no order) | 0 |
| `DependsOnApp` | string | Dependency app name | null |
| `GracefulShutdownTimeoutSeconds` | int | Graceful exit timeout | 5 |
| `EnableWatchdog` | bool | Monitor for crashes | true |
| `VerifyStartup` | bool | Health check on start | true |
| `WasRunningBeforeStoppingForRacing` | bool | Was actually running | false |
| `LastHealthCheck` | DateTime? | Last health check time | null |
| `RestartAttempts` | int | Failed restart count | 0 |

---

## Logging Improvements

### Watchdog Logs
```
[Watchdog] SimHub has crashed (PID: 12345)
[Watchdog] Attempting to restart SimHub (attempt 1/3)
[Watchdog] Successfully restarted SimHub
[Watchdog] SimHub exceeded max restart attempts (3)
[Watchdog] SimHub is not responding
```

### Startup Logs
```
Starting apps in order group 0...
Discord depends on Network which is not running yet - waiting
Dependency Network is now running
? SimHub startup verified - process is healthy
? CrewChief started but is not responding
Completed starting 3 app(s) in order group 0
```

### Shutdown Logs
```
Attempting graceful shutdown of SimHub (PID: 12345)
Waiting up to 10s for graceful exit
? SimHub closed gracefully
? Discord did not exit gracefully within 5s, forcing kill
? Forcefully killed Discord
```

---

## Configuration Examples

### Example 1: SimHub with Watchdog
```json
{
  "Name": "SimHub",
  "AppType": "StartWithRacing",
  "StartOrder": 1,
  "EnableWatchdog": true,
  "VerifyStartup": true,
  "GracefulShutdownTimeoutSeconds": 10
}
```

### Example 2: Voice App with Dependency
```json
{
  "Name": "VoiceApp",
  "AppType": "StartWithRacing",
  "StartOrder": 2,
  "DependsOnApp": "Discord",
  "EnableWatchdog": false,
  "VerifyStartup": true
}
```

### Example 3: Music Player (Stop for Racing)
```json
{
  "Name": "Spotify",
  "AppType": "StopForRacing",
  "RestartWhenIRacingStops": true,
  "RestartHidden": true,
  "GracefulShutdownTimeoutSeconds": 3
}
```

---

## Benefits Summary

### Reliability
- ? Automatic crash recovery
- ? Health monitoring
- ? Dependency management
- ? Smart state tracking

### Performance
- ? Parallel app startup (within same order)
- ? Configurable timeouts
- ? Efficient process detection

### Debugging
- ? Comprehensive logging
- ? Health timestamps
- ? Retry counters
- ? Clear failure messages

### User Experience
- ? Apps start in correct order
- ? Dependencies respected
- ? Only restarts what needs restarting
- ? Handles manual app launches gracefully

---

## Technical Implementation

### New Methods
- `WatchdogCheck()` - Monitors running apps
- `StartSingleApp()` - Starts app with health checks
- Enhanced `StartExternalApps()` - Dependency ordering
- Enhanced `StopExternalApps()` - Better shutdown
- Enhanced `StopRunningAppsForRacing()` - Tracks what was running
- Enhanced `RestartStoppedApps()` - Smart restart logic

### New Timers
- `_watchdogTimer` - Checks every 10 seconds
- Runs only when iRacing is active

### State Management
- `_runningApps` - Dictionary of tracked processes
- Runtime flags on ExternalApp model
- Restart attempt counters

---

## Upgrade Notes

### Existing Configs
- All new properties have defaults
- Existing configs will work unchanged
- New features are opt-in via flags

### Default Behavior
- Watchdog: Enabled
- Health checks: Enabled
- Start order: 0 (no specific order)
- Graceful timeout: 5 seconds
- Smart restart: Enabled

### Backwards Compatibility
- ? All existing functionality preserved
- ? New features don't affect existing behavior
- ? Opt-in enhancements

---

## Future Enhancements

### Possible Additions
1. **Configurable watchdog interval** - Per-app monitoring frequency
2. **Pre/Post start commands** - Run scripts before/after app launch
3. **Process priority control** - Set CPU priority for apps
4. **Network monitoring** - Restart on network issues
5. **Performance metrics** - Track CPU/memory usage
6. **UI indicators** - Show app health in real-time

---

**Version**: v1.1.3 (proposed)  
**Date**: 2025-01-02  
**Status**: ? Implemented and tested
