# Critical Issues to Fix - Session Summary

## Issues Identified:

### 1. Log Filters in Wrong Place ??
**Problem:** Added log filtering to Telemetry Debug tab
**Correct Location:** Should be in dedicated Logs tab (if exists) or removed
**Action:** Remove log filtering from Telemetry Debug tab

### 2. Car Name Detection Still Wrong ?
**Problem:** Despite 3 attempts, still showing wrong car (Porsche when driving Mercedes, safety car in MX-5)
**Root Cause:** Guessing at YAML structure without seeing actual data
**Action Needed:** 
- Option A: Add debug logging to dump actual YAML Drivers section structure
- Option B: User provides sample YAML from logs
- **STOP GUESSING** - need real data

### 3. Flag Lighting Lifecycle Broken ??
**Problem A:** LEDs trying to restore profile DURING session (between flags)
**Problem B:** After session ends, LEDs stay on last flag forever (don't restore)
**Expected:**
- During session, Flag=None ? LEDs OFF
- After session ends ? LEDs restore to profile colors
**Current Behavior:**
- During session: Tries to restore (wrong!)
- After session: Stays on last flag (wrong!)

### 4. Application Manager Missing Separate Controls ??
**Problem:** Start with Sim and Stop with Sim are coupled
**Use Case:** When viewing replays, don't want apps to start/stop automatically
**Action:** Add separate enable checkboxes for each behavior

---

## Priority Order:

1. **Remove log filtering from Telemetry Debug** (quick fix)
2. **Fix flag lighting lifecycle** (critical - currently broken)
3. **Add Application Manager separate controls** (important for replays)
4. **Car name detection** (need user's help with YAML sample)

---

## Action Plan:

### IMMEDIATE (This Session):

1. Remove log filters UI from TelemetryDebugTab
2. Fix flag lighting - investigate why profile restore isn't working
3. Add separate checkboxes to Application Manager

### NEEDS USER INPUT:

4. Car name detection - add debug logging to dump YAML or ask user for sample

Would you like me to:
A) Add debug logging to dump the Drivers YAML section so we can see the structure?
B) Proceed with fixes 1-3 and wait for you to provide YAML sample for car detection?
