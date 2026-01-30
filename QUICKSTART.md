# Quick Start Guide

## First Run

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
   - No main window (starts minimized by default)

## Opening the Settings Window

**Option 1:** Left-click the system tray icon

**Option 2:** Right-click the tray icon ? "Open Settings"

## Testing GoXLR API

1. Make sure **GoXLR Utility is running** (http://localhost:14564)

2. Click the **"Test GoXLR API"** button in the main window

3. In the test window:
   - Click **"Test Connection"** to verify GoXLR Utility is accessible
   - Click **"Refresh Status"** to see current profile and volumes
   - Use **Volume Up/Down** buttons to test volume adjustment
   - Select a profile and click **"Load Profile"** to test profile switching

## Configuring Your Serial Number

1. **Find your GoXLR serial number:**
   - Open GoXLR Utility
   - Look for serial number in the interface
   - OR use the test window - connection error will show available serials

2. **Edit the configuration file:**
   - Location: `%LocalAppData%\SimControlCentre\config.json`
   - Open in any text editor (Notepad, VS Code, etc.)
   - Find the line: `"serialNumber": ""`
   - Replace with your serial: `"serialNumber": "S220202153DI7"`
   - Save the file

3. **Restart the application**

4. **Test again** - should now show your current profile and volumes

## Configuration File Location

Press `Win + R` and paste this:
```
%LocalAppData%\SimControlCentre
```

You'll find `config.json` in this folder.

## Default Configuration

The app creates sensible defaults on first run:
- **Volume Step:** 10 (on 0-255 scale)
- **Cache Time:** 5000ms (5 seconds)
- **API Endpoint:** http://localhost:14564
- **Enabled Channels:** Game, Music, Chat, System
- **Predefined Profiles:**
  - Speakers - Personal
  - Headphones - Personal (Online)
  - Headphones - Work
  - iRacing

## Common Issues

### "GoXLR Utility not running"
- **Solution:** Start the TC-Helicon GoXLR Utility application
- It must be running for the API to work

### "Unable to connect" in test window
- **Check 1:** Is GoXLR Utility running?
- **Check 2:** Is your serial number configured correctly?
- **Check 3:** Try opening http://localhost:14564/api/get-devices in a browser

### System tray icon not appearing
- **Check:** Look in the "hidden icons" area of your taskbar
- **Fix:** Drag the icon to the main tray area

### Window doesn't open when clicking tray icon
- **Check:** The window might be off-screen (from previous session)
- **Fix:** Delete config.json and restart app to reset window position

## Next Steps

Once the API is working:
1. Configure your serial number
2. Test volume adjustments
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
