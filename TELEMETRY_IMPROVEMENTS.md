# iRacing Telemetry Improvements - More Data!

## ?? Issues Found

You discovered that many telemetry fields were showing 0 or "Unknown":

1. **SessionType** - Always "Unknown"
2. **SessionState** - Showing 4 (appeared to be "Checkered" but was actually "Racing")
3. **SessionTime, SessionTimeRemaining** - Always 0
4. **CurrentLap, TotalLaps** - Always 0
5. **Position, ClassPosition, TotalDrivers** - Always 0
6. **Throttle, Brake, Clutch** - Always 0
7. **FuelLevel, FuelUsePerLap** - Always 0
8. **LastLapTime, BestLapTime** - Always 0
9. **TrackName, CarName** - Always "Unknown"
10. **IsInPits** - Always false

## ?? Root Causes

### 1. SessionState Confusion
**Problem:** SessionState value 4 was being treated as a flag, but it's actually a session state!

**iRacing SessionState Values:**
```
0 = Invalid
1 = GetInCar
2 = Warmup
3 = ParadeLaps
4 = Racing      ? You were seeing this!
5 = Checkered
6 = CoolDown
```

Value 4 = **Racing** (not Checkered flag!)

### 2. SessionType Not a Telemetry Variable
**Problem:** SessionType doesn't exist in the telemetry variables!

**Solution:** SessionType comes from the **SessionInfo YAML block**, not the variable stream.

iRacing stores session metadata in a YAML string that includes:
- SessionType (Practice, Qualifying, Race, etc.)
- TrackDisplayName
- DriverCarName
- Weather data
- And more...

### 3. Most Variables Not Being Read
**Problem:** Only reading 5 variables (SessionFlags, SessionState, Speed, RPM, Gear)

**Solution:** Added 15+ more variables to capture all important telemetry.

## ? What Was Fixed

### Added Variable Reading
Now reading these variables:

#### Session Data
- **SessionTime** - Current session time (double)
- **SessionTimeRemain** - Time remaining (double)
- **SessionLapsTotal** - Total laps in session
- **Lap / LapCompleted** - Current lap number

#### Position & Competition
- **PlayerCarPosition** - Overall position
- **PlayerCarClassPosition** - Class position
- Total drivers count (from SessionInfo YAML)

#### Car Inputs
- **Throttle** - Throttle position (0-1)
- **Brake** - Brake position (0-1)
- **Clutch** - Clutch position (0-1)

#### Fuel
- **FuelLevel** - Current fuel level
- **FuelUsePerLap** - Fuel used per lap

#### Lap Times
- **LapLastLapTime** - Last completed lap time
- **LapBestLapTime** - Best lap time

#### Status
- **IsOnTrack** - Whether car is on track
- **IsInGarage** - Whether in pits/garage

### Added SessionInfo YAML Parsing
Now reading from the SessionInfo YAML block:

- **SessionType** - Practice, Qualifying, Race, etc.
- **TrackDisplayName** - Actual track name
- **DriverCarName** - Car model name
- **Driver count** - Total drivers in session

## ?? Before vs After

### Before (Minimal Data)
```json
{
  "SessionType": "Unknown",           // Not read
  "SessionState": 4,                  // Confusing!
  "SessionTime": 0,                   // Not read
  "SessionTimeRemaining": 0,          // Not read
  "CurrentLap": 0,                    // Not read
  "TotalLaps": 0,                     // Not read
  "Speed": 162.94,                    // ? Working
  "Rpm": 6208.82,                     // ? Working
  "Gear": 4,                          // ? Working
  "Position": 0,                      // Not read
  "TrackName": "Unknown",             // Not read
  "FuelLevel": 0                      // Not read
}
```

### After (Full Data!)
```json
{
  "SessionType": "Race",              // ? From YAML
  "SessionState": 4,                  // ? Racing state (not flag!)
  "SessionTime": 1234.56,             // ? Read
  "SessionTimeRemaining": 789.12,     // ? Read
  "CurrentLap": 5,                    // ? Read
  "TotalLaps": 20,                    // ? Read
  "Speed": 162.94,                    // ? Working
  "Rpm": 6208.82,                     // ? Working
  "Gear": 4,                          // ? Working
  "Throttle": 0.85,                   // ? Read
  "Brake": 0.0,                       // ? Read
  "Position": 3,                      // ? Read
  "TotalDrivers": 24,                 // ? Parsed from YAML
  "TrackName": "Watkins Glen",        // ? From YAML
  "CarName": "BMW M4 GT3",            // ? From YAML
  "FuelLevel": 45.2,                  // ? Read
  "LastLapTime": 106.234,             // ? Read
  "BestLapTime": 105.123,             // ? Read
  "IsInPits": false                   // ? Read
}
```

## ?? Technical Details

### SessionInfo YAML Structure
The SessionInfo block is a YAML-formatted string containing:

```yaml
WeekendInfo:
 TrackName: watkins_glen
 TrackDisplayName: Watkins Glen International
 SessionID: 12345
 
SessionInfo:
 Sessions:
  - SessionNum: 0
    SessionLaps: "20 laps"
    SessionType: Race
    
DriverInfo:
 DriverCarIdx: 0
 DriverUserID: 123456
 Drivers:
  - CarNumber: "1"
    DriverCarName: "BMW M4 GT3"
```

### Variable Reading Improvements
- **Switch statement** instead of if-else chain for better performance
- **Type-aware reading** - double for session times, int for laps, float for inputs
- **Proper boolean handling** - IsOnTrack, IsInGarage

### Helper Method Added
```csharp
private string? ParseYamlValue(string yaml, string key)
{
    // Finds "key:" in YAML
    // Extracts value on same line
    // Removes quotes if present
    // Returns cleaned value
}
```

## ?? Impact

### Timeline Scrubber Improvements
With SessionType now showing correctly, the timeline will display:
- "Practice Session" instead of "Unknown"
- "Qualifying Session" instead of "Unknown"
- "Race" instead of "Unknown"

### Better Telemetry Display
- See actual lap progress (Lap 5/20)
- See session time (12:34 / 20:00)
- See position (P3 of 24)
- See fuel status (45.2L, -2.1L/lap)
- See lap times (Best: 1:45.123, Last: 1:46.234)

### Future Features Enabled
Now that we have this data, we can build:
- **Fuel calculator** - "You need 3 more laps worth of fuel"
- **Lap time delta** - "0.234s slower than your best"
- **Position tracker** - "P3, +1.2s to P2, -0.8s from P4"
- **Session countdown** - "5 minutes remaining"

## ?? Testing

To verify the fixes:

1. **Start iRacing** and enter a session
2. **Open Telemetry Debug** tab in SimControlCentre
3. **Check the values:**
   - SessionType should show "Practice", "Qualifying", or "Race"
   - SessionState should show a number 1-6 (4 = Racing)
   - CurrentLap should increment as you complete laps
   - Position should show your current position
   - TrackName should show the actual track name
   - FuelLevel should show your fuel
   
4. **Record a session** with the Record button
5. **Play it back** and use the timeline scrubber
6. **Check JSON** in the recording file - all fields should have values

## ?? Files Modified

- `SimControlCentre\Services\iRacingTelemetryProvider.cs`
  - Added 15+ variable reads
  - Added SessionInfo YAML parsing
  - Added ParseYamlValue() helper method
  - Improved variable reading with switch statement

## ?? Next Steps

With full telemetry data now available, you could:

1. **Add telemetry overlays** to show key info
2. **Fuel warnings** when running low
3. **Lap time deltas** comparing to best lap
4. **Position tracking** with gap analysis
5. **Session countdown** timer

Or continue with the roadmap (Phase 2.2: Telemetry Optimization)!

## ?? Summary

**Before:** 5 variables read, SessionType broken, SessionState confusing  
**After:** 20+ variables read, SessionType from YAML, SessionState clear

**Your telemetry recordings will now have complete data!** ??
