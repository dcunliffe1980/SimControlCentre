using SimControlCentre.Models;
using System.Windows;

namespace SimControlCentre.Services;

/// <summary>
/// Manages hotkey registration and maps them to GoXLR actions
/// </summary>
public class HotkeyManager : IDisposable
{
    private readonly HotkeyService _hotkeyService;
    private readonly GoXLRService _goXLRService;
    private readonly AppSettings _settings;

    public HotkeyManager(HotkeyService hotkeyService, GoXLRService goXLRService, AppSettings settings)
    {
        _hotkeyService = hotkeyService;
        _goXLRService = goXLRService;
        _settings = settings;
    }

    /// <summary>
    /// Registers all configured hotkeys
    /// </summary>
    public int RegisterAllHotkeys()
    {
        Console.WriteLine("[HotkeyManager] Registering hotkeys...");

        // Unregister any existing hotkeys first
        _hotkeyService.UnregisterAll();

        int registered = 0;
        int failed = 0;

        // Register volume hotkeys
        foreach (var channelKvp in _settings.VolumeHotkeys)
        {
            var channel = channelKvp.Key;
            var hotkeys = channelKvp.Value;

            // Volume Up
            if (!string.IsNullOrWhiteSpace(hotkeys.VolumeUp))
            {
                if (_hotkeyService.RegisterHotkey(hotkeys.VolumeUp, () => AdjustVolume(channel, true)))
                {
                    Console.WriteLine($"[HotkeyManager] Registered Volume Up for {channel}: {hotkeys.VolumeUp}");
                    registered++;
                }
                else
                {
                    Console.WriteLine($"[HotkeyManager] Failed to register Volume Up for {channel}: {hotkeys.VolumeUp}");
                    failed++;
                }
            }

            // Volume Down
            if (!string.IsNullOrWhiteSpace(hotkeys.VolumeDown))
            {
                if (_hotkeyService.RegisterHotkey(hotkeys.VolumeDown, () => AdjustVolume(channel, false)))
                {
                    Console.WriteLine($"[HotkeyManager] Registered Volume Down for {channel}: {hotkeys.VolumeDown}");
                    registered++;
                }
                else
                {
                    Console.WriteLine($"[HotkeyManager] Failed to register Volume Down for {channel}: {hotkeys.VolumeDown}");
                    failed++;
                }
            }
        }

        // Register profile hotkeys
        foreach (var profileKvp in _settings.ProfileHotkeys)
        {
            var profile = profileKvp.Key;
            var hotkey = profileKvp.Value;

            if (!string.IsNullOrWhiteSpace(hotkey))
            {
                if (_hotkeyService.RegisterHotkey(hotkey, () => LoadProfile(profile)))
                {
                    Console.WriteLine($"[HotkeyManager] Registered profile hotkey for '{profile}': {hotkey}");
                    registered++;
                }
                else
                {
                    Console.WriteLine($"[HotkeyManager] Failed to register profile hotkey for '{profile}': {hotkey}");
                    failed++;
                }
            }
        }

        Console.WriteLine($"[HotkeyManager] Registration complete: {registered} successful, {failed} failed");
        return registered;
    }

    /// <summary>
    /// Adjusts volume for a channel
    /// </summary>
    private async void AdjustVolume(string channel, bool increase)
    {
        try
        {
            var result = await _goXLRService.AdjustVolumeAsync(channel, increase);
            if (result.Success)
            {
                Console.WriteLine($"[HotkeyManager] {result.Message}");
                
                // Show notification
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var app = (App)Application.Current;
                    app.ShowVolumeNotification(result.Message);
                });
            }
            else
            {
                Console.WriteLine($"[HotkeyManager] Failed to adjust {channel} volume: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotkeyManager] Error adjusting volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a profile
    /// </summary>
    private async void LoadProfile(string profileName)
    {
        try
        {
            var success = await _goXLRService.LoadProfileAsync(profileName);
            if (success)
            {
                Console.WriteLine($"[HotkeyManager] Loaded profile: {profileName}");
                
                // Show notification
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var app = (App)Application.Current;
                    app.ShowVolumeNotification($"Profile: {profileName}");
                });
            }
            else
            {
                Console.WriteLine($"[HotkeyManager] Failed to load profile: {profileName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HotkeyManager] Error loading profile: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _hotkeyService?.Dispose();
    }
}
