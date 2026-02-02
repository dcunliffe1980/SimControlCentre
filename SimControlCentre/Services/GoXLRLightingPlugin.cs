using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Plugin for GoXLR lighting device
    /// </summary>
    public class GoXLRLightingPlugin : ILightingDevicePlugin
    {
        private readonly GoXLRService _goXLRService;
        private readonly AppSettings _settings;
        private List<string> _selectedButtons = new();
        private string _deviceType = "Full"; // Default to Full

        public string PluginId => "goxlr";
        public string DisplayName => "GoXLR";
        public string Description => "GoXLR fader mute button LEDs";
        public bool IsEnabled { get; set; } = true;

        // All available buttons (will be filtered by device type)
        // Note: Sampler buttons are virtual (software) and not useful for lighting
        private static readonly List<string> FullSizeButtons = new()
        {
            // Fader Mute Buttons (physical hardware - best for lighting)
            "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute",
            
            // Function Buttons (physical hardware)
            "Bleep", "Cough",
            
            // Effect Selection Buttons (physical hardware - Full-size only)
            "EffectSelect1", "EffectSelect2", "EffectSelect3",
            "EffectSelect4", "EffectSelect5", "EffectSelect6",
            
            // Effect Type Buttons (physical hardware - Full-size only)
            "EffectFx", "EffectMegaphone", "EffectRobot", "EffectHardTune",
            
            // Global Color Controls (affects all LEDs)
            "Global", "Accent",
            
            // Fader Display Colors (scribble strip colors)
            "FaderA", "FaderB", "FaderC", "FaderD"
        };

        private static readonly List<string> MiniButtons = new()
        {
            // Fader Mute Buttons (physical hardware)
            "Fader1Mute", "Fader2Mute", "Fader3Mute",
            
            // Function Buttons (physical hardware)
            "Bleep", "Cough",
            
            // Global Color Controls (affects all LEDs)
            "Global", "Accent",
            
            // Fader Display Colors (scribble strip colors - Mini has 3)
            "FaderA", "FaderB", "FaderC"
        };

        // Exposed list of available buttons based on device type
        public static List<string> AvailableButtons { get; private set; } = FullSizeButtons;

        public GoXLRLightingPlugin(GoXLRService goXLRService, AppSettings settings)
        {
            _goXLRService = goXLRService;
            _settings = settings;
            
            // Initialize with detection
            _ = Task.Run(async () => await DetectDeviceTypeAsync());
        }

        private async Task DetectDeviceTypeAsync()
        {
            _deviceType = await _goXLRService.GetDeviceTypeAsync();
            
            // Update available buttons based on device type
            AvailableButtons = _deviceType == "Mini" ? MiniButtons : FullSizeButtons;
            
            // Default selection based on device type
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
            return new GoXLRLightingDevice(_goXLRService, _settings, _selectedButtons);
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
