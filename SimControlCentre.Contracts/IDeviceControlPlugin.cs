using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Plugin that provides device control (hotkeys, profiles, etc.)
    /// </summary>
    public interface IDeviceControlPlugin : IPlugin
    {
        /// <summary>
        /// Get all available actions this device can perform
        /// </summary>
        List<DeviceAction> GetAvailableActions();

        /// <summary>
        /// Execute an action with the given parameters
        /// </summary>
        Task<ActionResult> ExecuteActionAsync(string actionId, Dictionary<string, object>? parameters = null);

        /// <summary>
        /// Optional: Provide a custom configuration UI for this plugin
        /// Returns a UserControl that will be embedded in the Device Control tab
        /// </summary>
        object? GetConfigurationControl();

        /// <summary>
        /// Apply configuration from settings
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
    /// Parameter for an action
    /// </summary>
    public class ActionParameter
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public bool Required { get; set; } = true;
        public object? DefaultValue { get; set; }
        public List<string>? Choices { get; set; }
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
}
