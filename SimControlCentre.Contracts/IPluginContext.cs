namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Context provided to plugins for accessing app services
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// Application settings (shared across all plugins)
        /// </summary>
        object Settings { get; }

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
        /// Get the plugin data directory for storing plugin-specific files
        /// </summary>
        string GetPluginDataDirectory(string pluginId);
    }
}
