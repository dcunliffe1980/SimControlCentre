using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class ExternalAppsTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        private readonly iRacingMonitorService _iRacingMonitor;

        public ExternalAppsTab(ConfigurationService configService, AppSettings settings, iRacingMonitorService iRacingMonitor)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _iRacingMonitor = iRacingMonitor;

            // Subscribe to iRacing state changes
            _iRacingMonitor.iRacingStateChanged += OnIRacingStateChanged;

            // Initial status update
            UpdateiRacingStatus(_iRacingMonitor.IsiRacingRunning());

            // Load apps
            RefreshAppsList();
            
            // Start a timer to refresh app status every 2 seconds
            var refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            refreshTimer.Tick += (s, e) => UpdateRunningStatus();
            refreshTimer.Start();
        }

        private void UpdateRunningStatus()
        {
            // Update IsRunning status for all apps
            foreach (var app in _settings.ExternalApps)
            {
                try
                {
                    if (app.AppType == ExternalAppType.StartWithRacing)
                    {
                        // For start apps, check if the tracked PID is still running
                        if (app.ProcessId > 0)
                        {
                            try
                            {
                                var process = System.Diagnostics.Process.GetProcessById(app.ProcessId);
                                app.IsRunning = !process.HasExited;
                            }
                            catch (ArgumentException)
                            {
                                app.IsRunning = false;
                            }
                        }
                        else
                        {
                            app.IsRunning = false;
                        }
                    }
                    else // StopForRacing
                    {
                        // Check if any process with this name is running
                        var exeName = System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                        var processes = System.Diagnostics.Process.GetProcessesByName(exeName);
                        app.IsRunning = processes.Length > 0;
                    }
                }
                catch
                {
                    app.IsRunning = false;
                }
            }
            
            RefreshAppsList();
        }

        private void OnIRacingStateChanged(object? sender, iRacingStateChangedEventArgs e)
        {
            Dispatcher.Invoke(() => UpdateiRacingStatus(e.IsRunning));
        }

        private void UpdateiRacingStatus(bool isRunning)
        {
            if (isRunning)
            {
                iRacingStatusIndicator.Fill = Brushes.LimeGreen;
                iRacingStatusText.Text = "Running";
            }
            else
            {
                iRacingStatusIndicator.Fill = Brushes.Gray;
                iRacingStatusText.Text = "Not Running";
            }
        }

        private void RefreshAppsList()
        {
            // Separate apps by type
            var startApps = _settings.ExternalApps.Where(a => a.AppType == ExternalAppType.StartWithRacing).ToList();
            var stopApps = _settings.ExternalApps.Where(a => a.AppType == ExternalAppType.StopForRacing).ToList();
            
            StartAppsListBox.ItemsSource = null;
            StartAppsListBox.ItemsSource = startApps;
            
            StopAppsListBox.ItemsSource = null;
            StopAppsListBox.ItemsSource = stopApps;
        }

        private void AddStartApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExternalAppDialog(null, _settings, ExternalAppType.StartWithRacing);
            if (dialog.ShowDialog() == true && dialog.ResultApp != null)
            {
                _settings.ExternalApps.Add(dialog.ResultApp);
                _configService.Save(_settings);
                RefreshAppsList();
            }
        }

        private void AddStopApp_Click(object sender, RoutedEventArgs e)
        {
            // Show system tray app picker
            var picker = new SystemTrayAppPicker();
            if (picker.ShowDialog() == true && picker.ResultApp != null)
            {
                _settings.ExternalApps.Add(picker.ResultApp);
                _configService.Save(_settings);
                RefreshAppsList();
            }
        }

        private void AddApp_Click(object sender, RoutedEventArgs e)
        {
            // This method is now replaced by AddStartApp_Click and AddStopApp_Click
            // Keep for backwards compatibility
            AddStartApp_Click(sender, e);
        }

        private void EditApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ExternalApp app)
                return;

            var dialog = new ExternalAppDialog(app, _settings, app.AppType);
            if (dialog.ShowDialog() == true && dialog.ResultApp != null)
            {
                // Update the app in place
                var index = _settings.ExternalApps.IndexOf(app);
                if (index >= 0)
                {
                    _settings.ExternalApps[index] = dialog.ResultApp;
                    _configService.Save(_settings);
                    RefreshAppsList();
                }
            }
        }

        private void DeleteApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ExternalApp app)
                return;

            // Remove without confirmation
            _settings.ExternalApps.Remove(app);
            _configService.Save(_settings);
            RefreshAppsList();
        }

        private async void TestApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not ExternalApp app)
                return;

            var success = await _iRacingMonitor.StartApp(app);
            if (success)
            {
                MessageBox.Show($"'{app.Name}' started successfully (PID: {app.ProcessId})",
                    "Test Launch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Failed to start '{app.Name}'",
                    "Test Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
