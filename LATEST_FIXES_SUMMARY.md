# Latest Fixes Summary - GoXLR Lighting & Telemetry Display

## ? 1. GoXLR Lighting Simplified

### What Changed:
**Hidden Button Selection UI** - You don't need to pick individual buttons anymore!

**Default: Global LED** - All lighting now defaults to the "Global" button (the simplest option)

**Code Preserved** - The button selection code is still there (just hidden), in case you change your mind

### UI Before/After:

**Before:**
```
[?] Global  [?] Accent  [ ] FaderA  [?] FaderB  ...
Select which buttons to use for lighting
```

**After:**
```
(Button selection hidden - defaults to Global)
```

---

## ? 2. Flag None Behavior - During vs After Session

### During Session (Flag = None)
**New Behavior:** LEDs turn **OFF** (not restore profile)

**Why:** Between flags during a race, you want LEDs off, not showing your normal profile colors

**Example:**
```
Green flag ? Racing ? Yellow flag ? Caution ends ? Back to None
                                                        ?
                                                   LEDs OFF
```

### After Session Ends
**Behavior:** Profile colors **RESTORED**

**Why:** When telemetry disconnects (race over), you want your normal lighting back

**Example:**
```
Checkered flag ? Finish race ? iRacing closes ? Telemetry disconnects
                                                        ?
                                            Profile colors restored!
```

### Technical Implementation:

**During Session:**
```csharp
// Flag changes to None (debounced 500ms)
await ClearAllDevicesAsync();  // Turns off Global LED
```

**After Session:**
```csharp
// Telemetry disconnects
await RestoreProfileLightingAsync();  // Reloads GoXLR profile
```

---

## ? 3. Lap Display Fixed

### Issue Found:
```
Current Lap: 2 / 32767  ? WRONG!
```

**32767** is the default value for unlimited/timed races

### Root Cause:
iRacing uses `SessionLapsTotal = 32767` for timed races (no lap limit)

### Fix Applied:
**Smart Detection:**
- If `TotalLaps > 999` ? Timed race
- If `TotalLaps = 0` ? Unknown
- Otherwise ? Lap-based race

### Display Logic:

**Timed Race:**
```
Current Lap: 5
```

**Lap-Based Race:**
```
Current Lap: 5 / 20
```

**No Data:**
```
Current Lap: -
```

---

## ? 4. Driver Count & Position Fixed

### Issues Found:
```
Total Drivers: 1        ? WRONG! (Should be 24)
Position: 17 / 1        ? WRONG! (Shows 17th of 1 driver?!)
```

### Root Cause:
YAML parsing wasn't finding driver count correctly

### Fix Applied:

**Two Methods:**

1. **Primary:** Read `NumStarters` from YAML
```yaml
WeekendOptions:
 NumStarters: 24
```

2. **Fallback:** Count `CarIdx` entries
```yaml
Drivers:
 - CarIdx: 0
   UserName: Player1
 - CarIdx: 1
   UserName: Player2
   ...
```

### Display Logic:

**Full Data:**
```
Position: P3 / 24
Total Drivers: 24
```

**Partial Data (no driver count):**
```
Position: P3
Total Drivers: -
```

**No Data:**
```
Position: -
Total Drivers: -
```

---

## ?? Testing Recommendations

### 1. Test GoXLR Lighting During Race

**Start iRacing:**
1. Watch for Green flag ? Global LED lights up
2. Flag clears ? Global LED turns **OFF** ?
3. Yellow flag ? Global LED lights up
4. Flag clears ? Global LED turns **OFF** ?
5. Checkered flag ? Global LED lights up
6. Finish race, close iRacing
7. Profile colors **RESTORED** ?

**Expected:**
- LEDs on during flags
- LEDs **off** between flags
- Profile colors back after race

### 2. Test Lap Display

**Record a timed race:**
- Should show: "Lap 5" (no total)

**Record a lap-based race:**
- Should show: "Lap 5 / 20"

**No more "2 / 32767"!** ?

### 3. Test Driver Count

**Start a race with 24 drivers:**
- Total Drivers should show: "24"
- Position should show: "P17 / 24"

**No more "17 / 1"!** ?

### 4. Test Solo Practice

**Practice alone:**
- Total Drivers: Could show 1 or -
- Position: "P1" or "-"

---

## ?? Summary of Changes

### Files Modified:

1. **SimControlCentre\Views\Tabs\LightingTab.xaml**
   - Hidden button selection GroupBox (Visibility="Collapsed")
   - Code preserved for future use

2. **SimControlCentre\Services\LightingService.cs**
   - Added `ClearAllDevicesAsync()` - turns off LEDs
   - Modified `UpdateForFlagAsync()` - calls Clear instead of Restore during session
   - `RestoreProfileLightingAsync()` still used for session end

3. **SimControlCentre\Views\Tabs\TelemetryDebugTab.xaml.cs**
   - Smart lap display (detects timed races)
   - Smart position display (handles missing driver count)
   - Better null/zero handling

4. **SimControlCentre\Services\iRacingTelemetryProvider.cs**
   - Improved driver counting (NumStarters first, then CarIdx count)
   - Better YAML parsing
   - Logging for troubleshooting

---

## ?? User Impact

### Before:
- ? Had to select individual buttons
- ? Profile colors shown between flags during race
- ? Lap display: "2 / 32767"
- ? Driver count: "1" when should be "24"
- ? Position: "17 / 1" (nonsense!)

### After:
- ? Simple - just enable, uses Global by default
- ? LEDs off between flags during race
- ? Profile colors restored after race ends
- ? Lap display: "5" for timed, "5 / 20" for lap-based
- ? Driver count: "24" (correct!)
- ? Position: "P17 / 24" (makes sense!)

---

## ?? Ready to Test!

Record a new race session with these fixes and verify:
1. Global LED behavior (on/off/restore)
2. Lap count display
3. Driver count accuracy
4. Position display

Everything should now work as expected! ??
