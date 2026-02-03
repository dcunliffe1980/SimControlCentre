using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimControlCentre.Models;

namespace SimControlCentre.Services;

/// <summary>
/// Service for loading and saving application configuration
/// </summary>
public class ConfigurationService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SimControlCentre"
    );

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Loads configuration from file, or creates default if not found
    /// </summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                var defaultSettings = CreateDefaultSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? CreateDefaultSettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return CreateDefaultSettings();
        }
    }

    /// <summary>
    /// Saves configuration to file
    /// </summary>
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates default configuration with predefined profiles
    /// </summary>
    private AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            General = new GeneralSettings
            {
                SerialNumber = string.Empty, // User must configure
                VolumeStep = 10,
                VolumeCacheTimeMs = 30000, // Increased from 5000ms to 30000ms (30 seconds)
                ApiEndpoint = "http://localhost:14564"
            },
            EnabledChannels = new List<string>
            {
                "Game",
                "Music",
                "Chat",
                "System"
            },
            ProfileHotkeys = new Dictionary<string, string>
            {
                { "Speakers - Personal", string.Empty },
                { "Headphones - Personal (Online)", string.Empty },
                { "Headphones - Work", string.Empty },
                { "iRacing", string.Empty }
            },
            ProfileButtons = new Dictionary<string, string>(),
            VolumeHotkeys = new Dictionary<string, ChannelHotkeys>
            {
                // Example hotkeys - user can customize these
                { "Game", new ChannelHotkeys 
                    { 
                        VolumeUp = "Ctrl+Shift+Up", 
                        VolumeDown = "Ctrl+Shift+Down" 
                    } 
                },
                { "Music", new ChannelHotkeys 
                    { 
                        VolumeUp = "Ctrl+Shift+PageUp", 
                        VolumeDown = "Ctrl+Shift+PageDown" 
                    } 
                },
                { "Chat", new ChannelHotkeys() },
                { "System", new ChannelHotkeys() }
            },
            ControllerMappings = new List<ControllerMapping>(),
            Window = new WindowSettings
            {
                Width = 900,
                Height = 700,
                Left = 100,
                Top = 100,
                StartMinimized = true
            }
        };
    }

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string GetConfigFilePath() => ConfigFilePath;

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    public bool ConfigExists() => File.Exists(ConfigFilePath);
}

