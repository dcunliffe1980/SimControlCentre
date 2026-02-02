namespace SimControlCentre.Models
{
    /// <summary>
    /// Track flag status
    /// </summary>
    public enum FlagStatus
    {
        None = 0,           // No flag
        Green = 1,          // Green flag - racing
        Yellow = 2,         // Yellow/caution flag
        Blue = 3,           // Blue flag - being lapped
        White = 4,          // White flag - last lap
        Checkered = 5,      // Checkered flag - race end
        Red = 6,            // Red flag - race stopped
        Black = 7,          // Black flag - disqualification
        Debris = 8,         // Debris on track
        Crossed = 9,        // Crossed flags (unclear conditions)
        YellowWaving = 10,  // Waving yellow (danger)
        OneLapToGreen = 11  // One lap to green
    }

    /// <summary>
    /// Generic telemetry data that all sims should provide
    /// </summary>
    public class TelemetryData
    {
        // Session Info
        public bool IsConnected { get; set; }
        public string SessionType { get; set; } = "Unknown"; // Practice, Qualify, Race, etc.
        public int SessionState { get; set; } = 0; // 0=Invalid, 1=GetInCar, 2=Warmup, 3=Racing, etc.
        public double SessionTime { get; set; }
        public double SessionTimeRemaining { get; set; }
        public int CurrentLap { get; set; }
        public int TotalLaps { get; set; }
        
        // Flag Status
        public FlagStatus CurrentFlag { get; set; } = FlagStatus.None;
        public bool IsUnderCaution { get; set; }
        
        // Car State
        public float Speed { get; set; } // m/s or mph
        public float Rpm { get; set; }
        public int Gear { get; set; }
        public float Throttle { get; set; } // 0-1
        public float Brake { get; set; } // 0-1
        public float Clutch { get; set; } // 0-1
        
        // Position
        public int Position { get; set; }
        public int ClassPosition { get; set; }
        public int TotalDrivers { get; set; }
        
        // Track
        public string TrackName { get; set; } = "Unknown";
        public string CarName { get; set; } = "Unknown";
        public float TrackTemperature { get; set; } // Celsius
        public float AirTemperature { get; set; } // Celsius
        
        // Timing
        public float LastLapTime { get; set; }
        public float BestLapTime { get; set; }
        
        // Fuel
        public float FuelLevel { get; set; }
        public float FuelUsePerLap { get; set; }
        
        // Damage
        public float DamageLevel { get; set; } // 0-1
        
        // Pit
        public bool IsInPits { get; set; }
        public bool IsPitLimiter { get; set; }
        
        // Timestamp
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Source
        public string SourceSim { get; set; } = "Unknown";
    }

    /// <summary>
    /// Event args for telemetry updates
    /// </summary>
    public class TelemetryUpdatedEventArgs : EventArgs
    {
        public TelemetryData Data { get; set; }
        
        public TelemetryUpdatedEventArgs(TelemetryData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Event args for flag changes
    /// </summary>
    public class FlagChangedEventArgs : EventArgs
    {
        public FlagStatus OldFlag { get; set; }
        public FlagStatus NewFlag { get; set; }
        
        public FlagChangedEventArgs(FlagStatus oldFlag, FlagStatus newFlag)
        {
            OldFlag = oldFlag;
            NewFlag = newFlag;
        }
    }
}
