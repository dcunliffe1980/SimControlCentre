using System.Text.Json.Serialization;

namespace SimControlCentre.Plugins.GoXLR.Models;

/// <summary>
/// Button color configuration
/// </summary>
public class ButtonColours
{
    [JsonPropertyName("colour_one")]
    public string ColourOne { get; set; } = "000000";

    [JsonPropertyName("colour_two")]
    public string ColourTwo { get; set; } = "000000";
}

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

    public static GoXLRCommandRequest SetButtonColours(string serialNumber, string buttonId, string colourOne, string? colourTwo = null)
    {
        // If colourTwo is not provided, use colourOne for both
        var colours = colourTwo != null ? new object[] { buttonId, colourOne, colourTwo } : new object[] { buttonId, colourOne };
        
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetButtonColours", colours }
                }
            }
        };
    }

    public static GoXLRCommandRequest SetSimpleColor(string serialNumber, string target, string colour)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetSimpleColour", new object[] { target, colour } }
                }
            }
        };
    }

    public static GoXLRCommandRequest SetGlobalColour(string serialNumber, string colour)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object>
                {
                    { "SetGlobalColour", colour } // Direct string, NOT array!
                }
            }
        };
    }

    public static GoXLRCommandRequest SetFaderColors(string serialNumber, string faderName, string colourOne, string colourTwo)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetFaderColours", new object[] { faderName, colourOne, colourTwo } }
                }
            }
        };
    }

    public static GoXLRCommandRequest SetFaderMuteState(string serialNumber, string faderName, string muteState)
    {
        return new GoXLRCommandRequest
        {
            Command = new object[]
            {
                serialNumber,
                new Dictionary<string, object[]>
                {
                    { "SetFaderMuteState", new object[] { faderName, muteState } }
                }
            }
        };
    }
}
