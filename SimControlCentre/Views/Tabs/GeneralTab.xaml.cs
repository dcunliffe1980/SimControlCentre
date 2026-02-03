using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class GeneralTab : UserControl
    {
        // GoXLR now in plugins
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SimControlCentre";

        public GeneralTab( ConfigurationService configService, AppSettings settings)
        {
            InitializeComponent();
            _configService = configService;
            _settings = settings;
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load serial number
            SerialNumberBox.Text = _settings.General.SerialNumber;
            
            // Load volume settings
            VolumeStepBox.Text = _settings.General.VolumeStep.ToString();
            CacheTimeBox.Text = _settings.General.VolumeCacheTimeMs.ToString();
            
            // Load window settings
            StartMinimizedCheckBox.IsChecked = _settings.Window.StartMinimized;
            StartWithWindowsCheckBox.IsChecked = IsStartWithWindowsEnabled();
        }

        private async void DetectSerial_Click(object sender, RoutedEventArgs e)
        {
            DetectSerialBtn.IsEnabled = false;
            DetectSerialBtn.Content = "Detecting...";
            
            try
            {
                // TODO: GoXLR detection moved to plugin
                MessageBox.Show("GoXLR auto-detection has been moved to the plugin system.\n\nPlease enter your serial number manually.", 
                    "Feature Moved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DetectSerialBtn.IsEnabled = true;
                DetectSerialBtn.Content = "Auto-Detect";
            }
            await Task.CompletedTask;
        }

        private async Task SaveAndTestSerial(string serialNumber)
        {
            // Reload current settings from disk
            var currentSettings = _configService.Load();
            

            
            // Update serial number
            currentSettings.General.SerialNumber = serialNumber;
            
            // Save back to file
            _configService.Save(currentSettings);
            
            // Update in-memory settings
            _settings.General.SerialNumber = currentSettings.General.SerialNumber;
            
            // Brief delay to let UI update
            await Task.Delay(500);
            
            // Test connection
            await CheckConnectionAsync();
            
            // Show result
            if (ConnectionStatusText.Foreground == System.Windows.Media.Brushes.Green)
            {
                MessageBox.Show($"? Connected to GoXLR successfully!\n\nSerial: {serialNumber}", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Serial saved but connection test failed.\n\nSerial: {serialNumber}\n\nMake sure GoXLR Utility is running.", 
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SaveSerial_Click(object sender, RoutedEventArgs e)
        {
            // Update status
            ConnectionStatusText.Text = "Connection Status: Saving and testing...";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            // Reload current settings from disk
            var currentSettings = _configService.Load();
            
            // Update serial number
            currentSettings.General.SerialNumber = SerialNumberBox.Text.Trim();
            
            // Save back to file
            _configService.Save(currentSettings);
            
            // Update in-memory settings
            _settings.General.SerialNumber = currentSettings.General.SerialNumber;
            
            // Brief delay to let UI update
            await Task.Delay(500);
            
            MessageBox.Show("Serial number saved successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Re-check connection
            await CheckConnectionAsync();
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            await CheckConnectionAsync();
        }

        public async Task CheckConnectionAsync()
        {
            ConnectionStatusText.Text = "Connection Status: Checking...";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            
            // Check if serial is configured
            if (string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
            {
                ConnectionStatusText.Text = "Connection Status: ? Serial number not configured. Click 'Detect' to auto-detect.";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                return;
            }
            
            // TODO: Connection check moved to plugin - need plugin API to expose connection status
            ConnectionStatusText.Text = "Connection Status: GoXLR functionality moved to plugin";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            await Task.CompletedTask;
        }

        private void SaveVolumeSettings_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (!int.TryParse(VolumeStepBox.Text, out var volumeStep) || volumeStep < 1 || volumeStep > 255)
            {
                MessageBox.Show("Volume Step must be between 1 and 255", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(CacheTimeBox.Text, out var cacheTime) || cacheTime < 0)
            {
                MessageBox.Show("Cache Time must be a positive number", "Invalid Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update settings
            _settings.General.VolumeStep = volumeStep;
            _settings.General.VolumeCacheTimeMs = cacheTime;
            
            // Update window settings
            _settings.Window.StartMinimized = StartMinimizedCheckBox.IsChecked == true;

            // Save to file
            _configService.Save(_settings);

            MessageBox.Show("Volume settings saved successfully!\n\nRestart the app for cache time changes to take effect.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StartWithWindows_Changed(object sender, RoutedEventArgs e)
        {
            if (StartWithWindowsCheckBox.IsChecked == true)
            {
                EnableStartWithWindows();
            }
            else
            {
                DisableStartWithWindows();
            }
            
            // Save to settings
            _settings.Window.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
            _configService.Save(_settings);
        }

        private bool IsStartWithWindowsEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
                var value = key?.GetValue(AppName) as string;
                return !string.IsNullOrEmpty(value);
            }
            catch
            {
                return false;
            }
        }

        private void EnableStartWithWindows()
        {
            try
            {
                var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                    return;
                
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                key?.SetValue(AppName, $"\"{exePath}\"");
                
                Console.WriteLine($"[GeneralTab] Enabled start with Windows: {exePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeneralTab] Failed to enable start with Windows: {ex.Message}");
                MessageBox.Show($"Failed to enable start with Windows.\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableStartWithWindows()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                key?.DeleteValue(AppName, false);
                
                Console.WriteLine("[GeneralTab] Disabled start with Windows");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeneralTab] Failed to disable start with Windows: {ex.Message}");
            }
        }
    }
}





