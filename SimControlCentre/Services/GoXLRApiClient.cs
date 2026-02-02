using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SimControlCentre.Models;

namespace SimControlCentre.Services;

/// <summary>
/// Client for communicating with GoXLR Utility API
/// </summary>
public class GoXLRApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly int _cacheTimeMs;
    private Timer? _connectionWarmupTimer;
    private bool _isConnectionWarmed = false;
    
    // Volume cache
    private readonly Dictionary<string, CachedVolume> _volumeCache = new();
    private DateTime _lastFullRefresh = DateTime.MinValue;
    
    // Device status cache
    private GoXLRDevice? _cachedDeviceStatus;
    private DateTime _lastStatusRefresh = DateTime.MinValue;

    public GoXLRApiClient(string apiEndpoint, int cacheTimeMs = 5000)
    {
        _apiEndpoint = apiEndpoint;
        _cacheTimeMs = cacheTimeMs;
        
        // Use SocketsHttpHandler for better connection pooling
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10,
            EnableMultipleHttp2Connections = true
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiEndpoint),
            Timeout = TimeSpan.FromMilliseconds(2000) // Fast timeout for normal operations
        };
        
        // Start aggressive connection warming with longer timeout
        StartConnectionWarming();
    }

    private void StartConnectionWarming()
    {
        GoXLRDiagnostics.Info("GoXLRApiClient", "Starting connection warming...");
        
        // Warm connection immediately and aggressively on startup
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 10; i++) // Try 10 times (up to 50 seconds total)
            {
                GoXLRDiagnostics.Debug("GoXLRApiClient", $"Warmup attempt {i + 1}/10");
                var success = await WarmConnectionAsync(isInitialWarmup: true);
                if (success)
                {
                    GoXLRDiagnostics.Info("GoXLRApiClient", $"Connection warmed successfully on attempt {i + 1}");
                    break;
                }
                
                // Wait longer between attempts if failing (exponential backoff)
                var delay = Math.Min(1000 * (i + 1), 5000); // 1s, 2s, 3s, 4s, 5s max
                GoXLRDiagnostics.Debug("GoXLRApiClient", $"Waiting {delay}ms before next attempt...");
                await Task.Delay(delay);
            }
            
            if (!_isConnectionWarmed)
            {
                GoXLRDiagnostics.Warning("GoXLRApiClient", "Failed to warm connection after 10 attempts. GoXLR Utility may not be running.");
            }
        });
        
        // Keep connection warm with periodic pings every 5 seconds (more frequent)
        _connectionWarmupTimer = new Timer(async _ =>
        {
            await WarmConnectionAsync(isInitialWarmup: false);
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    private async Task<bool> WarmConnectionAsync(bool isInitialWarmup)
    {
        try
        {
            // Use longer timeout for initial warmup, shorter for keepalive
            using var cts = new CancellationTokenSource(isInitialWarmup ? 10000 : 3000);
            
            var response = await _httpClient.GetAsync("/api/get-devices", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                _isConnectionWarmed = true;
                // Read and discard response to ensure connection is fully established
                _ = await response.Content.ReadAsStringAsync();
                return true;
            }
        }
        catch (TaskCanceledException)
        {
            GoXLRDiagnostics.Warning("GoXLRApiClient", $"Connection warming timeout ({(isInitialWarmup ? "10s initial" : "3s keepalive")})");
            _isConnectionWarmed = false;
        }
        catch (Exception ex)
        {
            GoXLRDiagnostics.Error("GoXLRApiClient", $"Connection warming failed: {ex.Message}");
            _isConnectionWarmed = false;
        }
        return false;
    }

    public bool IsConnectionWarmed => _isConnectionWarmed;

    /// <summary>
    /// Gets the current device status including volumes and profiles
    /// </summary>
    public async Task<GoXLRDevice?> GetDeviceStatusAsync(string serialNumber)
    {
        try
        {
            // Return cached status if still valid
            if (_cachedDeviceStatus != null && 
                (DateTime.Now - _lastStatusRefresh).TotalMilliseconds < _cacheTimeMs)
            {
                Console.WriteLine($"[GoXLR] Using cached device status");
                return _cachedDeviceStatus;
            }

            Console.WriteLine($"[GoXLR] Fetching device status from API...");
            var response = await _httpClient.GetFromJsonAsync<GoXLRFullResponse>("/api/get-devices");
            
            if (response != null)
            {
                Console.WriteLine($"[GoXLR] Found {response.Mixers.Count} mixer(s)");
                
                if (response.Mixers.TryGetValue(serialNumber, out var device))
                {
                    Console.WriteLine($"[GoXLR] Found device with serial: {serialNumber}");
                    Console.WriteLine($"[GoXLR] Current profile: {device.ProfileName}");
                    Console.WriteLine($"[GoXLR] Volume count: {device.Levels.Volumes.Count}");
                    
                    _cachedDeviceStatus = device;
                    _lastStatusRefresh = DateTime.Now;
                    
                    // Update volume cache from device status
                    UpdateVolumeCacheFromDevice(device);
                    
                    return device;
                }
                else
                {
                    Console.WriteLine($"[GoXLR] Serial '{serialNumber}' not found. Available serials: {string.Join(", ", response.Mixers.Keys)}");
                }
            }
            else
            {
                Console.WriteLine($"[GoXLR] API returned null response");
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[GoXLR] Failed to connect to GoXLR Utility: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] Error getting device status: {ex.Message}");
            Console.WriteLine($"[GoXLR] Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Gets the volume for a specific channel (with caching)
    /// </summary>
    public async Task<int?> GetVolumeAsync(string serialNumber, string channel)
    {
        Console.WriteLine($"[GoXLR] GetVolume for channel: {channel}");
        
        // Check cache first
        if (_volumeCache.TryGetValue(channel, out var cached) && 
            (DateTime.Now - cached.Timestamp).TotalMilliseconds < _cacheTimeMs)
        {
            Console.WriteLine($"[GoXLR] Using cached volume for {channel}: {cached.Volume}");
            return cached.Volume;
        }

        Console.WriteLine($"[GoXLR] Cache miss/expired for {channel}, fetching from device");
        
        // Cache miss or expired - refresh from device with retry logic
        int maxRetries = 2;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var device = await GetDeviceStatusAsync(serialNumber);
                if (device?.Levels.Volumes.TryGetValue(channel, out var volume) == true)
                {
                    Console.WriteLine($"[GoXLR] Got volume for {channel}: {volume}");
                    _volumeCache[channel] = new CachedVolume { Volume = volume, Timestamp = DateTime.Now };
                    return volume;
                }
                
                Console.WriteLine($"[GoXLR] Failed to get volume for channel: {channel}");
                if (device != null)
                {
                    Console.WriteLine($"[GoXLR] Available channels: {string.Join(", ", device.Levels.Volumes.Keys)}");
                }
                return null; // Channel not found, don't retry
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoXLR] Attempt {attempt}/{maxRetries} failed for {channel}: {ex.Message}");
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"[GoXLR] Retrying in 500ms...");
                    await Task.Delay(500);
                }
            }
        }
        
        Console.WriteLine($"[GoXLR] All retry attempts failed for {channel}");
        return null;
    }

    /// <summary>
    /// Sets the volume for a specific channel
    /// </summary>
    public async Task<bool> SetVolumeAsync(string serialNumber, string channel, int volume)
    {
        try
        {
            // Clamp volume to valid range
            volume = Math.Clamp(volume, 0, 255);

            Console.WriteLine($"[GoXLR] SetVolume - Channel: {channel}, Volume: {volume}, Serial: {serialNumber}");
            
            var command = GoXLRCommandRequest.SetVolume(serialNumber, channel, volume);
            
            // Serialize to see what we're sending
            var json = System.Text.Json.JsonSerializer.Serialize(command);
            Console.WriteLine($"[GoXLR] Sending command: {json}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            
            Console.WriteLine($"[GoXLR] Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Response body: {responseBody}");
                
                // Update cache immediately
                _volumeCache[channel] = new CachedVolume { Volume = volume, Timestamp = DateTime.Now };
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Error response: {errorBody}");
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] Error setting volume: {ex.Message}");
            Console.WriteLine($"[GoXLR] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Adjusts volume by a delta amount (for hotkey up/down)
    /// </summary>
    public async Task<int?> AdjustVolumeAsync(string serialNumber, string channel, int delta)
    {
        Console.WriteLine($"[GoXLR] AdjustVolume - Channel: {channel}, Delta: {delta}");
        
        var currentVolume = await GetVolumeAsync(serialNumber, channel);
        if (currentVolume == null)
        {
            Console.WriteLine($"[GoXLR] Failed to get current volume for {channel}");
            return null;
        }

        Console.WriteLine($"[GoXLR] Current volume: {currentVolume.Value}");
        
        var newVolume = Math.Clamp(currentVolume.Value + delta, 0, 255);
        Console.WriteLine($"[GoXLR] New volume (after clamp): {newVolume}");
        
        var success = await SetVolumeAsync(serialNumber, channel, newVolume);

        return success ? newVolume : null;
    }

    /// <summary>
    /// Loads a profile by name
    /// </summary>
    public async Task<bool> LoadProfileAsync(string serialNumber, string profileName)
    {
        try
        {
            var command = GoXLRCommandRequest.LoadProfile(serialNumber, profileName);
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);

            if (response.IsSuccessStatusCode)
            {
                // Invalidate caches since profile changed
                InvalidateCache();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading profile: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the list of available profiles
    /// </summary>
    public async Task<List<string>> GetProfilesAsync(string serialNumber)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<GoXLRFullResponse>("/api/get-devices");
            return response?.Files.Profiles ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Gets the currently active profile name
    /// </summary>
    public async Task<string?> GetCurrentProfileAsync(string serialNumber)
    {
        var device = await GetDeviceStatusAsync(serialNumber);
        return device?.ProfileName;
    }

    /// <summary>
    /// Checks if GoXLR Utility is running and accessible
    /// </summary>
    public async Task<bool> IsConnectedAsync()
    {
        // Only return true if connection is actually warmed
        if (!_isConnectionWarmed)
        {
            return false;
        }
        
        try
        {
            using var cts = new CancellationTokenSource(2000); // 2 second timeout
            var response = await _httpClient.GetAsync("/api/get-devices", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Invalidates all caches (call after profile change)
    /// </summary>
    public void InvalidateCache()
    {
        _volumeCache.Clear();
        _cachedDeviceStatus = null;
        _lastStatusRefresh = DateTime.MinValue;
        _lastFullRefresh = DateTime.MinValue;
    }

    /// <summary>
    /// Sets a button color on the GoXLR
    /// </summary>
    public async Task<bool> SetButtonColourAsync(string serialNumber, string buttonId, string colourOne, string? colourTwo = null)
    {
        try
        {
            Console.WriteLine($"[GoXLR] SetButtonColour - Button: {buttonId}, Colour1: {colourOne}, Colour2: {colourTwo ?? "null"}");
            
            var command = GoXLRCommandRequest.SetButtonColours(serialNumber, buttonId, colourOne, colourTwo);
            
            var json = System.Text.Json.JsonSerializer.Serialize(command);
            Console.WriteLine($"[GoXLR] Sending command: {json}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            
            Console.WriteLine($"[GoXLR] Response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Response body: {responseBody}");
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Error response: {errorBody}");
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] Error setting button color: {ex.Message}");
            Console.WriteLine($"[GoXLR] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Sets a simple color (Global or Accent)
    /// </summary>
    public async Task<bool> SetSimpleColorAsync(string serialNumber, string target, string colour)
    {
        try
        {
            Console.WriteLine($"[GoXLR] SetSimpleColor - Target: {target}, Colour: {colour}");
            
            var command = GoXLRCommandRequest.SetSimpleColor(serialNumber, target, colour);
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GoXLR] Simple color set successfully");
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Error response: {errorBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] Error setting simple color: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the global color (affects all LEDs)
    /// </summary>
    public async Task<bool> SetGlobalColourAsync(string serialNumber, string colour)
    {
        try
        {
            Console.WriteLine($"[GoXLR] ========================================");
            Console.WriteLine($"[GoXLR] SetGlobalColour CALLED");
            Console.WriteLine($"[GoXLR] Serial: {serialNumber}");
            Console.WriteLine($"[GoXLR] Colour: {colour}");
            
            var command = GoXLRCommandRequest.SetGlobalColour(serialNumber, colour);
            var json = System.Text.Json.JsonSerializer.Serialize(command);
            Console.WriteLine($"[GoXLR] JSON: {json}");
            
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            Console.WriteLine($"[GoXLR] HTTP Status: {response.StatusCode}");
            
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GoXLR] Response Body: {responseBody}");
            Console.WriteLine($"[GoXLR] ========================================");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GoXLR] ? Global color set successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"[GoXLR] ? Failed to set global color");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] ? Exception setting global color: {ex.Message}");
            Console.WriteLine($"[GoXLR] Stack: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Sets fader colors
    /// </summary>
    public async Task<bool> SetFaderColorsAsync(string serialNumber, string faderName, string colourOne, string colourTwo)
    {
        try
        {
            Console.WriteLine($"[GoXLR] SetFaderColors - Fader: {faderName}, Colour1: {colourOne}, Colour2: {colourTwo}");
            
            var command = GoXLRCommandRequest.SetFaderColors(serialNumber, faderName, colourOne, colourTwo);
            var response = await _httpClient.PostAsJsonAsync("/api/command", command);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[GoXLR] Fader colors set successfully");
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[GoXLR] Error response: {errorBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoXLR] Error setting fader colors: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates volume cache from device status
    /// </summary>
    private void UpdateVolumeCacheFromDevice(GoXLRDevice device)
    {
        var now = DateTime.Now;
        foreach (var kvp in device.Levels.Volumes)
        {
            _volumeCache[kvp.Key] = new CachedVolume 
            { 
                Volume = kvp.Value, 
                Timestamp = now 
            };
        }
        _lastFullRefresh = now;
    }

    public void Dispose()
    {
        GoXLRDiagnostics.Info("GoXLRApiClient", "Disposing connection and stopping warmup timer");
        
        // Stop warmup timer first
        _connectionWarmupTimer?.Dispose();
        _connectionWarmupTimer = null;
        
        // Give warmup tasks a moment to complete
        Thread.Sleep(100);
        
        // Now dispose HttpClient
        _httpClient?.Dispose();
        
        _isConnectionWarmed = false;
    }

    private class CachedVolume
    {
        public int Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
