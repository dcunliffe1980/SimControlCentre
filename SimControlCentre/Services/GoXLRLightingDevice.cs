using System;
using System.Threading;
using System.Threading.Tasks;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// GoXLR implementation of lighting device using button LEDs
    /// </summary>
    public class GoXLRLightingDevice : ILightingDevice
    {
        private readonly GoXLRService _goXLRService;
        private readonly AppSettings _settings;
        private readonly List<string> _activeButtons;
        private Timer? _flashTimer;
        private bool _isFlashing;
        private LightingColor _flashColor1;
        private LightingColor _flashColor2;
        private bool _flashState;
        private string? _savedState; // Store previous button states

        public string DeviceName => "GoXLR";
        public bool IsAvailable => _goXLRService != null;

        public GoXLRLightingDevice(GoXLRService goXLRService, AppSettings settings, List<string> activeButtons)
        {
            _goXLRService = goXLRService;
            _settings = settings;
            _activeButtons = activeButtons ?? new List<string> { "Fader1Mute", "Fader2Mute", "Fader3Mute", "Fader4Mute" };
        }

        public async Task SetColorAsync(LightingColor color)
        {
            await StopFlashingAsync();

            var goxlrColor = MapToGoXLRColor(color);
            
            Logger.Info("GoXLR Lighting", $"SetColorAsync: {color} (hex: {goxlrColor}) on {_activeButtons.Count} button(s)");
            Logger.Info("GoXLR Lighting", $"Active Buttons: {string.Join(", ", _activeButtons)}");
            
            // Check if Global is in the list
            if (_activeButtons.Contains("Global"))
            {
                Logger.Info("GoXLR Lighting", "? Global IS in active buttons list");
            }
            else
            {
                Logger.Info("GoXLR Lighting", "? Global is NOT in active buttons list");
            }
            
            // Send all commands at once (parallel) for instant update
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tasks = _activeButtons.Select(button => 
            {
                Logger.Debug("GoXLR Lighting", $"Queuing {button}");
                return SetButtonColorAsync(button, goxlrColor);
            }).ToList();
            
            Logger.Info("GoXLR Lighting", $"Waiting for {tasks.Count} parallel tasks...");
            await Task.WhenAll(tasks);
            sw.Stop();
            
            Logger.Info("GoXLR Lighting", $"Color update complete in {sw.ElapsedMilliseconds}ms");
        }

        public Task StartFlashingAsync(LightingColor color1, LightingColor color2, int intervalMs)
        {
            Logger.Info("GoXLR Lighting", $"Starting flash: {color1}/{color2} at {intervalMs}ms interval");
            
            _isFlashing = true;
            _flashColor1 = color1;
            _flashColor2 = color2;
            _flashState = false;

            // Stop existing timer if any
            _flashTimer?.Dispose();
            
            // Create and start new timer
            _flashTimer = new Timer(async _ => await FlashUpdate(), null, 0, intervalMs);
            
            Logger.Info("GoXLR Lighting", "Flash timer started");
            
            return Task.CompletedTask;
        }

        public Task StopFlashingAsync()
        {
            if (_isFlashing)
            {
                Logger.Info("GoXLR Lighting", "Stopping flash");
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
                // Get current button colors from GoXLR
                // Store them so we can restore later
                // For now, just log that we're saving state
                Logger.Debug("GoXLR Lighting", "Saving current button states");
                
                // TODO: Query actual button states from GoXLR API if possible
                _savedState = "saved"; // Placeholder
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error("GoXLR Lighting", "Error saving state", ex);
            }
        }

        public async Task RestoreStateAsync()
        {
            try
            {
                if (_savedState != null)
                {
                    Logger.Debug("GoXLR Lighting", "Restoring previous button states");
                    
                    // For now, turn off all buttons (return to default)
                    await SetColorAsync(LightingColor.Off);
                    
                    _savedState = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GoXLR Lighting", "Error restoring state", ex);
            }
        }

        private async Task FlashUpdate()
        {
            if (!_isFlashing)
            {
                Logger.Debug("GoXLR Lighting", "Flash update called but not flashing");
                return;
            }

            try
            {
                _flashState = !_flashState;
                var color = _flashState ? _flashColor1 : _flashColor2;
                var goxlrColor = MapToGoXLRColor(color);

                Logger.Debug("GoXLR Lighting", $"Flash update: state={_flashState}, color={color}, hex={goxlrColor}");

                // Send all commands at once for synchronized flashing
                var tasks = _activeButtons.Select(button => SetButtonColorAsync(button, goxlrColor)).ToList();
                await Task.WhenAll(tasks);
                
                Logger.Debug("GoXLR Lighting", $"Flash update complete: {tasks.Count} buttons updated");
            }
            catch (Exception ex)
            {
                Logger.Error("GoXLR Lighting", "Error during flash update", ex);
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
                Logger.Error("GoXLR Lighting", $"Error setting button {buttonId} to {color}", ex);
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

