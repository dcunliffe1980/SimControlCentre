using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Plugin interface for device control functionality
    /// Allows dynamic discovery and execution of device actions
    /// </summary>
    public interface IDeviceControlPlugin
    {
        /// <summary>
        /// Unique identifier for this plugin
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Display name for UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Description of the device
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this plugin is currently available/enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets all available actions this device can perform
        /// </summary>
        List<DeviceAction> GetAvailableActions();

        /// <summary>
        /// Executes an action with the given parameters
        /// </summary>
        Task<ActionResult> ExecuteActionAsync(string actionId, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Gets configuration options for this plugin
        /// </summary>
        List<PluginConfigOption> GetConfigOptions();

        /// <summary>
        /// Applies configuration to the plugin
        /// </summary>
        void ApplyConfiguration(Dictionary<string, object> config);
    }

    /// <summary>
    /// Represents an action that a device can perform
    /// </summary>
    public class DeviceAction
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ActionParameter> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Represents a parameter for an action
    /// </summary>
    public class ActionParameter
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "string"; // "string", "int", "bool", "choice"
        public bool Required { get; set; } = true;
        public object? DefaultValue { get; set; }
        public List<string>? Choices { get; set; } // For "choice" type
    }

    /// <summary>
    /// Result of executing an action
    /// </summary>
    public class ActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
    }

    /// <summary>
    /// Configuration option for a plugin
    /// </summary>
    public class PluginConfigOption
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public object? DefaultValue { get; set; }
        public List<string>? AvailableOptions { get; set; }
    }
}
