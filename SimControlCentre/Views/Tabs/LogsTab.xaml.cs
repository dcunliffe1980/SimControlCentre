using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimControlCentre.Views.Tabs
{
    public partial class LogsTab : UserControl
    {
        private readonly List<string> _allLogEntries = new();
        private DispatcherTimer? _refreshTimer;
        private string? _currentLogFile;
        private long _lastFilePosition = 0;

        public LogsTab()
        {
            InitializeComponent();
            
            // Start auto-refresh timer (every 2 seconds)
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _refreshTimer.Tick += (s, e) => RefreshLogFromFile();
            _refreshTimer.Start();
            
            // Initial load
            RefreshLogFromFile();
        }

        private void RefreshLogFromFile()
        {
            try
            {
                var logsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimControlCentre", "logs");

                if (!Directory.Exists(logsPath))
                {
                    LogText.Text = $"Logs directory not found: {logsPath}";
                    return;
                }

                // Get today's log file
                var todayFile = Path.Combine(logsPath, $"SimControlCentre_{DateTime.Now:yyyy-MM-dd}.log");
                
                if (!File.Exists(todayFile))
                {
                    LogText.Text = $"No log file for today: {todayFile}";
                    return;
                }

                // If file changed, reset position
                if (_currentLogFile != todayFile)
                {
                    _currentLogFile = todayFile;
                    _lastFilePosition = 0;
                    _allLogEntries.Clear();
                }

                // Read new content since last position
                using var fileStream = new FileStream(todayFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                
                if (fileStream.Length > _lastFilePosition)
                {
                    fileStream.Seek(_lastFilePosition, SeekOrigin.Begin);
                    
                    using var reader = new StreamReader(fileStream);
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _allLogEntries.Add(line);
                    }
                    
                    _lastFilePosition = fileStream.Position;
                    
                    // Keep only last 1000 entries
                    while (_allLogEntries.Count > 1000)
                    {
                        _allLogEntries.RemoveAt(0);
                    }
                    
                    // Update display with filtering
                    UpdateFilteredLog();
                }
            }
            catch (Exception ex)
            {
                LogText.Text = $"Error reading log file: {ex.Message}";
            }
        }

        private void UpdateFilteredLog()
        {
            // Don't run if not fully initialized
            if (_allLogEntries == null || LogText == null || LogFilter_iRacingTelemetry == null)
                return;
            
            if (_allLogEntries.Count == 0)
            {
                LogText.Text = "No log entries yet...";
                return;
            }

            // Filter logs based on checkboxes
            var filteredLogs = _allLogEntries.Where(entry => ShouldShowLogEntry(entry)).ToList();
            
            // Update text (show last 500 entries)
            var displayEntries = filteredLogs.Skip(Math.Max(0, filteredLogs.Count - 500)).ToList();
            LogText.Text = string.Join("\n", displayEntries);
        }

        private bool ShouldShowLogEntry(string entry)
        {
            // Safety check - don't run if checkboxes not initialized
            if (LogFilter_iRacingTelemetry == null)
                return true;


            // Check which component this log is from
            if (entry.Contains("[iRacing Telemetry]") && LogFilter_iRacingTelemetry.IsChecked != true)
                return false;
            
            if (entry.Contains("[Lighting Service]") && LogFilter_LightingService.IsChecked != true)
                return false;
            
            if (entry.Contains("[GoXLR Service]") && LogFilter_GoXLRService.IsChecked != true)
                return false;
            
            if (entry.Contains("[Telemetry Service]") && LogFilter_TelemetryService.IsChecked != true)
                return false;
            
            if (entry.Contains("[Telemetry Debug]") && LogFilter_TelemetryDebug.IsChecked != true)
                return false;
            
            if (entry.Contains("[Telemetry Recorder]") && LogFilter_TelemetryRecorder.IsChecked != true)
                return false;
            
            if (entry.Contains("[iRacingMonitor]") && LogFilter_iRacingMonitor.IsChecked != true)
                return false;
            
            if (entry.Contains("[External Apps]") && LogFilter_ExternalApps.IsChecked != true)
                return false;
            
            if (entry.Contains("[App]") && LogFilter_App.IsChecked != true)
                return false;
            
            // Check if it's "Other" (no specific component tag)
            bool hasComponentTag = entry.Contains("[iRacing Telemetry]") ||
                                  entry.Contains("[Lighting Service]") ||
                                  entry.Contains("[GoXLR Service]") ||
                                  entry.Contains("[Telemetry Service]") ||
                                  entry.Contains("[Telemetry Debug]") ||
                                  entry.Contains("[Telemetry Recorder]") ||
                                  entry.Contains("[iRacingMonitor]") ||
                                  entry.Contains("[External Apps]") ||
                                  entry.Contains("[App]");
            
            if (!hasComponentTag && LogFilter_Other.IsChecked != true)
                return false;
            
            return true;
        }

        private void LogFilter_Changed(object sender, RoutedEventArgs e)
        {
            // Refresh the displayed logs
            UpdateFilteredLog();
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            _allLogEntries.Clear();
            LogText.Text = "Log view cleared. New entries will appear as they are logged.";
        }

        private void RefreshLog_Click(object sender, RoutedEventArgs e)
        {
            _lastFilePosition = 0; // Force reload entire file
            _allLogEntries.Clear();
            RefreshLogFromFile();
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimControlCentre", "logs");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(logsPath))
                {
                    Directory.CreateDirectory(logsPath);
                }
                
                // Open in Explorer
                Process.Start(new ProcessStartInfo
                {
                    FileName = logsPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open logs folder:\n\n{ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
