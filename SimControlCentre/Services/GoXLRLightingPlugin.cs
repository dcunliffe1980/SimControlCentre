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

        public string PluginId => "goxlr";
        public string DisplayName => "GoXLR";
        public string Description => "GoXLR fader mute button LEDs";
        public bool IsEnabled { get; set; } = true;

        // Available GoXLR buttons
        public static readonly List<string> AvailableButtons = new()
        {
            "Fader1Mute",
            "Fader2Mute",
            "Fader3Mute",
            "Fader4Mute",
            "Bleep",
            "Cough"
        };

        public GoXLRLightingPlugin(GoXLRService goXLRService, AppSettings settings)
        {
            _goXLRService = goXLRService;
            _settings = settings;
            
            // Default to all fader buttons
            _selectedButtons = new List<string> { "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute" };
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
