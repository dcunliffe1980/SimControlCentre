using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Coordinates lighting devices to respond to racing flags
    /// </summary>
    public class LightingService : IDisposable
    {
        private readonly List<ILightingDevice> _devices = new();
        private readonly List<ILightingDevicePlugin> _plugins = new();
        private FlagStatus _currentFlag = FlagStatus.None;
        private bool _isDisposed;

        public IReadOnlyList<ILightingDevicePlugin> Plugins => _plugins.AsReadOnly();
        public IReadOnlyList<ILightingDevice> Devices => _devices.AsReadOnly();

        public LightingService()
        {
            Logger.Info("Lighting Service", "Lighting service initialized");
        }

        /// <summary>
        /// Register a plugin (but don't create device yet)
        /// </summary>
        public void RegisterPlugin(ILightingDevicePlugin plugin)
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
        public void RegisterDevice(ILightingDevice device)
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
            
            if (flag == _currentFlag)
            {
                Logger.Debug("Lighting Service", "Flag unchanged, skipping");
                return; // No change
            }

            Logger.Info("Lighting Service", $"Flag changed: {_currentFlag} ? {flag}");

            var previousFlag = _currentFlag;
            _currentFlag = flag;

            // If going from a flag to None, restore previous state
            if (flag == FlagStatus.None && previousFlag != FlagStatus.None)
            {
                Logger.Info("Lighting Service", "Flag cleared - restoring previous lighting state");
                await RestoreAllDevicesAsync();
                return;
            }

            // If this is a new flag (not just None), save current state first
            if (previousFlag == FlagStatus.None && flag != FlagStatus.None)
            {
                Logger.Info("Lighting Service", "New flag detected - saving current lighting state");
                await SaveAllDevicesAsync();
            }

            // Apply the flag lighting
            await ApplyFlagLightingAsync(flag);
        }

        private async Task ApplyFlagLightingAsync(FlagStatus flag)
        {
            var availableDevices = _devices.Where(d => d.IsAvailable).ToList();

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

        private async Task ApplyFlagToDeviceAsync(ILightingDevice device, FlagStatus flag)
        {
            switch (flag)
            {
                case FlagStatus.Green:
                    await device.SetColorAsync(LightingColor.Green);
                    break;

                case FlagStatus.Yellow:
                    // Solid yellow for caution
                    await device.SetColorAsync(LightingColor.Yellow);
                    break;

                case FlagStatus.YellowWaving:
                    // Flashing yellow for waving yellow flag
                    await device.StartFlashingAsync(LightingColor.Yellow, LightingColor.Off, 500);
                    break;

                case FlagStatus.Red:
                    await device.SetColorAsync(LightingColor.Red);
                    break;

                case FlagStatus.Blue:
                    await device.SetColorAsync(LightingColor.Blue);
                    break;

                case FlagStatus.White:
                    await device.SetColorAsync(LightingColor.White);
                    break;

                case FlagStatus.Checkered:
                    // Flash white/off for checkered flag effect
                    await device.StartFlashingAsync(LightingColor.White, LightingColor.Off, 300);
                    break;

                case FlagStatus.Black:
                    // Use red flashing for black flag (warning)
                    await device.StartFlashingAsync(LightingColor.Red, LightingColor.Off, 500);
                    break;

                case FlagStatus.Debris:
                    // Orange/yellow for debris
                    await device.SetColorAsync(LightingColor.Orange);
                    break;

                case FlagStatus.OneLapToGreen:
                    // Flashing green
                    await device.StartFlashingAsync(LightingColor.Green, LightingColor.Off, 700);
                    break;

                case FlagStatus.None:
                    await device.RestoreStateAsync();
                    break;

                default:
                    Logger.Warning("Lighting Service", $"Unknown flag status: {flag}");
                    break;
            }
        }

        private async Task SaveAllDevicesAsync()
        {
            foreach (var device in _devices.Where(d => d.IsAvailable))
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
            foreach (var device in _devices.Where(d => d.IsAvailable))
            {
                try
                {
                    await device.RestoreStateAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error("Lighting Service", $"Error restoring state for {device.DeviceName}", ex);
                }
            }
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
