using SharpDX.DirectInput;

namespace SimControlCentre.Services;

/// <summary>
/// Service for detecting and reading DirectInput devices (joysticks, button boxes)
/// </summary>
public class DirectInputService : IDisposable
{
    private readonly DirectInput _directInput;
    private readonly Dictionary<Guid, Joystick> _devices = new();
    private readonly Dictionary<Guid, JoystickState> _previousStates = new();
    private System.Timers.Timer? _pollTimer;
    
    public event EventHandler<ButtonPressedEventArgs>? ButtonPressed;
    public event EventHandler<ButtonReleasedEventArgs>? ButtonReleased;

    public DirectInputService()
    {
        _directInput = new DirectInput();
    }

    /// <summary>
    /// Scans for and returns all connected DirectInput devices
    /// </summary>
    public List<DeviceInfo> GetConnectedDevices()
    {
        var devices = new List<DeviceInfo>();
        
        Console.WriteLine("[DirectInput] Scanning for devices...");
        
        // Scan for ALL device types BUT filter out keyboards and mice
        foreach (var deviceInstance in _directInput.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AllDevices))
        {
            // Skip keyboards and mice - they interfere with normal keyboard input
            if (deviceInstance.Type == DeviceType.Keyboard || 
                deviceInstance.Type == DeviceType.Mouse ||
                deviceInstance.ProductName.Contains("Keyboard", StringComparison.OrdinalIgnoreCase) ||
                deviceInstance.ProductName.Contains("Mouse", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[DirectInput] Skipping keyboard/mouse: {deviceInstance.ProductName}");
                continue;
            }
            
            Console.WriteLine($"[DirectInput] Found: {deviceInstance.ProductName} (Type: {deviceInstance.Type})");
            
            devices.Add(new DeviceInfo
            {
                InstanceGuid = deviceInstance.InstanceGuid,
                ProductGuid = deviceInstance.ProductGuid,
                Name = deviceInstance.ProductName,
                Type = deviceInstance.Type.ToString()
            });
        }
        
        Console.WriteLine($"[DirectInput] Total devices found: {devices.Count}");
        
        return devices;
    }

    /// <summary>
    /// Initializes and starts monitoring specific devices
    /// </summary>
    public bool InitializeDevice(Guid deviceGuid, IntPtr windowHandle)
    {
        try
        {
            var joystick = new Joystick(_directInput, deviceGuid);
            joystick.SetCooperativeLevel(windowHandle, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
            joystick.Acquire();
            
            _devices[deviceGuid] = joystick;
            _previousStates[deviceGuid] = joystick.GetCurrentState();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DirectInput] Failed to initialize device {deviceGuid}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts polling all initialized devices
    /// </summary>
    public void StartPolling(int intervalMs = 50)
    {
        _pollTimer = new System.Timers.Timer(intervalMs);
        _pollTimer.Elapsed += (s, e) => PollDevices();
        _pollTimer.Start();
        
        Console.WriteLine($"[DirectInput] Started polling {_devices.Count} device(s) every {intervalMs}ms");
    }

    /// <summary>
    /// Stops polling devices
    /// </summary>
    public void StopPolling()
    {
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _pollTimer = null;
        
        Console.WriteLine("[DirectInput] Stopped polling");
    }

    /// <summary>
    /// Polls all devices and fires events for button changes
    /// </summary>
    private void PollDevices()
    {
        foreach (var kvp in _devices)
        {
            var deviceGuid = kvp.Key;
            var joystick = kvp.Value;

            try
            {
                joystick.Poll();
                var currentState = joystick.GetCurrentState();
                var previousState = _previousStates[deviceGuid];

                // Check all buttons (DirectInput supports up to 128 buttons)
                for (int i = 0; i < currentState.Buttons.Length; i++)
                {
                    bool currentPressed = currentState.Buttons[i];
                    bool previousPressed = previousState.Buttons[i];

                    // Button pressed
                    if (currentPressed && !previousPressed)
                    {
                        ButtonPressed?.Invoke(this, new ButtonPressedEventArgs
                        {
                            DeviceGuid = deviceGuid,
                            ButtonNumber = i + 1 // 1-based for user display
                        });
                    }
                    // Button released
                    else if (!currentPressed && previousPressed)
                    {
                        ButtonReleased?.Invoke(this, new ButtonReleasedEventArgs
                        {
                            DeviceGuid = deviceGuid,
                            ButtonNumber = i + 1
                        });
                    }
                }

                _previousStates[deviceGuid] = currentState;
            }
            catch (SharpDX.SharpDXException)
            {
                // Device disconnected or error - try to reacquire
                try
                {
                    joystick.Acquire();
                }
                catch
                {
                    // Ignore reacquisition failures
                }
            }
        }
    }

    /// <summary>
    /// Gets the current state of all buttons on a device
    /// </summary>
    public bool[] GetButtonStates(Guid deviceGuid)
    {
        if (_devices.TryGetValue(deviceGuid, out var joystick))
        {
            try
            {
                joystick.Poll();
                var state = joystick.GetCurrentState();
                return state.Buttons;
            }
            catch
            {
                return Array.Empty<bool>();
            }
        }
        return Array.Empty<bool>();
    }

    public void Dispose()
    {
        StopPolling();
        
        foreach (var joystick in _devices.Values)
        {
            joystick?.Unacquire();
            joystick?.Dispose();
        }
        
        _devices.Clear();
        _previousStates.Clear();
        _directInput?.Dispose();
    }
}

/// <summary>
/// Information about a detected device
/// </summary>
public class DeviceInfo
{
    public Guid InstanceGuid { get; set; }
    public Guid ProductGuid { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Event args for button press
/// </summary>
public class ButtonPressedEventArgs : EventArgs
{
    public Guid DeviceGuid { get; set; }
    public int ButtonNumber { get; set; }
}

/// <summary>
/// Event args for button release
/// </summary>
public class ButtonReleasedEventArgs : EventArgs
{
    public Guid DeviceGuid { get; set; }
    public int ButtonNumber { get; set; }
}
