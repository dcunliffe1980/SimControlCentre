using System;
using System.IO;
using SimControlCentre.Contracts;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Implementation of IPluginContext that provides plugins access to app services
    /// </summary>
    public class PluginContext : IPluginContext
    {
        private readonly AppSettings _settings;
        private readonly ConfigurationService _configService;
        private Action<string>? _buttonCaptureCallback;

        public IPluginSettings Settings { get; }

        public PluginContext(AppSettings settings, ConfigurationService configService)
        {
            _settings = settings;
            _configService = configService;
            Settings = new PluginSettingsWrapper(settings, configService);
        }

        public void SaveSettings()
        {
            _configService.Save(_settings);
            LogInfo("Plugin Context", "Settings saved");
        }

        public void LogInfo(string category, string message)
        {
            Logger.Info(category, message);
        }

        public void LogError(string category, string message, Exception? exception = null)
        {
            Logger.Error(category, message, exception);
        }

        public void LogWarning(string category, string message)
        {
            Logger.Warning(category, message);
        }

        public void LogDebug(string category, string message)
        {
            Logger.Debug(category, message);
        }

        public string GetPluginDataDirectory(string pluginId)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimControlCentre",
                "Plugins",
                pluginId
            );
            Directory.CreateDirectory(directory);
            return directory;
        }

        public void StartButtonCapture(Action<string> onButtonCaptured)
        {
            _buttonCaptureCallback = onButtonCaptured;
            
            // Subscribe to DirectInputService
            var directInputService = App.GetDirectInputService();
            if (directInputService != null)
            {
                directInputService.ButtonPressed += OnDirectInputButtonPressed;
                LogDebug("Plugin Context", "Started button capture");
            }
            else
            {
                LogWarning("Plugin Context", "DirectInputService not available for button capture");
            }
        }

        public void StopButtonCapture()
        {
            // Unsubscribe from DirectInputService
            var directInputService = App.GetDirectInputService();
            if (directInputService != null)
            {
                directInputService.ButtonPressed -= OnDirectInputButtonPressed;
                LogDebug("Plugin Context", "Stopped button capture");
            }
            
            _buttonCaptureCallback = null;
        }

        private void OnDirectInputButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Format button string like: "DeviceName:{ProductGuid}:Button:{number}"
            // This matches the existing format used by HotkeyManager
            var buttonString = $"{e.DeviceName}:{{{e.ProductGuid}}}:Button:{e.ButtonNumber}";
            
            // Invoke the plugin's callback
            _buttonCaptureCallback?.Invoke(buttonString);
        }

    }


    /// <summary>
    /// Wrapper around AppSettings that implements IPluginSettings
    /// </summary>
    internal class PluginSettingsWrapper : IPluginSettings
    {
        private readonly AppSettings _settings;
        private readonly ConfigurationService _configService;

        public PluginSettingsWrapper(AppSettings settings, ConfigurationService configService)
        {
            _settings = settings;
            _configService = configService;
        }

        public T? GetValue<T>(string key)
        {
            // Support both dotted notation (Section.Property) and direct property access
            var parts = key.Split('.');
            
            try
            {
                // Direct AppSettings property (no dot)
                if (parts.Length == 1)
                {
                    // Special handling for VolumeHotkeys type conversion
                    if (key == "VolumeHotkeys" && typeof(T) == typeof(Dictionary<string, object>))
                    {
                        // Convert Dictionary<string, ChannelHotkeys> to Dictionary<string, object>
                        var volumeHotkeys = _settings.VolumeHotkeys;
                        var converted = new Dictionary<string, object>();
                        foreach (var kvp in volumeHotkeys)
                        {
                            converted[kvp.Key] = new Dictionary<string, object>
                            {
                                { "VolumeUp", kvp.Value.VolumeUp ?? "" },
                                { "VolumeUpButton", kvp.Value.VolumeUpButton ?? "" },
                                { "VolumeDown", kvp.Value.VolumeDown ?? "" },
                                { "VolumeDownButton", kvp.Value.VolumeDownButton ?? "" }
                            };
                        }
                        return (T)(object)converted;
                    }
                    
                    var property = typeof(AppSettings).GetProperty(key);
                    if (property != null && property.CanRead)
                    {
                        var value = property.GetValue(_settings);
                        if (value is T typedValue)
                            return typedValue;
                    }
                    return default;
                }

                
                // Dotted notation: Section.Property
                if (parts.Length != 2) return default;

                var section = parts[0];
                var propertyName = parts[1];

                switch (section.ToLower())
                {
                    case "goxlr":
                        var property = typeof(GeneralSettings).GetProperty(propertyName);
                        if (property != null && property.CanRead)
                        {
                            var value = property.GetValue(_settings.General);
                            if (value is T typedValue)
                                return typedValue;
                        }
                        break;

                    case "lighting":
                        var lightingProperty = typeof(LightingSettings).GetProperty(propertyName);
                        if (lightingProperty != null && lightingProperty.CanRead)
                        {
                            var value = lightingProperty.GetValue(_settings.Lighting);
                            if (value is T typedValue)
                                return typedValue;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Plugin Settings", $"Error getting value for key {key}", ex);
            }

            return default;
        }


        public void SetValue<T>(string key, T value)
        {
            var parts = key.Split('.');
            
            try
            {
                // Direct AppSettings property (no dot)
                if (parts.Length == 1)
                {
                    // Special handling for VolumeHotkeys type conversion
                    if (key == "VolumeHotkeys" && value is Dictionary<string, object> volumeDict)
                    {
                        // Convert Dictionary<string, object> to Dictionary<string, ChannelHotkeys>
                        var converted = new Dictionary<string, ChannelHotkeys>();
                        foreach (var kvp in volumeDict)
                        {
                            if (kvp.Value is Dictionary<string, object> channelDict)
                            {
                                var channelHotkeys = new ChannelHotkeys();
                                if (channelDict.TryGetValue("VolumeUp", out var upObj))
                                    channelHotkeys.VolumeUp = upObj?.ToString() ?? "";
                                if (channelDict.TryGetValue("VolumeUpButton", out var upBtnObj))
                                    channelHotkeys.VolumeUpButton = upBtnObj?.ToString() ?? "";
                                if (channelDict.TryGetValue("VolumeDown", out var downObj))
                                    channelHotkeys.VolumeDown = downObj?.ToString() ?? "";
                                if (channelDict.TryGetValue("VolumeDownButton", out var downBtnObj))
                                    channelHotkeys.VolumeDownButton = downBtnObj?.ToString() ?? "";
                                
                                converted[kvp.Key] = channelHotkeys;
                            }
                        }
                        _settings.VolumeHotkeys = converted;
                        return;
                    }
                    
                    var property = typeof(AppSettings).GetProperty(key);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(_settings, value);
                    }
                    return;
                }

                
                // Dotted notation: Section.Property
                if (parts.Length != 2) return;

                var section = parts[0];
                var propertyName = parts[1];

                switch (section.ToLower())
                {
                    case "goxlr":
                        var property = typeof(GeneralSettings).GetProperty(propertyName);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(_settings.General, value);
                        }
                        break;

                    case "lighting":
                        var lightingProperty = typeof(LightingSettings).GetProperty(propertyName);
                        if (lightingProperty != null && lightingProperty.CanWrite)
                        {
                            lightingProperty.SetValue(_settings.Lighting, value);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Plugin Settings", $"Error setting value for key {key}", ex);
            }
        }

        public bool HasValue(string key)
        {
            var value = GetValue<object>(key);
            return value != null;
        }
    }
}

