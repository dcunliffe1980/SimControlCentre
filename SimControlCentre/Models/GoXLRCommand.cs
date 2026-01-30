using System.Text.Json.Serialization;

namespace SimControlCentre.Models;

/// <summary>
/// Base command structure for GoXLR API
/// </summary>
public class GoXLRCommandRequest
{
    [JsonPropertyName("Command")]
    public object[] Command { get; set; } = Array.Empty<object>();

    public static GoXLRCommandRequest SetVolume(string serialNumber, string channel, int volume)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetVolume", new object[] { channel, volume } }
                }
            }
        };
    }

    public static GoXLRCommandRequest LoadProfile(string serialNumber, string profileName)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "LoadProfile", new object[] { profileName, false } }
                }
            }
        };
    }
}
