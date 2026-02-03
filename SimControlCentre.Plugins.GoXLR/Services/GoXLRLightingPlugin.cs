using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Plugins.GoXLR.Models;
using SimControlCentre.Contracts;

namespace SimControlCentre.Plugins.GoXLR.Services
{
    /// <summary>
    /// Plugin for GoXLR lighting device
    /// </summary>
    public class GoXLRLightingPlugin : ILightingPlugin
    {
        private readonly GoXLRService _goXLRService;
        
        private List<string> _selectedButtons = new();
        private string _deviceType = "Full"; // Default to Full
        private IPluginContext? _context;

        public string PluginId => "goxlr";
        public string DisplayName => "GoXLR";
        public string Description => "GoXLR fader mute button LEDs";
        public string Version => "1.0.0";
        public string Author => "SimControlCentre";
        public bool IsEnabled { get; set; } = true;

        public void Initialize(IPluginContext context)
        {
            _context = context;
            context.LogInfo("GoXLR Lighting Plugin", "Initialized");
        }

        public void Shutdown()
        {
            _context?.LogInfo("GoXLR Lighting Plugin", "Shutdown");
        }

        public object? GetConfigurationControl()
        {
            // TODO: Return WPF UserControl for button selection
            return null;
        }

    // All available buttons (will be filtered by device type)
    // Note: Sampler buttons are virtual (software) and not useful for lighting
    private static readonly List<string> FullSizeButtons = new()
    {
        // Global Color Controls (affects all LEDs)
        "Global", "Accent",
        
        // Fader Display Colors (scribble strip colors)
        "FaderA", "FaderB", "FaderC", "FaderD",
        
        // Fader Mute Buttons (physical hardware - best for lighting)
        "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute",
        
        // Function Buttons (physical hardware)
        "Bleep", "Cough",
        
        // Effect Selection Buttons (physical hardware - Full-size only)
        "EffectSelect1", "EffectSelect2", "EffectSelect3",
        "EffectSelect4", "EffectSelect5", "EffectSelect6",
        
        // Effect Type Buttons (physical hardware - Full-size only)
        "EffectFx", "EffectMegaphone", "EffectRobot", "EffectHardTune",
    };

    private static readonly List<string> MiniButtons = new()
    {
        // Global Color Controls (affects all LEDs)
        "Global", "Accent",
        
        // Fader Display Colors (scribble strip colors - Mini has 4)
        "FaderA", "FaderB", "FaderC", "FaderD",
        
        // Fader Mute Buttons (physical hardware - Mini has 4 faders!)
        "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute",
        
        // Function Buttons (physical hardware)
        "Bleep", "Cough",
    };

    // Display name mapping for user-friendly labels
    private static readonly Dictionary<string, string> ButtonDisplayNames = new()
    {
        { "Global", "Global" },
        { "Accent", "Accent" },
        { "FaderA", "Fader 1" },
        { "FaderB", "Fader 2" },
        { "FaderC", "Fader 3" },
        { "FaderD", "Fader 4" },
        { "Fader1Mute", "Fader 1 Mute" },
        { "Fader2Mute", "Fader 2 Mute" },
        { "Fader3Mute", "Fader 3 Mute" },
        { "Fader4Mute", "Fader 4 Mute" },
        { "Bleep", "Bleep" },
        { "Cough", "Cough" },
        { "EffectSelect1", "Effect Select 1" },
        { "EffectSelect2", "Effect Select 2" },
        { "EffectSelect3", "Effect Select 3" },
        { "EffectSelect4", "Effect Select 4" },
        { "EffectSelect5", "Effect Select 5" },
        { "EffectSelect6", "Effect Select 6" },
        { "EffectFx", "Effect FX" },
        { "EffectMegaphone", "Effect Megaphone" },
        { "EffectRobot", "Effect Robot" },
        { "EffectHardTune", "Effect Hard Tune" },
    };

    // Get display name for a button ID
    public static string GetDisplayName(string buttonId)
    {
        return ButtonDisplayNames.TryGetValue(buttonId, out var displayName) 
            ? displayName 
            : buttonId;
    }

        // Exposed list of available buttons based on device type
        public List<string> AvailableButtons { get; private set; } = new();

        public GoXLRLightingPlugin(GoXLRService goXLRService)
        {
            _goXLRService = goXLRService;
            
            // Start with Mini buttons (safer default)
            AvailableButtons = new List<string>(MiniButtons);
            
            // Initialize with detection
            _ = Task.Run(async () => await DetectDeviceTypeAsync());
        }

        private async Task DetectDeviceTypeAsync()
        {
            _deviceType = await _goXLRService.GetDeviceTypeAsync();
            
            // Update available buttons based on device type
            AvailableButtons = _deviceType == "Mini" 
                ? new List<string>(MiniButtons) 
                : new List<string>(FullSizeButtons);
            
            // Update default selection based on device type
            _selectedButtons = _deviceType == "Mini"
                ? new List<string> { "Fader1Mute", "Fader2Mute", "Fader3Mute" }
                : new List<string> { "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute" };
            
            Logger.Info("GoXLR Plugin", $"Detected device type: {_deviceType}");
            Logger.Info("GoXLR Plugin", $"Available buttons: {string.Join(", ", AvailableButtons)}");
        }

        public async Task<bool> IsHardwareAvailableAsync()
        {
            // Don't wait for connection - GoXLR might still be warming up
            // Just return true if GoXLR service exists
            return await Task.FromResult(_goXLRService != null);
        }

        public ILightingDevice CreateDevice()
        {
            return new GoXLRLightingDevice(_goXLRService, _context, _selectedButtons);
        }

        public IEnumerable<DeviceConfigOption> GetConfigOptions()
        {
            return new List<DeviceConfigOption>
            {
                new DeviceConfigOption
                {
                    Key = "selected_buttons",
                    DisplayName = "Active Buttons",
                    Description = "Select which GoXLR buttons to use for flag lighting",
                    Type = DeviceConfigType.MultiSelect,
                    DefaultValue = _selectedButtons,
                    AvailableOptions = AvailableButtons
                }
            };
        }

        public void ApplyConfiguration(Dictionary<string, object> config)
        {
            if (config.TryGetValue("selected_buttons", out var buttonsObj) && buttonsObj is List<string> buttons)
            {
                _selectedButtons = buttons;
                Logger.Info("GoXLR Plugin", $"Updated button selection: {string.Join(", ", buttons)}");
            }
        }
    }
}



