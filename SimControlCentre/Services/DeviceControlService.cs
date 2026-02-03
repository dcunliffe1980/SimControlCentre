using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Contracts;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Service for managing device control plugins
    /// </summary>
    public class DeviceControlService
    {
        private readonly List<IDeviceControlPlugin> _plugins = new();

        public IReadOnlyList<IDeviceControlPlugin> Plugins => _plugins.AsReadOnly();

        public DeviceControlService()
        {
            Logger.Info("Device Control Service", "Device Control service initialized");
        }

        /// <summary>
        /// Register a device control plugin
        /// </summary>
        public void RegisterPlugin(IDeviceControlPlugin plugin)
        {
            if (_plugins.Any(p => p.PluginId == plugin.PluginId))
            {
                Logger.Warning("Device Control Service", $"Plugin {plugin.PluginId} already registered");
                return;
            }

            Logger.Info("Device Control Service", $"Registering plugin: {plugin.DisplayName}");
            _plugins.Add(plugin);
        }

        /// <summary>
        /// Execute an action from a plugin
        /// </summary>
        public async Task<ActionResult> ExecuteActionAsync(string pluginId, string actionId, Dictionary<string, object>? parameters = null)
        {
            var plugin = _plugins.FirstOrDefault(p => p.PluginId == pluginId && p.IsEnabled);
            
            if (plugin == null)
            {
                return new ActionResult
                {
                    Success = false,
                    Message = $"Plugin '{pluginId}' not found or disabled"
                };
            }

            return await plugin.ExecuteActionAsync(actionId, parameters);
        }

        /// <summary>
        /// Get all available actions across all enabled plugins
        /// </summary>
        public Dictionary<string, List<DeviceAction>> GetAvailableActions()
        {
            var actions = new Dictionary<string, List<DeviceAction>>();
            
            foreach (var plugin in _plugins.Where(p => p.IsEnabled))
            {
                actions[plugin.PluginId] = plugin.GetAvailableActions();
            }
            
            return actions;
        }

        /// <summary>
        /// Get a specific plugin by ID
        /// </summary>
        public IDeviceControlPlugin? GetPlugin(string pluginId)
        {
            return _plugins.FirstOrDefault(p => p.PluginId == pluginId);
        }
    }
}
