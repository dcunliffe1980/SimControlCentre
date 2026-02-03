using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Plugins.GoXLR.Models;
using SimControlCentre.Contracts;

namespace SimControlCentre.Plugins.GoXLR.Services
{
    /// <summary>
    /// Device Control Plugin for GoXLR
    /// Provides profile switching, volume control, and channel muting
    /// </summary>
    public class GoXLRDeviceControlPlugin : IDeviceControlPlugin
    {
        private readonly GoXLRService _goXLRService;
        
        private IPluginContext? _context;
        
        public string PluginId => "goxlr-control";
        public string DisplayName => "GoXLR Device Control";
        public string Description => "Control GoXLR profiles, volume, and channel muting";
        public string Version => "1.0.0";
        public string Author => "SimControlCentre";
        public bool IsEnabled { get; set; } = true;

        public GoXLRDeviceControlPlugin(GoXLRService goXLRService)
        {
            _goXLRService = goXLRService;
            
        }

        public void Initialize(IPluginContext context)
        {
            _context = context;
            context.LogInfo("GoXLR Device Control Plugin", "Initialized");
        }

        public void Shutdown()
        {
            _context?.LogInfo("GoXLR Device Control Plugin", "Shutdown");
        }

        public object? GetConfigurationControl()
        {
            // TODO: Return WPF UserControl for hotkey configuration
            return null;
        }

        public void ApplyConfiguration(Dictionary<string, object> config)
        {
            // Configuration is handled by AppSettings
        }

        public List<DeviceAction> GetAvailableActions()
        {
            return new List<DeviceAction>
            {
                new DeviceAction
                {
                    Id = "switch_profile",
                    DisplayName = "Switch Profile",
                    Description = "Load a specific GoXLR profile",
                    Parameters = new List<ActionParameter>
                    {
                        new ActionParameter
                        {
                            Name = "profile_name",
                            DisplayName = "Profile Name",
                            Type = "string",
                            Required = true
                        }
                    }
                },
                new DeviceAction
                {
                    Id = "adjust_volume",
                    DisplayName = "Adjust Volume",
                    Description = "Increase or decrease channel volume",
                    Parameters = new List<ActionParameter>
                    {
                        new ActionParameter
                        {
                            Name = "channel",
                            DisplayName = "Channel",
                            Type = "choice",
                            Required = true,
                            Choices = new List<string> { "Music", "Game", "Chat", "System", "LineIn", "LineOut", "Mic", "Sample", "Headphones" }
                        },
                        new ActionParameter
                        {
                            Name = "increase",
                            DisplayName = "Increase",
                            Type = "bool",
                            Required = true,
                            DefaultValue = true
                        }
                    }
                },
                new DeviceAction
                {
                    Id = "set_volume",
                    DisplayName = "Set Volume",
                    Description = "Set channel volume to a specific value",
                    Parameters = new List<ActionParameter>
                    {
                        new ActionParameter
                        {
                            Name = "channel",
                            DisplayName = "Channel",
                            Type = "choice",
                            Required = true,
                            Choices = new List<string> { "Music", "Game", "Chat", "System", "LineIn", "LineOut", "Mic", "Sample", "Headphones" }
                        },
                        new ActionParameter
                        {
                            Name = "volume",
                            DisplayName = "Volume (0-255)",
                            Type = "int",
                            Required = true
                        }
                    }
                },
                new DeviceAction
                {
                    Id = "mute_channel",
                    DisplayName = "Mute Channel",
                    Description = "Mute or unmute a channel/fader",
                    Parameters = new List<ActionParameter>
                    {
                        new ActionParameter
                        {
                            Name = "fader",
                            DisplayName = "Fader",
                            Type = "choice",
                            Required = true,
                            Choices = new List<string> { "A", "B", "C", "D" }
                        },
                        new ActionParameter
                        {
                            Name = "mute_state",
                            DisplayName = "Mute State",
                            Type = "choice",
                            Required = true,
                            Choices = new List<string> { "Unmuted", "MutedToX", "MutedToAll" },
                            DefaultValue = "MutedToAll"
                        }
                    }
                },
                new DeviceAction
                {
                    Id = "toggle_mute",
                    DisplayName = "Toggle Mute",
                    Description = "Toggle mute state for a channel/fader",
                    Parameters = new List<ActionParameter>
                    {
                        new ActionParameter
                        {
                            Name = "fader",
                            DisplayName = "Fader",
                            Type = "choice",
                            Required = true,
                            Choices = new List<string> { "A", "B", "C", "D" }
                        }
                    }
                }
            };
        }

        public async Task<ActionResult> ExecuteActionAsync(string actionId, Dictionary<string, object>? parameters = null)
        {
            try
            {
                _context.LogInfo("GoXLR Control Plugin", $"Executing action: {actionId}");
                
                switch (actionId)
                {
                    case "switch_profile":
                        return await SwitchProfileAsync(parameters);
                    
                    case "adjust_volume":
                        return await AdjustVolumeAsync(parameters);
                    
                    case "set_volume":
                        return await SetVolumeAsync(parameters);
                    
                    case "mute_channel":
                        return await MuteChannelAsync(parameters);
                    
                    case "toggle_mute":
                        return await ToggleMuteAsync(parameters);
                    
                    default:
                        return new ActionResult
                        {
                            Success = false,
                            Message = $"Unknown action: {actionId}"
                        };
                }
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Control Plugin", $"Error executing action {actionId}", ex);
                return new ActionResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        private async Task<ActionResult> SwitchProfileAsync(Dictionary<string, object>? parameters)
        {
            if (parameters == null || !parameters.ContainsKey("profile_name"))
            {
                return new ActionResult { Success = false, Message = "Profile name parameter is required" };
            }

            var profileName = parameters["profile_name"].ToString();
            if (string.IsNullOrEmpty(profileName))
            {
                return new ActionResult { Success = false, Message = "Profile name cannot be empty" };
            }

            var success = await _goXLRService.LoadProfileAsync(profileName);
            
            return new ActionResult
            {
                Success = success,
                Message = success ? $"Switched to profile: {profileName}" : "Failed to switch profile"
            };
        }

        private async Task<ActionResult> AdjustVolumeAsync(Dictionary<string, object>? parameters)
        {
            if (parameters == null || !parameters.ContainsKey("channel") || !parameters.ContainsKey("increase"))
            {
                return new ActionResult { Success = false, Message = "Channel and increase parameters are required" };
            }

            var channel = parameters["channel"].ToString();
            var increase = Convert.ToBoolean(parameters["increase"]);

            var result = await _goXLRService.AdjustVolumeAsync(channel!, increase);
            
            return new ActionResult
            {
                Success = result.Success,
                Message = result.Message,
                Data = new Dictionary<string, object>
                {
                    { "new_volume", result.NewVolume },
                    { "percentage", result.Percentage }
                }
            };
        }

        private async Task<ActionResult> SetVolumeAsync(Dictionary<string, object>? parameters)
        {
            if (parameters == null || !parameters.ContainsKey("channel") || !parameters.ContainsKey("volume"))
            {
                return new ActionResult { Success = false, Message = "Channel and volume parameters are required" };
            }

            var channel = parameters["channel"].ToString();
            var volume = Convert.ToInt32(parameters["volume"]);

            if (volume < 0 || volume > 255)
            {
                return new ActionResult { Success = false, Message = "Volume must be between 0 and 255" };
            }

            var success = await _goXLRService.SetVolumeAsync(channel!, volume);
            
            return new ActionResult
            {
                Success = success,
                Message = success ? $"Set {channel} volume to {volume}" : "Failed to set volume"
            };
        }

        private async Task<ActionResult> MuteChannelAsync(Dictionary<string, object>? parameters)
        {
            if (parameters == null || !parameters.ContainsKey("fader") || !parameters.ContainsKey("mute_state"))
            {
                return new ActionResult { Success = false, Message = "Fader and mute_state parameters are required" };
            }

            var fader = parameters["fader"].ToString();
            var muteState = parameters["mute_state"].ToString();

            var success = await _goXLRService.SetChannelMuteStateAsync(fader!, muteState!);
            
            return new ActionResult
            {
                Success = success,
                Message = success ? $"Fader {fader} set to {muteState}" : "Failed to set mute state"
            };
        }

        private async Task<ActionResult> ToggleMuteAsync(Dictionary<string, object>? parameters)
        {
            if (parameters == null || !parameters.ContainsKey("fader"))
            {
                return new ActionResult { Success = false, Message = "Fader parameter is required" };
            }

            var fader = parameters["fader"].ToString();

            // For now, we'll just toggle between Unmuted and MutedToAll
            // In the future, we could track the current state and toggle accordingly
            // For now, we'll just mute to all as a simple implementation
            var success = await _goXLRService.SetChannelMuteStateAsync(fader!, "MutedToAll");
            
            return new ActionResult
            {
                Success = success,
                Message = success ? $"Toggled mute for fader {fader}" : "Failed to toggle mute"
            };
        }
    }
}





