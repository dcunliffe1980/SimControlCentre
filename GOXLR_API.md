# GoXLR API Client Implementation

## Overview
Complete HTTP client for GoXLR Utility API with volume caching, profile management, and error handling.

## Components Created

### Models

#### GoXLRDeviceStatus.cs
Response models for device status endpoint:
- `GoXLRDeviceStatusResponse` - Root response wrapper
- `GoXLRDevice` - Individual device with profile name, volumes, and files
- `GoXLRLevels` - Volume levels for all channels
- `GoXLRFiles` - Available profiles list

#### GoXLRCommand.cs
Command request models:
- `GoXLRCommandRequest` - Base command structure
- `SetVolume(serialNumber, channel, volume)` - Factory method for volume commands
- `LoadProfile(serialNumber, profileName)` - Factory method for profile commands

### Services

#### GoXLRApiClient.cs
Low-level HTTP client with caching:

**Key Features:**
- Volume caching (configurable duration, default 5 seconds)
- Device status caching
- Automatic cache invalidation on profile change
- Async/await throughout
- Error handling and logging
- Connection health check

**Key Methods:**
- `GetDeviceStatusAsync(serialNumber)` - Gets full device status
- `GetVolumeAsync(serialNumber, channel)` - Gets volume with caching
- `SetVolumeAsync(serialNumber, channel, volume)` - Sets volume (0-255)
- `AdjustVolumeAsync(serialNumber, channel, delta)` - Adjust volume by amount
- `LoadProfileAsync(serialNumber, profileName)` - Load profile
- `GetProfilesAsync(serialNumber)` - Get available profiles list
- `GetCurrentProfileAsync(serialNumber)` - Get active profile name
- `IsConnectedAsync()` - Check if GoXLR Utility is running
- `InvalidateCache()` - Clear all caches

**Caching Strategy:**
1. Volume requests check cache first
2. If cache miss or expired (> 5s), fetch from API
3. Volume changes update cache immediately
4. Profile changes invalidate all caches
5. Device status cached separately for profile/list queries

#### GoXLRService.cs
High-level service integrated with AppSettings:

**Key Features:**
- Wraps GoXLRApiClient with configuration
- Auto-configures from AppSettings
- Provides helper methods with configured serial number
- Volume adjustment with configured step amount
- Percentage calculation for feedback
- Connection status checking

**Key Methods:**
- `AdjustVolumeAsync(channel, increase)` - Adjust by VolumeStep
- `SetVolumeAsync(channel, volume)` - Set specific volume
- `GetVolumeAsync(channel)` - Get current volume
- `LoadProfileAsync(profileName)` - Load profile
- `GetProfilesAsync()` - Get available profiles
- `GetCurrentProfileAsync()` - Get active profile
- `IsConnectedAsync()` - Check connection
- `Reinitialize()` - Reload after settings change

**VolumeChangeResult:**
- `Success` - Whether operation succeeded
- `NewVolume` - New volume value (0-255)
- `Percentage` - Volume as percentage (0-100)
- `Message` - User-friendly message (e.g., "Game: 75%")

## Integration

### App.xaml.cs
- `_goXLRService` field added
- Initialized in `Application_Startup` with Settings
- Passed to MainWindow constructor
- Disposed in `Application_Exit`
- Accessible via `App.GetGoXLRService()`

### MainWindow.xaml.cs
- Accepts `GoXLRService` in constructor
- Available for UI/hotkey integration

## API Endpoints Used

### GET /api/get-devices
Returns all connected devices with full status:
```json
{
  "S220202153DI7": {
    "profile_name": "Headphones - Personal (Online)",
    "levels": {
      "volumes": {
        "Game": 108,
        "Music": 178,
        "Chat": 150,
        "System": 128,
        "Console": 128,
        "Mic": 200,
        "LineIn": 100,
        "Sample": 128
      }
    },
    "files": {
      "profiles": ["Profile1", "Profile2", ...]
    }
  }
}
```

### POST /api/command
Command structure for SetVolume:
```json
{
  "Command": [
    "S220202153DI7",
    {
      "SetVolume": ["Game", 150]
    }
  ]
}
```

Command structure for LoadProfile:
```json
{
  "Command": [
    "S220202153DI7",
    {
      "LoadProfile": ["iRacing", false]
    }
  ]
}
```

## Volume Scale
- **API Range:** 0-255
- **Percentage:** 0-100% (calculated as `(volume / 255) * 100`)
- **Default Step:** 10 (configurable in settings)

## Error Handling

### Connection Failures
- Returns `null` or `false` on failure
- Logs to console (future: proper logging system)
- Graceful degradation (app continues running)

### Common Scenarios:
1. **GoXLR Utility not running** ? `IsConnectedAsync()` returns false
2. **Invalid serial number** ? Device not found, returns null
3. **Network timeout** ? 1 second timeout, returns false
4. **Invalid channel name** ? API rejects, returns false

## Usage Examples

### Basic Volume Adjustment
```csharp
var goXLRService = App.GetGoXLRService();
var result = await goXLRService.AdjustVolumeAsync("Game", increase: true);
if (result.Success)
{
    Console.WriteLine(result.Message); // "Game: 75%"
}
```

### Load Profile
```csharp
var success = await goXLRService.LoadProfileAsync("iRacing");
```

### Check Connection
```csharp
var isConnected = await goXLRService.IsConnectedAsync();
if (!isConnected)
{
    MessageBox.Show("GoXLR Utility not running!");
}
```

### Get Current Status
```csharp
var profile = await goXLRService.GetCurrentProfileAsync();
var volume = await goXLRService.GetVolumeAsync("Music");
```

## Testing Without GoXLR

The API client handles missing GoXLR gracefully:
- All methods return null/false if GoXLR Utility not running
- Use `IsConnectedAsync()` to check before operations
- No exceptions thrown to user code

To test with real GoXLR:
1. Install TC-Helicon GoXLR Utility
2. Start utility (runs on http://localhost:14564)
3. Configure serial number in settings
4. Call API methods

## Performance Characteristics

### Caching Benefits:
- Volume adjustments: ~1ms (cache hit) vs ~20-50ms (API call)
- Rapid volume changes (spam hotkey): Nearly instant feedback
- Network overhead: Minimal due to aggressive caching

### Timeout Settings:
- HTTP timeout: 1000ms (1 second)
- Prevents UI freezing if GoXLR Utility slow/hung
- Fast fail for better user experience

### Cache Invalidation:
- Time-based: Automatic after 5 seconds (configurable)
- Event-based: Profile changes invalidate all caches
- Manual: `InvalidateCache()` method available

## Next Steps
1. ? Configuration Management (Complete)
2. ? System Tray Application (Complete)
3. ? GoXLR API Client (Complete)
4. **Global Hotkeys** - Register keyboard hotkeys for volume/profile control
5. Settings UI - Configuration interface in MainWindow
6. Controller Input - DirectInput/XInput/Raw Input detection

## Future Enhancements
- WebSocket support for real-time updates (if API adds it)
- Retry logic with exponential backoff
- Proper logging system (replace Console.WriteLine)
- Mute/unmute channel support
- Mic profile switching
- FX control (reverb, echo, etc.)
