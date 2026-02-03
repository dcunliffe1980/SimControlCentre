using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimControlCentre.Plugins.GoXLR.Models;
using SimControlCentre.Contracts;

namespace SimControlCentre.Plugins.GoXLR.Services
{
    /// <summary>
    /// GoXLR implementation of lighting device using button LEDs
    /// </summary>
    public class GoXLRLightingDevice : ILightingDevice
    {
        private readonly GoXLRService _goXLRService;
        private readonly IPluginContext _context;
        private readonly List<string> _activeButtons;
        private Timer? _flashTimer;
        private bool _isFlashing;
        private LightingColor _flashColor1;
        private LightingColor _flashColor2;
        private bool _flashState;
        private Dictionary<string, string>? _savedButtonStates;

        // ILightingDevice interface properties
        public string DeviceId => "goxlr";
        public string DeviceName => "GoXLR";
        public bool IsConnected => _goXLRService.IsConnected;

        public GoXLRLightingDevice(GoXLRService goXLRService, IPluginContext context, List<string> activeButtons)
        {
            _goXLRService = goXLRService;
            _context = context;
            _activeButtons = activeButtons ?? new List<string> { "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute" };
        }

        // ILightingDevice interface methods
        public Task<bool> InitializeAsync()
        {
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            StopFlashingAsync().Wait();
            return Task.CompletedTask;
        }

        public Task SetColorAsync(string hexColor)
        {
            var color = ParseHexToLightingColor(hexColor);
            return SetColorAsync(color);
        }

        public async Task SetColorsAsync(Dictionary<string, string> buttonColors)
        {
            foreach (var kvp in buttonColors)
            {
                await _goXLRService.SetButtonColorAsync(kvp.Key, kvp.Value);
            }
        }

        private LightingColor ParseHexToLightingColor(string hexColor)
        {
            return hexColor.ToUpper() switch
            {
                "FF0000" => LightingColor.Red,
                "00FF00" => LightingColor.Green,
                "0000FF" => LightingColor.Blue,
                "FFFF00" => LightingColor.Yellow,
                "FFFFFF" => LightingColor.White,
                "FF8800" => LightingColor.Orange,
                "FF00FF" => LightingColor.Purple,
                "000000" => LightingColor.Off,
                _ => LightingColor.White
            };
        }

        public async Task SetColorAsync(LightingColor color)
        {
            await StopFlashingAsync();

            var goxlrColor = MapToGoXLRColor(color);
            
            _context.LogInfo("GoXLR Lighting", $"SetColorAsync: {color} (hex: {goxlrColor}) on {_activeButtons.Count} button(s)");
            _context.LogInfo("GoXLR Lighting", $"Active Buttons: {string.Join(", ", _activeButtons)}");
            
            // Check if Global is in the list
            if (_activeButtons.Contains("Global"))
            {
                _context.LogInfo("GoXLR Lighting", "? Global IS in active buttons list");
            }
            else
            {
                _context.LogInfo("GoXLR Lighting", "? Global is NOT in active buttons list");
            }
            
            // Send all commands at once (parallel) for instant update
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = _activeButtons.Select(button => 
            {
                _context.LogDebug("GoXLR Lighting", $"Queuing {button}");
                return SetButtonColorAsync(button, goxlrColor);
            }).ToList();
            
            _context.LogInfo("GoXLR Lighting", $"Waiting for {tasks.Count} parallel tasks...");
            await Task.WhenAll(tasks);
            sw.Stop();
            
            _context.LogInfo("GoXLR Lighting", $"Color update complete in {sw.ElapsedMilliseconds}ms");
        }

        public Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs)
        {
            _context.LogInfo("GoXLR Lighting", $"Starting flash: {color1}/{color2} at {intervalMs}ms interval");
            
            _isFlashing = true;
            _flashColor1 = color1;
            _flashColor2 = color2;
            _flashState = false;

            // Stop existing timer if any
            _flashTimer?.Dispose();
            
            // Create and start new timer
            _flashTimer = new Timer(async _ => await FlashUpdate(), null, 0, intervalMs);
            
            _context.LogInfo("GoXLR Lighting", "Flash timer started");
            
            return Task.CompletedTask;
        }

        public Task StopFlashingAsync()
        {
            if (_isFlashing)
            {
                _context.LogInfo("GoXLR Lighting", "Stopping flash");
            }
            
            _isFlashing = false;
            _flashTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _flashTimer?.Dispose();
            _flashTimer = null;
            
            return Task.CompletedTask;
        }

        public async Task SaveStateAsync()
        {
            try
            {
                _context.LogInfo("GoXLR Lighting", "Marking that we need to restore profile after flag clears");
                // We don't need to actually save anything - we'll just reload the profile later
                _savedButtonStates = new Dictionary<string, string>(); // Non-null means "we need to restore"
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Lighting", "Error in SaveStateAsync", ex);
            }
        }

        public async Task RestoreStateAsync()
        {
            try
            {
                if (_savedButtonStates != null)
                {
                    _context.LogInfo("GoXLR Lighting", "Restoring profile button colors by reloading current profile");
                    
                    // Reload the current profile to restore all button colors
                    await _goXLRService.ReloadCurrentProfileAsync();
                    
                    _savedButtonStates = null;
                    _context.LogInfo("GoXLR Lighting", "Profile reloaded - button colors restored");
                }
                else
                {
                    _context.LogInfo("GoXLR Lighting", "No saved state marker, turning off buttons");
                    // If we never saved (e.g., manual Clear Flag button), just turn off
                    await SetColorAsync(LightingColor.Off);
                }
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Lighting", "Error restoring state", ex);
            }
        }



        private async Task FlashUpdate()
        {
            if (!_isFlashing)
            {
                _context.LogDebug("GoXLR Lighting", "Flash update called but not flashing");
                return;
            }

            try
            {
                _flashState = !_flashState;
                var color = _flashState ? _flashColor1 : _flashColor2;
                var goxlrColor = MapToGoXLRColor(color);

                _context.LogDebug("GoXLR Lighting", $"Flash update: state={_flashState}, color={color}, hex={goxlrColor}");

                // Send all commands at once for synchronized flashing
                var tasks = _activeButtons.Select(button => SetButtonColorAsync(button, goxlrColor)).ToList();
                await Task.WhenAll(tasks);
                
                _context.LogDebug("GoXLR Lighting", $"Flash update complete: {tasks.Count} buttons updated");
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Lighting", "Error during flash update", ex);
            }
        }

        private async Task SetButtonColorAsync(string buttonId, string color)
        {
            try
            {
                await _goXLRService.SetButtonColorAsync(buttonId, color);
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Lighting", $"Error setting button {buttonId} to {color}", ex);
            }
        }

        private string MapToGoXLRColor(LightingColor color)
        {
            return color switch
            {
                LightingColor.Red => "FF0000",
                LightingColor.Green => "00FF00",
                LightingColor.Blue => "0000FF",
                LightingColor.Yellow => "FFFF00",
                LightingColor.White => "FFFFFF",
                LightingColor.Orange => "FF8800",
                LightingColor.Purple => "FF00FF",
                LightingColor.Off => "000000",
                _ => "000000"
            };
        }
    }
}




