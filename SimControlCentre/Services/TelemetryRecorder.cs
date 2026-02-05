using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Records telemetry data to disk for later playback
    /// </summary>
    public class TelemetryRecorder : IDisposable
    {
        private readonly string _recordingsPath;
        private readonly List<TelemetrySnapshot> _snapshots = new();
        private DateTime _recordingStartTime;
        private bool _isRecording;
        private string? _currentFilePath;
        private TelemetryData? _lastData;
        private DateTime _lastSnapshotTime;
        private const int MinSnapshotIntervalMs = 100; // Limit to 10 snapshots per second max

        public bool IsRecording => _isRecording;
        public int SnapshotCount => _snapshots.Count;
        public string RecordingsDirectory => _recordingsPath;
        public TimeSpan RecordingDuration => _isRecording 
            ? DateTime.Now - _recordingStartTime 
            : TimeSpan.Zero;


        public TelemetryRecorder()
        {
            // Save recordings to Documents/SimControlCentre/TelemetryRecordings/
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _recordingsPath = Path.Combine(documentsPath, "SimControlCentre", "TelemetryRecordings");
            
            Directory.CreateDirectory(_recordingsPath);
            
            Logger.Info("Telemetry Recorder", $"Recordings path: {_recordingsPath}");
        }

        public void StartRecording()
        {
            if (_isRecording)
            {
                Logger.Warning("Telemetry Recorder", "Recording already in progress");
                return;
            }

            _snapshots.Clear();
            _recordingStartTime = DateTime.Now;
            _lastSnapshotTime = DateTime.MinValue; // Allow first snapshot immediately
            _isRecording = true;
            _lastData = null;

            Logger.Info("Telemetry Recorder", "Recording started");
        }

        public string? StopRecording()
        {
            if (!_isRecording)
            {
                Logger.Warning("Telemetry Recorder", "No recording in progress");
                return null;
            }

            _isRecording = false;
            
            if (_snapshots.Count == 0)
            {
                Logger.Warning("Telemetry Recorder", "No snapshots recorded - telemetry data may not have been available");
                Logger.Warning("Telemetry Recorder", "Recording duration was: " + (DateTime.Now - _recordingStartTime).TotalSeconds + " seconds");
                return null;
            }

            var duration = DateTime.Now - _recordingStartTime;

            Logger.Info("Telemetry Recorder", $"Recording stopped. Snapshots: {_snapshots.Count}, Duration: {duration.TotalSeconds:F1}s");

            // Generate filename from metadata
            var timestamp = _recordingStartTime.ToString("yyyyMMdd_HHmmss");
            var carName = SanitizeFileName(_lastData?.CarName ?? "Car");
            var trackName = SanitizeFileName(_lastData?.TrackName ?? "Track");
            
            // If both are unknown/default, just use timestamp
            if (carName == "Car" && trackName == "Track")
            {
                var fileName = $"recording_{timestamp}.json";
                _currentFilePath = Path.Combine(_recordingsPath, fileName);
            }
            else
            {
                var fileName = $"{carName}_{trackName}_{timestamp}.json";
                _currentFilePath = Path.Combine(_recordingsPath, fileName);
            }

            // Save to file
            var recording = new TelemetryRecording
            {
                RecordingStartTime = _recordingStartTime,
                RecordingEndTime = DateTime.Now,
                SourceSim = _lastData?.SourceSim ?? "Unknown",
                Snapshots = _snapshots,
                Metadata = new RecordingMetadata
                {
                    SnapshotCount = _snapshots.Count,
                    DurationSeconds = duration.TotalSeconds,
                    TrackName = _lastData?.TrackName ?? "Unknown",
                    CarName = _lastData?.CarName ?? "Unknown",
                    SessionType = _lastData?.SessionType ?? "Unknown",
                    RecordingDate = _recordingStartTime,
                    Description = $"{_lastData?.CarName ?? "Car"} @ {_lastData?.TrackName ?? "Track"}"
                }
            };

            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(_recordingsPath);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false, // Compact JSON - saves ~50% file size
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(recording, options);
                File.WriteAllText(_currentFilePath!, json);

                Logger.Info("Telemetry Recorder", $"Recording saved successfully: {Path.GetFileName(_currentFilePath)}");
                Logger.Info("Telemetry Recorder", $"Full path: {_currentFilePath}");
                Logger.Info("Telemetry Recorder", $"File size: {new FileInfo(_currentFilePath).Length / 1024} KB");
                return _currentFilePath;
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Recorder", $"Error saving recording to: {_currentFilePath}", ex);
                Logger.Error("Telemetry Recorder", $"Recordings path: {_recordingsPath}", ex);
                return null;
            }
        }

        public void RecordSnapshot(TelemetryData data)
        {
            if (!_isRecording) return;

            // Throttle snapshots to reduce file size
            var now = DateTime.Now;
            if (_snapshots.Count > 0 && (now - _lastSnapshotTime).TotalMilliseconds < MinSnapshotIntervalMs)
            {
                return; // Skip this snapshot
            }

            var timestampMs = (now - _recordingStartTime).TotalMilliseconds;
            
            _snapshots.Add(new TelemetrySnapshot
            {
                TimestampMs = timestampMs,
                Data = data
            });

            _lastData = data;
            _lastSnapshotTime = now;
            
            // Log first snapshot to confirm recording is working
            if (_snapshots.Count == 1)
            {
                Logger.Info("Telemetry Recorder", "First snapshot recorded successfully");
            }
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid filename characters
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            
            // Limit length
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
                
            return sanitized;
        }

        public List<string> GetAvailableRecordings()
        {
            try
            {
                // Get all JSON files (not just "telemetry_*.json" pattern)
                if (!Directory.Exists(_recordingsPath))
                {
                    return new List<string>();
                }
                
                var files = Directory.GetFiles(_recordingsPath, "*.json");
                return files.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Recorder", "Error listing recordings", ex);
                return new List<string>();
            }
        }

        public TelemetryRecording? LoadRecording(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var recording = JsonSerializer.Deserialize<TelemetryRecording>(json);
                
                Logger.Info("Telemetry Recorder", $"Loaded recording: {filePath} ({recording?.Snapshots.Count} snapshots)");
                return recording;
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Recorder", $"Error loading recording: {filePath}", ex);
                return null;
            }
        }

        public void Dispose()
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }

        public bool DeleteRecording(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Info("Telemetry Recorder", $"Recording deleted: {filePath}");
                    return true;
                }
                
                Logger.Warning("Telemetry Recorder", $"Recording not found: {filePath}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Recorder", $"Error deleting recording: {filePath}", ex);
                return false;
            }
        }
    }
}
