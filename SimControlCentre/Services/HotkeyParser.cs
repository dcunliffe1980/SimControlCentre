using System.Windows.Input;

namespace SimControlCentre.Services;

/// <summary>
/// Parses hotkey strings and converts to Win32 key codes
/// </summary>
public static class HotkeyParser
{
    /// <summary>
    /// Parses a hotkey string (e.g., "Ctrl+Shift+Up") into modifiers and key
    /// </summary>
    public static bool TryParse(string hotkeyString, out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;

        if (string.IsNullOrWhiteSpace(hotkeyString))
            return false;

        var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return false;

        // Last part is the key, everything else is modifiers
        var keyString = parts[^1];
        var modifierStrings = parts[..^1];

        // Parse modifiers
        foreach (var mod in modifierStrings)
        {
            switch (mod.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "win":
                case "windows":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    return false; // Unknown modifier
            }
        }

        // Parse key
        if (!Enum.TryParse<Key>(keyString, true, out key))
        {
            // Try some common aliases
            key = keyString.ToLowerInvariant() switch
            {
                "up" => Key.Up,
                "down" => Key.Down,
                "left" => Key.Left,
                "right" => Key.Right,
                "pageup" => Key.PageUp,
                "pagedown" => Key.PageDown,
                "home" => Key.Home,
                "end" => Key.End,
                "insert" => Key.Insert,
                "delete" => Key.Delete,
                _ => Key.None
            };
        }

        return key != Key.None;
    }

    /// <summary>
    /// Converts ModifierKeys to Win32 MOD_ flags
    /// </summary>
    public static uint ToWin32Modifiers(ModifierKeys modifiers)
    {
        uint mod = 0;
        if (modifiers.HasFlag(ModifierKeys.Control))
            mod |= 0x0002; // MOD_CONTROL
        if (modifiers.HasFlag(ModifierKeys.Shift))
            mod |= 0x0004; // MOD_SHIFT
        if (modifiers.HasFlag(ModifierKeys.Alt))
            mod |= 0x0001; // MOD_ALT
        if (modifiers.HasFlag(ModifierKeys.Windows))
            mod |= 0x0008; // MOD_WIN
        return mod;
    }

    /// <summary>
    /// Converts WPF Key to Win32 virtual key code
    /// </summary>
    public static uint ToVirtualKey(Key key)
    {
        return (uint)KeyInterop.VirtualKeyFromKey(key);
    }
}
