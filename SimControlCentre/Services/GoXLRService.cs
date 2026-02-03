using System.Net.Http;
using System.Net.Http.Json;
using SimControlCentre.Models;

namespace SimControlCentre.Services;

/// <summary>
/// Service for managing GoXLR API client with application settings
/// </summary>
public class GoXLRService : IDisposable
{
    private readonly AppSettings _settings;
    private GoXLRApiClient? _apiClient;

    public GoXLRService(AppSettings settings)
    {
        _settings = settings;
        InitializeClient();
    }

    private void InitializeClient()
    {
        // Only initialize once - prevent multiple instances
        if (_apiClient != null)
        {
            Console.WriteLine("[GoXLRService] API client already initialized, skipping");
            return;
        }
        
        Console.WriteLine("[GoXLRService] Initializing new API client");
        _apiClient = new GoXLRApiClient(
            _settings.General.ApiEndpoint,
            _settings.General.VolumeCacheTimeMs
        );
    }

    /// <summary>
    /// Gets the configured serial number from settings
    /// </summary>
    private string SerialNumber => _settings.General.SerialNumber;

    /// <summary>
    /// Checks if serial number is configured
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SerialNumber);

    /// <summary>
    /// Get the GoXLR device type (Full or Mini) from hardware info
    /// Uses the device_type field which is most reliable
    /// </summary>
    public async Task<string> GetDeviceTypeAsync()
    {
        if (!IsConfigured || _apiClient == null)
            return "Unknown";

        try
        {
            var status = await _apiClient.GetDeviceStatusAsync(SerialNumber);
            if (status?.Hardware?.DeviceType != null)
            {
                var deviceType = status.Hardware.DeviceType;
                Logger.Info("GoXLR Service", $"Detected device type from hardware: {deviceType}");
                return deviceType;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GoXLR Service", "Error getting device type", ex);
        }

        // Default to Mini to be conservative (show fewer effect buttons)
        Logger.Warning("GoXLR Service", "Could not detect device type, defaulting to Mini");
        return "Mini";
    }

    /// <summary>
    /// Checks if the connection to GoXLR is warmed and ready
    /// </summary>
    public bool IsConnectionWarmed => _apiClient?.IsConnectionWarmed ?? false;

    /// <summary>
    /// Adjusts volume for a channel by the configured step amount
    /// </summary>
    public async Task<VolumeChangeResult> AdjustVolumeAsync(string channel, bool increase)
    {
        // Auto-detect serial if not configured and only one device exists
        if (!IsConfigured)
        {
            var autoSerial = await TryAutoDetectSerialAsync();
            if (string.IsNullOrEmpty(autoSerial))
                return new VolumeChangeResult { Success = false, Message = "Serial number not configured. Click 'Detect' to auto-detect." };
            
            // Temporarily use auto-detected serial
            _settings.General.SerialNumber = autoSerial;
            Reinitialize();
        }

        if (_apiClient == null)
            return new VolumeChangeResult { Success = false, Message = "API client not initialized" };

        var delta = increase ? _settings.General.VolumeStep : -_settings.General.VolumeStep;
        var newVolume = await _apiClient.AdjustVolumeAsync(SerialNumber, channel, delta);

        if (newVolume.HasValue)
        {
            var percentage = (int)Math.Round((newVolume.Value / 255.0) * 100);
            return new VolumeChangeResult 
            { 
                Success = true, 
                NewVolume = newVolume.Value,
                Percentage = percentage,
                Message = $"{channel}: {percentage}%"
            };
        }

        return new VolumeChangeResult { Success = false, Message = "Failed to adjust volume. Check if GoXLR Utility is running and serial number is correct." };
    }
    
    
    /// <summary>
    /// Pre-warms the volume cache for a channel to avoid first-press delays
    /// </summary>
    public async Task WarmVolumeCacheAsync(string channel)
    {
        if (_apiClient == null || !IsConfigured)
            return;
        
        try
        {
            _ = await _apiClient.GetVolumeAsync(SerialNumber, channel);
        }
        catch
        {
            // Silently ignore cache warming failures
        }
    }

    /// <summary>
    /// Pre-warms the button color API to avoid first-press delays
    /// Makes a dummy call to establish HTTP connection and cache
    /// </summary>
    public async Task WarmButtonColorApiAsync()
    {
        if (_apiClient == null || !IsConfigured)
        {
            Logger.Warning("GoXLR Service", "Cannot warm button API - not configured");
            return;
        }

        try
        {
            Logger.Info("GoXLR Service", "Warming button color API...");
            
            // Get current device status to establish HTTP connection
            var status = await _apiClient.GetDeviceStatusAsync(SerialNumber);
            
            if (status != null)
            {
                Logger.Info("GoXLR Service", "? Button color API connection established and ready");
            }
            else
            {
                Logger.Warning("GoXLR Service", "Button API warmup returned null status");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning("GoXLR Service", $"Could not warm button API: {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to auto-detect serial number if only one device exists
    /// </summary>
    private async Task<string?> TryAutoDetectSerialAsync()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetFromJsonAsync<GoXLRFullResponse>(_settings.General.ApiEndpoint + "/api/get-devices");
            
            if (response?.Mixers != null && response.Mixers.Count == 1)
            {
                var serial = response.Mixers.Keys.First();
                Console.WriteLine($"[GoXLRService] Auto-detected serial number: {serial}");
                return serial;
            }
            else if (response?.Mixers != null && response.Mixers.Count > 1)
            {
                Console.WriteLine($"[GoXLRService] Multiple devices found, cannot auto-detect. Use 'Detect' button.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLRService] Auto-detect failed: {ex.Message}");
        }
        
        return null;
    }

    /// <summary>
    /// Sets volume for a channel to a specific value
    /// </summary>
    public async Task<bool> SetVolumeAsync(string channel, int volume)
    {
        if (!IsConfigured || _apiClient == null)
            return false;

        return await _apiClient.SetVolumeAsync(SerialNumber, channel, volume);
    }

    /// <summary>
    /// Gets current volume for a channel
    /// </summary>
    public async Task<int?> GetVolumeAsync(string channel)
    {
        if (!IsConfigured || _apiClient == null)
            return null;

        return await _apiClient.GetVolumeAsync(SerialNumber, channel);
    }

    /// <summary>
    /// Loads a profile by name
    /// </summary>
    public async Task<bool> LoadProfileAsync(string profileName)
    {
        if (!IsConfigured || _apiClient == null)
            return false;

        return await _apiClient.LoadProfileAsync(SerialNumber, profileName);
    }

    /// <summary>
    /// Gets list of available profiles
    /// </summary>
    public async Task<List<string>> GetProfilesAsync()
    {
        if (!IsConfigured || _apiClient == null)
            return new List<string>();

        return await _apiClient.GetProfilesAsync(SerialNumber);
    }

    /// <summary>
    /// Gets currently active profile name
    /// </summary>
    public async Task<string?> GetCurrentProfileAsync()
    {
        if (!IsConfigured || _apiClient == null)
            return null;

        return await _apiClient.GetCurrentProfileAsync(SerialNumber);
    }

    /// <summary>
    /// Checks if GoXLR Utility is running and accessible
    /// </summary>
    public async Task<bool> IsConnectedAsync()
    {
        if (_apiClient == null)
            return false;

        return await _apiClient.IsConnectedAsync();
    }

    /// <summary>
    /// Reinitializes the client (call after settings change)
    /// </summary>
    public void Reinitialize()
    {
        Console.WriteLine("[GoXLRService] Reinitialize called - disposing old client and creating new one");
        _apiClient?.Dispose();
        _apiClient = null;
        InitializeClient();
    }

    /// <summary>
    /// Set button color (for lighting integration)
    /// Supports buttons, global/accent colors, and fader colors
    /// </summary>
    public async Task SetButtonColorAsync(string buttonId, string color)
    {
        if (!IsConfigured)
        {
            Logger.Warning("GoXLR Service", "Cannot set button color - not configured");
            return;
        }

        try
        {
            Logger.Info("GoXLR Service", $"SetButtonColorAsync called: buttonId={buttonId}, color={color}");
            
            if (_apiClient != null)
            {
                // Check if it's the global color
                if (buttonId == "Global")
                {
                    Logger.Info("GoXLR Service", "========================================");
                    Logger.Info("GoXLR Service", "? GLOBAL COLOR DETECTED!");
                    Logger.Info("GoXLR Service", $"? Setting Global to: {color}");
                    Logger.Info("GoXLR Service", $"? Serial: {SerialNumber}");
                    Logger.Info("GoXLR Service", "========================================");
                    
                    Console.WriteLine($"[GoXLR Service] ??? GLOBAL DETECTED: {color} ???");
                    await _apiClient.SetGlobalColourAsync(SerialNumber, color);
                    
                    Logger.Info("GoXLR Service", "? SetGlobalColourAsync completed");
                }
                // Check if it's accent (uses SetSimpleColour with Accent target)
                else if (buttonId == "Accent")
                {
                    Logger.Info("GoXLR Service", $"Accent color detected, setting to {color}");
                    await _apiClient.SetSimpleColorAsync(SerialNumber, "Accent", color);
                    Logger.Info("GoXLR Service", "? SetSimpleColorAsync completed");
                }
                // Check if it's a fader color
                else if (buttonId.StartsWith("Fader") && buttonId.Length == 6) // FaderA, FaderB, etc.
                {
                    var faderName = buttonId.Substring(5); // Extract "A", "B", "C", or "D"
                    await _apiClient.SetFaderColorsAsync(SerialNumber, faderName, color, color);
                }
                // Regular button
                else
                {
                    await _apiClient.SetButtonColourAsync(SerialNumber, buttonId, color, color);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GoXLR Service", $"Error setting button color", ex);
        }
    }

    public void Dispose()
    {
        Console.WriteLine("[GoXLRService] Disposing service");
        _apiClient?.Dispose();
        _apiClient = null;
    }
}

/// <summary>
/// Result of a volume change operation
/// </summary>
public class VolumeChangeResult
{
    public bool Success { get; set; }
    public int NewVolume { get; set; }
    public int Percentage { get; set; }
    public string Message { get; set; } = string.Empty;
}
