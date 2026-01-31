# Hotkeys & Controller Buttons

## Overview
Control GoXLR volume and profiles using:
- **Global Keyboard Hotkeys** - Work from anywhere in Windows
- **Controller Buttons** - Button boxes, wheels, gamepads, etc.

## Keyboard Hotkeys

### Supported Modifiers
- `Ctrl` / `Control`
- `Shift`
- `Alt`
- `Win` / `Windows`

### Example Hotkey Strings
- `"Ctrl+Shift+Up"` - Typical volume up
- `"Alt+F1"` - Profile switching
- `"Win+PageDown"` - Alternative volume control
- `"Ctrl+Alt+M"` - Mic mute/unmute

### How It Works
1. **HotkeyParser.cs** - Parses hotkey strings to Win32 key codes
2. **HotkeyService.cs** - Registers hotkeys using Win32 `RegisterHotKey` API
3. **HotkeyManager.cs** - Maps hotkeys to GoXLR actions
4. Hotkeys work globally (even when app is minimized)

## Controller Buttons

### Supported Devices
Any DirectInput-compatible device:
- **Button Boxes** (e.g., PXN-CB1, Leo Bodnar)
- **Racing Wheels** (e.g., Fanatec, Thrustmaster, Logitech)
- **Game Controllers** (Xbox, PlayStation, generic gamepads)
- **HOTAS** (flight sim controllers)
- **Pedals** with buttons

### Button Format
Buttons are stored using **ProductGuid** for stability:
```
Format: DeviceName:{ProductGuid}:Button:{number}
Example: PXN-CB1:80013668-0000-0000-0000-504944564944:Button:1
Display: PXN-CB1 Btn 1
```

**Why ProductGuid?**
- **Instance GUID changes** every time you reconnect a device
- **Product GUID stays the same** - stable across reboots and reconnections
- Your button mappings work consistently!

### How It Works
1. **DirectInputService.cs** - Detects DirectInput devices, polls for button presses
2. **ControllerManager.cs** - Maps button presses to GoXLR actions
3. Button presses trigger same actions as keyboard hotkeys
4. Debouncing (100ms) prevents double-presses

### Device Detection
- Filters out keyboards and mice (won't interfere with typing)
- Scans for all button-capable devices on startup
- Displays device name, type, and GUID in Controllers tab
- Real-time button indicator (green flash) shows activity

## Configuration in UI

### Assigning Keyboard Hotkeys
1. Open Settings ? **Hotkeys tab**
2. Find the channel or profile you want to control
3. Click **"?/?? Capture"** button
4. Press your desired **keyboard key combination**
5. Shows in textbox (e.g., "Ctrl+Shift+Up")

### Assigning Controller Buttons
1. Open Settings ? **Hotkeys tab**
2. Find the channel or profile you want to control
3. Click **"?/?? Capture"** button
4. Press a **button on your controller**
5. Shows in textbox (e.g., "PXN-CB1 Btn 5")

### Unified Capture
The **"?/?? Capture"** button accepts **BOTH**:
- Keyboard keys
- Controller buttons
- You can even assign both to the same action!
- Display format: `"Ctrl+Shift+Up OR PXN-CB1 Btn 5"`

### Conflict Detection
- Warns if hotkey/button is already assigned
- Shows which action is using it
- Textbox turns red with "Already in use" message

### Clearing Assignments
- Click **"Clear"** button next to any mapping
- Removes both keyboard hotkey AND controller button
- Auto-saves immediately

## Configuration File Format

Hotkeys and buttons are stored in `config.json`:

```json
{
  "volumeHotkeys": {
    "Game": {
      "volumeUp": "Ctrl+Shift+Up",
      "volumeDown": "Ctrl+Shift+Down",
      "volumeUpButton": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:1",
      "volumeDownButton": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:2"
    },
    "Music": {
      "volumeUp": "Ctrl+Shift+PageUp",
      "volumeDown": "Ctrl+Shift+PageDown",
      "volumeUpButton": null,
      "volumeDownButton": null
    }
  },
  "profileHotkeys": {
    "iRacing": "Ctrl+Shift+F1",
    "Speakers - Personal": ""
  },
  "profileButtons": {
    "iRacing": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:5",
    "Speakers - Personal": ""
  }
}
```

## Technical Implementation

### Keyboard Hotkey Components

**HotkeyParser.cs**
- Parses strings like "Ctrl+Shift+Up" to Win32 key codes
- Supports all standard modifiers and keys
- Validates hotkey format

**HotkeyService.cs**
- Low-level Win32 `RegisterHotKey` API integration
- Window message hook for `WM_HOTKEY` messages
- Maps hotkey IDs to action callbacks

**HotkeyManager.cs**
- High-level manager connecting hotkeys to GoXLR actions
- Reads configuration and registers all hotkeys
- Handles unregistration on dispose

### Controller Button Components

**DirectInputService.cs**
- Scans for DirectInput devices
- Polls for button state changes (50ms interval)
- Filters out keyboards/mice
- Raises ButtonPressed/ButtonReleased events
- Stores ProductGuid and device name

**ControllerManager.cs**
- Listens to DirectInputService button events
- Matches button string format against configuration
- Executes GoXLR actions (volume, profile)
- Debouncing (100ms) prevents accidental double-presses

### Integration Flow

**Keyboard Hotkeys:**
1. User presses hotkey anywhere in Windows
2. Windows sends `WM_HOTKEY` message
3. HotkeyService intercepts and looks up action
4. Calls GoXLRService to adjust volume/load profile

**Controller Buttons:**
1. DirectInputService polls devices every 50ms
2. Detects button press, raises ButtonPressed event
3. ControllerManager receives event
4. Matches ProductGuid + button number to configuration
5. Calls GoXLRService to adjust volume/load profile

## System Tray Hotkey Toggle

Right-click tray icon ? **"? Enable Hotkeys"**

- **Checked:** Keyboard hotkeys are active
- **Unchecked:** Keyboard hotkeys are disabled
- Controller buttons always work (not affected by toggle)
- Useful for gaming to avoid conflicts

## Troubleshooting

### Keyboard Hotkeys Not Working

**Check:**
1. Are hotkeys registered? (Console shows registration count)
2. Is another app using the same hotkey?
3. Try different modifier combinations
4. Check "Enable Hotkeys" is checked in tray menu

**Solutions:**
- Use more specific combinations (Ctrl+Shift+Alt+Key)
- Avoid common shortcuts
- Check Task Manager for conflicting apps

### Controller Buttons Not Working

**Check:**
1. Is controller detected? (Controllers tab should list it)
2. Does button indicator flash when you press buttons?
3. Is button mapped correctly? (Should show device name + button number)
4. Did you reboot since mapping? (Old InstanceGuid mappings won't work)

**Solutions:**
- Recapture button mapping (uses ProductGuid now)
- Verify device name matches (e.g., "PXN-CB1 Btn 1")
- Check Controllers tab to confirm device detection
- Try unplugging/replugging device

### First Button Press Fails

**This is normal during cache warm-up:**
- Wait 5-10 seconds after "GoXLR Ready!" notification
- Press again - subsequent presses work immediately
- Cache lasts 30 seconds
- If you wait longer, first press may timeout again

### Hotkey Conflicts

If you see "Already in use":
1. Check which action is using that hotkey
2. Clear the old mapping first
3. Or use a different key combination

## Best Practices

### Keyboard Hotkeys
- **Use modifiers:** `Ctrl+Shift+Key` or `Ctrl+Alt+Key`
- **Avoid common shortcuts:** Don't use Ctrl+C, Ctrl+V, etc.
- **Volume control:** Use arrow keys or PageUp/PageDown
- **Profiles:** Use function keys (F1-F12)
- **Test in different apps:** Make sure no conflicts

### Controller Buttons
- **Label your buttons:** Physical labels help remember mappings
- **Group by function:** Volume on one side, profiles on another
- **Use button box:** Dedicated button boxes work best
- **Avoid accidental presses:** Position buttons carefully

### General Tips
- **Backup config.json:** Save your configuration!
- **Test thoroughly:** Verify all mappings work
- **Document mappings:** Keep a list of what each hotkey/button does
- **Start simple:** Configure frequently-used channels first
- **Add gradually:** Don't try to map everything at once

## Example Configurations

### Sim Racing Setup
```json
{
  "volumeHotkeys": {
    "Game": {
      "volumeUp": "Ctrl+Shift+Up",
      "volumeDown": "Ctrl+Shift+Down",
      "volumeUpButton": "Fanatec:btn-guid:Button:1",
      "volumeDownButton": "Fanatec:btn-guid:Button:2"
    },
    "Chat": {
      "volumeUpButton": "Fanatec:btn-guid:Button:3",
      "volumeDownButton": "Fanatec:btn-guid:Button:4"
    }
  },
  "profileButtons": {
    "iRacing": "Fanatec:btn-guid:Button:5"
  }
}
```

### Streaming Setup
```json
{
  "volumeHotkeys": {
    "Mic": {
      "volumeUp": "Ctrl+Alt+Up",
      "volumeDown": "Ctrl+Alt+Down"
    },
    "Music": {
      "volumeUp": "Ctrl+Alt+PageUp",
      "volumeDown": "Ctrl+Alt+PageDown"
    },
    "Chat": {
      "volumeUpButton": "PXN-CB1:btn-guid:Button:1",
      "volumeDownButton": "PXN-CB1:btn-guid:Button:2"
    }
  }
}
```

## Advanced: Manual Configuration

You can manually edit `config.json`, but **use the UI** - it's much easier and safer!

If you must manually edit:
1. Close SimControlCentre
2. Edit `%LocalAppData%\SimControlCentre\config.json`
3. Validate JSON syntax
4. Save file
5. Restart SimControlCentre

**Button string format:**
```
DeviceName:{ProductGuid}:Button:{number}
```

To find ProductGuid:
1. Open SimControlCentre ? Controllers tab
2. Note the "Product GUID" for your device
3. Use that GUID in the button string
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
