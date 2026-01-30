namespace SimControlCentre.Models;

/// <summary>
/// Hotkey configuration for a GoXLR channel's volume control
/// </summary>
public class ChannelHotkeys
{
    /// <summary>
    /// Hotkey to increase volume (e.g., "Ctrl+Shift+Up")
    /// </summary>
    public string VolumeUp { get; set; } = string.Empty;

    /// <summary>
    /// Hotkey to decrease volume (e.g., "Ctrl+Shift+Down")
    /// </summary>
    public string VolumeDown { get; set; } = string.Empty;
}
