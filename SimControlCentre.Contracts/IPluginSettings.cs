namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Settings accessor for plugins - provides read-only access to relevant settings
    /// </summary>
    public interface IPluginSettings
    {
        /// <summary>
        /// Get a setting value by key
        /// </summary>
        T? GetValue<T>(string key);

        /// <summary>
        /// Set a setting value by key
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Check if a setting exists
        /// </summary>
        bool HasValue(string key);
    }
}
