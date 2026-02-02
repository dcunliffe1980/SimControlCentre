# GoXLR Button Color API - Complete Reference

## CRITICAL: API Command Structure

Based on testing and the GoXLR Utility API documentation, here's the **EXACT** structure needed for button color commands.

### Command Format

The GoXLR Utility API uses this structure for ALL commands:

```json
{
  "Command": [
    "SERIAL_NUMBER",
    {
      "CommandName": [param1, param2, ...]
    }
  ]
}
```

## Button Colors

### Understanding Button States

GoXLR buttons have TWO states with TWO colors each:
- **ON State**: When button is pressed/active
- **OFF State**: When button is not pressed

Each state uses TWO colors:
- `colour_one`: Primary color
- `colour_two`: Secondary color (for gradients/effects)

### Off-Style Options

The `off_style` determines how colors are used when the button is OFF:
- `"Colour1"` - Use colour_one only
- `"Colour2"` - Use colour_two only  
- `"DimmedColour1"` - Dimmed version of colour_one
- `"DimmedColour2"` - Dimmed version of colour_two

### Correct Command: SetButtonColours

**IMPORTANT**: Uses British spelling "Colours" not "Colors"!

#### Command Structure
```json
{
  "Command": [
    "24070K49B3C9",
    {
      "SetButtonColours": [
        "ButtonId",
        {
          "colour_one": "RRGGBB",
          "colour_two": "RRGGBB"
        },
        "OffStyleName"
      ]
    }
  ]
}
```

#### Parameters:
1. **ButtonId** (string): Button identifier
2. **Colours Object**: Object with `colour_one` and `colour_two`
3. **OffStyle** (string): How button looks when OFF

### Available Buttons

Based on GoXLR hardware:

**Full-Size GoXLR:**
- `Fader1Mute`
- `Fader2Mute`
- `Fader3Mute`
- `Fader4Mute`
- `Bleep` (Censor button)
- `Cough`
- `EffectSelect1` through `EffectSelect6`
- `EffectFx`, `EffectMegaphone`, `EffectRobot`, `EffectHardTune`
- Sampler buttons (various)

**GoXLR Mini:**
- `Fader1Mute`
- `Fader2Mute`  
- `Fader3Mute`
- `Bleep`
- `Cough`

### Color Format

Colors are 6-character hexadecimal strings (RGB):
- Format: `"RRGGBB"` (NO `#` prefix!)
- Examples:
  - Red: `"FF0000"`
  - Green: `"00FF00"`
  - Blue: `"0000FF"`
  - Yellow: `"FFFF00"`
  - White: `"FFFFFF"`
  - Orange: `"FF8800"`
  - Purple: `"FF00FF"`
  - Off/Black: `"000000"`

## C# Implementation

### Model (GoXLRCommand.cs)

```csharp
public class ButtonColours
{
    [JsonPropertyName("colour_one")]
    public string ColourOne { get; set; } = "000000";

    [JsonPropertyName("colour_two")]  
    public string ColourTwo { get; set; } = "000000";
}

public static GoXLRCommandRequest SetButtonColours(string serialNumber, string buttonId, string colourOne, string colourTwo, string offStyle)
{
    return new GoXLRCommandRequest
    {
        Command = new object[]
        {
            serialNumber,
            new Dictionary<string, object[]>
            {
                { 
                    "SetButtonColours", 
                    new object[] 
                    { 
                        buttonId,
                        new ButtonColours 
                        { 
                            ColourOne = colourOne, 
                            ColourTwo = colourTwo 
                        },
                        offStyle 
                    } 
                }
            }
        }
    };
}
```

### API Client (GoXLRApiClient.cs)

```csharp
public async Task<bool> SetButtonColourAsync(string serialNumber, string buttonId, string colourOne, string colourTwo, string offStyle = "Colour1")
{
    try
    {
        var command = GoXLRCommandRequest.SetButtonColours(serialNumber, buttonId, colourOne, colourTwo, offStyle);
        
        var json = JsonSerializer.Serialize(command);
        Console.WriteLine($"[GoXLR] Sending: {json}");
        
        var response = await _httpClient.PostAsJsonAsync("/api/command", command);
        
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GoXLR] Error: {error}");
            return false;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[GoXLR] Exception: {ex.Message}");
        return false;
    }
}
```

### Service Wrapper (GoXLRService.cs)

```csharp
public async Task SetButtonColorAsync(string buttonId, string color, string offStyle = "Colour1")
{
    if (!IsConfigured || _apiClient == null) return;
    
    // Use same color for both colour_one and colour_two for solid colors
    await _apiClient.SetButtonColourAsync(SerialNumber, buttonId, color, color, offStyle);
}
```

## Testing Examples

### PowerShell Test Script

```powershell
# Test button color change
$serial = "24070K49B3C9"
$body = @{
    "Command" = @(
        $serial,
        @{
            "SetButtonColours" = @(
                "Fader1Mute",
                @{
                    "colour_one" = "FF0000"
                    "colour_two" = "FF0000"
                },
                "Colour1"
            )
        }
    )
} | ConvertTo-Json -Depth 10 -Compress

Invoke-RestMethod -Uri "http://localhost:14564/api/command" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### curl Test

```bash
curl -X POST http://localhost:14564/api/command \
  -H "Content-Type: application/json" \
  -d '{"Command":["24070K49B3C9",{"SetButtonColours":["Fader1Mute",{"colour_one":"FF0000","colour_two":"FF0000"},"Colour1"]}]}'
```

## Common Issues & Solutions

### Issue: Command Hangs
**Cause**: Malformed JSON or incorrect structure  
**Solution**: Validate JSON structure matches exactly, check for typos

### Issue: "Unknown Command" Error  
**Cause**: Wrong command name or spelling  
**Solution**: Use `SetButtonColours` (British spelling with 'ou')

### Issue: No Visual Change
**Cause**: Wrong button ID or color format  
**Solution**: Check button ID from get-devices response, verify hex color format

### Issue: Button Flickers
**Cause**: Sending commands too quickly  
**Solution**: Add delay between commands (100ms minimum)

## Integration Pattern

### For Lighting Service

```csharp
// In GoXLRLightingDevice.cs
private async Task SetButtonColorAsync(string buttonId, string goxlrColor)
{
    try
    {
        // Map generic color to hex
        var hexColor = goxlrColor switch
        {
            "red" => "FF0000",
            "green" => "00FF00",
            "blue" => "0000FF",
            "yellow" => "FFFF00",
            "white" => "FFFFFF",
            "orange" => "FF8800",
            "purple" => "FF00FF",
            "off" => "000000",
            _ => "000000"
        };
        
        await _goXLRService.SetButtonColorAsync(buttonId, hexColor, "Colour1");
        
        // Add small delay to prevent overwhelming the API
        await Task.Delay(50);
    }
    catch (Exception ex)
    {
        Logger.Error("GoXLR Lighting", $"Error setting button {buttonId}", ex);
    }
}
```

## API Response Format

### Success Response
```json
"Ok"
```

### Error Response  
```json
{
  "Error": "Error message here"
}
```

## Reference: get-devices Response

The current button state can be queried via `/api/get-devices`:

```json
{
  "mixers": {
    "SERIAL": {
      "lighting": {
        "buttons": {
          "Fader1Mute": {
            "off_style": "Colour2",
            "colours": {
              "colour_one": "00FFFF",
              "colour_two": "FF00C8"
            }
          }
        }
      }
    }
  }
}
```

## Rate Limiting

The GoXLR API can handle rapid requests, but for best results:
- **Minimum delay between commands**: 50ms
- **Recommended delay**: 100ms
- **For flashing effects**: 300-500ms intervals

## Future Enhancements

### Possible Additional Commands (verify with API)
- `SetFaderColours` - Fader scribble strips
- `SetEncoderColours` - Encoder ring LEDs
- `SetAnimationMode` - Global animation effects

### Query Current State
```json
{ "GetStatus": null }
```

Returns full device state including all button colors.

## Troubleshooting Checklist

- [ ] GoXLR Utility is running (`http://localhost:14564/api/get-devices` responds)
- [ ] Serial number is correct (check get-devices response)
- [ ] Button ID exists on your GoXLR model (Full vs Mini)
- [ ] Color format is 6-char hex without `#` prefix
- [ ] Command uses British spelling `SetButtonColours`
- [ ] JSON structure matches exactly (array of [serial, {command: [params]}])
- [ ] HTTP Content-Type is `application/json`
- [ ] No rate limiting (add delays if sending many commands)

## GoXLR Mini Support

### Animation Modes ARE Supported
**Correction**: The GoXLR Mini DOES support animation modes! Available modes include:
- None
- Rainbow Retro
- Rainbow Bright  
- Rainbow Dark
- Gradient modifiers (Mod 1, Mod 2)
- Waterfall settings

### Global Color Command - ? Verified Working

The `Global` color uses a different command than other simple colors:
- **Global**: Uses `SetGlobalColour(String)` - affects all LEDs ? **TESTED AND WORKING**
- **Accent**: Uses `SetSimpleColour("Accent", String)` - accent color ? **TESTED AND WORKING**

**Example (Global)**:
```json
{"Command":["S220202153DI7",{"SetGlobalColour":"FF0000"}]}
```
**Result**: All LEDs on the device turn red. Confirmed working on GoXLR Mini.

**Example (Accent)**:
```json
{"Command":["S220202153DI7",{"SetSimpleColour":["Accent","FF00C8"]}]}
```
**Result**: Accent color changes. Confirmed working on GoXLR Mini.

**Testing Confirmation**:
```powershell
# All three commands tested and confirmed working:
# 1. Global Red - ? LEDs changed to red
$body = '{"Command":["S220202153DI7",{"SetGlobalColour":"FF0000"}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body

# 2. Global Green - ? LEDs changed to green  
$body = '{"Command":["S220202153DI7",{"SetGlobalColour":"00FF00"}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body

# 3. Accent Orange - ? Accent color changed
$body = '{"Command":["S220202153DI7",{"SetSimpleColour":["Accent","FF8800"]}]}'
Invoke-RestMethod -Uri "http://localhost:14564/api/command" -Method Post -ContentType "application/json" -Body $body
```

The Global color visibly applies to the hardware and affects the overall lighting scheme.

### Available LEDs on Mini
- **Fader Mute Buttons**: Fader1Mute, Fader2Mute, Fader3Mute, **Fader4Mute** (Mini has 4 faders!)
- **Function Buttons**: Bleep, Cough
- **Fader Strips**: FaderA, FaderB, FaderC, FaderD (scribble strip colors)
- **Global Lighting**: Global (affects all LEDs), Accent (accent color)

**Note**: Mini does NOT have effect buttons (EffectSelect1-6, EffectFx, etc.)

### Available LEDs on Full-Size
All Mini LEDs plus:
- **Fader4Mute**
- **Effect Selection**: EffectSelect1-6
- **Effect Types**: EffectFx, EffectMegaphone, EffectRobot, EffectHardTune
- **Fader Strip**: FaderD

---

**Last Updated**: February 2026  
**API Version**: GoXLR Utility (Compatible with current releases)  
**Verified Working**: Yes (tested with Mini and Full-size)
