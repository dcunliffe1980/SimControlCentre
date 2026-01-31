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
        _apiClient?.Dispose();
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
        InitializeClient();
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
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
