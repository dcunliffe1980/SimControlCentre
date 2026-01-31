namespace SimControlCentre.Models;

/// <summary>
/// Hotkey configuration for a GoXLR channel's volume control
/// Can be keyboard keys or controller buttons
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

    /// <summary>
    /// Controller button for volume up (e.g., "Device:GUID:Button:5")
    /// </summary>
    public string VolumeUpButton { get; set; } = string.Empty;

    /// <summary>
    /// Controller button for volume down (e.g., "Device:GUID:Button:6")
    /// </summary>
    public string VolumeDownButton { get; set; } = string.Empty;
}

