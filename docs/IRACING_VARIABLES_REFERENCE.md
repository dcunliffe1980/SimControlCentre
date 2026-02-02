# iRacing Telemetry Variables Reference

This document lists all available telemetry variables from iRacing's shared memory (IRSDK), captured from a live session with 327 variables.

## Currently Used Variables

These are the variables currently implemented in `iRacingTelemetryProvider.cs`:

| Variable | Index | Type | Offset | Description | Usage |
|----------|-------|------|--------|-------------|-------|
| SessionFlags | 5 | 3 (bitField) | 24 | Current session flag status | Main flag detection |
| SessionState | 3 | 2 (int) | 16 | Current session state (0=Invalid, 1=GetInCar, 2=Warmup, 3=Racing, 4=Checkered, 5=CoolDown) | Session state tracking |
| Speed | 124 | 4 (float) | 6373 | Vehicle speed in m/s (converted to km/h) | Speed display |
| RPM | 89 | 4 (float) | 6248 | Engine RPM | RPM display |
| Gear | 88 | 2 (int) | 6244 | Current gear (-1=R, 0=N, 1+=forward gears) | Gear display |

## Variable Type Reference

iRacing uses the following type codes:
- **Type 1**: bool (1 byte)
- **Type 2**: int (4 bytes)
- **Type 3**: bitField (4 bytes)
- **Type 4**: float (4 bytes)
- **Type 5**: double (8 bytes)

## Complete Variable List

### Session Information (0-17)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 0 | SessionTime | 5 | 0 | Current session time (double) |
| 1 | SessionTick | 2 | 8 | Current tick in the session |
| 2 | SessionNum | 2 | 12 | Current session number |
| 3 | SessionState | 2 | 16 | Session state enum |
| 4 | SessionUniqueID | 2 | 20 | Unique session identifier |
| 5 | SessionFlags | 3 | 24 | Session flag bits |
| 6 | SessionTimeRemain | 5 | 28 | Time remaining in session |
| 7 | SessionLapsRemain | 2 | 36 | Laps remaining |
| 8 | SessionLapsRemainEx | 2 | 40 | Laps remaining (extended) |
| 9 | SessionTimeTotal | 5 | 44 | Total session time |
| 10 | SessionLapsTotal | 2 | 52 | Total laps in session |
| 11 | SessionJokerLapsRemain | 2 | 56 | Joker laps remaining |
| 12 | SessionOnJokerLap | 1 | 60 | On joker lap flag |
| 13 | SessionTimeOfDay | 4 | 61 | Time of day in seconds |
| 14 | RadioTransmitCarIdx | 2 | 65 | Car transmitting on radio |
| 15 | RadioTransmitRadioIdx | 2 | 69 | Radio channel index |
| 16 | RadioTransmitFrequencyIdx | 2 | 73 | Radio frequency index |
| 17 | DisplayUnits | 2 | 77 | Display units setting |

### System & Status (18-39)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 18 | DriverMarker | 1 | 81 | Driver marker visible |
| 19 | PushToTalk | 1 | 82 | Push to talk active |
| 20 | PushToPass | 1 | 83 | Push to pass active |
| 21 | ManualBoost | 1 | 84 | Manual boost active |
| 22 | ManualNoBoost | 1 | 85 | Manual no boost active |
| 23 | IsOnTrack | 1 | 86 | Car is on track |
| 24 | IsReplayPlaying | 1 | 87 | Replay is playing |
| 25 | ReplayFrameNum | 2 | 88 | Current replay frame |
| 26 | ReplayFrameNumEnd | 2 | 92 | Last replay frame |
| 27 | IsDiskLoggingEnabled | 1 | 96 | Disk logging enabled |
| 28 | IsDiskLoggingActive | 1 | 97 | Disk logging active |
| 29 | FrameRate | 4 | 98 | Current frame rate |
| 30 | CpuUsageFG | 4 | 102 | Foreground CPU usage |
| 31 | GpuUsage | 4 | 106 | GPU usage |
| 32 | ChanAvgLatency | 4 | 110 | Average channel latency |
| 33 | ChanLatency | 4 | 114 | Current channel latency |
| 34 | ChanQuality | 4 | 118 | Channel quality |
| 35 | ChanPartnerQuality | 4 | 122 | Partner channel quality |
| 36 | CpuUsageBG | 4 | 126 | Background CPU usage |
| 37 | ChanClockSkew | 4 | 130 | Channel clock skew |
| 38 | MemPageFaultSec | 4 | 134 | Memory page faults/sec |
| 39 | MemSoftPageFaultSec | 4 | 138 | Soft page faults/sec |

### Player Car Information (40-56)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 40 | PlayerCarPosition | 2 | 142 | Player position overall |
| 41 | PlayerCarClassPosition | 2 | 146 | Player position in class |
| 42 | PlayerCarClass | 2 | 150 | Player car class |
| 43 | PlayerTrackSurface | 2 | 154 | Track surface player is on |
| 44 | PlayerTrackSurfaceMaterial | 2 | 158 | Surface material type |
| 45 | PlayerCarIdx | 2 | 162 | Player car index |
| 46 | PlayerCarTeamIncidentCount | 2 | 166 | Team incident count |
| 47 | PlayerCarMyIncidentCount | 2 | 170 | Player incident count |
| 48 | PlayerCarDriverIncidentCount | 2 | 174 | Driver incident count |
| 49 | PlayerCarWeightPenalty | 4 | 178 | Weight penalty (kg) |
| 50 | PlayerCarPowerAdjust | 4 | 182 | Power adjustment (%) |
| 51 | PlayerCarDryTireSetLimit | 2 | 186 | Dry tire set limit |
| 52 | PlayerCarTowTime | 4 | 190 | Time being towed |
| 53 | PlayerCarInPitStall | 1 | 194 | In pit stall |
| 54 | PlayerCarPitSvStatus | 2 | 195 | Pit service status |
| 55 | PlayerTireCompound | 2 | 199 | Current tire compound |
| 56 | PlayerFastRepairsUsed | 2 | 203 | Fast repairs used |

### Multi-Car Arrays (57-79)
These variables are arrays containing data for all cars in the session:

| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 57 | CarIdxLap | 2 | 207 | Lap number for each car |
| 58 | CarIdxLapCompleted | 2 | 463 | Completed lap for each car |
| 59 | CarIdxLapDistPct | 4 | 719 | Lap distance % for each car |
| 60 | CarIdxTrackSurface | 2 | 975 | Track surface for each car |
| 61 | CarIdxTrackSurfaceMaterial | 2 | 1231 | Surface material for each car |
| 62 | CarIdxOnPitRoad | 1 | 1487 | On pit road for each car |
| 63 | CarIdxPosition | 2 | 1551 | Position for each car |
| 64 | CarIdxClassPosition | 2 | 1807 | Class position for each car |
| 65 | CarIdxClass | 2 | 2063 | Class for each car |
| 66 | CarIdxF2Time | 4 | 2319 | F2 time for each car |
| 67 | CarIdxEstTime | 4 | 2575 | Estimated time for each car |
| 68 | CarIdxLastLapTime | 4 | 2831 | Last lap time for each car |
| 69 | CarIdxBestLapTime | 4 | 3087 | Best lap time for each car |
| 70 | CarIdxBestLapNum | 2 | 3343 | Best lap number for each car |
| 71 | CarIdxTireCompound | 2 | 3599 | Tire compound for each car |
| 72 | CarIdxQualTireCompound | 2 | 3855 | Qual tire compound for each car |
| 73 | CarIdxQualTireCompoundLocked | 1 | 4111 | Qual tire locked for each car |
| 74 | CarIdxFastRepairsUsed | 2 | 4175 | Fast repairs used by each car |
| 75 | CarIdxSessionFlags | 3 | 4431 | Session flags for each car |
| 76 | PaceMode | 2 | 4687 | Current pace mode |
| 77 | CarIdxPaceLine | 2 | 4691 | Pace line for each car |
| 78 | CarIdxPaceRow | 2 | 4947 | Pace row for each car |
| 79 | CarIdxPaceFlags | 3 | 5203 | Pace flags for each car |

### Vehicle Controls & State (80-93)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 80 | OnPitRoad | 1 | 5459 | Player on pit road |
| 81 | CarIdxSteer | 4 | 5460 | Steering angle for each car |
| 82 | CarIdxRPM | 4 | 5716 | RPM for each car |
| 83 | CarIdxGear | 2 | 5972 | Gear for each car |
| 84 | SteeringWheelAngle | 4 | 6228 | Current steering angle |
| 85 | Throttle | 4 | 6232 | Throttle position (0-1) |
| 86 | Brake | 4 | 6236 | Brake position (0-1) |
| 87 | Clutch | 4 | 6240 | Clutch position (0-1) |
| 88 | Gear | 2 | 6244 | Current gear |
| 89 | RPM | 4 | 6248 | Engine RPM |
| 90 | PlayerCarSLFirstRPM | 4 | 6252 | Shift light first RPM |
| 91 | PlayerCarSLShiftRPM | 4 | 6256 | Shift light shift RPM |
| 92 | PlayerCarSLLastRPM | 4 | 6260 | Shift light last RPM |
| 93 | PlayerCarSLBlinkRPM | 4 | 6264 | Shift light blink RPM |

### Lap & Timing (94-123)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 94 | Lap | 2 | 6268 | Current lap number |
| 95 | LapCompleted | 2 | 6272 | Laps completed |
| 96 | LapDist | 4 | 6276 | Distance into lap (meters) |
| 97 | LapDistPct | 4 | 6280 | Distance into lap (%) |
| 98 | RaceLaps | 2 | 6284 | Total race laps |
| 99 | CarDistAhead | 4 | 6288 | Distance to car ahead |
| 100 | CarDistBehind | 4 | 6292 | Distance to car behind |
| 101 | LapBestLap | 2 | 6296 | Best lap number |
| 102 | LapBestLapTime | 4 | 6300 | Best lap time |
| 103 | LapLastLapTime | 4 | 6304 | Last lap time |
| 104 | LapCurrentLapTime | 4 | 6308 | Current lap time |
| 105 | LapLasNLapSeq | 2 | 6312 | Last N lap sequence |
| 106 | LapLastNLapTime | 4 | 6316 | Last N lap time |
| 107 | LapBestNLapLap | 2 | 6320 | Best N lap number |
| 108 | LapBestNLapTime | 4 | 6324 | Best N lap time |
| 109 | LapDeltaToBestLap | 4 | 6328 | Delta to best lap |
| 110 | LapDeltaToBestLap_DD | 4 | 6332 | Delta to best lap (DD) |
| 111 | LapDeltaToBestLap_OK | 1 | 6336 | Delta to best lap valid |
| 112 | LapDeltaToOptimalLap | 4 | 6337 | Delta to optimal lap |
| 113 | LapDeltaToOptimalLap_DD | 4 | 6341 | Delta to optimal (DD) |
| 114 | LapDeltaToOptimalLap_OK | 1 | 6345 | Delta to optimal valid |
| 115 | LapDeltaToSessionBestLap | 4 | 6346 | Delta to session best |
| 116 | LapDeltaToSessionBestLap_DD | 4 | 6350 | Delta to session best (DD) |
| 117 | LapDeltaToSessionBestLap_OK | 1 | 6354 | Delta to session best valid |
| 118 | LapDeltaToSessionOptimalLap | 4 | 6355 | Delta to session optimal |
| 119 | LapDeltaToSessionOptimalLap_DD | 4 | 6359 | Delta to session optimal (DD) |
| 120 | LapDeltaToSessionOptimalLap_OK | 1 | 6363 | Delta to session optimal valid |
| 121 | LapDeltaToSessionLastlLap | 4 | 6364 | Delta to session last lap |
| 122 | LapDeltaToSessionLastlLap_DD | 4 | 6368 | Delta to session last (DD) |
| 123 | LapDeltaToSessionLastlLap_OK | 1 | 6372 | Delta to session last valid |

### Physics & Motion (124-145)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 124 | Speed | 4 | 6373 | Speed in m/s |
| 125 | Yaw | 4 | 6377 | Yaw angle (radians) |
| 126 | YawNorth | 4 | 6381 | Yaw relative to north |
| 127 | Pitch | 4 | 6385 | Pitch angle (radians) |
| 128 | Roll | 4 | 6389 | Roll angle (radians) |
| 129 | EnterExitReset | 2 | 6393 | Enter/exit reset counter |
| 130 | TrackTemp | 4 | 6397 | Track temperature (C) |
| 131 | TrackTempCrew | 4 | 6401 | Track temp (crew) |
| 132 | AirTemp | 4 | 6405 | Air temperature (C) |
| 133 | TrackWetness | 2 | 6409 | Track wetness enum |
| 134 | Skies | 2 | 6413 | Sky condition enum |
| 135 | AirDensity | 4 | 6417 | Air density (kg/m³) |
| 136 | AirPressure | 4 | 6421 | Air pressure (Hg) |
| 137 | WindVel | 4 | 6425 | Wind velocity (m/s) |
| 138 | WindDir | 4 | 6429 | Wind direction (radians) |
| 139 | RelativeHumidity | 4 | 6433 | Relative humidity (%) |
| 140 | FogLevel | 4 | 6437 | Fog level (%) |
| 141 | Precipitation | 4 | 6441 | Precipitation (%) |
| 142 | SolarAltitude | 4 | 6445 | Solar altitude (radians) |
| 143 | SolarAzimuth | 4 | 6449 | Solar azimuth (radians) |
| 144 | WeatherDeclaredWet | 1 | 6453 | Weather declared wet |
| 145 | SteeringFFBEnabled | 1 | 6454 | FFB enabled |

### System & Display (146-193)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 146 | DCLapStatus | 2 | 6455 | Driver change lap status |
| 147 | DCDriversSoFar | 2 | 6459 | Drivers used so far |
| 148 | OkToReloadTextures | 1 | 6463 | OK to reload textures |
| 149 | LoadNumTextures | 1 | 6464 | Number of textures to load |
| 150 | CarLeftRight | 2 | 6465 | Car left/right indicator |
| 151 | PitsOpen | 1 | 6469 | Pits are open |
| 152 | VidCapEnabled | 1 | 6470 | Video capture enabled |
| 153 | VidCapActive | 1 | 6471 | Video capture active |
| 154 | PlayerIncidents | 2 | 6472 | Player incident count |
| 155 | PitRepairLeft | 4 | 6476 | Pit repair time left |
| 156 | PitOptRepairLeft | 4 | 6480 | Optional repair time left |
| 157 | PitstopActive | 1 | 6484 | Pitstop is active |
| 158 | FastRepairUsed | 2 | 6485 | Fast repairs used |
| 159 | FastRepairAvailable | 2 | 6489 | Fast repairs available |
| 160-177 | Tire Usage Stats | 2 | varies | LF/RF/LR/RR tires used/available |
| 178 | CamCarIdx | 2 | 6565 | Camera car index |
| 179 | CamCameraNumber | 2 | 6569 | Camera number |
| 180 | CamGroupNumber | 2 | 6573 | Camera group number |
| 181 | CamCameraState | 3 | 6577 | Camera state bits |
| 182 | IsOnTrackCar | 1 | 6581 | Viewing on-track car |
| 183 | IsInGarage | 1 | 6582 | In garage |
| 184 | SteeringWheelAngleMax | 4 | 6583 | Max steering angle |
| 185 | ShiftPowerPct | 4 | 6587 | Shift power percentage |
| 186 | ShiftGrindRPM | 4 | 6591 | Shift grind RPM |
| 187 | ThrottleRaw | 4 | 6595 | Raw throttle input |
| 188 | BrakeRaw | 4 | 6599 | Raw brake input |
| 189 | ClutchRaw | 4 | 6603 | Raw clutch input |
| 190 | HandbrakeRaw | 4 | 6607 | Raw handbrake input |
| 191 | BrakeABSactive | 1 | 6611 | ABS active |
| 192 | Shifter | 2 | 6612 | Shifter position |
| 193 | EngineWarnings | 3 | 6616 | Engine warning bits |

### Fuel & Pit Service (194-206)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 194 | FuelLevelPct | 4 | 6620 | Fuel level (%) |
| 195 | PitSvFlags | 3 | 6624 | Pit service flags |
| 196 | PitSvLFP | 4 | 6628 | Pit LF tire pressure |
| 197 | PitSvRFP | 4 | 6632 | Pit RF tire pressure |
| 198 | PitSvLRP | 4 | 6636 | Pit LR tire pressure |
| 199 | PitSvRRP | 4 | 6640 | Pit RR tire pressure |
| 200 | PitSvFuel | 4 | 6644 | Pit fuel amount |
| 201 | PitSvTireCompound | 2 | 6648 | Pit tire compound |
| 202 | CarIdxP2P_Status | 1 | 6652 | P2P status for each car |
| 203 | CarIdxP2P_Count | 2 | 6716 | P2P count for each car |
| 204 | P2P_Status | 1 | 6972 | Player P2P status |
| 205 | P2P_Count | 2 | 6973 | Player P2P count |
| 206 | SteeringWheelPctTorque | 4 | 6977 | Steering torque (%) |

### Force Feedback (207-227)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 207-215 | FFB Settings | 4 | varies | Various FFB parameters |
| 216 | ShiftIndicatorPct | 4 | 7014 | Shift indicator (%) |
| 217 | IsGarageVisible | 1 | 7018 | Garage UI visible |
| 218 | ReplayPlaySpeed | 2 | 7019 | Replay playback speed |
| 219 | ReplayPlaySlowMotion | 1 | 7023 | Replay slow motion |
| 220 | ReplaySessionTime | 5 | 7024 | Replay session time |
| 221 | ReplaySessionNum | 2 | 7032 | Replay session number |
| 222-225 | Tire Rumble | 4 | varies | LF/RF/LR/RR rumble pitch |
| 226 | SteeringWheelTorque_ST | 4 | 7052 | Steering torque (ST) |
| 227 | SteeringWheelTorque | 4 | 7076 | Steering torque |

### Advanced Physics (228-245)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 228 | VelocityZ_ST | 4 | 7080 | Z velocity (ST) |
| 229 | VelocityY_ST | 4 | 7104 | Y velocity (ST) |
| 230 | VelocityX_ST | 4 | 7128 | X velocity (ST) |
| 231 | VelocityZ | 4 | 7152 | Z velocity (m/s) |
| 232 | VelocityY | 4 | 7156 | Y velocity (m/s) |
| 233 | VelocityX | 4 | 7160 | X velocity (m/s) |
| 234 | YawRate_ST | 4 | 7164 | Yaw rate (ST) |
| 235 | PitchRate_ST | 4 | 7188 | Pitch rate (ST) |
| 236 | RollRate_ST | 4 | 7212 | Roll rate (ST) |
| 237 | YawRate | 4 | 7236 | Yaw rate (rad/s) |
| 238 | PitchRate | 4 | 7240 | Pitch rate (rad/s) |
| 239 | RollRate | 4 | 7244 | Roll rate (rad/s) |
| 240 | VertAccel_ST | 4 | 7248 | Vertical accel (ST) |
| 241 | LatAccel_ST | 4 | 7272 | Lateral accel (ST) |
| 242 | LongAccel_ST | 4 | 7296 | Longitudinal accel (ST) |
| 243 | VertAccel | 4 | 7320 | Vertical accel (m/s²) |
| 244 | LatAccel | 4 | 7324 | Lateral accel (m/s²) |
| 245 | LongAccel | 4 | 7328 | Longitudinal accel (m/s²) |

### Pit Commands & Setup (246-263)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 246 | dcStarter | 1 | 7332 | Start engine command |
| 247 | dcPitSpeedLimiterToggle | 1 | 7333 | Pit limiter toggle |
| 248-261 | Pit Setup | 4 | varies | Tire changes, fuel, pressures |
| 262 | dcToggleWindshieldWipers | 1 | 7390 | Toggle wipers |
| 263 | dcTriggerWindshieldWipers | 1 | 7391 | Trigger wipers |

### Engine & Systems (264-274)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 264 | FuelUsePerHour | 4 | 7392 | Fuel consumption rate |
| 265 | Voltage | 4 | 7396 | Battery voltage |
| 266 | WaterTemp | 4 | 7400 | Water temperature (C) |
| 267 | WaterLevel | 4 | 7404 | Water level (L) |
| 268 | FuelPress | 4 | 7408 | Fuel pressure (bar) |
| 269 | OilTemp | 4 | 7412 | Oil temperature (C) |
| 270 | OilPress | 4 | 7416 | Oil pressure (bar) |
| 271 | OilLevel | 4 | 7420 | Oil level (L) |
| 272 | ManifoldPress | 4 | 7424 | Manifold pressure (bar) |
| 273 | FuelLevel | 4 | 7428 | Fuel level (L) |
| 274 | Engine0_RPM | 4 | 7432 | Engine 0 RPM |

### Tire Telemetry (275-326)
Each tire (RF, LF, RR, LR) has detailed telemetry:
- Brake line pressure
- Cold pressure
- Odometer
- Temperature (Left/Middle/Right)
- Wear (Left/Middle/Right)
- Shock deflection & velocity

Example for Right Front (RF):
| Index | Variable | Type | Offset |
|-------|----------|------|--------|
| 275 | RFbrakeLinePress | 4 | 7436 |
| 276 | RFcoldPressure | 4 | 7440 |
| 277 | RFodometer | 4 | 7444 |
| 278 | RFtempCL | 4 | 7448 |
| 279 | RFtempCM | 4 | 7452 |
| 280 | RFtempCR | 4 | 7456 |
| 281 | RFwearL | 4 | 7460 |
| 282 | RFwearM | 4 | 7464 |
| 283 | RFwearR | 4 | 7468 |

*(Similar patterns for LF, RR, LR tires at different offsets)*

### Suspension (311-326)
| Index | Variable | Type | Offset | Description |
|-------|----------|------|--------|-------------|
| 311-326 | Shock Data | 4 | varies | Deflection and velocity for all shocks |

## Potentially Useful Variables for Future Features

### High Priority
- **Lap timing**: LapBestLapTime (102), LapLastLapTime (103), LapCurrentLapTime (104)
- **Position**: PlayerCarPosition (40), PlayerCarClassPosition (41)
- **Track location**: PlayerTrackSurface (43), IsOnTrack (23), OnPitRoad (80)
- **Fuel**: FuelLevel (273), FuelLevelPct (194), FuelUsePerHour (264)
- **Tire data**: Temperature and wear for all tires
- **Weather**: TrackTemp (130), AirTemp (132), TrackWetness (133)

### Medium Priority
- **Incidents**: PlayerIncidents (154)
- **Relative position**: CarDistAhead (99), CarDistBehind (100)
- **Engine warnings**: EngineWarnings (193)
- **Car systems**: WaterTemp (266), OilTemp (269), OilPress (270)
- **Pit service**: PitRepairLeft (155), PitOptRepairLeft (156)

### Low Priority
- **Telemetry**: LatAccel (244), LongAccel (245), VertAccel (243)
- **Multi-car data**: CarIdxLap (57), CarIdxPosition (63), CarIdxBestLapTime (69)
- **FFB data**: SteeringWheelTorque (227)
- **Camera**: CamCarIdx (178), CamCameraNumber (179)

## Notes

1. **_ST suffix**: Variables with "_ST" suffix are "session time" variants that provide historical data over time
2. **CarIdx arrays**: Many variables prefixed with "CarIdx" are arrays containing data for all cars in the session
3. **Bit fields**: Type 3 variables are bit fields that require bitwise operations to extract individual flags
4. **Unit conversions**: Speed is in m/s (multiply by 3.6 for km/h, multiply by 2.237 for mph)

## SessionFlags Bit Definitions

```csharp
irsdk_checkered             = 0x00000001
irsdk_white                 = 0x00000002
irsdk_green                 = 0x00000004
irsdk_yellow                = 0x00000008
irsdk_red                   = 0x00000010
irsdk_blue                  = 0x00000020
irsdk_debris                = 0x00000040
irsdk_crossed               = 0x00000080
irsdk_yellowWaving          = 0x00000100
irsdk_oneLapToGreen         = 0x00000200
irsdk_greenHeld             = 0x00000400
irsdk_tenToGo               = 0x00000800
irsdk_fiveToGo              = 0x00001000
irsdk_randomWaving          = 0x00002000
irsdk_caution               = 0x00004000
irsdk_cautionWaving         = 0x00008000
irsdk_black                 = 0x00010000
irsdk_disqualify            = 0x00020000
irsdk_servicible            = 0x00040000
irsdk_furled                = 0x00080000
irsdk_repair                = 0x00100000
```

## SessionState Enum Values

```
0 = Invalid (not in world)
1 = Get In Car
2 = Warmup
3 = Parade Laps
4 = Racing
5 = Checkered
6 = Cool Down
```

---

*Document generated from iRacing telemetry session data - Last updated: 2024*
