using System.Text.Json.Serialization;

namespace SimControlCentre.Models;

/// <summary>
/// Full response from GET /api/get-devices endpoint
/// </summary>
public class GoXLRFullResponse
{
    [JsonPropertyName("mixers")]
    public Dictionary<string, GoXLRDevice> Mixers { get; set; } = new();

    [JsonPropertyName("files")]
    public GoXLRFiles Files { get; set; } = new();
}

/// <summary>
/// Individual device status
/// </summary>
public class GoXLRDevice
{
    [JsonPropertyName("profile_name")]
    public string ProfileName { get; set; } = string.Empty;

    [JsonPropertyName("levels")]
    public GoXLRLevels Levels { get; set; } = new();

    [JsonPropertyName("hardware")]
    public GoXLRHardware Hardware { get; set; } = new();
}

/// <summary>
/// Hardware information
/// </summary>
public class GoXLRHardware
{
    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;
}

/// <summary>
/// Volume levels for all channels
/// </summary>
public class GoXLRLevels
{
    [JsonPropertyName("volumes")]
    public Dictionary<string, int> Volumes { get; set; } = new();
}

/// <summary>
/// Available profiles and files
/// </summary>
public class GoXLRFiles
{
    [JsonPropertyName("profiles")]
    public List<string> Profiles { get; set; } = new();
}
