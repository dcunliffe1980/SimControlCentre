# GoXLR Utility API - Complete Reference

**CRITICAL**: This document provides the EXACT API structure needed to communicate with the GoXLR Utility. Read this carefully to avoid common pitfalls.

---

## Table of Contents

1. [API Overview](#api-overview)
2. [Connection Methods](#connection-methods)
3. [Command Structure](#command-structure)
4. [Common Commands](#common-commands)
5. [Button Colors API](#button-colors-api)
6. [C# Implementation Guide](#c-implementation-guide)
7. [Testing & Debugging](#testing--debugging)
8. [Common Pitfalls](#common-pitfalls)
9. [Complete Examples](#complete-examples)

---

## API Overview

### What is the GoXLR Utility?

The GoXLR Utility is an API-driven daemon that controls GoXLR hardware. ALL configuration happens via JSON API calls - there's no direct hardware access.

### Base Information

- **Default Endpoint**: `http://localhost:14564`
- **API Base**: `/api/`
- **Content-Type**: `application/json`
- **Method**: `POST` for commands, `GET` for status

### Key Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/get-devices` | GET | Get all device info and current state |
| `/api/command` | POST | Send commands to devices |
| `/api/websocket` | WebSocket | Real-time updates via JSON patches |

---

## Connection Methods

### 1. HTTP REST API (Recommended for C#)

Simple POST requests to `/api/command`:

```http
POST http://localhost:14564/api/command
Content-Type: application/json

{
  "Command": [
    "SERIAL_NUMBER",
    {
      "CommandName": [param1, param2, ...]
    }
  ]
}
```

### 2. Unix Socket / Named Pipe

- Linux/Mac: `/tmp/goxlr.socket`
- Windows: `\\.\pipe\goxlr.socket` (Named Pipe `@goxlr.socket`)
- Message format: `[4-byte length][JSON message]`

### 3. WebSocket

- Endpoint: `ws://localhost:14564/api/websocket`
- Supports async requests with IDs
- Receives JSON Patch updates for real-time changes

---

## Command Structure

### CRITICAL: Universal Command Format

**ALL commands follow this exact structure:**

```json
{
  "Command": [
    "SERIAL_NUMBER",
    {
      "CommandName": [parameters]
    }
  ]
}
```

### Breaking It Down

1. **Root Object**: Always has a `"Command"` key
2. **Array with 2 elements**:
   - Element 0: Device serial number (string)
   - Element 1: Object containing the command
3. **Command Object**: Single key-value pair
   - Key: Command name (string)
   - Value: Array of parameters

### Getting the Serial Number

Query `/api/get-devices`:

```json
{
  "mixers": {
    "24070K49B3C9": { ... },  // <- This is the serial
    "S220202153DI7": { ... }  // <- Another example
  }
}
```

---

## Common Commands

### 1. SetVolume

**Purpose**: Set channel volume (0-255)

**Structure**:
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "SetVolume": ["Game", 200]
    }
  ]
}
```

**Parameters**:
- `[0]`: Channel name (string): `"Mic"`, `"Chat"`, `"Music"`, `"Game"`, `"Console"`, `"LineIn"`, `"System"`, `"Sample"`
- `[1]`: Volume level (integer): `0` to `255`

**C# Factory Method**:
```csharp
public static GoXLRCommandRequest SetVolume(string serialNumber, string channel, int volume)
{
    return new GoXLRCommandRequest
    {
        Command = new object[]
        {
            serialNumber,
            new Dictionary<string, object[]>
            {
                { "SetVolume", new object[] { channel, volume } }
            }
        }
    };
}
```

### 2. LoadProfile

**Purpose**: Load a saved GoXLR profile

**Structure**:
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "LoadProfile": ["My Profile", false]
    }
  ]
}
```

**Parameters**:
- `[0]`: Profile name (string)
- `[1]`: Save current profile first (boolean): Usually `false`

**C# Factory Method**:
```csharp
public static GoXLRCommandRequest LoadProfile(string serialNumber, string profileName)
{
    return new GoXLRCommandRequest
    {
        Command = new object[]
        {
            serialNumber,
            new Dictionary<string, object[]>
            {
                { "LoadProfile", new object[] { profileName, false } }
            }
        }
    };
}
```

### 3. SetButtonColours

**Purpose**: Set LED colors for GoXLR buttons

**CRITICAL**: This is the most commonly misunderstood command!

**Correct Structure**:
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "SetButtonColours": [
        "Fader1Mute",
        "FF0000",
        "FF0000"
      ]
    }
  ]
}
```

**Parameters** (from Rust source):
- `[0]`: Button ID (string): e.g., `"Fader1Mute"`, `"Bleep"`, `"Cough"`
- `[1]`: Primary color (string): 6-char hex WITHOUT `#`, e.g., `"FF0000"`
- `[2]`: Secondary color (string, optional): Same format

**WRONG** ?:
```json
// DO NOT USE THIS STRUCTURE!
{
  "SetButtonColours": [
    "Fader1Mute",
    {
      "colour_one": "FF0000",
      "colour_two": "FF0000"
    },
    "Colour1"
  ]
}
```

**C# Factory Method**:
```csharp
public static GoXLRCommandRequest SetButtonColours(string serialNumber, string buttonId, string colourOne, string? colourTwo = null)
{
    var colours = colourTwo != null 
        ? new object[] { buttonId, colourOne, colourTwo } 
        : new object[] { buttonId, colourOne };
    
    return new GoXLRCommandRequest
    {
        Command = new object[]
        {
            serialNumber,
            new Dictionary<string, object[]>
            {
                { "SetButtonColours", colours }
            }
        }
    };
}
```

### 4. SetButtonOffStyle

**Purpose**: Control how button looks when not pressed

**Structure**:
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "SetButtonOffStyle": [
        "Fader1Mute",
        "Colour1"
      ]
    }
  ]
}
```

**Off-Style Options**:
- `"Colour1"` - Use primary color when off
- `"Colour2"` - Use secondary color when off
- `"DimmedColour1"` - Dimmed primary color
- `"DimmedColour2"` - Dimmed secondary color

---

## Button Colors API

### Available Buttons

**Full-Size GoXLR**:
- Fader buttons: `Fader1Mute`, `Fader2Mute`, `Fader3Mute`, `Fader4Mute`
- Function buttons: `Bleep`, `Cough`
- Effect selects: `EffectSelect1` through `EffectSelect6`
- Effect types: `EffectFx`, `EffectMegaphone`, `EffectRobot`, `EffectHardTune`
- Sampler buttons: Various (see Rust enum)

**GoXLR Mini**:
- Fader buttons: `Fader1Mute`, `Fader2Mute`, `Fader3Mute`
- Function buttons: `Bleep`, `Cough`

### Color Format

**CRITICAL**: Colors are 6-character hex strings WITHOUT the `#` prefix

| Color | Hex Code | Example Use |
|-------|----------|-------------|
| Red | `FF0000` | Stop/Error indicators |
| Green | `00FF00` | Go/Success indicators |
| Blue | `0000FF` | Info indicators |
| Yellow | `FFFF00` | Warning/Caution |
| Orange | `FF8800` | Debris/Alert |
| Purple | `FF00FF` | Special states |
| White | `FFFFFF` | Checkered flag |
| Off/Black | `000000` | Turn off LED |

### Complete Button Color Example

```csharp
// Set Fader1Mute button to red
var command = new GoXLRCommandRequest
{
    Command = new object[]
    {
        "24070K49B3C9",
        new Dictionary<string, object[]>
        {
            { "SetButtonColours", new object[] { "Fader1Mute", "FF0000", "FF0000" } }
        }
    }
};

// Serialize and send
var json = JsonSerializer.Serialize(command);
var response = await httpClient.PostAsJsonAsync("http://localhost:14564/api/command", command);
```

---

## C# Implementation Guide

### 1. Command Request Model

```csharp
using System.Text.Json.Serialization;

namespace SimControlCentre.Models;

public class GoXLRCommandRequest
{
    [JsonPropertyName("Command")]
    public object[] Command { get; set; } = Array.Empty<object>();

    // Factory methods for each command type
    public static GoXLRCommandRequest SetVolume(string serialNumber, string channel, int volume)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetVolume", new object[] { channel, volume } }
                }
            }
        };
    }

    public static GoXLRCommandRequest SetButtonColours(string serialNumber, string buttonId, string colourOne, string? colourTwo = null)
    {
        var colours = colourTwo != null 
            ? new object[] { buttonId, colourOne, colourTwo } 
            : new object[] { buttonId, colourOne };
        
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetButtonColours", colours }
                }
            }
        };
    }
}
```

### 2. API Client Implementation

```csharp
public class GoXLRApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;

    public GoXLRApiClient(string apiEndpoint = "http://localhost:14564")
    {
        _apiEndpoint = apiEndpoint;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiEndpoint),
            Timeout = TimeSpan.FromSeconds(2)
        };
    }

    public async Task<bool> SendCommandAsync(GoXLRCommandRequest command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Success: {result}");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SetVolumeAsync(string serialNumber, string channel, int volume)
    {
        var command = GoXLRCommandRequest.SetVolume(serialNumber, channel, volume);
        return await SendCommandAsync(command);
    }

    public async Task<bool> SetButtonColourAsync(string serialNumber, string buttonId, string colourOne, string? colourTwo = null)
    {
        var command = GoXLRCommandRequest.SetButtonColours(serialNumber, buttonId, colourOne, colourTwo);
        return await SendCommandAsync(command);
    }
}
```

### 3. Service Wrapper with Configuration

```csharp
public class GoXLRService
{
    private readonly GoXLRApiClient _apiClient;
    private readonly AppSettings _settings;

    public string SerialNumber => _settings.General.SerialNumber;

    public GoXLRService(AppSettings settings)
    {
        _settings = settings;
        _apiClient = new GoXLRApiClient(settings.General.ApiEndpoint);
    }

    public async Task SetButtonColorAsync(string buttonId, string hexColor)
    {
        await _apiClient.SetButtonColourAsync(SerialNumber, buttonId, hexColor, hexColor);
    }

    public async Task AdjustVolumeAsync(string channel, bool increase)
    {
        var currentVolume = await GetCurrentVolumeAsync(channel);
        var newVolume = increase 
            ? Math.Min(255, currentVolume + _settings.General.VolumeStep)
            : Math.Max(0, currentVolume - _settings.General.VolumeStep);
        
        await _apiClient.SetVolumeAsync(SerialNumber, channel, newVolume);
    }
}
```

---

## Testing & Debugging

### PowerShell Quick Test

```powershell
# Test button color change
$body = @{
    Command = @(
        "24070K49B3C9",
        @{
            SetButtonColours = @(
                "Fader1Mute",
                "FF0000",
                "FF0000"
            )
        }
    )
} | ConvertTo-Json -Depth 10 -Compress

Invoke-RestMethod -Uri "http://localhost:14564/api/command" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### curl Test

```bash
curl -X POST http://localhost:14564/api/command \
  -H "Content-Type: application/json" \
  -d '{"Command":["24070K49B3C9",{"SetButtonColours":["Fader1Mute","FF0000","FF0000"]}]}'
```

### Debug Logging

Add to your API client:

```csharp
var json = JsonSerializer.Serialize(command);
Console.WriteLine($"[GoXLR] Sending: {json}");

var response = await _httpClient.PostAsJsonAsync("/api/command", command);
Console.WriteLine($"[GoXLR] Status: {response.StatusCode}");

var body = await response.Content.ReadAsStringAsync();
Console.WriteLine($"[GoXLR] Response: {body}");
```

### Check if GoXLR Utility is Running

```csharp
public async Task<bool> IsConnectedAsync()
{
    try
    {
        var response = await _httpClient.GetAsync("/api/get-devices");
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
```

---

## Common Pitfalls

### ? Pitfall 1: Wrong Command Structure

**WRONG**:
```json
{
  "SetVolume": ["Game", 200]
}
```

**CORRECT**:
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "SetVolume": ["Game", 200]
    }
  ]
}
```

### ? Pitfall 2: Missing Serial Number

You MUST include the device serial number. Get it from `/api/get-devices`.

### ? Pitfall 3: Using Object for Button Colors

**WRONG**:
```json
{
  "SetButtonColours": [
    "Fader1Mute",
    { "colour_one": "FF0000", "colour_two": "FF0000" },
    "Colour1"
  ]
}
```

**CORRECT**:
```json
{
  "SetButtonColours": [
    "Fader1Mute",
    "FF0000",
    "FF0000"
  ]
}
```

### ? Pitfall 4: Including `#` in Hex Colors

**WRONG**: `"#FF0000"`  
**CORRECT**: `"FF0000"`

### ? Pitfall 5: Sending Commands Too Fast

Add delays between commands (50-100ms) to avoid overwhelming the API.

### ? Pitfall 6: Not Handling Errors

Always check response status and log errors:

```csharp
if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadAsStringAsync();
    Logger.Error("GoXLR", $"Command failed: {error}");
}
```

---

## Complete Examples

### Example 1: Racing Flag Lighting System

```csharp
public class GoXLRLightingDevice : ILightingDevice
{
    private readonly GoXLRService _goXLRService;

    public async Task SetColorAsync(LightingColor color)
    {
        var hexColor = color switch
        {
            LightingColor.Red => "FF0000",
            LightingColor.Green => "00FF00",
            LightingColor.Yellow => "FFFF00",
            _ => "000000"
        };

        // Set all fader buttons to same color
        await _goXLRService.SetButtonColorAsync("Fader1Mute", hexColor);
        await Task.Delay(50); // Rate limiting
        await _goXLRService.SetButtonColorAsync("Fader2Mute", hexColor);
        await Task.Delay(50);
        await _goXLRService.SetButtonColorAsync("Fader3Mute", hexColor);
        await Task.Delay(50);
        await _goXLRService.SetButtonColorAsync("Fader4Mute", hexColor);
    }

    public async Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs)
    {
        var timer = new Timer(async _ =>
        {
            _flashState = !_flashState;
            await SetColorAsync(_flashState ? color1 : color2);
        }, null, 0, intervalMs);
    }
}
```

### Example 2: Volume Control Hotkeys

```csharp
public class VolumeHotkeyHandler
{
    private readonly GoXLRService _goXLRService;

    public async Task HandleHotkeyAsync(string channel, bool increase)
    {
        // Get current volume
        var status = await _goXLRService.GetDeviceStatusAsync();
        var currentVolume = status.Levels.Volumes[channel];

        // Calculate new volume
        var step = 10;
        var newVolume = increase
            ? Math.Min(255, currentVolume + step)
            : Math.Max(0, currentVolume - step);

        // Apply change
        await _goXLRService.SetVolumeAsync(channel, newVolume);

        // Show notification
        var percentage = (newVolume * 100) / 255;
        ShowNotification($"{channel}: {percentage}%");
    }
}
```

### Example 3: Profile Switching

```csharp
public class ProfileManager
{
    private readonly GoXLRService _goXLRService;

    public async Task LoadProfileAsync(string profileName)
    {
        var command = GoXLRCommandRequest.LoadProfile(
            _goXLRService.SerialNumber,
            profileName
        );

        var success = await _goXLRService.SendCommandAsync(command);
        
        if (success)
        {
            Logger.Info("Profile", $"Loaded profile: {profileName}");
        }
        else
        {
            Logger.Error("Profile", $"Failed to load: {profileName}");
        }
    }
}
```

---

## API Response Format

### Success Response

```json
"Ok"
```

Simple string, no object wrapper.

### Error Response

```json
{
  "Error": "Error message describing what went wrong"
}
```

---

## Rate Limiting & Best Practices

### Recommended Delays

- **Between commands**: 50-100ms
- **For flashing effects**: 300-500ms
- **Volume changes**: Immediate (handled by API)

### Connection Pooling

The API client should reuse HTTP connections:

```csharp
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = 10
};

_httpClient = new HttpClient(handler);
```

### Error Recovery

Always implement retry logic for transient failures:

```csharp
public async Task<bool> SendCommandWithRetryAsync(GoXLRCommandRequest command, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            if (response.IsSuccessStatusCode) return true;
            
            await Task.Delay(100 * (i + 1)); // Exponential backoff
        }
        catch
        {
            if (i == maxRetries - 1) throw;
        }
    }
    return false;
}
```

---

## Additional Resources

### Official Documentation
- **Wiki**: https://github.com/GoXLR-on-Linux/goxlr-utility/wiki/The-GoXLR-Utility-API
- **Source Code**: https://github.com/GoXLR-on-Linux/goxlr-utility
- **Rust IPC Library**: `/ipc/src/lib.rs` (defines all commands)

### Rust Command Definitions

The authoritative source is `ipc/src/lib.rs` in the GoXLR Utility repo. All commands are defined in the `GoXLRCommand` enum.

Example:
```rust
pub enum GoXLRCommand {
    SetVolume(ChannelName, u8),
    SetButtonColours(Button, String, Option<String>),
    SetButtonOffStyle(Button, ButtonColourOffStyle),
    LoadProfile(String, bool),
    // ... many more
}
```

### Type Definitions

Button IDs, color styles, and other enums are in `types/src/lib.rs`.

---

## Quick Reference Card

| Task | Command | Parameters |
|------|---------|------------|
| Set Volume | `SetVolume` | `[channel, volume]` |
| Load Profile | `LoadProfile` | `[name, save_current]` |
| Button Color | `SetButtonColours` | `[button, color1, color2?]` |
| Button Style | `SetButtonOffStyle` | `[button, style]` |
| Get Status | GET `/api/get-devices` | None |

---

## Troubleshooting Checklist

- [ ] GoXLR Utility is running (`http://localhost:14564/api/get-devices` responds)
- [ ] Serial number is correct (check `/api/get-devices`)
- [ ] Command structure follows: `{"Command":[serial,{command:[params]}]}`
- [ ] Button IDs match your GoXLR model (Full vs Mini)
- [ ] Colors are 6-char hex WITHOUT `#`
- [ ] No nested objects for button colors (just two strings)
- [ ] HTTP Content-Type is `application/json`
- [ ] Commands have 50-100ms delays between them
- [ ] Error responses are logged and handled

---

**Last Updated**: February 2026  
**GoXLR Utility Version**: Compatible with current releases  
**Tested With**: C# .NET 8, PowerShell 7, curl  
**Status**: Verified working ?

---

**For Future AI Threads**: This document contains the EXACT structures needed. Do not deviate from the command formats shown here. The most common mistake is using complex objects instead of simple parameters. Always refer to the Rust source (`lib.rs`) when in doubt.

