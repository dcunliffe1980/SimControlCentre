using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SimControlCentre.Services;

/// <summary>
/// Service for registering and handling global hotkeys
/// </summary>
public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _nextHotkeyId = 1;
    private HwndSource? _hwndSource;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Initializes the hotkey service with a window handle
    /// </summary>
    public void Initialize(IntPtr windowHandle)
    {
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        if (_hwndSource != null)
        {
            _hwndSource.AddHook(WndProc);
        }
    }

    /// <summary>
    /// Registers a global hotkey
    /// </summary>
    public bool RegisterHotkey(string hotkeyString, Action action)
    {
        if (!HotkeyParser.TryParse(hotkeyString, out var modifiers, out var key))
        {
            Console.WriteLine($"[Hotkey] Failed to parse hotkey: {hotkeyString}");
            return false;
        }

        return RegisterHotkey(modifiers, key, action);
    }

    /// <summary>
    /// Registers a global hotkey with specific modifiers and key
    /// </summary>
    public bool RegisterHotkey(ModifierKeys modifiers, Key key, Action action)
    {
        if (_hwndSource == null)
        {
            Console.WriteLine("[Hotkey] Service not initialized");
            return false;
        }

        var hotkeyId = _nextHotkeyId++;
        var mod = HotkeyParser.ToWin32Modifiers(modifiers);
        var vk = HotkeyParser.ToVirtualKey(key);

        if (RegisterHotKey(_hwndSource.Handle, hotkeyId, mod, vk))
        {
            _hotkeyActions[hotkeyId] = action;
            Console.WriteLine($"[Hotkey] Registered hotkey ID {hotkeyId}: {modifiers}+{key}");
            return true;
        }

        Console.WriteLine($"[Hotkey] Failed to register: {modifiers}+{key}");
        return false;
    }

    /// <summary>
    /// Unregisters all hotkeys
    /// </summary>
    public void UnregisterAll()
    {
        if (_hwndSource == null)
            return;

        foreach (var hotkeyId in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotKey(_hwndSource.Handle, hotkeyId);
            Console.WriteLine($"[Hotkey] Unregistered hotkey ID {hotkeyId}");
        }

        _hotkeyActions.Clear();
    }

    /// <summary>
    /// Window procedure to handle hotkey messages
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var hotkeyId = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(hotkeyId, out var action))
            {
                Console.WriteLine($"[Hotkey] Hotkey {hotkeyId} triggered");
                try
                {
                    action?.Invoke();
                    handled = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Hotkey] Error executing hotkey action: {ex.Message}");
                }
            }
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
        }
    }
}
