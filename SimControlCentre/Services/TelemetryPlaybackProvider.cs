using System;
using System.Threading;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Plays back recorded telemetry data
    /// </summary>
    public class TelemetryPlaybackProvider : ITelemetryProvider, IDisposable
    {
        private TelemetryRecording? _recording;
        private Timer? _playbackTimer;
        private DateTime _playbackStartTime;
        private int _currentSnapshotIndex;
        private bool _isPlaying;
        private bool _isDisposed;
        private float _playbackSpeed = 1.0f;
        private bool _loop;

        public string ProviderName => "Playback";
        public bool IsConnected => _isPlaying;
        public bool IsPlaying => _isPlaying;
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Math.Max(0.1f, Math.Min(10.0f, value));
        }
        public bool Loop
        {
            get => _loop;
            set => _loop = value;
        }
        public double Progress => _recording != null && _recording.Snapshots.Count > 0
            ? (double)_currentSnapshotIndex / _recording.Snapshots.Count
            : 0;

        public event EventHandler<TelemetryUpdatedEventArgs>? TelemetryUpdated;
        public event EventHandler<bool>? ConnectionChanged;

        public void LoadRecording(TelemetryRecording recording)
        {
            Stop();
            _recording = recording;
            _currentSnapshotIndex = 0;
            
            Logger.Info("Telemetry Playback", $"Recording loaded: {recording.Snapshots.Count} snapshots, {recording.Metadata.DurationSeconds:F1}s");
        }

        public void Start()
        {
            if (_recording == null)
            {
                Logger.Warning("Telemetry Playback", "No recording loaded");
                return;
            }

            if (_isPlaying)
            {
                Logger.Warning("Telemetry Playback", "Already playing");
                return;
            }

            _currentSnapshotIndex = 0;
            _playbackStartTime = DateTime.Now;
            _isPlaying = true;

            // Start playback timer at 30Hz (should be smooth enough, less CPU intensive)
            _playbackTimer = new Timer(PlaybackUpdate, null, 0, 33);

            ConnectionChanged?.Invoke(this, true);
            Logger.Info("Telemetry Playback", "Playback started");
        }

        public void Stop()
        {
            _playbackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _playbackTimer?.Dispose();
            _playbackTimer = null;

            if (_isPlaying)
            {
                _isPlaying = false;
                ConnectionChanged?.Invoke(this, false);
                Logger.Info("Telemetry Playback", "Playback stopped");
            }

            _currentSnapshotIndex = 0;
        }

        public void Pause()
        {
            if (_isPlaying)
            {
                _playbackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                Logger.Info("Telemetry Playback", "Playback paused");
            }
        }

        public void Resume()
        {
            if (_isPlaying && _playbackTimer != null)
            {
                _playbackTimer.Change(0, 33);
                Logger.Info("Telemetry Playback", "Playback resumed");
            }
        }

        public TelemetryData? GetLatestData()
        {
            if (_recording == null || _currentSnapshotIndex <= 0 || _currentSnapshotIndex > _recording.Snapshots.Count)
                return null;

            return _recording.Snapshots[_currentSnapshotIndex - 1].Data;
        }

        private void PlaybackUpdate(object? state)
        {
            if (_recording == null || !_isPlaying) return;

            try
            {
                // Calculate elapsed time with playback speed
                var elapsedMs = (DateTime.Now - _playbackStartTime).TotalMilliseconds * _playbackSpeed;

                // Find and play all snapshots up to current time (batch processing)
                var snapshotsToPlay = new List<TelemetrySnapshot>();
                
                while (_currentSnapshotIndex < _recording.Snapshots.Count)
                {
                    var snapshot = _recording.Snapshots[_currentSnapshotIndex];

                    if (snapshot.TimestampMs > elapsedMs)
                    {
                        // Not time for this snapshot yet
                        break;
                    }

                    snapshotsToPlay.Add(snapshot);
                    _currentSnapshotIndex++;
                }

                // Only send the last snapshot in this batch to avoid flooding
                if (snapshotsToPlay.Count > 0)
                {
                    var lastSnapshot = snapshotsToPlay[snapshotsToPlay.Count - 1];
                    TelemetryUpdated?.Invoke(this, new TelemetryUpdatedEventArgs(lastSnapshot.Data));
                }

                // Check if we've reached the end
                if (_currentSnapshotIndex >= _recording.Snapshots.Count)
                {
                    if (_loop)
                    {
                        // Loop back to start
                        Logger.Info("Telemetry Playback", "Looping playback");
                        _currentSnapshotIndex = 0;
                        _playbackStartTime = DateTime.Now;
                    }
                    else
                    {
                        // Stop playback
                        Logger.Info("Telemetry Playback", "Playback finished");
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Telemetry Playback", "Error during playback", ex);
                Stop();
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            Stop();
            _isDisposed = true;
        }
    }
}
