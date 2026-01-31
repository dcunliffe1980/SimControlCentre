# Configuration Management

## Overview
Complete configuration system for SimControlCentre with JSON persistence. All settings stored in a single `config.json` file in the user's local app data folder.

## Configuration File Location

```
%LocalAppData%\SimControlCentre\config.json
```

**Full Path Example:**
```
C:\Users\YourName\AppData\Local\SimControlCentre\config.json
```

**Quick Access:**
- Press `Win+R`
- Type: `%LocalAppData%\SimControlCentre`
- Press Enter

## Configuration Models

### AppSettings (Root)
Main configuration container holding all settings:

```csharp
public class AppSettings
{
    public GeneralSettings General { get; set; }
    public Dictionary<string, ChannelHotkeys> VolumeHotkeys { get; set; }
    public Dictionary<string, string> ProfileHotkeys { get; set; }
    public Dictionary<string, string> ProfileButtons { get; set; }
    public List<ControllerMapping> ControllerMappings { get; set; }
    public List<string> EnabledChannels { get; set; }
    public WindowSettings Window { get; set; }
}
```

### GeneralSettings
GoXLR connection and volume behavior:

```csharp
public class GeneralSettings
{
    public string SerialNumber { get; set; }           // GoXLR device serial
    public int VolumeStep { get; set; } = 10;          // Volume adjustment (0-255)
    public int VolumeCacheTimeMs { get; set; } = 30000; // Cache duration (30s)
    public string ApiEndpoint { get; set; } = "http://localhost:14564";
}
```

### ChannelHotkeys
Keyboard and controller assignments per channel:

```csharp
public class ChannelHotkeys
{
    public string? VolumeUp { get; set; }              // Keyboard hotkey
    public string? VolumeDown { get; set; }            // Keyboard hotkey
    public string? VolumeUpButton { get; set; }        // Controller button
    public string? VolumeDownButton { get; set; }      // Controller button
}
```

**Button Format:**
```
DeviceName:{ProductGuid}:Button:{number}
Example: PXN-CB1:80013668-0000-0000-0000-504944564944:Button:1
```

### WindowSettings
Window position and startup behavior:

```csharp
public class WindowSettings
{
    public double Width { get; set; } = 900;
    public double Height { get; set; } = 700;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool StartMinimized { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
}
```

### ControllerMapping (Legacy)
Old controller mapping format (still supported):

```csharp
public class ControllerMapping
{
    public string ControllerId { get; set; }
    public int ButtonNumber { get; set; }
    public string KeyboardKey { get; set; }
    public bool ToggleMode { get; set; }
    public string Description { get; set; }
}
```

**Note:** New button mappings use the `VolumeUpButton`/`VolumeDownButton` fields in `ChannelHotkeys`.

## Example config.json

```json
{
  "general": {
    "serialNumber": "S220202153DI7",
    "volumeStep": 10,
    "volumeCacheTimeMs": 30000,
    "apiEndpoint": "http://localhost:14564"
  },
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
    },
    "Chat": {
      "volumeUp": null,
      "volumeDown": null,
      "volumeUpButton": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:3",
      "volumeDownButton": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:4"
    },
    "Mic": {
      "volumeUp": "Ctrl+Alt+Up",
      "volumeDown": "Ctrl+Alt+Down",
      "volumeUpButton": null,
      "volumeDownButton": null
    }
  },
  "profileHotkeys": {
    "iRacing": "Ctrl+Shift+F1",
    "Speakers - Personal": "Ctrl+Shift+F2",
    "Headphones - Personal (Online)": "",
    "Headphones - Work": ""
  },
  "profileButtons": {
    "iRacing": "PXN-CB1:80013668-0000-0000-0000-504944564944:Button:5",
    "Speakers - Personal": "",
    "Headphones - Personal (Online)": "",
    "Headphones - Work": ""
  },
  "controllerMappings": [],
  "enabledChannels": [
    "Mic",
    "Game",
    "Chat",
    "Music",
    "System"
  ],
  "window": {
    "width": 900,
    "height": 700,
    "left": 100,
    "top": 100,
    "startMinimized": true,
    "startWithWindows": false
  }
}
```

## ConfigurationService

### Methods

**`Load()`** - Loads configuration from file
- Returns `AppSettings` object
- Creates default config if file doesn't exist
- Creates directory if needed
- Handles JSON parsing errors gracefully

**`Save(AppSettings settings)`** - Saves configuration to file
- Creates directory if needed
- Writes formatted JSON (indented)
- Atomic write (temp file + rename)

**`GetConfigFilePath()`** - Returns full path to config.json

**`ConfigExists()`** - Checks if config file exists

### Usage Example

```csharp
var configService = new ConfigurationService();

// Load configuration
var settings = configService.Load();

// Modify settings
settings.General.SerialNumber = "S220202153DI7";
settings.ProfileHotkeys["iRacing"] = "Ctrl+Shift+F1";
settings.ProfileButtons["iRacing"] = "PXN-CB1:guid:Button:5";
settings.EnabledChannels.Add("LineIn");
settings.Window.StartWithWindows = true;

// Save changes
configService.Save(settings);
```

## Default Configuration

On first run, creates default config with:

**General Settings:**
- No serial number (must configure)
- Volume step: 10
- Cache time: 30 seconds
- API endpoint: http://localhost:14564

**Enabled Channels:**
- Mic, Game, Chat, Music, System

**Profiles:**
- 4 predefined profiles (no hotkeys assigned):
  - Speakers - Personal
  - Headphones - Personal (Online)
  - Headphones - Work
  - iRacing

**Volume Hotkeys:**
- Empty for all channels (user configurable)

**Controller Mappings:**
- Empty (user configurable)

**Window Settings:**
- 900×700 size at position (100, 100)
- Start minimized: true
- Start with Windows: false

## Manual Editing

### When to Edit Manually
- Batch configuration changes
- Copying config between machines
- Advanced customization
- Troubleshooting

### How to Edit Manually
1. **Close SimControlCentre** (important!)
2. Open `%LocalAppData%\SimControlCentre\config.json` in text editor
3. Make changes (validate JSON syntax!)
4. Save file
5. Start SimControlCentre

### JSON Validation
- Use VS Code or other editor with JSON validation
- Online validators: jsonlint.com
- Invalid JSON will cause config to reset to defaults

## Configuration Tips

### Backup Your Config
```powershell
# Backup
Copy-Item "$env:LOCALAPPDATA\SimControlCentre\config.json" `
          "$env:LOCALAPPDATA\SimControlCentre\config.backup.json"

# Restore
Copy-Item "$env:LOCALAPPDATA\SimControlCentre\config.backup.json" `
          "$env:LOCALAPPDATA\SimControlCentre\config.json"
```

### Reset to Defaults
1. Close SimControlCentre
2. Delete `config.json`
3. Start SimControlCentre ? Creates new default config

### Transfer Config to Another PC
1. Copy `config.json` from PC1
2. Place in `%LocalAppData%\SimControlCentre\` on PC2
3. **Update serial number if different GoXLR device**
4. **Recapture controller buttons if different devices**

### Button String Format
**Find ProductGuid:**
1. Open SimControlCentre ? Controllers tab
2. Note "Product GUID" for your device
3. Use format: `DeviceName:{ProductGuid}:Button:{number}`

**Example:**
```
PXN-CB1:80013668-0000-0000-0000-504944564944:Button:5
```

## Troubleshooting

### Config Not Loading
- Check file exists at correct location
- Verify JSON syntax (no trailing commas, quotes correct)
- Check file permissions
- Try deleting and letting app recreate

### Changes Not Persisting
- Make sure app is closed when editing manually
- Check ConfigurationService.Save() is being called
- Verify no file permission issues

### Button Mappings Not Working After Reboot
- Old format used InstanceGuid (unstable)
- New format uses ProductGuid (stable)
- **Solution:** Recapture all button mappings via UI

### Window Off-Screen
- Delete `config.json`
- Or manually edit `window.left` and `window.top` to `100`

## Advanced: Programmatic Access

### Reading Config
```csharp
var configService = new ConfigurationService();
var settings = configService.Load();

// Access settings
var serial = settings.General.SerialNumber;
var gameVolUp = settings.VolumeHotkeys["Game"].VolumeUp;
var profiles = settings.ProfileHotkeys.Keys;
```

### Modifying Config
```csharp
// Add new channel hotkeys
settings.VolumeHotkeys["LineIn"] = new ChannelHotkeys
{
    VolumeUp = "Ctrl+Shift+L",
    VolumeDown = "Ctrl+Shift+K"
};

// Add new profile
settings.ProfileHotkeys["NewProfile"] = "Ctrl+F3";
settings.ProfileButtons["NewProfile"] = "";

// Enable channel
if (!settings.EnabledChannels.Contains("LineIn"))
{
    settings.EnabledChannels.Add("LineIn");
}

// Save
configService.Save(settings);
```

## Configuration Versioning

Currently no version field - all configs are compatible. Future versions may add:
- `configVersion` field
- Migration logic for old configs
- Deprecation warnings

## Security

### Sensitive Data
- Serial number is not sensitive (device identifier)
- No passwords or tokens stored
- Config file is user-readable JSON

### File Permissions
- Stored in user's AppData (user-only access)
- No special permissions required
- Not encrypted (no sensitive data)

## Performance

### Load Time
- < 1ms for typical config
- Synchronous load on startup

### Save Time
- < 5ms for typical config
- Atomic write prevents corruption
- Auto-saves don't block UI
