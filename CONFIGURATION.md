# Configuration Management System

## Overview
Complete configuration management for SimControlCentre with JSON persistence.

## Models Created

### AppSettings (Root)
Main configuration container holding all settings.

### GeneralSettings
- `SerialNumber` - GoXLR device serial (user must configure)
- `VolumeStep` - Volume adjustment increment (default: 10)
- `VolumeCacheTimeMs` - Cache duration for volume values (default: 5000ms)
- `ApiEndpoint` - GoXLR Utility API URL (default: http://localhost:14564)

### ChannelHotkeys
- `VolumeUp` - Hotkey string for increasing volume
- `VolumeDown` - Hotkey string for decreasing volume

### ControllerMapping
- `ControllerId` - Controller identifier
- `ButtonNumber` - Button number (1-based)
- `KeyboardKey` - Key to simulate
- `ToggleMode` - For latch switches (single keypress vs hold)
- `Description` - Optional description

### WindowSettings
- `Width`, `Height` - Window dimensions (default: 900x700)
- `Left`, `Top` - Window position
- `StartMinimized` - Launch in system tray (default: true)

## ConfigurationService

### File Location
`%LocalAppData%\SimControlCentre\config.json`

### Methods
- `Load()` - Loads config from file, creates default if missing
- `Save(AppSettings)` - Saves config to JSON file
- `GetConfigFilePath()` - Returns full path to config file
- `ConfigExists()` - Checks if config file exists

### Default Configuration
Includes:
- 4 predefined profiles:
  - Speakers - Personal
  - Headphones - Personal (Online)
  - Headphones - Work
  - iRacing
- 4 enabled channels: Game, Music, Chat, System
- Empty hotkey bindings (user configurable)
- Empty controller mappings

### JSON Format
- Indented for readability
- camelCase property names
- Enums serialized as strings

## Usage Example

```csharp
var configService = new ConfigurationService();
var settings = configService.Load();

// Modify settings
settings.General.SerialNumber = "S220202153DI7";
settings.ProfileHotkeys["iRacing"] = "Ctrl+Shift+F1";

// Save changes
configService.Save(settings);
```

## Next Steps
1. ? Configuration Management (Complete)
2. System Tray Application
3. GoXLR API Client
4. Global Hotkey Registration
5. Controller Input Detection
