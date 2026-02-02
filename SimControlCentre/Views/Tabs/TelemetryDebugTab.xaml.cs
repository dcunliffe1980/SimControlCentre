using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class TelemetryDebugTab : UserControl
    {
        private readonly TelemetryService _telemetryService;
        private readonly List<string> _rawDataLog = new();
        private const int MaxLogEntries = 20;
        private TelemetryPlaybackProvider? _playbackProvider;
        private DispatcherTimer? _recordingTimer;
        private DispatcherTimer? _playbackTimer;
        private bool _pendingRecording;

        public TelemetryDebugTab(TelemetryService telemetryService)
        {
            InitializeComponent();
            
            _telemetryService = telemetryService;
            
            // Subscribe to telemetry events only
            _telemetryService.TelemetryUpdated += OnTelemetryUpdated;
            _telemetryService.ConnectionChanged += OnConnectionChanged;
            _telemetryService.FlagChanged += OnFlagChanged;
            
            UpdateConnectionStatus();
            LoadAvailableRecordings();
        }

        private void OnTelemetryUpdated(object? sender, TelemetryUpdatedEventArgs e)
        {
            // Update UI on dispatcher thread
            Dispatcher.Invoke(() =>
            {
                UpdateUI(e.Data);
                UpdateConnectionStatus(); // Update connection status with each telemetry update
                LogRawData(e.Data);
            });
        }

        private void OnConnectionChanged(object? sender, bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateConnectionStatus();
                
                // If connection is established and we're waiting to record, start recording
                if (isConnected && _pendingRecording)
                {
                    _telemetryService.Recorder.StartRecording();
                    _pendingRecording = false;
                    
                    AddRawLogEntry("========================================");
                    AddRawLogEntry("TELEMETRY CONNECTED - Recording started");
                    AddRawLogEntry("========================================");
                }
                // If connection is lost while recording, stop recording
                else if (!isConnected && _telemetryService.Recorder.IsRecording)
                {
                    AddRawLogEntry("Telemetry disconnected - stopping recording");
                    StopRecording_Click(this, new RoutedEventArgs());
                }
            });
        }

        private void OnFlagChanged(object? sender, FlagChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AddRawLogEntry($"FLAG CHANGE: {e.OldFlag} ? {e.NewFlag}");
            });
        }

        private void UpdateConnectionStatus()
        {
            bool isConnected = _telemetryService.IsConnected;
            var provider = _telemetryService.ActiveProvider;

            StatusText.Text = isConnected ? "Connected" : "Disconnected";
            StatusText.Foreground = isConnected ? Brushes.Green : Brushes.Orange;
            
            SimNameText.Text = provider?.ProviderName ?? "None";
        }

        private void UpdateUI(TelemetryData data)
        {
            // Update last update time
            LastUpdateText.Text = data.UpdatedAt.ToString("HH:mm:ss.fff");

            // Flag Status
            FlagText.Text = data.CurrentFlag.ToString();
            FlagText.Foreground = GetFlagColor(data.CurrentFlag);
            CautionText.Text = data.IsUnderCaution ? "Yes" : "No";

            // Session Info
            SessionTypeText.Text = data.SessionType;
            SessionStateText.Text = GetSessionStateName(data.SessionState);
            LapText.Text = data.TotalLaps > 0 
                ? $"{data.CurrentLap} / {data.TotalLaps}" 
                : data.CurrentLap.ToString();
            PositionText.Text = data.TotalDrivers > 0 
                ? $"{data.Position} / {data.TotalDrivers}" 
                : data.Position.ToString();
            TrackText.Text = data.TrackName;

            // Car Data
            SpeedText.Text = $"{data.Speed:F1} km/h";
            RpmText.Text = data.Rpm.ToString("F0");
            GearText.Text = data.Gear.ToString();
            PitsText.Text = data.IsInPits ? "Yes" : "No";
        }

        private void LogRawData(TelemetryData data)
        {
            var logEntry = $"[{data.UpdatedAt:HH:mm:ss}] Flag={data.CurrentFlag}, " +
                          $"State={data.SessionState}, " +
                          $"Lap={data.CurrentLap}, " +
                          $"Pos={data.Position}, " +
                          $"Speed={data.Speed:F1} km/h, " +
                          $"RPM={data.Rpm:F0}, " +
                          $"Gear={data.Gear}";
            
            AddRawLogEntry(logEntry);
        }

        private void AddRawLogEntry(string entry)
        {
            _rawDataLog.Add(entry);
            
            // Keep only last N entries
            while (_rawDataLog.Count > MaxLogEntries)
            {
                _rawDataLog.RemoveAt(0);
            }

            // Update text
            RawDataText.Text = string.Join("\n", _rawDataLog);
        }

        private Brush GetFlagColor(FlagStatus flag)
        {
            return flag switch
            {
                FlagStatus.Green => Brushes.LimeGreen,
                FlagStatus.Yellow or FlagStatus.YellowWaving => Brushes.Yellow,
                FlagStatus.Blue => Brushes.DeepSkyBlue,
                FlagStatus.White => Brushes.White,
                FlagStatus.Checkered => Brushes.WhiteSmoke,
                FlagStatus.Red => Brushes.Red,
                FlagStatus.Black => Brushes.Black,
                FlagStatus.Debris => Brushes.Orange,
                FlagStatus.OneLapToGreen => Brushes.YellowGreen,
                _ => Brushes.Gray
            };
        }

        private string GetSessionStateName(int state)
        {
            return state switch
            {
                0 => "Invalid",
                1 => "Get In Car",
                2 => "Warmup",
                3 => "Racing",
                4 => "Checkered",
                5 => "Cool Down",
                _ => $"Unknown ({state})"
            };
        }

        // Recording/Playback Methods
        
        private void LoadAvailableRecordings()
        {
            var recordings = _telemetryService.Recorder.GetAvailableRecordings();
            
            RecordingsComboBox.Items.Clear();
            
            // Just use filenames - much simpler and more reliable
            foreach (var filePath in recordings.OrderByDescending(f => File.GetCreationTime(f)))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var item = new ComboBoxItem
                {
                    Content = fileName,
                    Tag = filePath
                };
                RecordingsComboBox.Items.Add(item);
            }
            
            if (RecordingsComboBox.Items.Count > 0)
            {
                RecordingsComboBox.SelectedIndex = 0;
            }
        }

        private void StartRecording_Click(object sender, RoutedEventArgs e)
        {
            // Check if telemetry is connected (means we have valid session data)
            if (_telemetryService.IsConnected)
            {
                // Telemetry is connected, start recording immediately
                _telemetryService.Recorder.StartRecording();
                
                StartRecordingButton.IsEnabled = false;
                StopRecordingButton.IsEnabled = true;
                RecordingStatusText.Text = "Recording...";
                RecordingStatusText.Foreground = Brushes.Red;
                
                AddRawLogEntry("========================================");
                AddRawLogEntry("RECORDING STARTED");
                AddRawLogEntry("========================================");
            }
            else
            {
                // Wait for telemetry connection
                _pendingRecording = true;
                
                StartRecordingButton.IsEnabled = false;
                StopRecordingButton.IsEnabled = true;
                RecordingStatusText.Text = "Waiting for telemetry...";
                RecordingStatusText.Foreground = Brushes.Orange;
                
                AddRawLogEntry("========================================");
                AddRawLogEntry("RECORDING ARMED - Waiting for telemetry");
                AddRawLogEntry("========================================");
            }
            
            // Start timer to update recording status
            _recordingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _recordingTimer.Tick += (s, args) =>
            {
                if (_pendingRecording)
                {
                    RecordingStatusText.Text = "Waiting for telemetry...";
                    RecordingStatusText.Foreground = Brushes.Orange;
                }
                else if (_telemetryService.Recorder.IsRecording)
                {
                    var duration = _telemetryService.Recorder.RecordingDuration;
                    var count = _telemetryService.Recorder.SnapshotCount;
                    RecordingStatusText.Text = $"Recording: {duration:mm\\:ss} ({count} snapshots)";
                    RecordingStatusText.Foreground = Brushes.Red;
                }
            };
            _recordingTimer.Start();
        }

        private void StopRecording_Click(object sender, RoutedEventArgs e)
        {
            _recordingTimer?.Stop();
            _recordingTimer = null;
            _pendingRecording = false;
            
            var filePath = _telemetryService.Recorder.StopRecording();
            
            StartRecordingButton.IsEnabled = true;
            StopRecordingButton.IsEnabled = false;
            RecordingStatusText.Text = "Not recording";
            RecordingStatusText.Foreground = Brushes.Gray;
            
            if (filePath != null)
            {
                AddRawLogEntry($"Recording saved: {Path.GetFileName(filePath)}");
                AddRawLogEntry("========================================");
                
                LoadAvailableRecordings();
                MessageBox.Show($"Recording saved!\n\n{Path.GetFileName(filePath)}", 
                    "Recording Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var snapshotCount = _telemetryService.Recorder.SnapshotCount;
                
                AddRawLogEntry($"ERROR: Failed to save recording (Snapshots: {snapshotCount})");
                AddRawLogEntry("========================================");
                
                if (snapshotCount == 0)
                {
                    MessageBox.Show("No telemetry data was recorded.\n\n" +
                        "Make sure iRacing is running and you're in a session (not at the menu).\n\n" +
                        "Check the logs for more details.",
                        "No Data Recorded", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Failed to save recording. Check the logs for details.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RecordingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecordingsComboBox.SelectedItem is ComboBoxItem item)
            {
                PlayButton.IsEnabled = true;
                DeleteRecordingButton.IsEnabled = true;
                PlaybackStatusText.Text = "Ready to play";
                PlaybackStatusText.Foreground = Brushes.Gray;
            }
            else
            {
                PlayButton.IsEnabled = false;
                DeleteRecordingButton.IsEnabled = false;
                PlaybackStatusText.Text = "No recording selected";
                PlaybackStatusText.Foreground = Brushes.Gray;
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingsComboBox.SelectedItem is not ComboBoxItem item) return;
            
            var filePath = item.Tag as string;
            if (filePath == null) return;
            
            var recording = _telemetryService.Recorder.LoadRecording(filePath);
            if (recording == null)
            {
                MessageBox.Show("Failed to load recording", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Create playback provider if needed
            if (_playbackProvider == null)
            {
                _playbackProvider = new TelemetryPlaybackProvider();
                _telemetryService.RegisterProvider(_playbackProvider);
            }
            
            // Load and start playback
            _playbackProvider.LoadRecording(recording);
            _playbackProvider.Loop = LoopCheckBox.IsChecked ?? false;
            _playbackProvider.Start();
            
            PlayButton.IsEnabled = false;
            StopPlaybackButton.IsEnabled = true;
            PlaybackStatusText.Text = $"Playing: {recording.Metadata.Description}";
            PlaybackStatusText.Foreground = Brushes.Green;
            
            // Start timer to update playback status
            _playbackTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _playbackTimer.Tick += (s, args) =>
            {
                if (_playbackProvider != null)
                {
                    var progress = _playbackProvider.Progress * 100;
                    PlaybackStatusText.Text = $"Playing: {progress:F0}%";
                    
                    if (!_playbackProvider.IsPlaying && !_playbackProvider.Loop)
                    {
                        StopPlayback_Click(this, new RoutedEventArgs());
                    }
                }
            };
            _playbackTimer.Start();
            
            AddRawLogEntry("========================================");
            AddRawLogEntry($"PLAYBACK STARTED: {recording.Metadata.Description}");
            AddRawLogEntry($"Duration: {recording.Metadata.DurationSeconds:F1}s, Snapshots: {recording.Metadata.SnapshotCount}");
            AddRawLogEntry("========================================");
        }

        private void StopPlayback_Click(object sender, RoutedEventArgs e)
        {
            _playbackTimer?.Stop();
            _playbackTimer = null;
            
            _playbackProvider?.Stop();
            
            PlayButton.IsEnabled = RecordingsComboBox.SelectedItem != null;
            StopPlaybackButton.IsEnabled = false;
            PlaybackStatusText.Text = "Stopped";
            PlaybackStatusText.Foreground = Brushes.Gray;
            
            AddRawLogEntry("========================================");
            AddRawLogEntry("PLAYBACK STOPPED");
            AddRawLogEntry("========================================");
        }

        private void DeleteRecording_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingsComboBox.SelectedItem is not ComboBoxItem item) return;
            
            var filePath = item.Tag as string;
            if (filePath == null) return;
            
            var fileName = Path.GetFileName(filePath);
            var result = MessageBox.Show(
                $"Are you sure you want to delete this recording?\n\n{fileName}", 
                "Delete Recording", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Stop playback if this recording is playing
                if (_playbackProvider?.IsPlaying == true)
                {
                    StopPlayback_Click(this, new RoutedEventArgs());
                }
                
                if (_telemetryService.Recorder.DeleteRecording(filePath))
                {
                    AddRawLogEntry($"Recording deleted: {fileName}");
                    LoadAvailableRecordings();
                    MessageBox.Show("Recording deleted successfully.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to delete recording. Check the logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

