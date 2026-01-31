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
        private readonly GoXLRService _goXLRService;
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SimControlCentre";

        public GeneralTab(GoXLRService goXLRService, ConfigurationService configService, AppSettings settings)
        {
            InitializeComponent();
            
            _goXLRService = goXLRService;
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
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await httpClient.GetAsync("http://localhost:14564/api/get-devices");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var fullResponse = System.Text.Json.JsonSerializer.Deserialize<GoXLRFullResponse>(json);
                    
                    if (fullResponse?.Mixers != null && fullResponse.Mixers.Count > 0)
                    {
                        var serialNumbers = fullResponse.Mixers.Keys.ToList();
                        
                        if (serialNumbers.Count == 1)
                        {
                            // Auto-populate single device
                            SerialNumberBox.Text = serialNumbers[0];
                            
                            // Automatically save and test
                            await SaveAndTestSerial(serialNumbers[0]);
                        }
                        else
                        {
                            // Multiple devices - let user choose
                            var deviceList = string.Join("\n", serialNumbers);
                            var result = MessageBox.Show(
                                $"Found {serialNumbers.Count} GoXLR devices:\n\n{deviceList}\n\nUsing first device. Click OK to accept or Cancel to enter manually.",
                                "Multiple Devices Found",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Question);
                            
                            if (result == MessageBoxResult.OK)
                            {
                                SerialNumberBox.Text = serialNumbers[0];
                                
                                // Automatically save and test
                                await SaveAndTestSerial(serialNumbers[0]);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("No GoXLR devices found. Make sure your GoXLR is connected and GoXLR Utility is running.",
                            "No Devices Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Unable to connect to GoXLR Utility.\n\nMake sure GoXLR Utility is running on http://localhost:14564",
                        "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error detecting serial number:\n\n{ex.Message}\n\nMake sure GoXLR Utility is running.",
                    "Detection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DetectSerialBtn.IsEnabled = true;
                DetectSerialBtn.Content = "Detect";
            }
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
            
            var isConnected = await _goXLRService.IsConnectedAsync();
            
            if (isConnected)
            {
                var profile = await _goXLRService.GetCurrentProfileAsync();
                if (profile != null)
                {
                    ConnectionStatusText.Text = $"Connection Status: ? Connected (Profile: {profile})";
                    ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    ConnectionStatusText.Text = "Connection Status: ? Connected (Check serial number)";
                    ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                }
            }
            else
            {
                ConnectionStatusText.Text = "Connection Status: ? GoXLR Utility not running";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
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
