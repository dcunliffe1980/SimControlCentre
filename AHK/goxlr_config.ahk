; GoXLR Volume Control - GUI Application
; Requires GoXLR Utility to be running

#Requires AutoHotkey v2.0
#SingleInstance Force

; ========================================
; Global Variables
; ========================================

global SerialNumber := "S220202153DI7"
global ApiUrl := "http://localhost:14564"
global VolumeStep := 10
global CacheRefreshTime := 5000

global EnabledChannels := Map()
global KeyBindings := Map()
global ProfileBindings := Map()
global ControllerMappings := Map()
global VolumeCache := Map()
global LastRefreshTime := 0
global AvailableProfiles := []
global CurrentProfile := ""

; Toggle button state tracking
global ToggleStates := Map()

; Window size settings
global WindowWidth := 620
global WindowHeight := 600

; Available channels
global AllChannels := ["Game", "Music", "Chat", "System", "Console", "Mic", "LineIn", "Sample"]

; GUI controls references
global GuiControls := Map()
global SettingsGui := ""
global ControllerTestGui := ""
global IsCapturingKey := false
global CaptureChannel := ""
global CaptureDirection := ""
global CaptureProfile := ""
global CaptureControllerMapping := ""

; System tray setup
TraySetIcon("shell32.dll", 168)  ; Speaker icon
A_IconTip := "GoXLR Volume Control"

; Create tray menu
A_TrayMenu.Delete()
A_TrayMenu.Add("Settings", (*) => ShowSettingsGui())
A_TrayMenu.Add("Controller Tester", (*) => ShowControllerTester())
A_TrayMenu.Add("Reload Profiles", (*) => LoadProfiles())
A_TrayMenu.Add()
A_TrayMenu.Add("Exit", (*) => ExitApp())
A_TrayMenu.Default := "Settings"

; ========================================
; Load Configuration
; ========================================

LoadConfig()
LoadProfiles()
InitializeHotkeys()
InitializeControllerMappings()
RefreshVolumes()

; Show settings on first run
if (EnabledChannels.Count == 0 && ProfileBindings.Count == 0) {
    ShowSettingsGui()
} else {
    TrayTip("GoXLR Volume Control", "Running in system tray. Right-click tray icon for settings.", 1)
}

; ========================================
; Controller Tester
; ========================================

ShowControllerTester() {
    global ControllerTestGui
    
    if (ControllerTestGui) {
        ControllerTestGui.Show()
        return
    }
    
    ControllerTestGui := Gui(, "Controller Button Tester")
    ControllerTestGui.SetFont("s10")
    
    ControllerTestGui.Add("Text", "w500", "Press any button on your controller/button box.")
    ControllerTestGui.Add("Text", "w500", "The button name will appear below:")
    
    statusText := ControllerTestGui.Add("Text", "w500 h400 +Border", "Waiting for button press...")
    
    ControllerTestGui.Add("Button", "w100", "Close").OnEvent("Click", (*) => CloseControllerTester())
    
    ControllerTestGui.OnEvent("Close", (*) => CloseControllerTester())
    
    ; Start monitoring
    SetTimer(() => MonitorControllers(statusText), 50)
    
    ControllerTestGui.Show()
}

MonitorControllers(statusText) {
    global ControllerTestGui
    
    if (!ControllerTestGui)
        return
    
    static lastButtons := Map()
    output := "Detected Controllers:`n`n"
    
    ; Check all possible controllers
    Loop 16 {
        controllerNum := A_Index
        
        ; Check if this controller exists by checking if any button responds
        controllerExists := false
        
        ; Check all buttons for this controller
        Loop 128 {
            buttonNum := A_Index
            buttonName := controllerNum . "Joy" . buttonNum
            
            if GetKeyState(buttonName) {
                controllerExists := true
                output .= "PRESSED: " . buttonName . "`n"
                
                ; Track this button
                if (!lastButtons.Has(buttonName)) {
                    lastButtons[buttonName] := true
                }
            } else {
                ; Check if button was just released
                if (lastButtons.Has(buttonName)) {
                    lastButtons.Delete(buttonName)
                }
            }
        }
        
        ; Check POV hat
        pov := GetKeyState(controllerNum . "JoyPOV")
        if (pov != -1) {
            controllerExists := true
            output .= "Controller " . controllerNum . " POV: " . pov . "°`n"
        }
        
        ; Check axes
        if (controllerExists) {
            x := GetKeyState(controllerNum . "JoyX")
            y := GetKeyState(controllerNum . "JoyY")
            z := GetKeyState(controllerNum . "JoyZ")
            r := GetKeyState(controllerNum . "JoyR")
            u := GetKeyState(controllerNum . "JoyU")
            v := GetKeyState(controllerNum . "JoyV")
            
            output .= "`nController " . controllerNum . " Info:`n"
            output .= "  Name: " . GetKeyState(controllerNum . "JoyName") . "`n"
            output .= "  Buttons: " . GetKeyState(controllerNum . "JoyButtons") . "`n"
            output .= "  Axes: X=" . Round(x) . " Y=" . Round(y) . " Z=" . Round(z) . "`n"
            output .= "        R=" . Round(r) . " U=" . Round(u) . " V=" . Round(v) . "`n`n"
        }
    }
    
    if (output == "Detected Controllers:`n`n") {
        output .= "No controllers detected.`n`n"
        output .= "If your button box works in games, try:`n"
        output .= "1. Unplugging and replugging the device`n"
        output .= "2. Checking Windows Game Controllers (joy.cpl)`n"
        output .= "3. Testing with another program first"
    }
    
    statusText.Value := output
}

CloseControllerTester() {
    global ControllerTestGui
    
    if (ControllerTestGui) {
        ControllerTestGui.Destroy()
        ControllerTestGui := ""
    }
}

; ========================================
; Configuration Functions
; ========================================

LoadConfig() {
    global EnabledChannels, KeyBindings, ProfileBindings, ControllerMappings, VolumeStep, CacheRefreshTime, SerialNumber
    global WindowWidth, WindowHeight
    
    configFile := A_ScriptDir . "\goxlr_config.ini"
    
    if !FileExist(configFile) {
        ; Set defaults
        EnabledChannels := Map("Game", true, "Chat", true)
        KeyBindings := Map(
            "Game", Map("Up", "^+>", "Down", "^+<"),
            "Chat", Map("Up", "^+]", "Down", "^+[")
        )
        ; Default profiles
        ProfileBindings := Map(
            "Speakers - Personal", "",
            "Headphones - Personal (Online)", "",
            "Headphones - Work", "",
            "iRacing", ""
        )
        ControllerMappings := Map()
        SaveConfig()
        return
    }
    
    ; Load settings
    SerialNumber := IniRead(configFile, "Settings", "SerialNumber", "S220202153DI7")
    VolumeStep := Integer(IniRead(configFile, "Settings", "VolumeStep", "10"))
    CacheRefreshTime := Integer(IniRead(configFile, "Settings", "CacheRefreshTime", "5000"))
    WindowWidth := Integer(IniRead(configFile, "Settings", "WindowWidth", "620"))
    WindowHeight := Integer(IniRead(configFile, "Settings", "WindowHeight", "600"))
    
    ; Load enabled channels
    EnabledChannels := Map()
    channelList := IniRead(configFile, "Channels", "ChannelList", "")
    if (channelList != "") {
        for channel in StrSplit(channelList, ",") {
            EnabledChannels[channel] := true
        }
    }
    
    ; Load key bindings
    KeyBindings := Map()
    for channel in AllChannels {
        upKey := IniRead(configFile, "KeyBindings", channel . "_Up", "")
        downKey := IniRead(configFile, "KeyBindings", channel . "_Down", "")
        if (upKey != "" && downKey != "")
            KeyBindings[channel] := Map("Up", upKey, "Down", downKey)
    }
    
    ; Load profile bindings
    ProfileBindings := Map()
    Loop {
        profileName := IniRead(configFile, "ProfileBindings", "Profile" . A_Index . "_Name", "")
        if (profileName == "")
            break
        profileKey := IniRead(configFile, "ProfileBindings", "Profile" . A_Index . "_Key", "")
        ProfileBindings[profileName] := profileKey
    }
    
    ; Load controller mappings
    ControllerMappings := Map()
    Loop {
        controllerButton := IniRead(configFile, "ControllerMappings", "Mapping" . A_Index . "_Controller", "")
        if (controllerButton == "")
            break
        keyboardKey := IniRead(configFile, "ControllerMappings", "Mapping" . A_Index . "_Keyboard", "")
        isToggle := IniRead(configFile, "ControllerMappings", "Mapping" . A_Index . "_Toggle", "0")
        ControllerMappings[controllerButton] := Map("Key", keyboardKey, "Toggle", isToggle = "1")
    }
}

SaveConfig() {
    global EnabledChannels, KeyBindings, ProfileBindings, ControllerMappings, VolumeStep, CacheRefreshTime, SerialNumber
    global WindowWidth, WindowHeight
    
    configFile := A_ScriptDir . "\goxlr_config.ini"
    
    ; Delete old sections
    IniDelete(configFile, "ProfileBindings")
    IniDelete(configFile, "Channels")
    IniDelete(configFile, "ControllerMappings")
    
    ; Save settings
    IniWrite(SerialNumber, configFile, "Settings", "SerialNumber")
    IniWrite(VolumeStep, configFile, "Settings", "VolumeStep")
    IniWrite(CacheRefreshTime, configFile, "Settings", "CacheRefreshTime")
    IniWrite(WindowWidth, configFile, "Settings", "WindowWidth")
    IniWrite(WindowHeight, configFile, "Settings", "WindowHeight")
    
    ; Save enabled channels as comma-separated list
    channelList := ""
    for channel in EnabledChannels {
        channelList .= (channelList != "" ? "," : "") . channel
    }
    IniWrite(channelList, configFile, "Channels", "ChannelList")
    
    ; Save key bindings
    for channel in AllChannels {
        if KeyBindings.Has(channel) {
            IniWrite(KeyBindings[channel]["Up"], configFile, "KeyBindings", channel . "_Up")
            IniWrite(KeyBindings[channel]["Down"], configFile, "KeyBindings", channel . "_Down")
        }
    }
    
    ; Save profile bindings
    index := 1
    for profileName, profileKey in ProfileBindings {
        IniWrite(profileName, configFile, "ProfileBindings", "Profile" . index . "_Name")
        IniWrite(profileKey, configFile, "ProfileBindings", "Profile" . index . "_Key")
        index++
    }
    
    ; Save controller mappings
    index := 1
    for controllerButton, mapping in ControllerMappings {
        IniWrite(controllerButton, configFile, "ControllerMappings", "Mapping" . index . "_Controller")
        IniWrite(mapping["Key"], configFile, "ControllerMappings", "Mapping" . index . "_Keyboard")
        IniWrite(mapping["Toggle"] ? "1" : "0", configFile, "ControllerMappings", "Mapping" . index . "_Toggle")
        index++
    }
}

LoadProfiles() {
    global AvailableProfiles, CurrentProfile, ApiUrl
    
    try {
        whr := ComObject("WinHttp.WinHttpRequest.5.1")
        whr.Open("GET", ApiUrl . "/api/get-devices", false)
        whr.SetTimeouts(1000, 1000, 1000, 1000)
        whr.Send()
        
        if (whr.Status == 200) {
            response := whr.ResponseText
            
            ; Extract current profile
            if RegExMatch(response, '"profile_name":"([^"]+)"', &match)
                CurrentProfile := match[1]
            
            ; Extract available profiles
            if RegExMatch(response, '"profiles":\s*\[([^\]]+)\]', &match) {
                profilesStr := match[1]
                AvailableProfiles := []
                
                ; Parse profile names
                pos := 1
                while (pos := RegExMatch(profilesStr, '"([^"]+)"', &pMatch, pos)) {
                    AvailableProfiles.Push(pMatch[1])
                    pos += StrLen(pMatch[0])
                }
            }
        }
    } catch {
        AvailableProfiles := []
        CurrentProfile := ""
    }
}

; ========================================
; Settings GUI
; ========================================

ShowSettingsGui() {
    global SettingsGui, GuiControls, EnabledChannels, KeyBindings, ProfileBindings, ControllerMappings
    global VolumeStep, CacheRefreshTime, AllChannels, AvailableProfiles, CurrentProfile, WindowWidth, WindowHeight
    
    if (SettingsGui) {
        SettingsGui.Show()
        return
    }
    
    ; Create scrollable GUI
    SettingsGui := Gui("+Resize", "GoXLR Volume Control - Settings")
    SettingsGui.SetFont("s10")
    
    ; Settings section
    SettingsGui.Add("GroupBox", "w600 h120", "General Settings")
    SettingsGui.Add("Text", "xp+15 yp+25", "Serial Number:")
    GuiControls["SerialNumber"] := SettingsGui.Add("Edit", "x+10 w200", SerialNumber)
    
    SettingsGui.Add("Text", "xm+15 y+10", "Volume Step:")
    GuiControls["VolumeStep"] := SettingsGui.Add("Edit", "x+10 w80", VolumeStep)
    SettingsGui.Add("Text", "x+5", "(1-50)")
    
    SettingsGui.Add("Text", "xm+15 y+10", "Cache Time (ms):")
    GuiControls["CacheRefreshTime"] := SettingsGui.Add("Edit", "x+10 w80", CacheRefreshTime)
    SettingsGui.Add("Text", "x+5", "(1000-30000)")
    
    ; Current profile display
    SettingsGui.Add("GroupBox", "xm w600 h50", "Current Profile")
    SettingsGui.Add("Text", "xp+15 yp+25", "Active Profile:")
    GuiControls["CurrentProfile"] := SettingsGui.Add("Text", "x+10 w400", CurrentProfile)
    
    ; Profile bindings section
    SettingsGui.Add("GroupBox", "xm w600 h40", "Profile Hotkeys")
    
    ; Add profile dropdown and button
    yPos := SettingsGui.MarginY + 220
    SettingsGui.Add("Text", "xm+15 y" . yPos, "Add Profile:")
    
    ; Filter out profiles that are already added
    availableToAdd := []
    for profile in AvailableProfiles {
        if !ProfileBindings.Has(profile)
            availableToAdd.Push(profile)
    }
    
    GuiControls["AddProfileDropdown"] := SettingsGui.Add("DropDownList", "x+10 w350", availableToAdd)
    SettingsGui.Add("Button", "x+10 w100", "Add Profile").OnEvent("Click", (*) => AddProfile())
    
    ; Display existing profile bindings
    yPos += 40
    GuiControls["ProfileControls"] := []
    for profileName in ProfileBindings {
        CreateProfileControls(profileName, yPos)
        yPos += 35
    }
    
    ; Channels section
    yPos += 20
    SettingsGui.Add("GroupBox", "xm w600 h40 y" . yPos, "Channel Volume Controls")
    
    ; Add channel dropdown and button
    yPos += 35
    SettingsGui.Add("Text", "xm+15 y" . yPos, "Add Channel:")
    
    ; Filter out channels that are already added
    availableChannels := []
    for channel in AllChannels {
        if !EnabledChannels.Has(channel)
            availableChannels.Push(channel)
    }
    
    GuiControls["AddChannelDropdown"] := SettingsGui.Add("DropDownList", "x+10 w350", availableChannels)
    SettingsGui.Add("Button", "x+10 w100", "Add Channel").OnEvent("Click", (*) => AddChannel())
    
    ; Display existing channel controls
    yPos += 40
    GuiControls["ChannelControls"] := []
    for channel in EnabledChannels {
        CreateChannelControls(channel, yPos)
        yPos += 35
    }
    
    ; Controller Mappings section
    yPos += 20
    SettingsGui.Add("GroupBox", "xm w600 h100 y" . yPos, "Controller to Keyboard Mappings")
    SettingsGui.Add("Text", "xm+15 yp+25", "Map controller buttons to keyboard keys")
    SettingsGui.Add("Text", "xm+15 y+5", "(Enable 'Toggle' for latch switches)")
    SettingsGui.Add("Text", "xm+15 y+5", "Tip: Use the Controller Tester to find button names!")
    
    yPos += 80
    SettingsGui.Add("Button", "xm+15 y" . yPos . " w150", "Add New Mapping").OnEvent("Click", (*) => AddControllerMapping())
    SettingsGui.Add("Button", "x+10 w150", "Test Controller").OnEvent("Click", (*) => ShowControllerTester())
    
    ; Display existing controller mappings
    yPos += 40
    GuiControls["ControllerMappingControls"] := []
    for controllerButton in ControllerMappings {
        CreateControllerMappingControls(controllerButton, yPos)
        yPos += 35
    }
    
    ; Bottom buttons
    yPos += 20
    SettingsGui.Add("Button", "xm y" . yPos . " w180", "Save && Apply").OnEvent("Click", (*) => SaveAndApply())
    SettingsGui.Add("Button", "x+10 w180", "Cancel").OnEvent("Click", (*) => CloseSettings())
    SettingsGui.Add("Button", "x+10 w180", "Refresh Profiles").OnEvent("Click", (*) => RefreshAndReopenSettings())
    
    SettingsGui.OnEvent("Close", (*) => CloseSettings())
    SettingsGui.OnEvent("Size", GuiSize)
    
    ; Show with saved size
    SettingsGui.Show("w" . WindowWidth . " h" . WindowHeight)
}

GuiSize(GuiObj, MinMax, Width, Height) {
    global WindowWidth, WindowHeight
    
    ; Save the new window size
    if (MinMax != -1) {  ; Not minimized
        WindowWidth := Width
        WindowHeight := Height
        SaveConfig()
    }
}

AddProfile() {
    global GuiControls, ProfileBindings
    
    dropdown := GuiControls["AddProfileDropdown"]
    profileName := dropdown.Text
    
    if (profileName == "") {
        MsgBox("Please select a profile to add.")
        return
    }
    
    ; Add the profile with no key binding
    ProfileBindings[profileName] := ""
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

AddChannel() {
    global GuiControls, EnabledChannels
    
    dropdown := GuiControls["AddChannelDropdown"]
    channelName := dropdown.Text
    
    if (channelName == "") {
        MsgBox("Please select a channel to add.")
        return
    }
    
    ; Add the channel
    EnabledChannels[channelName] := true
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

AddControllerMapping() {
    global ControllerMappings
    
    ; Add a placeholder mapping
    ControllerMappings["NewMapping" . A_TickCount] := Map("Key", "", "Toggle", false)
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

CreateProfileControls(profile, yPos) {
    global SettingsGui, GuiControls, ProfileBindings
    
    ; Profile name
    SettingsGui.Add("Text", "xm+15 y" . yPos . " w200", profile)
    
    ; Key binding
    keyText := ProfileBindings[profile]
    editCtrl := SettingsGui.Add("Edit", "x+10 w250 ReadOnly", keyText)
    GuiControls["Profile_" . profile] := editCtrl
    
    SettingsGui.Add("Button", "x+5 w80", "Capture").OnEvent("Click", (*) => CaptureProfileKey(profile))
    
    if (keyText != "")
        SettingsGui.Add("Button", "x+5 w60", "Clear").OnEvent("Click", (*) => ClearProfileKey(profile))
    else
        SettingsGui.Add("Text", "x+5 w60", "")  ; Spacer
    
    SettingsGui.Add("Button", "x+5 w70", "Remove").OnEvent("Click", (*) => RemoveProfile(profile))
}

CreateChannelControls(channel, yPos) {
    global SettingsGui, GuiControls, KeyBindings
    
    ; Channel name
    SettingsGui.Add("Text", "xm+15 y" . yPos . " w80", channel)
    
    ; Up key binding
    upKeyText := KeyBindings.Has(channel) ? KeyBindings[channel]["Up"] : ""
    GuiControls["Up_" . channel] := SettingsGui.Add("Edit", "x+10 w110 ReadOnly", upKeyText)
    SettingsGui.Add("Button", "x+5 w60", "Up").OnEvent("Click", (*) => CaptureKey(channel, "Up"))
    
    if (upKeyText != "")
        SettingsGui.Add("Button", "x+5 w50", "Clear").OnEvent("Click", (*) => ClearChannelKey(channel, "Up"))
    else
        SettingsGui.Add("Text", "x+5 w50", "")  ; Spacer
    
    ; Down key binding
    downKeyText := KeyBindings.Has(channel) ? KeyBindings[channel]["Down"] : ""
    GuiControls["Down_" . channel] := SettingsGui.Add("Edit", "x+5 w110 ReadOnly", downKeyText)
    SettingsGui.Add("Button", "x+5 w60", "Down").OnEvent("Click", (*) => CaptureKey(channel, "Down"))
    
    if (downKeyText != "")
        SettingsGui.Add("Button", "x+5 w50", "Clear").OnEvent("Click", (*) => ClearChannelKey(channel, "Down"))
    else
        SettingsGui.Add("Text", "x+5 w50", "")  ; Spacer
    
    SettingsGui.Add("Button", "x+5 w70", "Remove").OnEvent("Click", (*) => RemoveChannel(channel))
}

CreateControllerMappingControls(controllerButton, yPos) {
    global SettingsGui, GuiControls, ControllerMappings
    
    mapping := ControllerMappings[controllerButton]
    
    ; Controller button
    GuiControls["Controller_" . controllerButton] := SettingsGui.Add("Edit", "xm+15 y" . yPos . " w120 ReadOnly", controllerButton)
    SettingsGui.Add("Button", "x+5 w60", "Button").OnEvent("Click", (*) => CaptureControllerButton(controllerButton))
    
    ; Arrow
    SettingsGui.Add("Text", "x+10 w20", "→")
    
    ; Keyboard key
    GuiControls["Keyboard_" . controllerButton] := SettingsGui.Add("Edit", "x+10 w120 ReadOnly", mapping["Key"])
    SettingsGui.Add("Button", "x+5 w60", "Key").OnEvent("Click", (*) => CaptureKeyboardKey(controllerButton))
    
    ; Toggle checkbox
    GuiControls["Toggle_" . controllerButton] := SettingsGui.Add("Checkbox", "x+10 w70", "Toggle")
    GuiControls["Toggle_" . controllerButton].Value := mapping["Toggle"]
    
    ; Remove button
    SettingsGui.Add("Button", "x+10 w70", "Remove").OnEvent("Click", (*) => RemoveControllerMapping(controllerButton))
}

RemoveProfile(profile) {
    global ProfileBindings
    
    if ProfileBindings.Has(profile)
        ProfileBindings.Delete(profile)
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

RemoveChannel(channel) {
    global EnabledChannels, KeyBindings
    
    if EnabledChannels.Has(channel)
        EnabledChannels.Delete(channel)
    
    if KeyBindings.Has(channel)
        KeyBindings.Delete(channel)
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

RemoveControllerMapping(controllerButton) {
    global ControllerMappings
    
    if ControllerMappings.Has(controllerButton)
        ControllerMappings.Delete(controllerButton)
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

CaptureKey(channel, direction) {
    global IsCapturingKey, CaptureChannel, CaptureDirection, CaptureProfile, CaptureControllerMapping, GuiControls
    
    if IsCapturingKey
        return
    
    IsCapturingKey := true
    CaptureChannel := channel
    CaptureDirection := direction
    CaptureProfile := ""
    CaptureControllerMapping := ""
    
    controlName := direction . "_" . channel
    GuiControls[controlName].Value := "Press key combo (ESC to cancel)..."
    
    ; Start monitoring with timer-based approach
    SetTimer(CheckForKeyPress, 50)
}

CaptureProfileKey(profile) {
    global IsCapturingKey, CaptureChannel, CaptureDirection, CaptureProfile, CaptureControllerMapping, GuiControls
    
    if IsCapturingKey
        return
    
    IsCapturingKey := true
    CaptureProfile := profile
    CaptureChannel := ""
    CaptureDirection := ""
    CaptureControllerMapping := ""
    
    controlName := "Profile_" . profile
    GuiControls[controlName].Value := "Press key combo (ESC to cancel)..."
    
    ; Start monitoring with timer-based approach
    SetTimer(CheckForKeyPress, 50)
}

CaptureControllerButton(oldButton) {
    global IsCapturingKey, CaptureControllerMapping, GuiControls
    
    if IsCapturingKey
        return
    
    IsCapturingKey := true
    CaptureControllerMapping := "Controller_" . oldButton
    
    GuiControls["Controller_" . oldButton].Value := "Press controller button (ESC to cancel)..."
    
    ; Start monitoring with timer-based approach
    SetTimer(CheckForControllerButton, 50)
}

CaptureKeyboardKey(controllerButton) {
    global IsCapturingKey, CaptureControllerMapping, GuiControls
    
    if IsCapturingKey
        return
    
    IsCapturingKey := true
    CaptureControllerMapping := "Keyboard_" . controllerButton
    
    GuiControls["Keyboard_" . controllerButton].Value := "Press keyboard key (ESC to cancel)..."
    
    ; Start monitoring with timer-based approach
    SetTimer(CheckForKeyPress, 50)
}

CheckForControllerButton() {
    global IsCapturingKey, CaptureControllerMapping, GuiControls, ControllerMappings
    
    if !IsCapturingKey
        return
    
    ; Check for ESC to cancel
    if GetKeyState("Escape", "P") {
        ; Extract the old button name
        oldButton := StrReplace(CaptureControllerMapping, "Controller_", "")
        
        ; Restore original value
        if ControllerMappings.Has(oldButton)
            GuiControls["Controller_" . oldButton].Value := oldButton
        else
            GuiControls["Controller_" . oldButton].Value := ""
        
        IsCapturingKey := false
        SetTimer(CheckForControllerButton, 0)
        
        ; Wait for ESC to be released
        KeyWait("Escape")
        return
    }
    
    ; Check for joystick buttons - expanded range
    Loop 16 {
        controllerNum := A_Index
        Loop 128 {  ; Increased from 32 to 128
            buttonNum := A_Index
            buttonName := controllerNum . "Joy" . buttonNum
            
            if GetKeyState(buttonName) {
                ; Extract the old button name
                oldButton := StrReplace(CaptureControllerMapping, "Controller_", "")
                
                ; Update the mapping key
                if ControllerMappings.Has(oldButton) {
                    ; Store old mapping data
                    oldMapping := ControllerMappings[oldButton]
                    
                    ; Delete old entry
                    ControllerMappings.Delete(oldButton)
                    
                    ; Add new entry with new button name
                    ControllerMappings[buttonName] := oldMapping
                    
                    ; Update the GUI control
                    GuiControls["Controller_" . oldButton].Value := buttonName
                }
                
                IsCapturingKey := false
                SetTimer(CheckForControllerButton, 0)
                
                ; Refresh GUI to update control names
                SetTimer(() => RefreshGUI(), -500)
                return
            }
        }
    }
}

RefreshGUI() {
    CloseSettings()
    ShowSettingsGui()
}

CheckForKeyPress() {
    global IsCapturingKey, CaptureChannel, CaptureDirection, CaptureProfile, CaptureControllerMapping
    global GuiControls, KeyBindings, ProfileBindings, ControllerMappings
    
    static lastPressedKey := ""
    
    if !IsCapturingKey
        return
    
    ; Check for ESC to cancel
    if GetKeyState("Escape", "P") {
        ; Restore original value
        if (CaptureProfile != "") {
            GuiControls["Profile_" . CaptureProfile].Value := ProfileBindings[CaptureProfile]
        } else if (CaptureControllerMapping != "") {
            ; Restore controller mapping keyboard value
            controllerButton := StrReplace(CaptureControllerMapping, "Keyboard_", "")
            if ControllerMappings.Has(controllerButton)
                GuiControls["Keyboard_" . controllerButton].Value := ControllerMappings[controllerButton]["Key"]
            else
                GuiControls["Keyboard_" . controllerButton].Value := ""
        } else {
            controlName := CaptureDirection . "_" . CaptureChannel
            originalValue := KeyBindings.Has(CaptureChannel) ? KeyBindings[CaptureChannel][CaptureDirection] : ""
            GuiControls[controlName].Value := originalValue
        }
        
        IsCapturingKey := false
        SetTimer(CheckForKeyPress, 0)
        lastPressedKey := ""
        
        ; Wait for ESC to be released
        KeyWait("Escape")
        return
    }
    
    ; Check for regular keys (excluding modifiers, escape, and mouse buttons)
    Loop 256 {
        vk := A_Index - 1
        key := GetKeyName(Format("vk{:X}", vk))
        
        ; Skip if it's a modifier key, escape, or mouse button
        if (key ~= "i)^(Control|Alt|Shift|LWin|RWin|Escape|LControl|RControl|LAlt|RAlt|LShift|RShift|LButton|RButton|MButton|XButton1|XButton2)$")
            continue
        
        ; Skip if key name is empty
        if (key == "")
            continue
        
        ; Check if this key is pressed
        if GetKeyState(key, "P") {
            ; Avoid duplicate captures
            if (lastPressedKey == key)
                continue
            
            lastPressedKey := key
            
            ; Build modifier string - check physical state
            modifiers := ""
            if GetKeyState("Ctrl", "P")
                modifiers .= "^"
            if GetKeyState("Alt", "P")
                modifiers .= "!"
            if GetKeyState("Shift", "P")
                modifiers .= "+"
            if GetKeyState("LWin", "P") || GetKeyState("RWin", "P")
                modifiers .= "#"
            
            ; Build the hotkey string
            hotkeyString := modifiers . key
            
            ; Store the binding
            if (CaptureProfile != "") {
                ProfileBindings[CaptureProfile] := hotkeyString
                GuiControls["Profile_" . CaptureProfile].Value := hotkeyString
            } else if (CaptureControllerMapping != "") {
                ; Store keyboard key for controller mapping
                controllerButton := StrReplace(CaptureControllerMapping, "Keyboard_", "")
                if ControllerMappings.Has(controllerButton) {
                    ControllerMappings[controllerButton]["Key"] := hotkeyString
                    GuiControls["Keyboard_" . controllerButton].Value := hotkeyString
                }
            } else {
                if !KeyBindings.Has(CaptureChannel)
                    KeyBindings[CaptureChannel] := Map()
                
                KeyBindings[CaptureChannel][CaptureDirection] := hotkeyString
                controlName := CaptureDirection . "_" . CaptureChannel
                GuiControls[controlName].Value := hotkeyString
            }
            
            ; Stop capturing
            IsCapturingKey := false
            SetTimer(CheckForKeyPress, 0)
            lastPressedKey := ""
            
            ; Wait for key to be released
            KeyWait(key)
            return
        }
    }
    
    ; Reset lastPressedKey if no keys are pressed
    anyKeyPressed := false
    Loop 256 {
        vk := A_Index - 1
        key := GetKeyName(Format("vk{:X}", vk))
        if (key != "" && GetKeyState(key, "P")) {
            anyKeyPressed := true
            break
        }
    }
    if (!anyKeyPressed)
        lastPressedKey := ""
    
    ; Check for joystick buttons (only if not capturing controller mapping)
    if (CaptureControllerMapping == "") {
        Loop 16 {
            controllerNum := A_Index
            Loop 128 {  ; Increased from 32 to 128
                buttonNum := A_Index
                buttonName := controllerNum . "Joy" . buttonNum
                
                if GetKeyState(buttonName) {
                    ; Build modifier string
                    modifiers := ""
                    if GetKeyState("Ctrl", "P")
                        modifiers .= "^"
                    if GetKeyState("Alt", "P")
                        modifiers .= "!"
                    if GetKeyState("Shift", "P")
                        modifiers .= "+"
                    if GetKeyState("LWin", "P") || GetKeyState("RWin", "P")
                        modifiers .= "#"
                    
                    hotkeyString := modifiers . buttonName
                    
                    ; Store the binding
                    if (CaptureProfile != "") {
                        ProfileBindings[CaptureProfile] := hotkeyString
                        GuiControls["Profile_" . CaptureProfile].Value := hotkeyString
                    } else {
                        if !KeyBindings.Has(CaptureChannel)
                            KeyBindings[CaptureChannel] := Map()
                        
                        KeyBindings[CaptureChannel][CaptureDirection] := hotkeyString
                        controlName := CaptureDirection . "_" . CaptureChannel
                        GuiControls[controlName].Value := hotkeyString
                    }
                    
                    IsCapturingKey := false
                    SetTimer(CheckForKeyPress, 0)
                    lastPressedKey := ""
                    return
                }
            }
        }
    }
}

ClearProfileKey(profile) {
    global ProfileBindings
    
    if ProfileBindings.Has(profile)
        ProfileBindings[profile] := ""
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

ClearChannelKey(channel, direction) {
    global KeyBindings
    
    if KeyBindings.Has(channel) {
        KeyBindings[channel][direction] := ""
        
        ; If both are empty, remove the channel from KeyBindings
        if (KeyBindings[channel]["Up"] == "" && KeyBindings[channel]["Down"] == "")
            KeyBindings.Delete(channel)
    }
    
    ; Refresh the GUI
    CloseSettings()
    ShowSettingsGui()
}

RefreshAndReopenSettings() {
    global SettingsGui
    
    LoadProfiles()
    
    if SettingsGui {
        CloseSettings()
        ShowSettingsGui()
    }
}

SaveAndApply() {
    global SettingsGui, GuiControls
    global VolumeStep, CacheRefreshTime, SerialNumber, ControllerMappings
    
    ; Get values from GUI
    SerialNumber := GuiControls["SerialNumber"].Value
    VolumeStep := Integer(GuiControls["VolumeStep"].Value)
    CacheRefreshTime := Integer(GuiControls["CacheRefreshTime"].Value)
    
    ; Update toggle states from checkboxes
    for controllerButton in ControllerMappings {
        if GuiControls.Has("Toggle_" . controllerButton)
            ControllerMappings[controllerButton]["Toggle"] := GuiControls["Toggle_" . controllerButton].Value
    }
    
    ; Validate
    if (VolumeStep < 1 || VolumeStep > 50) {
        MsgBox("Volume Step must be between 1 and 50")
        return
    }
    
    if (CacheRefreshTime < 1000 || CacheRefreshTime > 30000) {
        MsgBox("Cache Time must be between 1000 and 30000")
        return
    }
    
    ; Save and reload
    SaveConfig()
    InitializeHotkeys()
    InitializeControllerMappings()
    RefreshVolumes()
    
    TrayTip("Settings Saved", "Configuration applied successfully.", 1)
    CloseSettings()
}

CloseSettings() {
    global SettingsGui, GuiControls
    
    if SettingsGui {
        SettingsGui.Destroy()
        SettingsGui := ""
        GuiControls := Map()
    }
}

; ========================================
; Volume Control Functions
; ========================================

InitializeHotkeys() {
    global EnabledChannels, KeyBindings, ProfileBindings, VolumeCache
    
    ; Disable all existing hotkeys
    try Hotkey("*", "Off")
    
    ; Clear cache
    VolumeCache := Map()
    
    ; Set up hotkeys for enabled channels
    for channel, enabled in EnabledChannels {
        if (enabled && KeyBindings.Has(channel)) {
            VolumeCache[channel] := -1
            
            upKey := KeyBindings[channel]["Up"]
            downKey := KeyBindings[channel]["Down"]
            
            if (upKey != "" && downKey != "") {
                ; Create hotkeys with proper closure
                CreateVolumeHotkeys(channel, upKey, downKey)
            }
        }
    }
    
    ; Set up hotkeys for profiles
    for profileName, profileKey in ProfileBindings {
        if (profileKey != "") {
            CreateProfileHotkey(profileName, profileKey)
        }
    }
}

InitializeControllerMappings() {
    global ControllerMappings, ToggleStates
    
    ; Clear existing toggle states
    ToggleStates := Map()
    
    ; Set up controller button monitoring
    for controllerButton, mapping in ControllerMappings {
        if (mapping["Key"] != "") {
            if (mapping["Toggle"]) {
                ; Initialize toggle state
                ToggleStates[controllerButton] := false
                ; Create toggle hotkey
                CreateToggleHotkey(controllerButton, mapping["Key"])
            } else {
                ; Create regular passthrough hotkey
                CreatePassthroughHotkey(controllerButton, mapping["Key"])
            }
        }
    }
}

CreateVolumeHotkeys(ch, upKey, downKey) {
    global VolumeStep
    try {
        Hotkey(upKey, (*) => AdjustVolume(ch, VolumeStep))
        Hotkey(downKey, (*) => AdjustVolume(ch, -VolumeStep))
    }
}

CreateProfileHotkey(profileName, profileKey) {
    try {
        Hotkey(profileKey, (*) => LoadProfile(profileName))
    }
}

CreatePassthroughHotkey(controllerBtn, keyboardKey) {
    try {
        ; On press - send key down
        Hotkey(controllerBtn, (*) => Send("{" . keyboardKey . " down}"))
        ; On release - send key up
        Hotkey(controllerBtn . " up", (*) => Send("{" . keyboardKey . " up}"))
    }
}

CreateToggleHotkey(controllerBtn, keyboardKey) {
    global ToggleStates
    
    try {
        ; Only trigger on button press (down)
        Hotkey(controllerBtn, (*) => HandleToggle(controllerBtn, keyboardKey))
    }
}

HandleToggle(controllerBtn, keyboardKey) {
    global ToggleStates
    
    ; Toggle the state
    ToggleStates[controllerBtn] := !ToggleStates[controllerBtn]
    
    ; Send single keypress (down then up)
    Send("{" . keyboardKey . "}")
    
    ; Optional: Show feedback
    state := ToggleStates[controllerBtn] ? "ON" : "OFF"
    ToolTip(controllerBtn . " → " . keyboardKey . " (" . state . ")")
    SetTimer(() => ToolTip(), -1000)
}

AdjustVolume(channel, adjustment) {
    global SerialNumber, ApiUrl, VolumeCache, LastRefreshTime, VolumeStep, CacheRefreshTime
    
    try {
        ; Refresh cache if needed
        currentTime := A_TickCount
        if (VolumeCache[channel] == -1 || currentTime - LastRefreshTime > CacheRefreshTime) {
            RefreshVolumes()
        }
        
        ; Get current volume from cache
        currentVolume := VolumeCache[channel]
        
        if (currentVolume == -1)
            currentVolume := 128
        
        ; Calculate new volume
        newVolume := Max(0, Min(255, currentVolume + adjustment))
        
        ; Update cache
        VolumeCache[channel] := newVolume
        
        ; Set volume
        SetVolumeAsync(channel, newVolume)
        
        ; Show feedback
        percentage := Round((newVolume / 255) * 100)
        ToolTip(channel . " Volume: " . percentage . "%")
        SetTimer(() => ToolTip(), -800)
        
    } catch as err {
        ToolTip("Error: " . err.Message)
        SetTimer(() => ToolTip(), -2000)
    }
}

LoadProfile(profileName) {
    global SerialNumber, ApiUrl, CurrentProfile
    
    try {
        whr := ComObject("WinHttp.WinHttpRequest.5.1")
        whr.Open("POST", ApiUrl . "/api/command", false)
        whr.SetRequestHeader("Content-Type", "application/json")
        whr.SetTimeouts(1000, 1000, 1000, 1000)
        
        command := '{"Command":["' . SerialNumber . '",{"LoadProfile":["' . profileName . '",false]}]}'
        whr.Send(command)
        
        if (whr.Status == 200) {
            CurrentProfile := profileName
            ToolTip("Profile: " . profileName)
            SetTimer(() => ToolTip(), -1500)
        }
    } catch as err {
        ToolTip("Error loading profile: " . err.Message)
        SetTimer(() => ToolTip(), -2000)
    }
}

RefreshVolumes() {
    global ApiUrl, VolumeCache, LastRefreshTime, EnabledChannels, CurrentProfile
    
    try {
        whr := ComObject("WinHttp.WinHttpRequest.5.1")
        whr.Open("GET", ApiUrl . "/api/get-devices", false)
        whr.SetTimeouts(500, 500, 500, 500)
        whr.Send()
        
        if (whr.Status == 200) {
            response := whr.ResponseText
            
            ; Update current profile
            if RegExMatch(response, '"profile_name":"([^"]+)"', &match)
                CurrentProfile := match[1]
            
            ; Update volumes for all enabled channels
            for channel, enabled in EnabledChannels {
                if (enabled) {
                    if RegExMatch(response, '"' . channel . '":\s*(\d+)', &match)
                        VolumeCache[channel] := Integer(match[1])
                }
            }
            
            LastRefreshTime := A_TickCount
        }
    } catch {
        ; Keep using cached values
    }
}

SetVolumeAsync(channel, volume) {
    global SerialNumber, ApiUrl
    
    try {
        whr := ComObject("WinHttp.WinHttpRequest.5.1")
        whr.Open("POST", ApiUrl . "/api/command", false)
        whr.SetRequestHeader("Content-Type", "application/json")
        whr.SetTimeouts(500, 500, 500, 500)
        
        command := '{"Command":["' . SerialNumber . '",{"SetVolume":["' . channel . '",' . volume . ']}]}'
        whr.Send(command)
    } catch {
        ; Fail silently
    }
}