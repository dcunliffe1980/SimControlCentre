using System;
using System.Collections.Generic;

namespace SimControlCentre.Models
{
    /// <summary>
    /// Represents a recorded telemetry session
    /// </summary>
    public class TelemetryRecording
    {
        public string Version { get; set; } = "1.0";
        public DateTime RecordingStartTime { get; set; }
        public DateTime RecordingEndTime { get; set; }
        public string SourceSim { get; set; } = string.Empty;
        public List<TelemetrySnapshot> Snapshots { get; set; } = new();
        public RecordingMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Single telemetry data snapshot with timing
    /// </summary>
    public class TelemetrySnapshot
    {
        public double TimestampMs { get; set; }
        public TelemetryData Data { get; set; } = new();
    }

    /// <summary>
    /// Recording metadata
    /// </summary>
    public class RecordingMetadata
    {
        public string Description { get; set; } = string.Empty;
        public int SnapshotCount { get; set; }
        public double DurationSeconds { get; set; }
        public string TrackName { get; set; } = string.Empty;
        public string CarName { get; set; } = string.Empty;
        public string SessionType { get; set; } = string.Empty;
        public DateTime RecordingDate { get; set; }
    }
}
