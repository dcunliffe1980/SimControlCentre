using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views;

/// <summary>
/// Test window for GoXLR API functionality
/// </summary>
public partial class GoXLRTestWindow : Window
{
    private readonly GoXLRService _goXLRService;
    private readonly AppSettings _settings;

    public GoXLRTestWindow(GoXLRService goXLRService, AppSettings settings)
    {
        _goXLRService = goXLRService;
        _settings = settings;
        InitializeComponent();
        
        Loaded += async (s, e) => await RefreshStatus();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Testing connection...";
        var isConnected = await _goXLRService.IsConnectedAsync();
        StatusText.Text = isConnected 
            ? "? Connected to GoXLR Utility" 
            : "? GoXLR Utility not running";
    }

    private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatus();
    }

    private async void TestRawAPI_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "=== Testing Raw API ===\n";
        StatusText.Text += "Checking if GoXLR Utility is running...\n";
        
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            StatusText.Text += $"Connecting to: {_settings.General.ApiEndpoint}/api/get-devices\n";
            var response = await httpClient.GetAsync($"{_settings.General.ApiEndpoint}/api/get-devices");
            
            StatusText.Text += $"Status: {response.StatusCode}\n\n";
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                
                // Pretty print the JSON
                var formatted = System.Text.Json.JsonSerializer.Serialize(
                    System.Text.Json.JsonSerializer.Deserialize<object>(json),
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                StatusText.Text += $"Raw JSON response:\n{formatted}\n";
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                StatusText.Text += $"Failed with status: {response.StatusCode}\n";
                StatusText.Text += $"Response body: {body}\n";
            }
        }
        catch (TaskCanceledException)
        {
            StatusText.Text += "\n? TIMEOUT: GoXLR Utility not responding\n";
            StatusText.Text += "\nIs GoXLR Utility running?\n";
            StatusText.Text += "Check: http://localhost:14564 in your browser\n";
        }
        catch (HttpRequestException ex)
        {
            StatusText.Text += $"\n? CONNECTION ERROR: {ex.Message}\n";
            StatusText.Text += "\nGoXLR Utility is not running or not accessible.\n";
            StatusText.Text += "Make sure TC-Helicon GoXLR Utility is started.\n";
        }
        catch (Exception ex)
        {
            StatusText.Text += $"\n? Error: {ex.Message}\n";
            StatusText.Text += $"Type: {ex.GetType().Name}\n";
        }
    }

    private async Task RefreshStatus()
    {
        StatusText.Text = "Loading...";
        
        var currentProfile = await _goXLRService.GetCurrentProfileAsync();
        var profiles = await _goXLRService.GetProfilesAsync();
        
        if (currentProfile != null)
        {
            StatusText.Text = $"Current Profile: {currentProfile}\n";
            StatusText.Text += $"Available Profiles: {string.Join(", ", profiles)}\n\n";
            
            foreach (var channel in _settings.EnabledChannels)
            {
                var volume = await _goXLRService.GetVolumeAsync(channel);
                if (volume.HasValue)
                {
                    var percentage = (int)Math.Round((volume.Value / 255.0) * 100);
                    StatusText.Text += $"{channel}: {volume.Value}/255 ({percentage}%)\n";
                }
            }
        }
        else
        {
            StatusText.Text = "Unable to connect to GoXLR Utility.\n" +
                            "Make sure:\n" +
                            "1. GoXLR Utility is running\n" +
                            "2. Serial number is configured correctly";
        }
    }

    private async void VolumeUp_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelCombo.SelectedItem is ComboBoxItem item && item.Content is string channel)
        {
            StatusText.Text = $"=== Volume Up for {channel} ===\n";
            StatusText.Text += $"Step 1: Getting current volume...\n";
            
            // Get current volume first to show it
            var currentVol = await _goXLRService.GetVolumeAsync(channel);
            if (currentVol.HasValue)
            {
                StatusText.Text += $"? Current volume: {currentVol.Value}/255\n";
            }
            else
            {
                StatusText.Text += $"? Failed to get current volume\n";
            }
            
            StatusText.Text += $"\nStep 2: Adjusting volume...\n";
            var result = await _goXLRService.AdjustVolumeAsync(channel, increase: true);
            
            if (result.Success)
            {
                StatusText.Text += $"? SUCCESS: {result.Message}\n";
                StatusText.Text += $"? New volume: {result.NewVolume}/255 ({result.Percentage}%)\n";
                await Task.Delay(500);
                await RefreshStatus();
            }
            else
            {
                StatusText.Text += $"\n? FAILED: {result.Message}\n";
                StatusText.Text += $"\nDiagnostics:\n";
                StatusText.Text += $"- Serial configured: {!string.IsNullOrEmpty(_settings.General.SerialNumber)}\n";
                StatusText.Text += $"- Serial value: '{_settings.General.SerialNumber}'\n";
                StatusText.Text += $"- API endpoint: {_settings.General.ApiEndpoint}\n";
            }
        }
    }

    private async void VolumeDown_Click(object sender, RoutedEventArgs e)
    {
        if (ChannelCombo.SelectedItem is ComboBoxItem item && item.Content is string channel)
        {
            StatusText.Text = $"=== Volume Down for {channel} ===\n";
            StatusText.Text += $"Step 1: Getting current volume...\n";
            
            // Get current volume first to show it
            var currentVol = await _goXLRService.GetVolumeAsync(channel);
            if (currentVol.HasValue)
            {
                StatusText.Text += $"? Current volume: {currentVol.Value}/255\n";
            }
            else
            {
                StatusText.Text += $"? Failed to get current volume\n";
            }
            
            StatusText.Text += $"\nStep 2: Adjusting volume...\n";
            var result = await _goXLRService.AdjustVolumeAsync(channel, increase: false);
            
            if (result.Success)
            {
                StatusText.Text += $"? SUCCESS: {result.Message}\n";
                StatusText.Text += $"? New volume: {result.NewVolume}/255 ({result.Percentage}%)\n";
                await Task.Delay(500);
                await RefreshStatus();
            }
            else
            {
                StatusText.Text += $"\n? FAILED: {result.Message}\n";
                StatusText.Text += $"\nDiagnostics:\n";
                StatusText.Text += $"- Serial configured: {!string.IsNullOrEmpty(_settings.General.SerialNumber)}\n";
                StatusText.Text += $"- Serial value: '{_settings.General.SerialNumber}'\n";
                StatusText.Text += $"- API endpoint: {_settings.General.ApiEndpoint}\n";
            }
        }
    }

    private async void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCombo.SelectedItem is ComboBoxItem item && item.Content is string profile)
        {
            StatusText.Text = $"Loading profile: {profile}...";
            var success = await _goXLRService.LoadProfileAsync(profile);
            if (success)
            {
                StatusText.Text = $"? Profile loaded: {profile}";
                await Task.Delay(500);
                await RefreshStatus();
            }
            else
            {
                StatusText.Text = $"? Failed to load profile: {profile}";
            }
        }
    }
}
