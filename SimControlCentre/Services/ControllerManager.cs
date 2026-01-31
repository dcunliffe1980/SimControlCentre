using SimControlCentre.Models;

namespace SimControlCentre.Services;

/// <summary>
/// Manages controller input and maps buttons to GoXLR actions
/// </summary>
public class ControllerManager : IDisposable
{
    private readonly DirectInputService _directInputService;
    private readonly GoXLRService _goXLRService;
    private readonly AppSettings _settings;
    private readonly Dictionary<string, DateTime> _lastButtonPress = new();
    private const int DEBOUNCE_MS = 200; // Prevent double-triggers

    public ControllerManager(DirectInputService directInputService, GoXLRService goXLRService, AppSettings settings)
    {
        _directInputService = directInputService;
        _goXLRService = goXLRService;
        _settings = settings;

        // Subscribe to button events
        _directInputService.ButtonPressed += OnButtonPressed;
        _directInputService.ButtonReleased += OnButtonReleased;
    }

    /// <summary>
    /// Initialize and start monitoring all configured controllers
    /// </summary>
    public void InitializeControllers(IntPtr windowHandle)
    {
        Console.WriteLine("[ControllerManager] Initializing controllers...");

        // Get all connected devices and initialize them all
        var devices = _directInputService.GetConnectedDevices();
        Console.WriteLine($"[ControllerManager] Found {devices.Count} device(s)");

        foreach (var device in devices)
        {
            Console.WriteLine($"[ControllerManager] Device: {device.Name} ({device.InstanceGuid})");
            
            // Initialize device
            if (_directInputService.InitializeDevice(device.InstanceGuid, windowHandle))
            {
                Console.WriteLine($"[ControllerManager] Initialized: {device.Name}");
            }
        }

        // Start polling
        _directInputService.StartPolling(50); // Poll every 50ms
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        // Debounce using ProductGuid for consistency
        var key = $"{e.ProductGuid}_{e.ButtonNumber}";
        if (_lastButtonPress.TryGetValue(key, out var lastPress))
        {
            if ((DateTime.Now - lastPress).TotalMilliseconds < DEBOUNCE_MS)
                return;
        }
        _lastButtonPress[key] = DateTime.Now;

        Console.WriteLine($"[ControllerManager] Button {e.ButtonNumber} pressed on {e.DeviceName} (Product: {e.ProductGuid})");

        // Check Volume Hotkeys for button mappings - format is DeviceName:{ProductGuid}:Button:{number}
        // We match on ProductGuid (position 1) and ButtonNumber (position 3)
        
        foreach (var channelKvp in _settings.VolumeHotkeys)
        {
            var channel = channelKvp.Key;
            var hotkeys = channelKvp.Value;

            if (!string.IsNullOrWhiteSpace(hotkeys.VolumeUpButton) && MatchesButton(hotkeys.VolumeUpButton, e.ProductGuid, e.ButtonNumber))
            {
                Console.WriteLine($"[ControllerManager] Executing Volume Up for {channel}");
                _ = _goXLRService.AdjustVolumeAsync(channel, true);
                return;
            }

            if (!string.IsNullOrWhiteSpace(hotkeys.VolumeDownButton) && MatchesButton(hotkeys.VolumeDownButton, e.ProductGuid, e.ButtonNumber))
            {
                Console.WriteLine($"[ControllerManager] Executing Volume Down for {channel}");
                _ = _goXLRService.AdjustVolumeAsync(channel, false);
                return;
            }
        }

        // Check Profile Buttons
        foreach (var profileKvp in _settings.ProfileButtons)
        {
            if (!string.IsNullOrWhiteSpace(profileKvp.Value) && MatchesButton(profileKvp.Value, e.ProductGuid, e.ButtonNumber))
            {
                Console.WriteLine($"[ControllerManager] Loading profile: {profileKvp.Key}");
                _ = _goXLRService.LoadProfileAsync(profileKvp.Key);
                return;
            }
        }

        // Fallback: Check old ControllerMappings format
        var mapping = _settings.ControllerMappings.FirstOrDefault(m =>
            m.ButtonNumber == e.ButtonNumber);

        if (mapping != null)
        {
            ExecuteAction(mapping);
        }
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        // Could handle button release for non-toggle actions here
    }
    
    /// <summary>
    /// Checks if a button string matches the given ProductGuid and button number
    /// Format: DeviceName:{ProductGuid}:Button:{number}
    /// </summary>
    private bool MatchesButton(string buttonString, Guid productGuid, int buttonNumber)
    {
        var parts = buttonString.Split(':');
        if (parts.Length < 4)
            return false;
        
        // Check ProductGuid (position 1)
        if (!Guid.TryParse(parts[1], out var storedProductGuid))
            return false;
        
        if (storedProductGuid != productGuid)
            return false;
        
        // Check button number (position 3)
        if (!int.TryParse(parts[3], out var storedButtonNumber))
            return false;
        
        return storedButtonNumber == buttonNumber;
    }

    /// <summary>
    /// Executes the GoXLR action for a controller mapping
    /// </summary>
    private async void ExecuteAction(ControllerMapping mapping)
    {
        Console.WriteLine($"[ControllerManager] Executing action for button {mapping.ButtonNumber}: {mapping.Description}");

        // Parse the keyboard key to determine action
        // Format: "Volume:Game:Up" or "Profile:iRacing" or "Hotkey:Ctrl+F1"
        var parts = mapping.KeyboardKey.Split(':');
        
        if (parts.Length < 2)
        {
            Console.WriteLine($"[ControllerManager] Invalid action format: {mapping.KeyboardKey}");
            return;
        }

        var actionType = parts[0];

        try
        {
            switch (actionType.ToLower())
            {
                case "volume":
                    if (parts.Length >= 3)
                    {
                        var channel = parts[1];
                        var direction = parts[2].ToLower();
                        var increase = direction == "up";
                        
                        var result = await _goXLRService.AdjustVolumeAsync(channel, increase);
                        if (result.Success)
                        {
                            Console.WriteLine($"[ControllerManager] {result.Message}");
                        }
                    }
                    break;

                case "profile":
                    var profileName = parts[1];
                    var success = await _goXLRService.LoadProfileAsync(profileName);
                    if (success)
                    {
                        Console.WriteLine($"[ControllerManager] Loaded profile: {profileName}");
                    }
                    break;

                case "hotkey":
                    // Could trigger a keyboard hotkey here if needed
                    Console.WriteLine($"[ControllerManager] Hotkey action not yet implemented: {parts[1]}");
                    break;

                default:
                    Console.WriteLine($"[ControllerManager] Unknown action type: {actionType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ControllerManager] Error executing action: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _directInputService.ButtonPressed -= OnButtonPressed;
        _directInputService.ButtonReleased -= OnButtonReleased;
        _directInputService?.Dispose();
    }
}
