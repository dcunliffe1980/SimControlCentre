# Settings UI Implementation

## Overview
Modern tabbed interface for configuring all aspects of SimControlCentre.

## Phase 5: Settings UI - Initial Implementation

### Features Implemented

#### **General Tab**
Settings for GoXLR connection and volume behavior.

**GoXLR Configuration:**
- Serial Number input with auto-detect button
- Save Serial Number button
- Connection Status display with color coding
- Test Connection button

**Volume Settings:**
- Volume Step (1-255) - How much to adjust volume per hotkey press
- Volume Cache Time (ms) - How long to cache volume values
- Save Volume Settings button

**Quick Actions:**
- Open GoXLR Test Window - Debug and test API
- Reload Hotkeys - Re-register hotkeys after config changes

#### **Hotkeys Tab**
View currently configured hotkeys.

**Current Implementation:**
- Display all configured volume hotkeys (per channel)
- Display all configured profile hotkeys
- Shows count of total hotkeys

**Coming Next:**
- Visual hotkey capture interface
- Edit/delete existing hotkeys
- Add new hotkeys
- Conflict detection

#### **Channels Tab**
Manage which GoXLR channels are enabled.

**Coming Next:**
- Checkbox list of all channels (Game, Music, Chat, System, etc.)
- Enable/disable channels
- Reorder channels

#### **About Tab**
Application information and utilities.

**Current Implementation:**
- Application name and version
- Description
- Configuration file path display
- Open Config Folder button

## UI Design

### Layout
- **TabControl** - Main navigation between settings sections
- **ScrollViewer** - Each tab scrollable for small screens
- **GroupBox** - Logical grouping of related settings
- **Min/Max Window Size** - 700x500 minimum, 900x700 default

### Color Coding
- **Green** - Connected/Success
- **Orange** - Testing/Warning
- **Red** - Error/Not Running
- **Gray** - Unknown/Waiting

## New Functionality

### Save Volume Settings
```csharp
// Validates and saves volume step and cache time
// Shows success message with note about restart for cache changes
```

### Open Config Folder
```csharp
// Opens Windows Explorer to the config folder
// Uses System.Diagnostics.Process.Start()
```

### Input Validation
- Volume Step: Must be 1-255
- Cache Time: Must be positive number
- Shows warning MessageBox for invalid input

## Settings Persistence

### On Change
Settings are saved immediately when:
- Serial number is detected/saved
- Volume settings are saved
- Other settings modified (future)

### On Exit
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
