# SimControlCentre - GoXLR Control Center

A comprehensive Windows application for controlling the TC-Helicon GoXLR audio mixer via keyboard hotkeys and controller buttons.

## ? Features

- **?? Keyboard Hotkeys** - Global hotkeys for volume control and profile switching
- **?? Controller Support** - Use button boxes, wheels, or any DirectInput device
- **?? Volume Control** - Adjust volume for any GoXLR channel
- **?? Profile Management** - Quick profile switching with hotkeys or buttons
- **?? Easy Configuration** - Intuitive UI for managing channels, profiles, and hotkeys
- **?? Auto-Start** - Start with Windows and minimize to system tray
- **? Fast & Reliable** - Intelligent caching and retry logic for smooth operation
- **?? Stable Mappings** - Controller buttons work consistently across reboots

## ?? Quick Start

### Prerequisites
- Windows 10/11
- .NET 8 Runtime (or use self-contained build)
- TC-Helicon GoXLR with GoXLR Utility running

### Installation

1. **Download the latest release** from [Releases](https://github.com/dcunliffe1980/SimControlCentre/releases)
2. **Run `SimControlCentre.exe`**
3. **Look for the "SC" icon in your system tray**
4. **Right-click ? Open Settings**

### First-Time Setup

1. **Auto-Detect Serial Number:**
   - Go to General tab ? Click "Detect Serial Number"
   - OR manually enter your GoXLR serial number

2. **Configure Channels:**
   - Go to Channels & Profiles tab
   - Check the channels you want to control
   - Click "Save Changes"

3. **Assign Hotkeys:**
   - Go to Hotkeys tab
   - Click "?/?? Capture" next to any channel
   - Press a keyboard key OR controller button
   - Done!

## ?? Completed Features

### Phase 1: Configuration Management ?
- JSON configuration in `%LocalAppData%\SimControlCentre\config.json`
- Default settings with sensible defaults
- Auto-creates config on first run
- Window position/size persistence

### Phase 2: System Tray Application ?
- Runs in system tray with context menu
- Auto-generated "SC" icon
- Minimize/close to tray behavior
- Window position persistence
- Start minimized option
- Start with Windows option

### Phase 3: GoXLR API Client ?
- Volume control (get, set, adjust by step)
- Profile management (list, load, get current)
- Intelligent caching (30 seconds, configurable)
- Retry logic for reliability
- Connection health check
- Pre-warming cache on startup
- Waits for GoXLR Daemon on startup

### Phase 4: Global Hotkeys ?
- Win32 `RegisterHotKey` API integration
- Hotkey manager with conflict detection
- Volume up/down per channel
- Profile switching
- Modifier support (Ctrl, Shift, Alt, Win)
- Enable/disable all hotkeys from tray menu

### Phase 5: Settings UI ?
- General settings (serial, volume step, cache time)
- Connection status display with auto-detect
- Hotkey configuration with inline capture
- Unified keyboard/controller capture UI
- Test GoXLR window for manual testing
- Channels & Profiles management
- Dynamic channel enable/disable
- Profile fetching from GoXLR
- Start with Windows toggle

### Phase 6: Controller Input ?
- DirectInput integration (SharpDX)
- Button press/release detection
- Stable ProductGuid-based mappings
- Device name display (e.g., "PXN-CB1 Btn 1")
- Conflict detection
- Supports wheels, button boxes, gamepads
- Filters out keyboards/mice
- Real-time button indicator
- Debouncing (100ms)

## ?? Supported Devices

Any DirectInput-compatible device:
- **Button Boxes** (e.g., PXN-CB1)
- **Racing Wheels** (e.g., Fanatec CSL DD)
- **Game Controllers** (Xbox, PlayStation, generic)
- **HOTAS** (flight sim controllers)
- **Pedals** with buttons

## ?? Documentation
## ?? Documentation

- **[Quick Start Guide](QUICKSTART.md)** - Get started in 5 minutes
- **[Configuration](CONFIGURATION.md)** - Settings file format and options
- **[GoXLR API](GOXLR_API.md)** - API endpoints and models
- **[Hotkeys](HOTKEYS.md)** - Keyboard and controller button setup
- **[Settings UI](SETTINGS_UI.md)** - UI tabs and features
- **[System Tray](SYSTEM_TRAY.md)** - Tray icon and menu options

## ??? Architecture

```
SimControlCentre/
??? Models/
?   ??? AppSettings.cs          # Root configuration
?   ??? GeneralSettings.cs      # Serial, API, volume step
?   ??? ChannelHotkeys.cs       # Keyboard + button mappings
?   ??? WindowSettings.cs       # Window position, startup options
?   ??? GoXLR*.cs              # API response models
??? Services/
?   ??? ConfigurationService.cs # JSON persistence
?   ??? GoXLRApiClient.cs      # HTTP client with caching
?   ??? GoXLRService.cs        # High-level GoXLR operations
?   ??? HotkeyService.cs       # Win32 hotkey registration
?   ??? HotkeyManager.cs       # Hotkey lifecycle management
?   ??? DirectInputService.cs  # Controller button detection
?   ??? ControllerManager.cs   # Button-to-action mapping
??? Views/
?   ??? MainWindow.xaml        # Settings UI
?   ??? GoXLRTestWindow.xaml   # API testing utility
??? App.xaml                   # Application entry point

```

## ?? Building from Source

### Prerequisites
- Visual Studio 2022 or .NET 8 SDK
- Windows 10/11

### Build Commands

```powershell
# Restore dependencies
dotnet restore

# Build
dotnet build SimControlCentre/SimControlCentre.csproj -c Release

# Run
dotnet run --project SimControlCentre/SimControlCentre.csproj

# Publish self-contained executable
dotnet publish SimControlCentre/SimControlCentre.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `SimControlCentre\bin\Release\net8.0-windows\win-x64\publish\SimControlCentre.exe`

## ?? Usage Examples

### Volume Control
- **Keyboard:** Assign `Ctrl+Shift+Up/Down` for Game volume
- **Controller:** Assign button 1/2 on your button box

### Profile Switching
- **Keyboard:** Assign `Ctrl+F1` to load "iRacing" profile
- **Controller:** Assign button 5 to switch profiles

### Auto-Start
- Go to General tab ? Check "Start with Windows"
- App starts silently on login, ready to use

## ?? Troubleshooting

### Controller buttons not working
1. Go to Controllers tab ? Verify your device is detected
2. Go to Hotkeys tab ? Recapture the button mapping
3. Check that button mappings show device name (e.g., "PXN-CB1 Btn 1")

### First button press fails
- This is normal during cache warm-up
- Wait 5-10 seconds after "GoXLR Ready!" notification
- Subsequent presses will work immediately

### GoXLR not detected
1. Ensure GoXLR Utility (daemon) is running
2. Check General tab ? Connection Status
3. Click "Detect Serial Number"
4. If fails, manually enter serial from GoXLR Utility

## ?? License

[Add your license here]

## ?? Acknowledgments

- **GoXLR Utility** by FrostyCoolSlug
- **SharpDX** for DirectInput support
- **Hardcodet.NotifyIcon.Wpf** for system tray

## ?? Contact

[Your contact information]

---

**Made with ?? for sim racers and streamers**
?   ??? GoXLRChannel.cs
?   ??? GoXLRDeviceStatus.cs
?   ??? GoXLRCommand.cs
??? Services/
?   ??? ConfigurationService.cs
?   ??? GoXLRApiClient.cs
?   ??? GoXLRService.cs
??? Views/
?   ??? GoXLRTestWindow.xaml / .cs
??? Resources/
?   ??? README.md (icon instructions)
??? App.xaml / App.xaml.cs
??? MainWindow.xaml / MainWindow.xaml.cs
??? SimControlCentre.csproj
```

---

## ?? Testing

### Test GoXLR API
1. Build and run application
2. Click tray icon to open settings
3. Click "Test GoXLR API" button
4. Test window opens with:
   - Connection test
   - Volume control (up/down per channel)
   - Profile loading
   - Status display with current volumes

### Prerequisites for Testing
- GoXLR Utility installed and running
- GoXLR device connected
- Serial number configured in settings

### Testing Without GoXLR
- All API calls fail gracefully
- Returns null/false without exceptions
- Check connection status with `IsConnectedAsync()`

---

## ?? Technology Stack

- **.NET 8** - Target framework
- **WPF** - UI framework
- **C#** - Primary language
- **JSON** - Configuration format
- **Hardcodet.NotifyIcon.Wpf** - System tray

---

## ?? Current Capabilities

### Working Features
? System tray application with context menu  
? Configuration persistence (JSON)  
? Window position/size memory  
? GoXLR API communication  
? Volume adjustment with caching  
? Profile switching  
? Connection health check  
? Test utility for manual API testing  

### Not Yet Implemented
? Global hotkeys  
? Settings UI  
? Controller input  
? iRacing integration  
? Auto-launch apps  

---

## ?? Configuration File Example

Location: `%LocalAppData%\SimControlCentre\config.json`

```json
{
  "general": {
    "serialNumber": "",
    "volumeStep": 10,
    "volumeCacheTimeMs": 5000,
    "apiEndpoint": "http://localhost:14564"
  },
  "enabledChannels": [
    "Game",
    "Music",
    "Chat",
    "System"
  ],
  "profileHotkeys": {
    "Speakers - Personal": "",
    "Headphones - Personal (Online)": "",
    "Headphones - Work": "",
    "iRacing": ""
  },
  "volumeHotkeys": {
    "Game": {
      "volumeUp": "",
      "volumeDown": ""
    },
    "Music": {
      "volumeUp": "",
      "volumeDown": ""
    },
    "Chat": {
      "volumeUp": "",
      "volumeDown": ""
    },
    "System": {
      "volumeUp": "",
      "volumeDown": ""
    }
  },
  "controllerMappings": [],
  "window": {
    "width": 900,
    "height": 700,
    "left": 100,
    "top": 100,
    "startMinimized": true
  }
}
```

---

## ?? Running the Application

### Build
```powershell
dotnet build SimControlCentre/SimControlCentre.csproj
```

### Run
```powershell
dotnet run --project SimControlCentre/SimControlCentre.csproj
```

### Release Build
```powershell
dotnet publish SimControlCentre/SimControlCentre.csproj -c Release -r win-x64 --self-contained
```

---

## ?? Documentation Files

- **README.md** - This file
- **CONFIGURATION.md** - Configuration system details
- **SYSTEM_TRAY.md** - System tray implementation
- **GOXLR_API.md** - GoXLR API client documentation
- **AHK/** - Original AutoHotkey script (reference)

---

## ?? Milestones

- [x] Project created (.NET 8 WPF)
- [x] Configuration management
- [x] System tray integration
- [x] GoXLR API client
- [x] Test utility
- [ ] Global hotkeys
- [ ] Settings UI
- [ ] Controller input
- [ ] iRacing integration

**Current Progress: 3/8 major phases complete (37.5%)**
