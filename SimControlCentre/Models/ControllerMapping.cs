namespace SimControlCentre.Models;

/// <summary>
/// Maps a controller/button box button to a keyboard key
/// </summary>
public class ControllerMapping
{
    /// <summary>
    /// Controller identifier (e.g., "Controller1", "Controller2")
    /// </summary>
    public string ControllerId { get; set; } = string.Empty;

    /// <summary>
    /// Button number on the controller (1-based)
    /// </summary>
    public int ButtonNumber { get; set; }

    /// <summary>
    /// Keyboard key to simulate (e.g., "F1", "Ctrl+A")
    /// </summary>
    public string KeyboardKey { get; set; } = string.Empty;

    /// <summary>
    /// Toggle mode for latch switches (send single keypress instead of hold)
    /// </summary>
    public bool ToggleMode { get; set; }

    /// <summary>
    /// Optional description for the mapping
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
