# Settings UI Guide

## Overview
Modern tabbed interface for configuring all aspects of SimControlCentre. Five main tabs provide complete control over GoXLR integration, hotkeys, channels, profiles, and controllers.

## Tab 1: General

### GoXLR Connection
**Serial Number:**
- Text input for GoXLR serial number
- "Detect Serial Number" button - Auto-detects if only one device connected
- "Save Serial Number" button - Saves to configuration
- Connection Status display with color coding:
  - ?? Green: "? Connected (Profile: YourProfile)"
  - ?? Orange: "? Serial number not configured"
  - ?? Red: "? GoXLR Utility not running"
- "Test Connection" button - Verifies connection manually

**API Endpoint:**
- Default: `http://localhost:14564`
- Change only if using custom GoXLR Utility port

### Volume Settings
**Volume Step (0-255):**
- How much to adjust volume per hotkey/button press
- Default: 10
- Lower = finer control, Higher = bigger jumps

**Volume Cache Time (ms):**
- How long to cache volume values before refetching
- Default: 30000 (30 seconds)
- Lower = more API calls, Higher = less frequent updates

**Auto-Save:** Changes save immediately

### General Settings
**Start application minimized to system tray:**
- Checkbox - App starts hidden on launch
- Default: Checked

**Start application with Windows:**
- Checkbox - Creates Windows startup registry entry
- Auto-saves immediately
- Works with Current User (no UAC prompt)

### Quick Actions
**Open GoXLR Test Window:**
- Opens API testing utility
- Test volume adjustments
- Test profile switching
- View current device status

**Reload Hotkeys:**
- Re-registers all keyboard hotkeys
- Useful after manual config.json edits
- Shows count of registered hotkeys

---

## Tab 2: Hotkeys

### Overview
Configure keyboard hotkeys and controller buttons for volume control and profile switching.

### Volume Hotkeys Section
Lists all enabled channels with up/down controls:

**For Each Channel (e.g., "Game"):**
- **Volume Up:** Textbox showing current hotkey/button (e.g., "Ctrl+Shift+Up OR PXN-CB1 Btn 1")
- **"?/?? Capture" button:** Click to capture new keyboard key or controller button
- **"Clear" button:** Removes both keyboard and controller assignments
- **Volume Down:** Same controls as Volume Up

**Inline Capture:**
1. Click "?/?? Capture"
2. Textbox turns yellow with "Press key or button..."
3. Press keyboard key OR controller button
4. Textbox updates with new assignment
5. Auto-saves immediately

**Conflict Detection:**
- If key/button already assigned, textbox turns red
- Shows "Already in use" message
- Reverts to original after 3 seconds

### Profile Hotkeys Section
Lists all configured profiles:

**For Each Profile (e.g., "iRacing"):**
- **Hotkey/Button:** Textbox showing current assignment
- **"?/?? Capture" button:** Capture keyboard or controller
- **"Clear" button:** Remove assignment

### Display Format
- **Keyboard only:** `Ctrl+Shift+Up`
- **Controller only:** `PXN-CB1 Btn 5`
- **Both assigned:** `Ctrl+Shift+Up OR PXN-CB1 Btn 5`

### Auto-Save
- All changes save immediately
- Hotkeys re-register automatically
- No manual "Save" button needed

---

## Tab 3: Channels & Profiles

### Channels Section
**Enable/Disable Channels:**
- Checkbox list of all GoXLR channels:
  - ? Mic
  - ? Line In
  - ? System
  - ? Game
  - ? Chat
  - ? Sample
  - ? Music
  - ? Headphones
  - ? Mic Monitor
  - ? Line Out

**Behavior:**
- Only checked channels appear in Hotkeys tab
- Unchecking a channel removes it from Hotkeys
- Auto-saves on change

### Profiles Section
**Auto-Fetch from GoXLR:**
- On startup, waits for GoXLR Daemon
- Fetches all profiles from GoXLR Utility
- Populates dropdown with available profiles

**Profile Status:**
- "Loading profiles..." - During startup
- "Found X profile(s) - Y available to add" - Ready
- "No profiles found" - GoXLR Utility issue

**Add Profiles:**
- **Dropdown:** Shows profiles from GoXLR that aren't configured yet
- **"Add Selected Profile" button:** Adds profile to configured list
- **"? Refresh" button:** Refetches profiles from GoXLR

**Configured Profiles List:**
- Shows all profiles you've added
- Each has a "Remove" button
- These appear in Hotkeys tab for assignment

**Auto-Save:**
- Adding/removing profiles saves immediately
- Updates Hotkeys tab in real-time

---

## Tab 4: Controllers

### Controller List
Shows all detected DirectInput devices:

**For Each Device:**
- Device Name (e.g., "PXN-CB1")
- Device Type (e.g., "Joystick")
- Instance GUID
- Product GUID (used for stable button mappings)

**Device Filtering:**
- Automatically filters out keyboards and mice
- Only shows button-capable devices
- Updates on "Refresh Controllers" click

### Button Indicator
**Real-Time Activity:**
- Green circle indicator
- Flashes when ANY button is pressed on ANY controller
- Helps verify device is working

**Label:** "Button Activity: ?" (green when active, gray when idle)

### Refresh Controllers Button
- Rescans for DirectInput devices
- Updates the list
- Useful after plugging/unplugging devices

### Device Detection
**Automatic Initialization:**
- All detected devices are automatically initialized on startup
- No manual setup required
- Devices are ready to use immediately

---

## Tab 5: About

### Application Information
- **Name:** SimControlCentre
- **Version:** [Current Version]
- **Description:** GoXLR Control Center with keyboard and controller support

### Configuration
**Config File Path:**
- Displays: `C:\Users\[You]\AppData\Local\SimControlCentre\config.json`
- **"Open Config Folder" button:** Opens folder in Windows Explorer

**Purpose:**
- Manual config editing (advanced users)
- Backup configuration
- View/edit raw JSON

---

## UI Design Principles

### Layout
- **TabControl** - Main navigation between sections
- **ScrollViewer** - Each tab scrollable for small screens
- **GroupBox** - Logical grouping of related settings
- **Window Size:**
  - Minimum: 700×500
  - Default: 900×700
  - Remembers position and size

### Color Coding
- ?? **Green** - Connected/Success/Active
- ?? **Orange** - Testing/Warning/Processing
- ?? **Red** - Error/Disconnected
- ? **Gray** - Unknown/Waiting/Inactive

### Visual Feedback
- **Yellow background** - Capture mode active
- **Light pink background** - Conflict detected
- **Green flash** - Button press detected
- **Balloon tips** - System tray notifications

### Auto-Save Philosophy
- No manual "Save" buttons in Hotkeys/Channels tabs
- Changes persist immediately
- Reduces user error (forgetting to save)
- "Save Changes" button only in General tab for Volume Settings

---

## Settings Persistence

### Auto-Save Triggers
Settings save automatically when:
- Serial number detected/saved
- Channel checked/unchecked
- Profile added/removed
- Hotkey/button captured
- Start with Windows toggled

### Manual Save Required
Only Volume Settings require clicking "Save Volume Settings":
- Volume Step changes
- Cache Time changes
- These affect performance, so require explicit confirmation

### On Exit
- Window position and size saved automatically
- No confirmation dialogs
- Clean shutdown

---

## Keyboard Shortcuts (Future)

Planned shortcuts for Settings window:
- `Ctrl+S` - Save (on tabs with Save button)
- `Escape` - Cancel capture mode
- `F5` - Refresh controllers
- `Ctrl+Tab` - Next tab
- `Ctrl+Shift+Tab` - Previous tab

---

## Best Practices

### Configuration Workflow
1. **General Tab:**
   - Detect serial number
   - Test connection
   - Configure volume settings

2. **Channels & Profiles Tab:**
   - Enable channels you use
   - Add profiles from GoXLR

3. **Hotkeys Tab:**
   - Assign keyboard hotkeys
   - Assign controller buttons
   - Test each assignment

4. **Controllers Tab:**
   - Verify devices detected
   - Check button indicator works

### Testing
- Use "Test GoXLR Window" to verify API connection
- Test each hotkey individually
- Press controller buttons and watch indicator
- Test volume changes in different apps

### Troubleshooting
- **Connection fails:** Check GoXLR Utility is running
- **Hotkey conflicts:** Use more modifiers
- **Controller not detected:** Click "Refresh Controllers"
- **Button doesn't work:** Recapture using ProductGuid format

---

## Technical Notes

### Window Handle
- Settings window provides window handle for Win32 hotkey registration
- Must exist before hotkeys can be registered
- Handle obtained via `WindowInteropHelper`

### DirectInput Polling
- Controllers polled every 50ms
- Button events raised on state changes
- Debouncing (100ms) prevents double-presses

### Configuration Loading
- Config loaded on startup
- UI populated from `AppSettings` object
- Changes update `AppSettings` and save to JSON

### Validation
- Serial number format not validated (any string accepted)
- Volume step must be 1-255
- Cache time must be positive integer
- Invalid input shows warning MessageBox
Window position and size are saved automatically on application exit (existing behavior).

### Reload
"Reload Hotkeys" button re-reads config from disk and re-registers all hotkeys without restart.

## Next Steps for Phase 5

### Priority Features:
1. **Hotkey Capture Interface**
   - Click button to start capture
   - Press key combination
   - Save to configuration
   - Real-time validation

2. **Hotkey Management**
   - Edit existing hotkeys
   - Delete hotkeys
   - Clear hotkey (set to empty)
   - Conflict detection (warn if hotkey already used)

3. **Channel Management**
   - Enable/disable channels via checkboxes
   - Show which channels have volume hotkeys configured
   - Quick add hotkeys for enabled channels

4. **Profile Management**
   - List available profiles from GoXLR
   - Assign hotkeys to profiles
   - Test profile loading
   - Show currently active profile

5. **Visual Improvements**
   - Better spacing and alignment
   - Icons for tabs
   - Tooltips for all inputs
   - Status indicators
   - Progress bars for long operations

### Future Enhancements:
- **Import/Export Settings** - Backup and share configurations
- **Hotkey Templates** - Pre-defined hotkey schemes
- **Theme Support** - Dark/Light mode
- **Advanced Settings Tab** - API endpoint, timeouts, logging
- **Controller Tab** - Button box configuration (Phase 6)

## User Workflow

### First Run:
1. App starts ? Auto-detects serial ? Saves automatically
2. User opens Settings window
3. Sees "General" tab with detected serial and green connection status
4. Can adjust volume step if desired
5. Navigate to "Hotkeys" tab to see default hotkeys
6. Close window ? Continues running in tray

### Configuration Changes:
1. User opens Settings window
2. Navigates to desired tab
3. Makes changes
4. Clicks "Save" button
5. See success message
6. Changes applied immediately (or on restart for cache time)

### Troubleshooting:
1. "About" tab shows config file location
2. "Open Config Folder" button opens Explorer
3. Can manually edit JSON if needed
4. "Reload Hotkeys" applies changes without restart

## Testing

### Manual Testing:
1. ? Open Settings window from tray
2. ? Navigate between tabs
3. ? Detect serial number
4. ? Save serial number
5. ? Test connection
6. ? Change volume step
7. ? Change cache time
8. ? Save volume settings
9. ? Open GoXLR test window
10. ? Reload hotkeys
11. ? View hotkey status
12. ? Open config folder
13. ? Resize window
14. ? Close and reopen window

### Validation Testing:
- ? Volume step < 1 ? Warning
- ? Volume step > 255 ? Warning
- ? Cache time < 0 ? Warning
- ? Non-numeric input ? Warning

## Current Status

**Completed:**
- ? Tabbed interface structure
- ? General settings tab (fully functional)
- ? Hotkeys tab (read-only display)
- ? Channels tab (placeholder)
- ? About tab (info + utilities)
- ? Input validation
- ? Settings persistence
- ? Config folder access

**In Progress:**
- ?? Hotkey capture interface
- ?? Hotkey editing
- ?? Channel management

**Planned:**
- ? Profile management UI
- ? Controller configuration (Phase 6)
- ? Visual enhancements

## Architecture

### XAML Structure:
```xml
Window
  ?? Grid
      ?? TabControl
          ?? TabItem "General"
          ?   ?? ScrollViewer
          ?       ?? StackPanel
          ?           ?? GroupBox "GoXLR Configuration"
          ?           ?? GroupBox "Volume Settings"
          ?           ?? GroupBox "Quick Actions"
          ?? TabItem "Hotkeys"
          ?? TabItem "Channels"
          ?? TabItem "About"
```

### Code Structure:
- `MainWindow.xaml.cs` - Event handlers for all UI interactions
- `ConfigurationService` - Load/save settings
- `HotkeyManager` - Register/unregister hotkeys
- `GoXLRService` - Test connection, get status

## Next Phase
After completing Phase 5 (Settings UI), we'll move to:
**Phase 6: Controller Input Detection**
- DirectInput for button boxes
- XInput for Xbox controllers
- Raw Input API fallback
- Button mapping to GoXLR actions
