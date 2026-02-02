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
        // Check if hardware is connected/available
        return await CheckDeviceConnected();
    }

    public ILightingDevice CreateDevice()
    {
        // Create and return device instance
        return new MyLightingDevice(_config);
    }

    public IEnumerable<DeviceConfigOption> GetConfigOptions()
    {
        // Return list of configuration options
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
        // Apply configuration changes
        if (config.TryGetValue("brightness", out var brightness))
        {
            _brightness = (int)brightness;
        }
    }
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

---

**Status**: Implemented ?  
**Version**: 1.0  
**Last Updated**: February 2026

