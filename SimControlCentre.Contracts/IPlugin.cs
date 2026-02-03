namespace SimControlCentre.Contracts
{
    /// <summary>
    /// Base interface for all SimControlCentre plugins
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Unique identifier for this plugin (e.g., "goxlr", "philips-hue")
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Display name shown in the UI
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Plugin author/creator
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Description of what this plugin does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Whether this plugin is currently enabled
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Called when the plugin is loaded and should initialize
        /// </summary>
        void Initialize(IPluginContext context);

        /// <summary>
        /// Called when the plugin should clean up and shutdown
        /// </summary>
        void Shutdown();
    }
}
