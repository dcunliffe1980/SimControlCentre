using System.Threading.Tasks;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Interface for lighting devices that can respond to racing flags
    /// </summary>
    public interface ILightingDevice
    {
        /// <summary>
        /// Name of the lighting device
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Is the device currently available/connected
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Set a solid color on the device
        /// </summary>
        Task SetColorAsync(LightingColor color);

        /// <summary>
        /// Start flashing between two colors
        /// </summary>
        Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs);

        /// <summary>
        /// Stop any flashing and return to solid color
        /// </summary>
        Task StopFlashingAsync();

        /// <summary>
        /// Save the current state (before applying flag colors)
        /// </summary>
        Task SaveStateAsync();

        /// <summary>
        /// Restore the previously saved state
        /// </summary>
        Task RestoreStateAsync();
    }

    /// <summary>
    /// Standard lighting colors
    /// </summary>
    public enum LightingColor
    {
        Off,
        Red,
        Green,
        Blue,
        Yellow,
        White,
        Orange,
        Purple,
        Custom
    }
}
