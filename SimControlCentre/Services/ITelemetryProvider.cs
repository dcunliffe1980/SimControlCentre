using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Generic interface for telemetry providers (iRacing, ACC, etc.)
    /// </summary>
    public interface ITelemetryProvider
    {
        /// <summary>
        /// Name of the sim/provider
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Is the provider currently connected to the sim
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Start reading telemetry data
        /// </summary>
        void Start();
        
        /// <summary>
        /// Stop reading telemetry data
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Get the latest telemetry data
        /// </summary>
        TelemetryData? GetLatestData();
        
        /// <summary>
        /// Event fired when telemetry data is updated
        /// </summary>
        event EventHandler<TelemetryUpdatedEventArgs>? TelemetryUpdated;
        
        /// <summary>
        /// Event fired when connection state changes
        /// </summary>
        event EventHandler<bool>? ConnectionChanged;
    }
}
