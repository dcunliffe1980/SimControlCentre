# Telemetry Recording & Playback Feature

## Overview

The telemetry recording and playback system allows you to record live telemetry data from iRacing and replay it later for testing and debugging purposes. This is especially useful because:

- **Replays don't have full telemetry** - iRacing replays don't send all the same data as live sessions
- **Test without driving** - Debug telemetry features without having to be in an active race
- **Reproduce issues** - Record a session where something went wrong and replay it to debug
- **Share data** - Send recordings to others for troubleshooting

## Location

Recordings are saved to: `Documents\SimControlCentre\TelemetryRecordings\`

File format: `telemetry_YYYYMMDD_HHMMSS.json`

## How to Use

### Recording Live Telemetry

1. **Start iRacing** and join a session (practice, race, etc.)
2. **Open SimControlCentre** to the Telemetry Debug tab
3. **Wait for connection** - Status should show "Connected" and "Active Sim: iRacing"
4. **Click "? Record"** - Recording starts immediately
5. **Drive around** - All telemetry data is being captured
6. **Click "? Stop"** - Recording is saved to disk
7. **Note the filename** - Recording is automatically named with date/time

### Playing Back Recordings

1. **Select a recording** from the dropdown in the Playback section
2. **(Optional) Check "Loop"** if you want continuous playback
3. **Click "? Play"** - Playback starts from the beginning
4. **Watch the telemetry update** - The UI updates as if you were driving
5. **Click "? Stop"** to stop playback early

## Features

### Recording
- **Automatic timestamping** - Each snapshot is timestamped relative to recording start
- **Full telemetry data** - Captures all currently implemented variables (flags, speed, RPM, gear, etc.)
- **Metadata** - Includes session type, track name, duration, snapshot count
- **Real-time status** - Shows recording duration and snapshot count while recording

### Playback
- **Accurate timing** - Plays back at the same rate it was recorded
- **Looping** - Can loop playback continuously for testing
- **Progress indicator** - Shows playback progress percentage
- **Auto-stop** - Stops when playback finishes (unless looping)

## File Format

Recordings are saved as JSON files with the following structure:

```json
{
  "Version": "1.0",
  "RecordingStartTime": "2024-01-15T14:30:00",
  "RecordingEndTime": "2024-01-15T14:32:30",
  "SourceSim": "iRacing",
  "Metadata": {
    "Description": "Recorded 2024-01-15 14:30:00",
    "SnapshotCount": 1500,
    "DurationSeconds": 150.0,
    "TrackName": "Circuit de Spa-Francorchamps",
    "SessionType": "Race"
  },
  "Snapshots": [
    {
      "TimestampMs": 0.0,
      "Data": {
        "IsConnected": true,
        "SourceSim": "iRacing",
        "UpdatedAt": "2024-01-15T14:30:00",
        "CurrentFlag": "Green",
        "Speed": 245.5,
        "Rpm": 8500,
        "Gear": 6,
        ...
      }
    },
    ...
  ]
}
```

## Use Cases

### 1. Debugging Flag Changes
Record a full race to capture all flag transitions (green, yellow, blue, white, checkered) and replay to test flag handling logic.

### 2. Testing UI Updates
Record different scenarios (pit stops, crashes, different speeds) and replay to test how the UI responds to various telemetry states.

### 3. Reproducing Bugs
If you notice a bug during a race, record it and replay later to investigate without having to recreate the conditions.

### 4. Development Without Sim
Work on telemetry features without needing to launch iRacing - just play back a recording.

### 5. Sharing Test Cases
Send recording files to other developers or in bug reports to demonstrate specific issues.

## Technical Details

### Services
- **TelemetryRecorder** (`Services\TelemetryRecorder.cs`) - Records telemetry to disk
- **TelemetryPlaybackProvider** (`Services\TelemetryPlaybackProvider.cs`) - Plays back recorded data
- **TelemetryService** - Manages recording and integrates playback provider

### Models
- **TelemetryRecording** (`Models\TelemetryRecording.cs`) - Recording file structure
- **TelemetrySnapshot** - Single telemetry data point with timestamp
- **RecordingMetadata** - Recording information

### Integration
The recorder is integrated into the TelemetryService and automatically records all telemetry updates when recording is active. The playback provider implements ITelemetryProvider and integrates seamlessly with the existing telemetry system.

## Limitations

- **File size** - Long recordings with high update rates can produce large files
- **Playback speed** - Currently fixed at 1x speed (could be enhanced to support speed control)
- **Variables** - Only records currently implemented variables (Speed, RPM, Gear, Flags, SessionState)
- **No session YAML data** - Only records telemetry variables, not the full session info YAML

## Future Enhancements

Potential improvements:
- Variable playback speed (0.5x, 2x, etc.)
- Recording compression
- Selective variable recording (to reduce file size)
- Recording editing/trimming
- Export to other formats
- Recording search/filtering

## Tips

- **Record during live sessions** - Replays don't have the same telemetry depth
- **Keep recordings short** - Record specific scenarios rather than entire races
- **Name recordings meaningfully** - Add notes about what was recorded
- **Test flag changes** - Start recording before a flag change to capture the transition
- **Use loop mode** - Great for continuous testing of a specific scenario

---

*Feature added: February 2026*
*For issues or enhancements, see the GitHub repository*
