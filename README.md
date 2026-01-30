# SimControlCentre - Project Status

## ? Completed Phases

### Phase 1: Configuration Management
**Status:** Complete ?

**Components:**
- `Models/AppSettings.cs` - Root configuration
- `Models/GeneralSettings.cs` - Serial number, API endpoint, volume step
- `Models/ChannelHotkeys.cs` - Volume up/down hotkeys
- `Models/ControllerMapping.cs` - Controller button mappings
- `Models/WindowSettings.cs` - Window position/size
- `Services/ConfigurationService.cs` - JSON persistence

**Features:**
- JSON configuration in `%LocalAppData%\SimControlCentre\config.json`
- Default settings with 4 predefined profiles
- Auto-creates config on first run
- Validates and saves window settings

**Documentation:** CONFIGURATION.md

---

### Phase 2: System Tray Application
**Status:** Complete ?

**Components:**
- `App.xaml` / `App.xaml.cs` - Application lifecycle
- `MainWindow.xaml` / `MainWindow.xaml.cs` - Settings window
- Hardcodet.NotifyIcon.Wpf - NuGet package

**Features:**
- Runs in system tray with context menu
- Auto-generated icon ("SC" text) with custom icon support
- Minimize to tray behavior
- Close to tray (not exit)
- Window position/size persistence
- Start minimized option

**Documentation:** SYSTEM_TRAY.md

---

### Phase 3: GoXLR API Client
**Status:** Complete ?

**Components:**
- `Models/GoXLRDeviceStatus.cs` - API response models
- `Models/GoXLRCommand.cs` - API request models
- `Services/GoXLRApiClient.cs` - HTTP client with caching
- `Services/GoXLRService.cs` - High-level service wrapper
- `Views/GoXLRTestWindow.xaml` / `.cs` - Test utility

**Features:**
- Volume control (get, set, adjust)
- Profile management (list, load, get current)
- Intelligent caching (5 seconds default)
- Connection health check
- Error handling and graceful degradation
- Percentage calculation for feedback
- Test window for manual API testing

**API Endpoints:**
- GET `/api/get-devices` - Device status
- POST `/api/command` - SetVolume, LoadProfile

**Documentation:** GOXLR_API.md

---

## ?? Next Phases

### Phase 4: Global Hotkeys (Next)
**Goal:** Register keyboard hotkeys for volume and profile control

**Planned Components:**
- Hotkey registration service (Win32 API)
- Hotkey manager with conflict detection
- Integration with GoXLRService
- Configurable hotkeys via UI

**Features to implement:**
- Register hotkeys from configuration
- Volume up/down hotkeys per channel
- Profile switching hotkeys
- Hotkey capture UI for configuration
- Modifier support (Ctrl, Shift, Alt, Win)

**Technologies:**
- Win32 `RegisterHotKey` API
- WPF key capture
- P/Invoke for native calls

---

### Phase 5: Settings UI
**Goal:** Complete configuration interface

**Features:**
- General settings editor (serial number, volume step)
- Channel management (enable/disable)
- Profile hotkey configuration
- Volume hotkey configuration
- Controller mapping editor
- Window settings
- Connection status display

---

### Phase 6: Controller Input Detection
**Goal:** Support button boxes and controllers

**Planned Technologies:**
- DirectInput (XInput wrapper)
- Raw Input API
- Windows.Gaming.Input (UWP)

**Features:**
- Multi-API detection strategy
- Button press/release detection
- Toggle mode for latch switches
- Keyboard key simulation
- Controller tester utility

---

### Phase 7: iRacing Integration
**Goal:** Auto-launch apps and profile switching

**Features:**
- iRacing SDK integration
- Session detection
- App launcher (SimHub, CrewChief, etc.)
- App closer/killer on session end
- Auto profile switching

---

## ?? Project Structure

```
SimControlCentre/
??? Models/
?   ??? AppSettings.cs
?   ??? GeneralSettings.cs
?   ??? ChannelHotkeys.cs
?   ??? ControllerMapping.cs
?   ??? WindowSettings.cs
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
