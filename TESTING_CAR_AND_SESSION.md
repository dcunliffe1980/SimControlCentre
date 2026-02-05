# Testing Guide - Car Name & SessionType Fixes

## ?? What Was Fixed

### 1. Car Name Detection
**Problem:** Car showing as "Unknown"

**Root Cause:** Was looking for `DriverCarName` field which doesn't exist

**Fix:** Now looks for `CarScreenName` (correct field in YAML)

**Fallback:** If CarScreenName not found, uses `CarPath` (e.g., "bmwm4gt3")

---

### 2. SessionType Tracking
**Problem:** SessionType stuck on "Practice" during playback

**Possible Cause:** iRacing might not increment `SessionInfoUpdate` counter when session changes

**Fix:** Added extensive logging to diagnose the issue

---

## ?? Testing Steps

### Test 1: Car Name
1. **Start iRacing** with any car
2. **Open SimControlCentre**
3. **Check Telemetry Debug tab**
4. **Look at Car Data section:**
   ```
   Car: BMW M4 GT3    ? Should show actual car name
   ```
5. **If still "Unknown":** Check logs for what fields were found

---

### Test 2: SessionType - NEW Recording

**Important:** Record a NEW session with this build!

1. **Start iRacing** in Practice mode
2. **Start SimControlCentre**
3. **Start Recording** in Telemetry Debug tab
4. **Stay in Practice** for ~30 seconds
5. **Switch to Qualifying** (in iRacing)
6. **Watch logs** - should see:
   ```
   [iRacing Telemetry] SessionInfoUpdate CHANGED: 1 ? 2 (SESSION CHANGE DETECTED!)
   [iRacing Telemetry] ========================================
   [iRacing Telemetry] READING SESSION INFO (Update #2)
   [iRacing Telemetry] ========================================
   [iRacing Telemetry] SessionInfo updated: Type=Qualifying, ...
   ```
7. **Stay in Qualifying** for ~30 seconds
8. **Switch to Race** (in iRacing)
9. **Watch logs** - should see session change again
10. **Stop recording**

---

### Test 3: SessionType - Playback

1. **Play back** the recording from Test 2
2. **Watch Session Info:**
   ```
   Session Type: Practice
   ```
3. **Scrub to middle** (where you switched to Qualifying)
4. **Session Type should change to:** `Qualifying`
5. **Scrub to end** (where you switched to Race)
6. **Session Type should change to:** `Race`

---

## ?? What to Look For

### ? Success Indicators:

**Car Name:**
- Shows actual car name (e.g., "BMW M4 GT3", "Porsche 911 GT3 R")
- Or shows car path (e.g., "bmwm4gt3") if CarScreenName missing

**SessionType:**
- Logs show "SESSION CHANGE DETECTED" when switching sessions
- SessionType updates during playback
- Timeline scrubber shows different types as you scrub

---

### ? Failure Indicators:

**Car Name Still Unknown:**
- Check logs: What fields ARE being found?
- Possible solution: Provide a sample of your YAML (I can extract it from logs)

**SessionType Still Stuck:**
Two possibilities:

1. **iRacing doesn't increment SessionInfoUpdate counter:**
   - Logs will show: Initial value, never changes
   - This means iRacing doesn't signal session changes via this counter
   - Alternative solution needed

2. **Counter increments but YAML doesn't change:**
   - Logs will show: "SESSION CHANGE DETECTED"
   - But SessionType stays the same in YAML
   - iRacing might use different method to store session type

---

## ?? Log Locations

**While Running:**
- Shown in Telemetry Debug tab "Raw Data" section

**After Running:**
- `%LOCALAPPDATA%\SimControlCentre\logs\`
- Look for lines with `[iRacing Telemetry]`

---

## ?? What to Report

If issues persist, please provide:

### For Car Name Issue:
```
[iRacing Telemetry] SessionInfo updated: Type=?, Track=?, Car=?, Drivers=?
```

### For SessionType Issue:
```
Does SessionInfoUpdate change when you switch sessions?
- Yes, I see "SESSION CHANGE DETECTED" ? Good, next step: check YAML
- No, counter never changes ? iRacing doesn't use this counter

Does SessionType show in logs when YAML is read?
- Yes, shows "Type=Practice" then "Type=Qualifying" ? Should work!
- No, always shows same type ? YAML itself doesn't update
```

---

## ?? Next Steps If Still Broken

### If Car Name Still Unknown:
I'll need to see what fields ARE available. Can extract from logs:
```
[iRacing Telemetry] SessionInfo updated: Type=Practice, Track=Watkins Glen, Car=Unknown
```
Then I'll try different YAML fields.

### If SessionType Still Stuck:
Two options:

**Option A:** If counter increments but YAML doesn't change:
- iRacing might store session type differently
- May need to look at SessionNum or other fields

**Option B:** If counter never changes:
- iRacing might not signal session changes this way
- May need to detect session changes by other means (SessionNum, SessionState, etc.)

---

## ?? Expected Results

After this fix:

**Car Name:** ? Should work immediately  
**SessionType:** ? Depends on how iRacing signals session changes

Test it out and let me know what you see in the logs! ??
