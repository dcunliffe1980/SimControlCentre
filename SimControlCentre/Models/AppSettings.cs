namespace SimControlCentre.Models;

/// <summary>
/// Root configuration model for the application
/// </summary>
public class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public List<string> EnabledChannels { get; set; } = new();
    public Dictionary<string, string> ProfileHotkeys { get; set; } = new();
    public Dictionary<string, string> ProfileButtons { get; set; } = new(); // Controller buttons for profiles
    public Dictionary<string, ChannelHotkeys> VolumeHotkeys { get; set; } = new();
    public List<ControllerMapping> ControllerMappings { get; set; } = new();
    public WindowSettings Window { get; set; } = new();
    public List<ExternalApp> ExternalApps { get; set; } = new(); // Apps to start/stop with iRacing
}

