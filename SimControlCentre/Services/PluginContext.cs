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
            // Parse key like "GoXLR.ApiEndpoint" or "GoXLR.SerialNumber"
            var parts = key.Split('.');
            if (parts.Length != 2) return default;

            var section = parts[0];
            var propertyName = parts[1];

            try
            {
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
            if (parts.Length != 2) return;

            var section = parts[0];
            var propertyName = parts[1];

            try
            {
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
