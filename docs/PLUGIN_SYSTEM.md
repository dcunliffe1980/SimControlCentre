# Lighting Device Plugin System

## Overview

The lighting device plugin system provides a modular architecture for adding support for different lighting hardware (GoXLR, Philips Hue, Nanoleaf, etc.) without modifying core code.

## Architecture

```
ILightingDevicePlugin (interface)
??? Defines plugin contract
??? Configuration management
??? Device creation

Concrete Plugins
??? GoXLRLightingPlugin
??? PhilipsHueLightingPlugin (future)
??? NanoleafLightingPlugin (future)

LightingService
??? Plugin registration
??? Plugin discovery
??? Device lifecycle management
??? Flag coordination

LightingTab (UI)
??? Plugin configuration
??? Device selection
??? Test controls
```

## Architecture Principles

### Single Source of Truth Pattern

The plugin system follows the principle that **core services are the single source of truth** for hardware state:

```
GoXLRService (Core)
??? Manages connection to GoXLR Utility
??? Handles warmup and health checks
??? Provides API access to all features
??? Single point of failure handling

GoXLRLightingPlugin (Extension)
??? Receives GoXLRService instance
??? Does NOT check hardware availability
??? Trusts the core service
??? API calls fail gracefully if disconnected
```

### Why Plugins Don't Check Hardware

**Problem with duplicate checking:**
```csharp
// ? BAD: Duplicate connection checking
public async Task InitializeAsync()
{
    if (await plugin.IsHardwareAvailableAsync()) // Redundant!
    {
        var device = plugin.CreateDevice();
    }
}
```

**Issues:**
- Race conditions (GoXLR might connect during check)
- Duplicate code (same logic in multiple places)
- Slow initialization (waiting for multiple timeouts)
- False negatives (hardware available but check fails)

**Better approach:**
```csharp
// ? GOOD: Always create, fail gracefully
public async Task InitializeAsync()
{
    var device = plugin.CreateDevice();
    // Device handles connection state internally
}
```

**Benefits:**
- Single connection check in GoXLRService
- Devices work whenever hardware becomes available
- Faster initialization
- More reliable

### Graceful Failure Pattern

```csharp
public async Task SetButtonColorAsync(string buttonId, string color)
{
    if (!IsConfigured)
    {
        Logger.Warning("Not configured");
        return; // Graceful exit
    }

    try
    {
        await _apiClient.SetButtonColourAsync(...);
    }
    catch (Exception ex)
    {
        Logger.Error("Error setting button color", ex);
        // Doesn't crash - just logs
    }
}
```

The device **always exists** but operations **gracefully fail** if hardware unavailable.

## Creating a New Plugin

### 1. Implement ILightingDevicePlugin

```csharp
public class MyDevicePlugin : ILightingDevicePlugin
{
    public string PluginId => "my-device"; // Unique identifier
    public string DisplayName => "My Device";
    public string Description => "My awesome lighting device";
    public bool IsEnabled { get; set; } = true;

    public async Task<bool> IsHardwareAvailableAsync()
    {
        // IMPORTANT: Only use this for device DISCOVERY
        // Not for connection checking!
        // 
        // Good use case: Philips Hue bridge discovery
        // Bad use case: Checking if shared service is connected
        //
        // For devices using a shared service (like GoXLRService),
        // just return true - the service handles connection state
        return await Task.FromResult(true);
    }

    public ILightingDevice CreateDevice()
    {
        // Always create the device
        // It will handle connection failures gracefully
        return new MyLightingDevice(_config);
    }

    public IEnumerable<DeviceConfigOption> GetConfigOptions()
    {
        return new List<DeviceConfigOption>
        {
            new DeviceConfigOption
            {
                Key = "brightness",
                DisplayName = "Brightness",
                Description = "LED brightness level",
                Type = DeviceConfigType.Integer,
                DefaultValue = 100
            }
        };
    }

    public void ApplyConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("brightness", out var brightness))
        {
            _brightness = (int)brightness;
        }
    }
}
```

### When to Use IsHardwareAvailableAsync

**? Use for device DISCOVERY:**
```csharp
// Philips Hue: Scan network for bridges
public async Task<bool> IsHardwareAvailableAsync()
{
    var bridges = await ScanForHueBridges();
    return bridges.Any();
}

// Nanoleaf: Discover panels on network
public async Task<bool> IsHardwareAvailableAsync()
{
    return await DiscoverNanoleafDevices();
}
```

**? Don't use for CONNECTION checking:**
```csharp
// BAD: Duplicate connection check
public async Task<bool> IsHardwareAvailableAsync()
{
    return await _goXLRService.IsConnectedAsync(); // Redundant!
}

// GOOD: Trust the shared service
public async Task<bool> IsHardwareAvailableAsync()
{
    return await Task.FromResult(_goXLRService != null);
}
```



### 2. Implement ILightingDevice

```csharp
public class MyLightingDevice : ILightingDevice
{
    public string DeviceName => "My Device";
    public bool IsAvailable => /* check availability */;

    public async Task SetColorAsync(LightingColor color)
    {
        // Set solid color on device
    }

    public async Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs)
    {
        // Start flashing animation
    }

    public async Task StopFlashingAsync()
    {
        // Stop any flashing
    }

    public async Task SaveStateAsync()
    {
        // Save current state before flag changes
    }

    public async Task RestoreStateAsync()
    {
        // Restore previous state after flag clears
    }
}
```

### 3. Register Plugin

```csharp
// In App.xaml.cs
var myPlugin = new MyDevicePlugin();
_lightingService.RegisterPlugin(myPlugin);

// Initialize (creates devices from enabled plugins)
await _lightingService.InitializeAsync();
```

## Configuration System

### DeviceConfigOption Types

```csharp
public enum DeviceConfigType
{
    Boolean,      // On/off toggle
    String,       // Text input
    Integer,      // Number input
    MultiSelect   // Multiple choice (e.g., buttons)
}
```

### MultiSelect Example (GoXLR Buttons)

```csharp
new DeviceConfigOption
{
    Key = "selected_buttons",
    DisplayName = "Active Buttons",
    Description = "Select which buttons to use",
    Type = DeviceConfigType.MultiSelect,
    DefaultValue = new List<string> { "Button1", "Button2" },
    AvailableOptions = new List<string> { "Button1", "Button2", "Button3", "Button4" }
}
```

## GoXLR Plugin Example

### Features

- **Configurable Buttons**: Choose which fader/function buttons to use
- **Dynamic Selection**: UI automatically generates checkboxes
- **Real-time Updates**: Changes apply immediately
- **Saved Configuration**: Settings persist across restarts (future)

### Implementation

```csharp
public class GoXLRLightingPlugin : ILightingDevicePlugin
{
    private List<string> _selectedButtons = new() 
    { 
        "Fader1Mute", 
        "Fader2Mute", 
        "Fader3Mute", 
        "Fader4Mute" 
    };

    public ILightingDevice CreateDevice()
    {
        // Pass selected buttons to device
        return new GoXLRLightingDevice(_goXLRService, _settings, _selectedButtons);
    }

    public void ApplyConfiguration(Dictionary<string, object> config)
    {
        if (config.TryGetValue("selected_buttons", out var buttons) && buttons is List<string> list)
        {
            _selectedButtons = list;
            // Reinitialize device with new button selection
        }
    }
}
```

### Device Implementation

```csharp
public class GoXLRLightingDevice : ILightingDevice
{
    private readonly List<string> _activeButtons;

    public GoXLRLightingDevice(GoXLRService service, AppSettings settings, List<string> activeButtons)
    {
        _activeButtons = activeButtons;
    }

    public async Task SetColorAsync(LightingColor color)
    {
        var hexColor = MapToGoXLRColor(color);
        
        // Apply to all active buttons
        foreach (var button in _activeButtons)
        {
            await _service.SetButtonColorAsync(button, hexColor);
            await Task.Delay(50); // Rate limiting
        }
    }
}
```

## UI Integration

### Lighting Tab Structure

```
???????????????????????????????????????
? Settings                            ?
? ? Enable flag lighting             ?
???????????????????????????????????????

???????????????????????????????????????
? Active Buttons (GoXLR)              ?
? ? Fader1Mute  ? Fader2Mute         ?
? ? Fader3Mute  ? Fader4Mute         ?
? ? Bleep       ? Cough              ?
???????????????????????????????????????

???????????????????????????????????????
? Connected Devices                    ?
? ? GoXLR (4 buttons active)          ?
???????????????????????????????????????

???????????????????????????????????????
? Test Flag Lighting                   ?
? [Green] [Yellow] [Red] [Blue]       ?
? [White] [Checkered] [Clear]         ?
???????????????????????????????????????
```

### Dynamic Configuration UI

```csharp
private void LoadButtonSelection()
{
    ButtonSelectionPanel.Children.Clear();

    var plugin = _lightingService.Plugins.OfType<GoXLRLightingPlugin>().FirstOrDefault();
    
    foreach (var option in plugin.GetConfigOptions())
    {
        if (option.Type == DeviceConfigType.MultiSelect)
        {
            foreach (var choice in option.AvailableOptions)
            {
                var checkbox = new CheckBox
                {
                    Content = choice,
                    IsChecked = ((List<string>)option.DefaultValue).Contains(choice)
                };
                
                checkbox.Checked += (s, e) => ApplyConfiguration();
                ButtonSelectionPanel.Children.Add(checkbox);
            }
        }
    }
}
```

## Plugin Lifecycle

### 1. Registration Phase

```csharp
// App startup
_lightingService.RegisterPlugin(new GoXLRLightingPlugin(...));
_lightingService.RegisterPlugin(new PhilipsHuePlugin(...));
```

### 2. Initialization Phase

```csharp
await _lightingService.InitializeAsync();
// - Checks hardware availability
// - Creates device instances
// - Applies saved configuration
```

### 3. Runtime Phase

```csharp
// Flag changes trigger all devices
_telemetryService.FlagChanged += async (s, e) =>
{
    await _lightingService.UpdateForFlagAsync(e.NewFlag);
};
```

### 4. Configuration Changes

```csharp
// User changes button selection
plugin.ApplyConfiguration(new Dictionary<string, object>
{
    { "selected_buttons", new List<string> { "Fader1Mute", "Bleep" } }
});

// Reinitialize to apply changes
await _lightingService.InitializeAsync();
```

## Extending to Other Areas

### Where Else Plugins Would Be Useful

1. **Telemetry Providers**
   ```csharp
   ITelemetryProviderPlugin
   ??? iRacingTelemetryPlugin
   ??? AssettoCorsa Plugin
   ??? rFactor2Plugin
   ```

2. **Controller Input**
   ```csharp
   IControllerPlugin
   ??? DirectInputPlugin
   ??? XInputPlugin (Xbox)
   ??? DualSensePlugin (PS5)
   ```

3. **Audio Mixers**
   ```csharp
   IAudioMixerPlugin
   ??? GoXLRPlugin
   ??? WaveXLRPlugin
   ??? VoiceMeeterPlugin
   ```

4. **Notification Systems**
   ```csharp
   INotificationPlugin
   ??? DiscordPlugin
   ??? StreamDeckPlugin
   ??? OBSWebSocketPlugin
   ```

## Generic Plugin System

### Universal Plugin Interface

```csharp
public interface IPlugin
{
    string PluginId { get; }
    string DisplayName { get; }
    string Description { get; }
    string Version { get; }
    bool IsEnabled { get; set; }
    
    Task<bool> InitializeAsync();
    Task ShutdownAsync();
    IEnumerable<IConfigOption> GetConfigOptions();
    void ApplyConfiguration(Dictionary<string, object> config);
}

public interface IPluginManager
{
    void RegisterPlugin(IPlugin plugin);
    Task<bool> InitializeAllAsync();
    T? GetPlugin<T>() where T : class, IPlugin;
    IReadOnlyList<IPlugin> GetPlugins();
}
```

### Implementation Example

```csharp
public class PluginManager : IPluginManager
{
    private readonly List<IPlugin> _plugins = new();

    public void RegisterPlugin(IPlugin plugin)
    {
        _plugins.Add(plugin);
        Logger.Info("Plugin Manager", $"Registered: {plugin.DisplayName} v{plugin.Version}");
    }

    public async Task<bool> InitializeAllAsync()
    {
        foreach (var plugin in _plugins.Where(p => p.IsEnabled))
        {
            try
            {
                await plugin.InitializeAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Plugin Manager", $"Failed to initialize {plugin.DisplayName}", ex);
            }
        }
        return true;
    }

    public T? GetPlugin<T>() where T : class, IPlugin
    {
        return _plugins.OfType<T>().FirstOrDefault();
    }
}
```

## Benefits

### ? **Modularity**
- Add/remove devices without touching core code
- Each plugin is self-contained

### ? **Extensibility**
- Easy to add support for new hardware
- Community plugins possible

### ? **Configurability**
- Per-device configuration
- User-controlled options

### ? **Maintainability**
- Clear separation of concerns
- Easy to debug individual plugins

### ? **Testability**
- Mock plugins for testing
- Test devices in isolation

## Future Enhancements

### Plugin Discovery
```csharp
// Auto-discover plugins in /plugins directory
var pluginFiles = Directory.GetFiles("plugins", "*.dll");
foreach (var file in pluginFiles)
{
    var assembly = Assembly.LoadFrom(file);
    var pluginTypes = assembly.GetTypes()
        .Where(t => typeof(ILightingDevicePlugin).IsAssignableFrom(t));
    
    foreach (var type in pluginTypes)
    {
        var plugin = Activator.CreateInstance(type);
        _lightingService.RegisterPlugin(plugin);
    }
}
```

### Plugin Marketplace
- Download plugins from online repository
- Automatic updates
- Community-developed plugins

### Hot Reload
- Load/unload plugins at runtime
- Update plugins without restart

### Plugin Dependencies
- Manage plugin dependencies
- Version compatibility checking

## Design Decisions & Best Practices

### Why Not Make Volume Control a Plugin?

**Question**: Should volume control, profile switching, etc. also use the plugin system?

**Answer**: No - here's the reasoning:

#### Core vs Extension Features

**Core Features** (Direct usage):
```csharp
// These are fundamental GoXLR features
_hotkeyManager = new HotkeyManager(_goXLRService);
_profileManager = new ProfileManager(_goXLRService);
```

- Always available when GoXLR is present
- Not optional - users expect these to work
- Part of the core application functionality
- No need for enable/disable

**Extension Features** (Plugins):
```csharp
// These are optional enhancements
_lightingService.RegisterPlugin(new GoXLRLightingPlugin(_goXLRService));
_lightingService.RegisterPlugin(new PhilipsHuePlugin(...));
```

- Optional - users might not want them
- Can be enabled/disabled
- Extend core functionality
- Multiple implementations possible

### Single Source of Truth Architecture

```
???????????????????????????????????????
?        GoXLRService (Core)          ?
?  - Connection management            ?
?  - API access                       ?
?  - Health checking                  ?
???????????????????????????????????????
                  ?
                  ? (shared instance)
                  ?
        ??????????????????????
        ?                    ?
???????????????    ???????????????????
?  Core       ?    ?  Extensions     ?
?  Features   ?    ?  (Plugins)      ?
???????????????    ???????????????????
? - Volume    ?    ? - Lighting      ?
? - Profiles  ?    ? - Automations   ?
? - Routing   ?    ? - Integrations  ?
???????????????    ???????????????????
```

### Initialization Order

```csharp
// App.xaml.cs - Startup

// 1. Core service (always first)
_goXLRService = new GoXLRService(Settings);

// 2. Core features (use service directly)
_hotkeyManager = new HotkeyManager(_goXLRService);

// 3. Extension plugins (receive service)
_lightingService = new LightingService();
_lightingService.RegisterPlugin(new GoXLRLightingPlugin(_goXLRService, Settings));

// 4. Initialize plugins in background
_ = Task.Run(async () => await _lightingService.InitializeAsync());

// 5. Warmup happens once for everything
_ = Task.Run(async () => await WaitForGoXLRUtilityIndefinitely());
```

### Benefits of This Approach

? **No Duplicate Initialization**
- GoXLRService initializes once
- All features use the same instance
- Single warmup sequence

? **Clear Separation**
- Core = Always on, direct usage
- Plugins = Optional, can be disabled

? **Graceful Degradation**
- If GoXLR disconnects, core features retry
- Plugins fail gracefully
- No cascading failures

? **Easy to Understand**
- Clear which features are essential
- Clear which are extensions
- Easy to maintain

### Anti-Patterns to Avoid

? **Don't duplicate connection checking:**
```csharp
// BAD
if (await _goXLRService.IsConnectedAsync()) // Check 1
{
    if (await plugin.IsHardwareAvailableAsync()) // Check 2 - redundant!
    {
        var device = plugin.CreateDevice();
    }
}
```

? **Don't make core features optional:**
```csharp
// BAD - Volume control should always work
_volumeService.RegisterPlugin(new VolumePlugin(...));
```

? **Don't create separate service instances:**
```csharp
// BAD - Multiple instances
_goXLRService1 = new GoXLRService(...); // For volume
_goXLRService2 = new GoXLRService(...); // For lighting
```

? **Do share the single instance:**
```csharp
// GOOD - Single source of truth
_goXLRService = new GoXLRService(...);
_hotkeyManager = new HotkeyManager(_goXLRService);
_lightingPlugin = new GoXLRLightingPlugin(_goXLRService, Settings);
```

### Future Considerations

If you need to make core features pluggable later:

1. **Create a base plugin for the service itself:**
```csharp
public interface IAudioMixerPlugin : IPlugin
{
    IAudioMixerService CreateService();
}

// Then register different mixers
_mixerService = new AudioMixerService();
_mixerService.RegisterPlugin(new GoXLRMixerPlugin());
_mixerService.RegisterPlugin(new WaveXLRPlugin());
```

2. **Use a strategy pattern for the core:**
```csharp
public class VolumeController
{
    private IVolumeStrategy _strategy;
    
    public VolumeController(IVolumeStrategy strategy)
    {
        _strategy = strategy; // Could be GoXLR, WaveXLR, etc.
    }
}
```

But only do this **when you actually need multiple implementations** of core functionality.

---

**Status**: Implemented ?  
**Version**: 1.0  
**Last Updated**: February 2026



