# Quick Start Guide

## Installation

### Option 1: Download Pre-Built Executable
1. Download `SimControlCentre.exe` from [Releases](https://github.com/dcunliffe1980/SimControlCentre/releases)
2. Place it anywhere you like (e.g., `C:\Program Files\SimControlCentre\`)
3. Double-click to run
4. Look for the "SC" icon in your system tray

### Option 2: Build from Source

1. **Build the application:**
   ```powershell
   dotnet build SimControlCentre/SimControlCentre.csproj
   ```

2. **Run the application:**
   ```powershell
   dotnet run --project SimControlCentre/SimControlCentre.csproj
   ```

3. **You should see:**
   - A "SC" icon appears in your system tray
   - Balloon notification: "GoXLR Daemon Detected" ? "GoXLR Ready!"
   - No main window (starts minimized by default)

## Opening the Settings Window

**Option 1:** Left-click the system tray icon

**Option 2:** Right-click the tray icon ? "Open Settings"

## First-Time Setup

### 1. Configure Your GoXLR Serial Number

**Auto-Detect (Recommended):**
1. Open Settings ? General tab
2. Click "Detect Serial Number"
3. Should auto-populate if you have one GoXLR device
4. Connection Status turns green ?

**Manual Entry:**
1. Find your serial in GoXLR Utility
2. Enter it in the "Serial Number" field
3. Click "Test Connection"

### 2. Select Channels to Control

1. Go to **Channels & Profiles tab**
2. Check the channels you want hotkeys for:
   - ? Mic
   - ? Game  
   - ? Chat
   - ? Music
   - etc.
3. Changes save automatically

### 3. Add Your Profiles

Your GoXLR profiles are auto-fetched!

1. Stay on **Channels & Profiles tab**
2. Wait for profiles to populate in the dropdown
3. Select a profile ? Click "Add Selected Profile"
4. Repeat for all profiles you want hotkeys for

**OR add custom profiles:**
- Type a name ? Click "Add Custom Profile"

### 4. Assign Keyboard Hotkeys

1. Go to **Hotkeys tab**
2. Find the channel/profile you want to control
3. Click "?/?? Capture" button
4. Press your desired key combination (e.g., `Ctrl+Shift+Up`)
5. Done! Shows in the textbox

**Tips:**
- Use modifiers (Ctrl, Shift, Alt) to avoid conflicts
- Clear button removes the hotkey
- Conflict detection shows if key is already in use

### 5. Assign Controller Buttons (Optional)

If you have a button box, wheel, or controller:

1. Go to **Hotkeys tab**
2. Click "?/?? Capture" button
3. **Press a button on your controller** instead of a keyboard key
4. Shows as "PXN-CB1 Btn 1" (device name + button number)
5. Done!

**Verify your controller is detected:**
- Go to **Controllers tab**
- Should list all detected devices
- Button indicator flashes green when you press buttons

## Testing Your Setup

### Test Volume Control
1. Assign a hotkey to "Game Volume Up"
2. Press the hotkey
3. Game volume should increase

### Test Profile Switching
1. Assign a hotkey to a profile (e.g., "iRacing")
2. Press the hotkey
3. GoXLR should switch to that profile

### Test Controller Buttons
1. Assign a button to "Music Volume Down"
2. Press that button on your controller
3. Music volume should decrease

## Auto-Start with Windows

1. Go to **General tab**
2. Check "Start application with Windows"
3. Next time you boot, app starts automatically!

## Configuration File Location

Press `Win + R` and paste this:
```
%LocalAppData%\SimControlCentre
```

You'll find `config.json` in this folder. Backup this file to save your configuration!

## System Tray Menu

Right-click the "SC" icon:
- **Open Settings** - Opens settings window
- **? Enable Hotkeys** - Toggle all keyboard hotkeys on/off
- **Exit** - Quit the application

## Common Issues

### "GoXLR Daemon Not Found"
- **Solution:** Wait 2 minutes for GoXLR Utility (daemon) to start
- Or manually start TC-Helicon GoXLR Utility
- App checks every 5 seconds

### Controller buttons not working after reboot
- **This is fixed!** Buttons now use ProductGuid (stable across reboots)
- If you have old mappings, just recapture them once
- They'll show device name like "PXN-CB1 Btn 1"

### First button press fails, then works
- **Normal behavior during cache warm-up**
- Wait 5-10 seconds after "GoXLR Ready!" notification
- Or just press again - subsequent presses work immediately
- Cache lasts 30 seconds

### Hotkey conflict detected
- Try different key combination
- Check if another app is using that key
- Use more modifiers (Ctrl+Shift+Alt)

### System tray icon not appearing
- Check the "hidden icons" area (^ arrow in taskbar)
- Drag the "SC" icon to main tray area

### Window doesn't open when clicking tray icon
- Window might be off-screen from previous session
- Delete `%LocalAppData%\SimControlCentre\config.json`
- Restart app to reset window position

## Advanced Tips

### Volume Step Adjustment
- Default: 10 (on 0-255 scale)
- Lower = finer control, Higher = bigger jumps
- General tab ? Volume Step

### Cache Time Adjustment
- Default: 30 seconds
- Lower = more responsive to external changes, more API calls
- Higher = faster button presses, less accurate
- General tab ? Volume Cache Time (ms)

### Disable Hotkeys Temporarily
- Right-click tray icon ? Uncheck "Enable Hotkeys"
- Useful when playing games with conflicting keys
- Controller buttons still work!

## Next Steps

? Configured serial number  
? Added channels and profiles  
? Assigned hotkeys  
? Tested everything works  

**You're all set!** Enjoy seamless GoXLR control! ??

## Need Help?

Check the other documentation:
- [Configuration Details](CONFIGURATION.md)
- [Hotkey Setup](HOTKEYS.md)
- [Settings UI Guide](SETTINGS_UI.md)
3. Test profile switching
4. Wait for next phase: **Global Hotkeys** (coming soon!)

## Need Help?

Check the documentation files:
- **README.md** - Project overview and status
- **CONFIGURATION.md** - Configuration system details
- **SYSTEM_TRAY.md** - System tray behavior
- **GOXLR_API.md** - API client documentation

## Exiting the Application

**Important:** Closing the settings window doesn't exit the app!

To actually exit:
1. Right-click the system tray icon
2. Click "Exit"

The app will save your window position and then terminate.
