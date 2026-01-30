using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using SimControlCentre.Models;
using SimControlCentre.Services;
using SimControlCentre.Views;

namespace SimControlCentre;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ConfigurationService _configService;
    private readonly GoXLRService _goXLRService;

    public MainWindow(AppSettings settings, ConfigurationService configService, GoXLRService goXLRService)
    {
        _settings = settings;
        _configService = configService;
        _goXLRService = goXLRService;

        InitializeComponent();

        // Set initial values
        UpdateSerialNumberDisplay(_settings.General.SerialNumber);
        ConfigPathText.Text = _configService.GetConfigFilePath();

        // Restore window position and size
        RestoreWindowSettings();

        // Handle window state changes
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
        
        // Check connection on startup (but only if serial is configured)
        if (!string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
        {
            // Delay to allow API warm-up from App.xaml.cs
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000); // Wait for warm-up
                await Dispatcher.InvokeAsync(async () => await CheckConnectionAsync());
            });
        }
        else
        {
            // Show waiting message if auto-detect will run
            ConnectionStatusText.Text = "Connection Status: Waiting for auto-detect...";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        
        // Show hotkey status
        UpdateHotkeyStatus();
    }

    public void UpdateSerialNumberDisplay(string serialNumber)
    {
        SerialNumberBox.Text = serialNumber;
        _settings.General.SerialNumber = serialNumber;
    }

    public async Task TestConnectionAfterAutoDetect()
    {
        // Update status to show we're testing
        ConnectionStatusText.Text = "Connection Status: Testing connection...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Brief delay to let UI update
        await Task.Delay(500);
        
        // Test connection
        await CheckConnectionAsync();
        
        // Show result notification
        if (ConnectionStatusText.Foreground == System.Windows.Media.Brushes.Green)
        {
            var app = (App)Application.Current;
            app.ShowVolumeNotification($"✓ Connected to GoXLR - Ready!");
        }
    }

    private async Task SaveAndTestSerial(string serialNumber)
    {
        // Update status to show we're saving
        ConnectionStatusText.Text = "Connection Status: Saving and testing...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Reload current settings from disk
        var currentSettings = _configService.Load();
        
        // Update serial number
        currentSettings.General.SerialNumber = serialNumber;
        
        // Save back to file
        _configService.Save(currentSettings);
        
        // Update in-memory settings
        _settings.General.SerialNumber = currentSettings.General.SerialNumber;
        
        // No need to Reinitialize - Settings object is shared, serial will be picked up automatically
        // Brief delay to let UI update
        await Task.Delay(500);
        
        // Test connection
        await CheckConnectionAsync();
        
        // Show result
        if (ConnectionStatusText.Foreground == System.Windows.Media.Brushes.Green)
        {
            MessageBox.Show($"✓ Connected to GoXLR successfully!\n\nSerial: {serialNumber}", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Serial saved but connection test failed.\n\nSerial: {serialNumber}\n\nMake sure GoXLR Utility is running.", 
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateHotkeyStatus()
    {
        var status = "Configured Hotkeys:\n\n";
        
        int count = 0;
        foreach (var channel in _settings.VolumeHotkeys)
        {
            if (!string.IsNullOrWhiteSpace(channel.Value.VolumeUp))
            {
                status += $"{channel.Key} Up: {channel.Value.VolumeUp}\n";
                count++;
            }
            if (!string.IsNullOrWhiteSpace(channel.Value.VolumeDown))
            {
                status += $"{channel.Key} Down: {channel.Value.VolumeDown}\n";
                count++;
            }
        }
        
        foreach (var profile in _settings.ProfileHotkeys)
        {
            if (!string.IsNullOrWhiteSpace(profile.Value))
            {
                status += $"Profile '{profile.Key}': {profile.Value}\n";
                count++;
            }
        }
        
        if (count == 0)
        {
            status += "No hotkeys configured.\n";
            status += "Edit config file to add hotkeys.";
        }
        else
        {
            status += $"\nTotal: {count} hotkey(s)";
        }
        
        HotkeyStatusText.Text = status;
    }

    private void ReloadHotkeys_Click(object sender, RoutedEventArgs e)
    {
        // Reload settings from disk
        var newSettings = _configService.Load();
        
        // Copy new settings to app settings
        App.GetSettings().VolumeHotkeys.Clear();
        foreach (var kvp in newSettings.VolumeHotkeys)
            App.GetSettings().VolumeHotkeys[kvp.Key] = kvp.Value;
            
        App.GetSettings().ProfileHotkeys.Clear();
        foreach (var kvp in newSettings.ProfileHotkeys)
            App.GetSettings().ProfileHotkeys[kvp.Key] = kvp.Value;
        
        // Re-register hotkeys
        var hotkeyManager = App.GetHotkeyManager();
        if (hotkeyManager != null)
        {
            var count = hotkeyManager.RegisterAllHotkeys();
            MessageBox.Show($"Reloaded {count} hotkey(s)", "Hotkeys Reloaded", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            UpdateHotkeyStatus();
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
        
        // No need to Reinitialize - Settings object is shared, serial will be picked up automatically
        // Brief delay to let UI update
        await Task.Delay(500);
        
        MessageBox.Show("Serial number saved successfully!", "Success", 
            MessageBoxButton.OK, MessageBoxImage.Information);
        
        // Re-check connection
        await CheckConnectionAsync();
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
                var fullResponse = System.Text.Json.JsonSerializer.Deserialize<Models.GoXLRFullResponse>(json);
                
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

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        await CheckConnectionAsync();
    }

    private async Task CheckConnectionAsync()
    {
        ConnectionStatusText.Text = "Connection Status: Checking...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Check if serial is configured
        if (string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
        {
            ConnectionStatusText.Text = "Connection Status: ⚠ Serial number not configured. Click 'Detect' to auto-detect.";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        var isConnected = await _goXLRService.IsConnectedAsync();
        
        if (isConnected)
        {
            var profile = await _goXLRService.GetCurrentProfileAsync();
            if (profile != null)
            {
                ConnectionStatusText.Text = $"Connection Status: ✓ Connected (Profile: {profile})";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ConnectionStatusText.Text = "Connection Status: ✓ Connected (Check serial number)";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }
        else
        {
            ConnectionStatusText.Text = "Connection Status: ✗ GoXLR Utility not running";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void TestGoXLR_Click(object sender, RoutedEventArgs e)
    {
        var testWindow = new GoXLRTestWindow(_goXLRService, _settings);
        testWindow.Owner = this;
        testWindow.ShowDialog();
    }

    private void RestoreWindowSettings()
    {
        if (_settings.Window.Width > 0 && _settings.Window.Height > 0)
        {
            Width = _settings.Window.Width;
            Height = _settings.Window.Height;
        }

        // Ensure window is on screen
        if (_settings.Window.Left >= 0 && _settings.Window.Top >= 0)
        {
            Left = _settings.Window.Left;
            Top = _settings.Window.Top;
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Hide to tray when minimized
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Don't actually close, just hide to tray
        e.Cancel = true;
        Hide();
    }

    public void SaveWindowSettings()
    {
        // Reload settings from disk to preserve any manual edits
        var currentSettings = _configService.Load();
        
        // Only update window settings
        if (WindowState == WindowState.Normal)
        {
            currentSettings.Window.Width = Width;
            currentSettings.Window.Height = Height;
            currentSettings.Window.Left = Left;
            currentSettings.Window.Top = Top;
        }

        _configService.Save(currentSettings);
    }

    protected override void OnClosed(EventArgs e)
    {
        SaveWindowSettings();
        base.OnClosed(e);
    }
}
