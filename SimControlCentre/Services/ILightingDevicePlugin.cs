using System.Threading.Tasks;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Plugin interface for lighting devices
    /// Allows dynamic discovery and loading of lighting device implementations
    /// </summary>
    public interface ILightingDevicePlugin
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
        /// Check if the hardware is available
        /// </summary>
        Task<bool> IsHardwareAvailableAsync();

        /// <summary>
        /// Create an instance of the lighting device
        /// </summary>
        ILightingDevice CreateDevice();

        /// <summary>
        /// Get configuration options for this device
        /// Returns list of configurable options (e.g., which buttons to use)
        /// </summary>
        IEnumerable<DeviceConfigOption> GetConfigOptions();

        /// <summary>
        /// Apply configuration to the device
        /// </summary>
        void ApplyConfiguration(Dictionary<string, object> config);
    }

    /// <summary>
    /// Configuration option for a device
    /// </summary>
    public class DeviceConfigOption
    {
        public string Key { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public DeviceConfigType Type { get; set; }
        public object? DefaultValue { get; set; }
        public List<string>? AvailableOptions { get; set; } // For multi-select
    }

    public enum DeviceConfigType
    {
        Boolean,
        String,
        Integer,
        MultiSelect // For selecting multiple buttons/lights
    }
}
