# System Tray Integration

## Overview
SimControlCentre runs as a system tray application with intelligent window management, balloon notifications, and hotkey control.

## Features

### System Tray Icon
- ? Auto-generated "SC" icon (blue background, white text)
- ? Tooltip: "SimControlCentre - GoXLR Control"
- ? Left-click opens Settings window
- ? Right-click shows context menu
- ? Always visible in system tray

### Context Menu
Right-click the tray icon to access:

**"Open Settings"** (bold, default action)
- Opens the Settings window
- Same as left-clicking the icon
- Shows and activates window if minimized

**"? Enable Hotkeys"** (checkable)
- Toggle all keyboard hotkeys on/off
- Checkmark shows current state:
  - ? Checked = Hotkeys enabled (default)
  - ? Unchecked = Hotkeys disabled
- Controller buttons always work (unaffected)
- Useful for gaming to avoid conflicts
- Shows balloon notification when toggled

**"Exit"**
- Saves configuration and window settings
- Closes application completely
- Only way to truly exit (closing window just hides it)

### Window Management
**Start Behavior:**
- Starts minimized to tray by default
- Configurable via "Start minimized" checkbox in General tab
- No taskbar icon on startup (if minimized)

**Minimize Behavior:**
- Minimizing window hides it to tray
- Removes from taskbar
- Application continues running
- Access via tray icon

**Close Behavior:**
- Closing window (X button) hides it to tray
- Does NOT exit application
- Use "Exit" from tray menu to actually quit

**Window Position:**
- Position and size saved on exit
- Restored on next startup
- Minimum size: 700×500 pixels
- Default size: 900×700 pixels

### Balloon Notifications
System tray shows informative balloon tips:

**On Startup:**
- "GoXLR Daemon Detected" - When daemon process found
- "GoXLR Ready!" - When fully connected and cache warmed
- "GoXLR Daemon Not Found" - If daemon doesn't start within 2 minutes

**On Hotkey Actions:**
- "Hotkeys Enabled" - Shows count of registered hotkeys
- "Hotkeys Disabled" - All keyboard hotkeys temporarily disabled

**On Volume Changes:** (Optional, if notifications enabled)
- "Game: 75%" - Quick volume feedback

### Auto-Start with Windows
**Registry-Based:**
- Creates entry in `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- No UAC prompt (user-level only)
- Enable via "Start with Windows" checkbox in General tab
- Automatically starts minimized to tray on login

**Startup Flow When Auto-Starting:**
1. Windows starts, runs SimControlCentre.exe
2. App waits for GoXLR Daemon (up to 2 minutes, checking every 5 seconds)
3. Shows "GoXLR Daemon Detected" balloon
4. Tests connection with retries
5. Pre-warms volume cache
6. Shows "GoXLR Ready!" balloon
7. Sits in tray, ready to use

## Architecture

### App.xaml.cs
**Responsibilities:**
- System tray icon lifecycle
- Application startup and shutdown
- Configuration loading/saving
- Services initialization (hotkeys, controllers, GoXLR)
- Window instance management

**Key Components:**
```csharp
private TaskbarIcon? _notifyIcon;              // System tray icon
private MainWindow? _mainWindow;               // Settings window
private ConfigurationService _configService;   // Config persistence
private GoXLRService? _goXLRService;          // GoXLR API wrapper
private HotkeyService? _hotkeyService;        // Win32 hotkeys
private HotkeyManager? _hotkeyManager;        // Hotkey-to-action mapping
private DirectInputService? _directInputService; // Controller detection
private ControllerManager? _controllerManager;   // Button-to-action mapping
public AppSettings Settings;                   // Current configuration
```

**Key Methods:**
- `Application_Startup()` - Initializes everything
- `Application_Exit()` - Cleanup and save
- `OpenSettingsWindow()` - Shows/activates main window
- `ToggleHotkeys_Click()` - Handles hotkey toggle
- `ShowVolumeNotification()` - Shows balloon tips
- `CreateSimpleIcon()` - Generates "SC" icon programmatically

### MainWindow.xaml.cs
**Responsibilities:**
- Settings UI host (5 tabs)
- Window state management
- Configuration changes
- Window position persistence

**Key Methods:**
- `RestoreWindowSettings()` - Applies saved position/size
- `SaveWindowSettings()` - Saves position/size to config
- `MainWindow_StateChanged()` - Handles minimize to tray
- `MainWindow_Closing()` - Prevents close, hides instead

### Behavior Flows

**Startup Flow:**
```
1. Load config.json
2. Create system tray icon
3. Wait for GoXLR Daemon (up to 2 minutes)
4. Test connection (with retries)
5. Pre-warm volume cache
6. Show "GoXLR Ready!" notification
7. Create MainWindow (hidden if StartMinimized=true)
8. Initialize hotkeys and controllers
9. Ready to use!
```

**Window Show Flow:**
```
1. User clicks tray icon OR context menu "Open Settings"
2. If window hidden, show it
3. Set WindowState = Normal (not minimized)
4. Activate() - brings to front
5. Window visible on taskbar
```

**Window Hide Flow:**
```
1. User minimizes window OR closes window
2. MainWindow_StateChanged or MainWindow_Closing event
3. Hide() - removes from taskbar
4. Window instance still exists (not disposed)
5. Tray icon remains visible
```

**Exit Flow:**
```
1. User clicks "Exit" in tray menu
2. SaveWindowSettings() - persist position/size
3. Dispose all services (hotkeys, controllers, GoXLR)
4. Dispose tray icon
5. Application.Shutdown()
```

**Hotkey Toggle Flow:**
```
1. User clicks "Enable Hotkeys" in tray menu (checked/unchecked)
2. If unchecked ? HotkeyManager.TemporaryUnregisterAll()
3. If checked ? HotkeyManager.RegisterAllHotkeys()
4. Show balloon notification with result
5. Controller buttons continue to work (unaffected)
```

## Implementation Details

### Icon Generation
**Auto-Generated Icon:**
- 16×16 pixels
- Blue background (#007ACC)
- White "SC" text
- Created programmatically using `DrawingVisual`
- No external icon file needed

**Code:**
```csharp
private ImageSource CreateSimpleIcon()
{
    var drawingVisual = new DrawingVisual();
    using (var context = drawingVisual.RenderOpen())
    {
        // Blue background
        context.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(0, 122, 204)),
            null, new Rect(0, 0, 16, 16));
        
        // White "SC" text
        var formattedText = new FormattedText("SC", ...);
        context.DrawText(formattedText, ...);
    }
    
    var bitmap = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(drawingVisual);
    return bitmap;
}
```

### Context Menu Definition
**XAML (App.xaml):**
```xaml
<ContextMenu x:Key="TrayContextMenu">
    <MenuItem Header="Open Settings" Click="OpenSettings_Click" FontWeight="Bold" />
    <Separator />
    <MenuItem Name="ToggleHotkeysMenuItem" 
             Header="Enable Hotkeys" 
             IsCheckable="True" 
             IsChecked="True"
             Click="ToggleHotkeys_Click"/>
    <Separator />
    <MenuItem Header="Exit" Click="Exit_Click" />
</ContextMenu>
```

### Window-to-Tray Behavior
**MainWindow.xaml.cs:**
```csharp
private void MainWindow_StateChanged(object? sender, EventArgs e)
{
    if (WindowState == WindowState.Minimized)
    {
        Hide(); // Hide instead of minimize to taskbar
    }
}

private void MainWindow_Closing(object? sender, CancelEventArgs e)
{
    e.Cancel = true; // Prevent actual close
    Hide();          // Just hide the window
}
```

## Configuration

### Start Minimized
**JSON:**
```json
{
  "window": {
    "startMinimized": true
  }
}
```

**UI:** General tab ? "Start application minimized to system tray"

### Start with Windows
**JSON:**
```json
{
  "window": {
    "startWithWindows": true
  }
}
```

**UI:** General tab ? "Start application with Windows"

**Registry Entry Created:**
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
Name: SimControlCentre
Value: "C:\Path\To\SimControlCentre.exe"
```

## Best Practices

### For Users
- **Minimize vs. Close:** Both hide to tray, use "Exit" to quit
- **Start with Windows:** Enable for seamless background operation
- **Hotkey Toggle:** Disable temporarily when gaming to avoid conflicts
- **Check Tray:** Look for "SC" icon if you can't find the app

### For Developers
- **Always dispose tray icon** - Prevents orphaned icons
- **Save on exit** - Window settings must persist
- **Handle StateChanged** - For minimize-to-tray behavior
- **Cancel Closing** - Prevent accidental exit
- **Create single window** - Don't recreate MainWindow repeatedly

## Troubleshooting

### Icon Not Showing
**Check:**
- Icon might be in "hidden icons" area (^ arrow in taskbar)
- Try restarting app
- Check if another instance is running

**Solution:**
- Drag icon to main tray area
- Kill other instances: `taskkill /im SimControlCentre.exe /f`

### Can't Find Window
**Check:**
- Window might be off-screen from previous session
- Check if minimized to tray

**Solution:**
- Delete config.json to reset window position
- Or manually edit `window.left` and `window.top` to `100`

### App Won't Exit
**Check:**
- Make sure you're using "Exit" from tray menu, not just closing window

**Solution:**
- Right-click tray icon ? Exit
- Or use Task Manager to kill process

### Start with Windows Not Working
**Check:**
- Registry entry exists: `regedit` ? `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Path is correct and executable exists

**Solution:**
- Uncheck and recheck "Start with Windows"
- Verify path in registry has quotes: `"C:\Path\To\App.exe"`

## NuGet Dependencies

### Hardcodet.Wpf.TaskbarNotification (v2.0.1)
- Pure WPF system tray icon implementation
- Better than Windows Forms NotifyIcon for WPF apps
- Supports WPF context menus and data binding
- Event handling for clicks and balloon tips

**Package:** `Hardcodet.NotifyIcon.Wpf`

## Future Enhancements

Potential improvements:
- Custom icon file support (user-provided .ico)
- Notification settings (show/hide, duration)
- Quick volume adjustments from tray menu
- Profile switching from tray menu
- Recent actions history
- System tray tooltip shows current profile
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
