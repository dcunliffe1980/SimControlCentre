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
        
        // Advanced options
        public int StartOrder { get; set; } = 0; // Lower numbers start first (0 = no specific order)
        public string? DependsOnApp { get; set; } = null; // Name of app that must start first
        public int GracefulShutdownTimeoutSeconds { get; set; } = 5; // How long to wait for graceful exit
        public bool EnableWatchdog { get; set; } = true; // Monitor and restart if crashed
        public bool VerifyStartup { get; set; } = true; // Check if app actually started successfully
        
        // For UI display
        public string IconPath { get; set; } = string.Empty;
        
        // Runtime state (not saved to config)
        [System.Text.Json.Serialization.JsonIgnore]
        public int ProcessId { get; set; } = 0;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsRunning { get; set; } = false;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool WasStoppedByUs { get; set; } = false;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public bool WasRunningBeforeStoppingForRacing { get; set; } = false;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime? LastHealthCheck { get; set; } = null;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public int RestartAttempts { get; set; } = 0;
    }
}


