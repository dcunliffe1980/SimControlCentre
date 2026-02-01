namespace SimControlCentre.Models
{
    /// <summary>
    /// Type of external app management
    /// </summary>
    public enum ExternalAppType
    {
        StartWithRacing,  // Launch when iRacing starts
        StopForRacing     // Close when iRacing starts to free resources
    }

    /// <summary>
    /// Configuration for an external application to launch/stop with iRacing
    /// </summary>
    public class ExternalApp
    {
        public string Name { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public ExternalAppType AppType { get; set; } = ExternalAppType.StartWithRacing;
        
        // Options for StartWithRacing apps
        public bool StartWithiRacing { get; set; } = true; // Can be unchecked to temporarily disable
        public bool StartHidden { get; set; } = false;
        public bool StopWithiRacing { get; set; } = true;
        public int DelayStartSeconds { get; set; } = 0;
        public int DelayStopSeconds { get; set; } = 120;
        
        // Options for StopForRacing apps
        public bool RestartWhenIRacingStops { get; set; } = true;
        public bool RestartHidden { get; set; } = true; // Restart minimized
        public int DelayRestartSeconds { get; set; } = 2; // Small delay before restart
        
        // For UI display
        public string IconPath { get; set; } = string.Empty;
        
        // Runtime state (not saved to config)
        [System.Text.Json.Serialization.JsonIgnore]
        public int ProcessId { get; set; } = 0;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsRunning { get; set; } = false;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool WasStoppedByUs { get; set; } = false;
    }
}


