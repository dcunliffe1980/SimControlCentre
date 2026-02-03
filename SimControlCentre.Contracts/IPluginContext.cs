namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Context provided to plugins for accessing app services
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Application settings accessor
        /// </summary>
        IPluginSettings Settings { get; }

        /// <summary>
        /// Save the current settings to disk
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// Log an informational message
        /// </summary>
        void LogInfo(string category, string message);

        /// <summary>
        /// Log an error message with optional exception
        /// </summary>
        void LogError(string category, string message, Exception? exception = null);

        /// <summary>
        /// Log a warning message
        /// </summary>
        void LogWarning(string category, string message);

        /// <summary>
        /// Log a debug message
        /// </summary>
        void LogDebug(string category, string message);

        /// <summary>
        /// Get the plugin data directory for storing plugin-specific files
        /// </summary>
        string GetPluginDataDirectory(string pluginId);
        
        /// <summary>
        /// Start capturing button input from controllers.
        /// When a button is pressed, the callback will be invoked with a formatted string like "PXN-CB1: Btn 5"
        /// </summary>
        /// <param name="onButtonCaptured">Callback invoked when a button is captured</param>
        void StartButtonCapture(Action<string> onButtonCaptured);
        
        /// <summary>
        /// Stop capturing button input
        /// </summary>
        void StopButtonCapture();
    }
}


