# Global Hotkeys Implementation

## Overview
Global keyboard shortcuts to control GoXLR volume and switch profiles from anywhere in Windows.

## Components Created

### Services

#### HotkeyParser.cs
Parses hotkey strings and converts them to Win32 key codes.

**Key Methods:**
- `TryParse(string, out ModifierKeys, out Key)` - Parses hotkey strings like "Ctrl+Shift+Up"
- `ToWin32Modifiers(ModifierKeys)` - Converts WPF modifiers to Win32 flags
- `ToVirtualKey(Key)` - Converts WPF Key to virtual key code

**Supported Modifiers:**
- `Ctrl` / `Control`
- `Shift`
- `Alt`
- `Win` / `Windows`

**Example Hotkey Strings:**
- `"Ctrl+Shift+Up"`
- `"Alt+F1"`
- `"Win+PageDown"`
- `"Ctrl+Alt+M"`

#### HotkeyService.cs
Low-level Win32 hotkey registration service.

**Key Features:**
- Registers global hotkeys using Win32 `RegisterHotKey` API
- Handles `WM_HOTKEY` messages via window procedure hook
- Maps hotkey IDs to action callbacks
- Unregisters all hotkeys on dispose

**Key Methods:**
- `Initialize(IntPtr windowHandle)` - Sets up window message hook
- `RegisterHotkey(string, Action)` - Registers hotkey from string
- `RegisterHotkey(ModifierKeys, Key, Action)` - Registers with parsed keys
- `UnregisterAll()` - Removes all registered hotkeys

#### HotkeyManager.cs
High-level manager that connects hotkeys to GoXLR actions.

**Key Features:**
- Reads hotkey configuration from AppSettings
- Registers volume up/down hotkeys for each channel
- Registers profile switching hotkeys
- Executes GoXLR commands when hotkeys are triggered
- Logs all registration success/failures

**Key Methods:**
- `RegisterAllHotkeys()` - Registers all configured hotkeys
- `AdjustVolume(channel, increase)` - Called when volume hotkey pressed
- `LoadProfile(profileName)` - Called when profile hotkey pressed

## Integration

### App.xaml.cs
**Initialization:**
1. Creates `HotkeyService` after MainWindow is created
2. Gets window handle using `WindowInteropHelper`
3. Initializes service with window handle
4. Creates `HotkeyManager` with GoXLR and settings
5. Calls `RegisterAllHotkeys()` to register all configured hotkeys

**Cleanup:**
- Disposes `HotkeyManager` (which unregisters all hotkeys)
- Disposes `HotkeyService`

### Configuration
Hotkeys are configured in `config.json`:

```json
{
  "volumeHotkeys": {
    "Game": {
      "volumeUp": "Ctrl+Shift+Up",
      "volumeDown": "Ctrl+Shift+Down"
    },
    "Music": {
      "volumeUp": "Ctrl+Shift+PageUp",
      "volumeDown": "Ctrl+Shift+PageDown"
    }
  },
  "profileHotkeys": {
    "iRacing": "Ctrl+Shift+F1",
    "Speakers - Personal": "Ctrl+Shift+F2"
  }
}
```

## Default Hotkeys

The application creates these example hotkeys by default:

### Volume Control
- **Game Volume Up**: `Ctrl+Shift+Up`
- **Game Volume Down**: `Ctrl+Shift+Down`
- **Music Volume Up**: `Ctrl+Shift+PageUp`
- **Music Volume Down**: `Ctrl+Shift+PageDown`

### Profile Switching
- No profile hotkeys configured by default (add manually)

## How It Works

### Registration Process
1. Application starts
2. MainWindow created (creates window handle)
3. HotkeyService initialized with window handle
4. HotkeyManager reads configuration
5. For each configured hotkey:
   - Parse hotkey string to modifiers + key
   - Convert to Win32 codes
   - Call `RegisterHotKey` Win32 API
   - Map hotkey ID to action callback

### Execution Process
1. User presses registered hotkey anywhere in Windows
2. Windows sends `WM_HOTKEY` message to window
3. HotkeyService's `WndProc` intercepts message
4. Looks up action callback for hotkey ID
5. Executes callback
6. Callback calls GoXLR API to adjust volume or load profile

### Cleanup Process
1. Application closes
2. `HotkeyManager.Dispose()` called
3. Calls `HotkeyService.UnregisterAll()`
4. For each registered hotkey:
   - Call `UnregisterHotKey` Win32 API
   - Remove from action dictionary

## Limitations & Notes

### Windows Restrictions
- Hotkeys must be unique system-wide
- If another app has registered the same hotkey, registration fails
- Some hotkeys are reserved by Windows (e.g., `Win+L`)

### Registration Failures
Common reasons hotkeys fail to register:
1. Already registered by another application
2. Reserved by Windows
3. Invalid key combination
4. Application doesn't have window handle yet

### Best Practices
- Use modifier combinations (Ctrl+Shift+Key) to avoid conflicts
- Avoid common shortcuts (Ctrl+C, Ctrl+V, etc.)
- Check console output for registration success/failure
- Test hotkeys in different applications

## Testing

### Manual Testing
1. Run application
2. Check console output for hotkey registration messages
3. Try pressing configured hotkeys
4. Volume should adjust or profile should load
5. Check console for action execution messages

### Debugging
Enable verbose logging to see:
- Hotkey registration attempts
- Parse success/failure
- Win32 API call results
- Hotkey trigger events
- GoXLR command execution

### Console Output Example
```
[HotkeyManager] Registering hotkeys...
[Hotkey] Registered hotkey ID 1: Control, Shift+Up
[HotkeyManager] Registered Volume Up for Game: Ctrl+Shift+Up
[Hotkey] Registered hotkey ID 2: Control, Shift+Down
[HotkeyManager] Registered Volume Down for Game: Ctrl+Shift+Down
[HotkeyManager] Registration complete: 4 successful, 0 failed

... (user presses Ctrl+Shift+Up) ...

[Hotkey] Hotkey 1 triggered
[HotkeyManager] Game: 75%
[GoXLR] SetVolume - Channel: Game, Volume: 192, Serial: S220202153DI7
```

## Future Enhancements

### Planned Features
1. **Toast Notifications** - Show volume percentage in popup
2. **Hotkey Configuration UI** - Visual hotkey editor with conflict detection
3. **Hotkey Capture** - Click button and press keys to set hotkey
4. **Conflict Detection** - Warn when hotkey is already registered
5. **Hotkey Profiles** - Different hotkey sets for different scenarios
6. **OSD (On-Screen Display)** - Show volume bar overlay

### Potential Improvements
- **Rebinding at runtime** - Change hotkeys without restart
- **Hotkey enable/disable** - Temporarily disable hotkeys
- **Per-application hotkeys** - Different hotkeys for different apps
- **Chained hotkeys** - Press sequence of keys (Ctrl+K, Ctrl+V)

## Known Issues

### Issue: Hotkey Not Working
**Symptoms:** Console shows registration success but hotkey doesn't trigger

**Possible Causes:**
1. Another app registered the same hotkey first
2. Window lost focus and messages not processing
3. Hotkey service not properly initialized

**Solutions:**
- Try different hotkey combination
- Restart application
- Check if other apps are using the hotkey

### Issue: Registration Fails
**Symptoms:** Console shows "Failed to register" message

**Possible Causes:**
1. Hotkey already registered by another app
2. Invalid hotkey combination
3. Windows reserved hotkey

**Solutions:**
- Use different key combination
- Close conflicting applications
- Avoid Win+Key combinations (many reserved by Windows)

## API Reference

### HotkeyParser
```csharp
// Parse hotkey string
if (HotkeyParser.TryParse("Ctrl+Shift+Up", out var modifiers, out var key))
{
    // Use modifiers and key
}

// Convert to Win32 codes
uint mod = HotkeyParser.ToWin32Modifiers(ModifierKeys.Control | ModifierKeys.Shift);
uint vk = HotkeyParser.ToVirtualKey(Key.Up);
```

### HotkeyService
```csharp
var service = new HotkeyService();
service.Initialize(windowHandle);

// Register hotkey
service.RegisterHotkey("Ctrl+Shift+Up", () => 
{
    Console.WriteLine("Hotkey pressed!");
});

// Unregister all
service.UnregisterAll();
service.Dispose();
```

### HotkeyManager
```csharp
var manager = new HotkeyManager(hotkeyService, goXLRService, settings);
manager.RegisterAllHotkeys();
manager.Dispose();
```

## Next Steps
1. ? Configuration Management (Complete)
2. ? System Tray Application (Complete)
3. ? GoXLR API Client (Complete)
4. ? Global Hotkeys (Complete)
5. **Settings UI** - Configuration interface with hotkey editor
6. Controller Input - DirectInput/XInput/Raw Input detection
7. iRacing Integration - Auto-launch apps, profile switching
