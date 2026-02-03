using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Plugin that provides lighting/RGB control for devices
    /// </summary>
    public interface ILightingPlugin : IPlugin
    {
        /// <summary>
        /// Create a lighting device instance
        /// </summary>
        ILightingDevice CreateDevice();

        /// <summary>
        /// Optional: Provide a custom configuration UI for this plugin
        /// Returns a UserControl that will be embedded in the Lighting tab
        /// </summary>
        object? GetConfigurationControl();

        /// <summary>
        /// Apply configuration from settings
        /// </summary>
        void ApplyConfiguration(Dictionary<string, object> config);
    }

    /// <summary>
    /// Represents a lighting device that can display colors
    /// </summary>
    public interface ILightingDevice
    {
        string DeviceId { get; }
        string DeviceName { get; }
        bool IsConnected { get; }

        Task<bool> InitializeAsync();
        Task SetColorAsync(string hexColor);
        Task SetColorAsync(LightingColor color);
        Task SetColorsAsync(Dictionary<string, string> buttonColors);
        Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs);
        Task StopFlashingAsync();
        Task DisconnectAsync();
        Task SaveStateAsync();
        Task RestoreStateAsync();
    }
}

