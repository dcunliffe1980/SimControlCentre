# System Tray Implementation

## Overview
SimControlCentre now runs as a system tray application with proper window management and configuration persistence.

## Features Implemented

### System Tray Icon
- ? Application runs in system tray
- ? Context menu with "Open Settings" and "Exit" options
- ? Left-click tray icon to open settings window
- ? Auto-generated fallback icon ("SC" text) if custom icon not provided
- ? Tooltip: "SimControlCentre - GoXLR Control"

### Window Management
- ? Starts minimized to tray (configurable via `settings.Window.StartMinimized`)
- ? Minimizing window hides it to tray instead of taskbar
- ? Closing window hides it to tray (doesn't exit application)
- ? Window position and size saved on exit
- ? Window position and size restored on startup
- ? Minimum window size: 700x500

### Configuration Integration
- ? Loads `AppSettings` on startup via `ConfigurationService`
- ? Saves window settings (width, height, position) on exit
- ? Creates default config file if not found
- ? Config location: `%LocalAppData%\SimControlCentre\config.json`

## Architecture

### App.xaml.cs
**Responsibilities:**
- Application lifecycle management
- System tray icon creation and management
- Configuration service initialization
- Main window creation (but not showing by default)

**Key Properties:**
- `_notifyIcon` - Hardcodet TaskbarIcon instance
- `_mainWindow` - Single MainWindow instance
- `_configService` - Configuration service instance
- `Settings` - Current AppSettings (accessible app-wide)

**Key Methods:**
- `Application_Startup()` - Loads config, creates tray icon, creates window
- `Application_Exit()` - Saves window settings, disposes tray icon
- `OpenSettingsWindow()` - Shows/activates main window
- `GetDefaultIcon()` - Generates fallback icon if custom not found

### MainWindow.xaml.cs
**Responsibilities:**
- Settings UI host (UI content to be added next)
- Window state management (minimize to tray)
- Window settings persistence

**Key Methods:**
- `RestoreWindowSettings()` - Applies saved window position/size
- `SaveWindowSettings()` - Persists window position/size to config
- `MainWindow_StateChanged()` - Handles minimize to tray
- `MainWindow_Closing()` - Prevents close, hides to tray instead

### Behavior Flow

**Startup:**
1. App loads configuration from JSON
2. Creates system tray icon with context menu
3. Creates MainWindow instance (but hidden)
4. If `StartMinimized = false`, shows window

**User clicks tray icon:**
1. Shows MainWindow
2. Sets WindowState to Normal
3. Activates window (brings to front)

**User minimizes window:**
1. MainWindow detects StateChanged to Minimized
2. Hides window (removes from taskbar)
3. Application continues running in tray

**User closes window:**
1. MainWindow_Closing event cancels close
2. Hides window instead
3. Application continues running in tray

**User clicks "Exit" in tray menu:**
1. Triggers Application.Shutdown()
2. Application_Exit saves window settings
3. Disposes tray icon
4. Application terminates

## NuGet Dependencies

### Hardcodet.NotifyIcon.Wpf (v2.0.1)
- Pure WPF system tray icon implementation
- Better than Windows Forms NotifyIcon for WPF apps
- Supports WPF context menus and commands

## Custom Icon

To use a custom icon:
1. Create or obtain a 32x32 .ico file
2. Place it at: `SimControlCentre/Resources/app.ico`
3. The app will automatically detect and use it

If no icon found, a blue square with "SC" text is generated as fallback.

## Configuration

### Window Settings in config.json
```json
{
  "window": {
    "width": 900,
    "height": 700,
    "left": 100,
    "top": 100,
    "startMinimized": true
  }
}
```

### Key Behaviors
- `startMinimized: true` - App starts hidden in tray
- `startMinimized: false` - App starts with window visible
- Window position/size only saved when window is in Normal state (not maximized/minimized)

## Next Steps
1. ? Configuration Management (Complete)
2. ? System Tray Application (Complete)
3. **GoXLR API Client** - HTTP client for volume/profile control
4. Settings UI - Configuration interface in MainWindow
5. Global Hotkeys - Keyboard hotkey registration
6. Controller Input - DirectInput/XInput/Raw Input detection

## Testing the Application

### Build and Run
```powershell
dotnet build SimControlCentre/SimControlCentre.csproj
dotnet run --project SimControlCentre/SimControlCentre.csproj
```

### Expected Behavior
1. Application starts with tray icon (no window)
2. Click tray icon ? Settings window appears
3. Minimize window ? Window hides to tray
4. Close window ? Window hides to tray
5. Right-click tray ? Context menu with Exit option
6. Exit ? Window position/size saved, app terminates

### Configuration File
After first run, check:
```
%LocalAppData%\SimControlCentre\config.json
```

Should contain default settings with your 4 profiles.
