namespace SimControlCentre.Models;

/// <summary>
/// General application settings
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// GoXLR device serial number (e.g., "S220202153DI7")
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// Volume adjustment step (0-255 scale)
    /// </summary>
    public int VolumeStep { get; set; } = 10;

    /// <summary>
    /// Volume cache time in milliseconds
    /// </summary>
    public int VolumeCacheTimeMs { get; set; } = 5000;

    /// <summary>
    /// GoXLR Utility API endpoint
    /// </summary>
    public string ApiEndpoint { get; set; } = "http://localhost:14564";
}
