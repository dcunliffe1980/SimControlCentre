using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Models;
using ILightingDevice = SimControlCentre.Contracts.ILightingDevice;
using ILightingPlugin = SimControlCentre.Contracts.ILightingPlugin;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Coordinates lighting devices to respond to racing flags
    /// </summary>
    public class LightingService : IDisposable
    {
        private readonly List<SimControlCentre.Contracts.ILightingDevice> _devices = new();
        private readonly List<ILightingPlugin> _plugins = new();
        private FlagStatus _currentFlag = FlagStatus.None;
        private bool _isDisposed;
        private DateTime _lastNoneFlagTime = DateTime.MinValue;
        private const int NoneDebounceMs = 500; // Ignore None flags for 500ms after a real flag

        public IReadOnlyList<ILightingPlugin> Plugins => _plugins.AsReadOnly();

        public IReadOnlyList<SimControlCentre.Contracts.ILightingDevice> Devices => _devices.AsReadOnly();

        public LightingService()
        {
            Logger.Info("Lighting Service", "Lighting service initialized");
        }

        /// <summary>
        /// Register a plugin (but don't create device yet)
        /// </summary>
        public void RegisterPlugin(ILightingPlugin plugin)
        {
            if (_plugins.Any(p => p.PluginId == plugin.PluginId))
            {
                Logger.Warning("Lighting Service", $"Plugin {plugin.PluginId} already registered");
                return;
            }

            Logger.Info("Lighting Service", $"Registering plugin: {plugin.DisplayName}");
            _plugins.Add(plugin);
        }

        /// <summary>
        /// Initialize all enabled plugins and create their devices
        /// Devices will handle connection failures gracefully
        /// </summary>
        public async Task InitializeAsync()
        {
            Logger.Info("Lighting Service", "Starting device initialization...");
            
            // Clear existing devices first
            _devices.Clear();
            Logger.Info("Lighting Service", "Cleared existing devices");
            
            foreach (var plugin in _plugins.Where(p => p.IsEnabled))
            {
                try
                {
                    Logger.Info("Lighting Service", $"Creating device for plugin: {plugin.DisplayName}");
                    
                    // Always create the device - let it handle connection state internally
                    // This avoids duplicate connection checks and race conditions
                    var device = plugin.CreateDevice();
                    RegisterDevice(device);
                    
                    Logger.Info("Lighting Service", $"? Device registered: {plugin.DisplayName}");
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error initializing plugin {plugin.DisplayName}", ex);
                }
            }
            
            Logger.Info("Lighting Service", $"Initialization complete. {_devices.Count} device(s) registered");
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Register a lighting device
        /// </summary>
        public void RegisterDevice(SimControlCentre.Contracts.ILightingDevice device)
        {
            if (_devices.Contains(device))
            {
                Logger.Warning("Lighting Service", $"Device {device.DeviceName} already registered");
                return;
            }

            Logger.Info("Lighting Service", $"Registering lighting device: {device.DeviceName}");
            _devices.Add(device);
        }

        /// <summary>
        /// Update lighting based on flag status
        /// </summary>
        public async Task UpdateForFlagAsync(FlagStatus flag)
        {
            Logger.Info("Lighting Service", $"UpdateForFlagAsync called with: {flag}, current: {_currentFlag}, devices: {_devices.Count}");
            
            // Debounce None flags - ignore brief None flags that might be telemetry glitches
            if (flag == FlagStatus.None && _currentFlag != FlagStatus.None)
            {
                var timeSinceNone = (DateTime.Now - _lastNoneFlagTime).TotalMilliseconds;
                if (timeSinceNone < NoneDebounceMs)
                {
                    Logger.Debug("Lighting Service", $"Ignoring brief None flag (only {timeSinceNone}ms since last real flag)");
                    return;
                }
                
                Logger.Info("Lighting Service", $"Flag cleared after {timeSinceNone}ms - turning off LEDs during session");
                _currentFlag = FlagStatus.None;
                // During a session, just turn off LEDs (don't restore profile)
                await ClearAllDevicesAsync();
                return;
            }
            
            if (flag == _currentFlag)
            {
                Logger.Debug("Lighting Service", "Flag unchanged, skipping");
                return; // No change
            }

            Logger.Info("Lighting Service", $"Flag changed: {_currentFlag} ? {flag}");

            var previousFlag = _currentFlag;
            _currentFlag = flag;
            
            // Track when we got a non-None flag
            if (flag != FlagStatus.None)
            {
                _lastNoneFlagTime = DateTime.Now;
            }

            // If this is a new flag (from None), save current state first
            if (previousFlag == FlagStatus.None && flag != FlagStatus.None)
            {
                Logger.Info("Lighting Service", "New flag detected - saving current profile lighting state");
                await SaveAllDevicesAsync();
            }

            // Apply the flag lighting
            await ApplyFlagLightingAsync(flag);
        }



        private async Task ApplyFlagLightingAsync(FlagStatus flag)
        {
            var availableDevices = _devices.Where(d => d.IsConnected).ToList();

            if (!availableDevices.Any())
            {
                Logger.Debug("Lighting Service", "No available lighting devices");
                return;
            }

            Logger.Info("Lighting Service", $"Applying flag lighting: {flag} to {availableDevices.Count} device(s)");

            foreach (var device in availableDevices)
            {
                try
                {
                    await ApplyFlagToDeviceAsync(device, flag);
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error applying flag to {device.DeviceName}", ex);
                }
            }
        }

        private async Task ApplyFlagToDeviceAsync(SimControlCentre.Contracts.ILightingDevice device, FlagStatus flag)
        {
            switch (flag)
            {
                case FlagStatus.Green:
                    // Solid green - race start / clear track
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Green);
                    break;

                case FlagStatus.Yellow:
                    // Solid yellow - caution / no passing
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Yellow);
                    break;

                case FlagStatus.YellowWaving:
                    // Slow flashing yellow (1 second cycle) - danger ahead
                    await device.StartFlashingAsync(SimControlCentre.Contracts.LightingColor.Yellow, SimControlCentre.Contracts.LightingColor.Off, 500);
                    break;

                case FlagStatus.Blue:
                    // Solid blue - being lapped, hold your line
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Blue);
                    break;

                case FlagStatus.White:
                    // Solid white - final lap
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.White);
                    break;

                case FlagStatus.Checkered:
                    // Fast flashing white (SimHub style: 250ms) - race end
                    await device.StartFlashingAsync(SimControlCentre.Contracts.LightingColor.White, SimControlCentre.Contracts.LightingColor.Off, 250);
                    break;

                case FlagStatus.Red:
                    // Solid red - session stopped
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Red);
                    break;

                case FlagStatus.Black:
                    // Solid red (SimHub uses red for black flag visibility)
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Red);
                    break;

                case FlagStatus.Debris:
                    // Slow flashing orange - debris/surface warning
                    await device.StartFlashingAsync(SimControlCentre.Contracts.LightingColor.Orange, SimControlCentre.Contracts.LightingColor.Off, 500);
                    break;

                case FlagStatus.OneLapToGreen:
                    // Medium flashing green (700ms) - one lap to restart
                    await device.StartFlashingAsync(SimControlCentre.Contracts.LightingColor.Green, SimControlCentre.Contracts.LightingColor.Off, 700);
                    break;

                case FlagStatus.Crossed:
                    // Solid yellow - unclear conditions (default to caution)
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Yellow);
                    break;

                case FlagStatus.None:
                    // Turn off lights when no flag
                    await device.StopFlashingAsync();
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Off);
                    break;

                default:
                    Logger.Warning("Lighting Service", $"Unknown flag status: {flag}");
                    break;
            }
        }

        private async Task SaveAllDevicesAsync()
        {
            Logger.Info("Lighting Service", "Saving device states before flag lighting");
            
            foreach (var device in _devices.Where(d => d.IsConnected))
            {
                try
                {
                    await device.SaveStateAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error saving state for {device.DeviceName}", ex);
                }
            }
        }

        private async Task RestoreAllDevicesAsync()
        {
            Logger.Info("Lighting Service", "Restoring device states (clearing flags)");
            
            foreach (var device in _devices.Where(d => d.IsConnected))
            {
                try
                {
                    await device.StopFlashingAsync();
                    await device.RestoreStateAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error restoring state for {device.DeviceName}", ex);
                }
            }
        }
        
        private async Task ClearAllDevicesAsync()
        {
            Logger.Info("Lighting Service", "Clearing all devices (turning off LEDs)");
            
            foreach (var device in _devices.Where(d => d.IsConnected))
            {
                try
                {
                    await device.StopFlashingAsync();
                    await device.SetColorAsync(SimControlCentre.Contracts.LightingColor.Off);
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error clearing {device.DeviceName}", ex);
                }
            }
        }
        
        /// <summary>
        /// Restore profile lighting (called when telemetry disconnects)
        /// </summary>
        public async Task RestoreProfileLightingAsync()

        {
            Logger.Info("Lighting Service", "Restoring profile lighting after session end");
            
            // Reset flag state
            _currentFlag = FlagStatus.None;
            _lastNoneFlagTime = DateTime.MinValue;
            
            // Stop any flashing and restore saved state
            await RestoreAllDevicesAsync();
        }


        public void Dispose()
        {
            if (_isDisposed) return;

            Logger.Info("Lighting Service", "Disposing lighting service");

            // Stop all flashing and restore states
            Task.Run(async () =>
            {
                await RestoreAllDevicesAsync();
            }).Wait();

            _isDisposed = true;
        }
    }
}




