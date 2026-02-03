namespace SimControlCentre.Models;

/// <summary>
/// Settings for lighting plugins and flag-based lighting
/// </summary>
public class LightingSettings
{
    /// <summary>
    /// Enable/disable plugins by plugin ID
    /// </summary>
    public Dictionary<string, bool> EnabledPlugins { get; set; } = new()
    {
        { "goxlr", true },      // GoXLR plugin enabled by default
        { "hue", false },       // Philips Hue (future)
        { "nanoleaf", false }   // Nanoleaf (future)
    };

    /// <summary>
    /// Plugin-specific configuration
    /// Key: plugin ID (e.g., "goxlr")
    /// Value: Dictionary of config key/value pairs
    /// </summary>
    public Dictionary<string, Dictionary<string, object>> PluginConfigs { get; set; } = new();

    /// <summary>
    /// Which LEDs/buttons are selected for GoXLR flag lighting
    /// </summary>
    public List<string> GoXlrSelectedButtons { get; set; } = new()
    {
        "Fader1Mute",
        "Fader2Mute",
        "Fader3Mute",
        "Fader4Mute"
    };

    /// <summary>
    /// Enable/disable flag lighting globally
    /// </summary>
    public bool EnableFlagLighting { get; set; } = true;

    /// <summary>
    /// Custom colors for specific flags (future feature)
    /// Key: FlagStatus name (e.g., "Yellow", "Red")
    /// Value: Hex color (e.g., "FFFF00")
    /// </summary>
    public Dictionary<string, string> CustomFlagColors { get; set; } = new();
}
