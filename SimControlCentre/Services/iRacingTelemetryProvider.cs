using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// iRacing telemetry provider using memory-mapped files
    /// </summary>
    public class iRacingTelemetryProvider : ITelemetryProvider, IDisposable
    {
        private const string MemoryMapName = "Local\\IRSDKMemMapFileName";
        private const int UpdateIntervalMs = 100; // Update 10 times per second
        
        private MemoryMappedFile? _memoryMappedFile;
        private Timer? _updateTimer;
        private bool _isConnected;
        private bool _hasValidData; // True when we've received actual session data
        private TelemetryData? _latestData;
        private bool _isDisposed;

        public string ProviderName => "iRacing";
        public bool IsConnected => _isConnected && _hasValidData; // Only connected if we have valid data

        public event EventHandler<TelemetryUpdatedEventArgs>? TelemetryUpdated;
        public event EventHandler<bool>? ConnectionChanged;

        public void Start()
        {
            Logger.Info("iRacing Telemetry", "============================================");
            Logger.Info("iRacing Telemetry", "Starting iRacing telemetry provider");
            Logger.Info("iRacing Telemetry", $"Memory map name: {MemoryMapName}");
            Logger.Info("iRacing Telemetry", $"Update interval: {UpdateIntervalMs}ms");
            Logger.Info("iRacing Telemetry", "============================================");
            
            // Start update timer
            _updateTimer = new Timer(UpdateTelemetry, null, 0, UpdateIntervalMs);
            
            Logger.Info("iRacing Telemetry", "Update timer started - will check for iRacing every 100ms");
        }

        public void Stop()
        {
            Logger.Info("iRacing Telemetry", "Stopping iRacing telemetry provider");
            
            _updateTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            Disconnect();
        }

        public TelemetryData? GetLatestData()
        {
            return _latestData;
        }

        private void UpdateTelemetry(object? state)
        {
            try
            {
                // Try to open memory-mapped file
                if (_memoryMappedFile == null)
                {
                    TryConnect();
                }

                if (_memoryMappedFile == null)
                {
                    // Still not connected - log every 100 attempts (10 seconds)
                    return;
                }
                
                if (!_isConnected)
                {
                    // File exists but not marked as connected yet
                    Logger.Warning("iRacing Telemetry", "Memory-mapped file exists but not connected?");
                    return;
                }

                // Read telemetry data
                var data = ReadTelemetryData();
                if (data != null)
                {
                    _latestData = data;
                    
                    // Fire ConnectionChanged the first time we get valid data
                    if (_isConnected && !_hasValidData)
                    {
                        _hasValidData = true;
                        Logger.Info("iRacing Telemetry", "??? VALID TELEMETRY DATA RECEIVED ???");
                        ConnectionChanged?.Invoke(this, true);
                    }
                    
                    TelemetryUpdated?.Invoke(this, new TelemetryUpdatedEventArgs(data));
                }
                else
                {
                    // No data returned - log occasionally
                    Logger.Debug("iRacing Telemetry", "ReadTelemetryData returned null");
                }
            }
            catch (FileNotFoundException)
            {
                // iRacing not running or memory map not available
                if (_isConnected)
                {
                    Logger.Info("iRacing Telemetry", "Memory-mapped file disappeared - iRacing stopped");
                    Disconnect();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("iRacing Telemetry", "Error reading telemetry", ex);
                
                // Only disconnect on repeated errors
                if (_isConnected)
                {
                    Disconnect();
                }
            }
        }

        private void TryConnect()
        {
            // Check if iRacing process is running first
            var iRacingProcesses = System.Diagnostics.Process.GetProcessesByName("iRacingSim64DX11");
            if (iRacingProcesses.Length == 0)
            {
                // iRacing not running, don't spam the logs
                return;
            }

            try
            {
                Logger.Debug("iRacing Telemetry", $"Attempting to open: {MemoryMapName}");
                
                _memoryMappedFile = MemoryMappedFile.OpenExisting(MemoryMapName);
                
                Logger.Info("iRacing Telemetry", "Memory-mapped file opened successfully!");
                
                // Verify we can read the header
                using var accessor = _memoryMappedFile.CreateViewAccessor();
                int status = accessor.ReadInt32(4); // Read status at offset 4
                int version = accessor.ReadInt32(0); // Read version at offset 0
                
                Logger.Info("iRacing Telemetry", $"Header read successfully: version={version}, status={status}");
                
                if (!_isConnected)
                {
                    _isConnected = true;
                    Logger.Info("iRacing Telemetry", "Memory-mapped file connected (waiting for session data...)");
                    // Don't fire ConnectionChanged yet - wait for valid telemetry data
                }
            }
            catch (FileNotFoundException ex)
            {
                // This is expected when iRacing isn't running
                // Only log once per minute to avoid spam
                Logger.Debug("iRacing Telemetry", $"Memory-mapped file not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error("iRacing Telemetry", $"Unexpected error opening memory-mapped file: {ex.GetType().Name}", ex);
            }
        }

        private void Disconnect()
        {
            if (_memoryMappedFile != null)
            {
                _memoryMappedFile.Dispose();
                _memoryMappedFile = null;
            }

            if (_isConnected || _hasValidData)
            {
                _isConnected = false;
                _hasValidData = false;
                Logger.Info("iRacing Telemetry", "Disconnected from iRacing");
                ConnectionChanged?.Invoke(this, false);
            }
        }

        private TelemetryData? ReadTelemetryData()
        {
            if (_memoryMappedFile == null)
            {
                Logger.Debug("iRacing Telemetry", "ReadTelemetryData: _memoryMappedFile is null");
                return null;
            }

            try
            {
                using var accessor = _memoryMappedFile.CreateViewAccessor();
                
                // Read basic header fields using byte array to avoid struct issues
                byte[] headerBytes = new byte[112]; // Read enough for the header
                accessor.ReadArray(0, headerBytes, 0, 112);
                
                int version = BitConverter.ToInt32(headerBytes, 0);
                int status = BitConverter.ToInt32(headerBytes, 4);
                int tickRate = BitConverter.ToInt32(headerBytes, 8);
                
                // Status bit 0 = connected
                if ((status & 1) == 0)
                {
                    return null;
                }

                // Read variable information - FIXED OFFSETS
                // Header structure:
                // +0: ver, +4: status, +8: tickRate
                // +12: sessionInfoUpdate, +16: sessionInfoLen, +20: sessionInfoOffset
                // +24: numVars, +28: varHeaderOffset, +32: numBuf, +36: bufLen
                int numVars = BitConverter.ToInt32(headerBytes, 24);         // FIXED: was 28
                int varHeaderOffset = BitConverter.ToInt32(headerBytes, 28); // FIXED: was 32
                int numBuf = BitConverter.ToInt32(headerBytes, 32);          // FIXED: was 36
                int bufLen = BitConverter.ToInt32(headerBytes, 36);          // FIXED: was 40
                int latestBufOffset = BitConverter.ToInt32(headerBytes, 52); // varBuf[0].bufOffset

                // Find SessionFlags variable and other telemetry
                const int VAR_HEADER_SIZE = 144;
                uint? sessionFlags = null;
                int? sessionState = null;
                float? speed = null;
                float? rpm = null;
                int? gear = null;
                
                // Scan ALL variables to find what we need
                for (int i = 0; i < numVars; i++)
                {
                    int headerPos = varHeaderOffset + (i * VAR_HEADER_SIZE);
                    
                    // Read variable header
                    byte[] varHeaderBytes = new byte[VAR_HEADER_SIZE];
                    accessor.ReadArray(headerPos, varHeaderBytes, 0, VAR_HEADER_SIZE);
                    
                    // Parse variable info
                    int varType = BitConverter.ToInt32(varHeaderBytes, 0);
                    int varOffsetInBuffer = BitConverter.ToInt32(varHeaderBytes, 4);
                    
                    // Read variable name (starts at offset 16, max 32 bytes)
                    string varName = System.Text.Encoding.ASCII.GetString(varHeaderBytes, 16, 32).TrimEnd('\0');
                    
                    if (string.IsNullOrEmpty(varName))
                        continue;
                    
                    // Calculate actual position in memory
                    int dataPos = latestBufOffset + varOffsetInBuffer;
                    
                    // Read the value based on variable name
                    byte[] valueBytes = new byte[8]; // Enough for any primitive type
                    accessor.ReadArray(dataPos, valueBytes, 0, 8);
                    
                    if (varName == "SessionFlags")
                    {
                        sessionFlags = BitConverter.ToUInt32(valueBytes, 0);
                    }
                    else if (varName == "SessionState")
                    {
                        sessionState = BitConverter.ToInt32(valueBytes, 0);
                    }
                    else if (varName == "Speed")
                    {
                        speed = BitConverter.ToSingle(valueBytes, 0);
                    }
                    else if (varName == "RPM")
                    {
                        rpm = BitConverter.ToSingle(valueBytes, 0);
                    }
                    else if (varName == "Gear")
                    {
                        gear = BitConverter.ToInt32(valueBytes, 0);
                    }
                }

                // Create telemetry data
                var data = new TelemetryData
                {
                    IsConnected = true,
                    SourceSim = "iRacing",
                    UpdatedAt = DateTime.Now,
                    SessionState = sessionState ?? 0,
                    Speed = (speed ?? 0f) * 3.6f, // Convert m/s to km/h
                    Rpm = rpm ?? 0f,
                    Gear = gear ?? 0
                };

                if (sessionFlags.HasValue)
                {
                    data.CurrentFlag = ParseFlagStatus(sessionFlags.Value);
                }
                else
                {
                    data.CurrentFlag = FlagStatus.None;
                }

                return data;
            }
            catch (Exception ex)
            {
                Logger.Error("iRacing Telemetry", "Error parsing telemetry data", ex);
                return null;
            }
        }

        private FlagStatus ParseFlagStatus(uint sessionFlags)
        {
            // iRacing session flags bit definitions
            const uint CheckeredFlag = 0x00000001;
            const uint WhiteFlag = 0x00000002;
            const uint GreenFlag = 0x00000004;
            const uint YellowFlag = 0x00000008;
            const uint RedFlag = 0x00000010;
            const uint BlueFlag = 0x00000020;
            const uint DebrisFlag = 0x00000040;
            const uint CrossedFlag = 0x00000080;
            const uint YellowWaving = 0x00000100;
            const uint OneLapToGreen = 0x00000200;
            const uint BlackFlag = 0x00010000;

            // Priority order (most important first)
            if ((sessionFlags & CheckeredFlag) != 0) return FlagStatus.Checkered;
            if ((sessionFlags & RedFlag) != 0) return FlagStatus.Red;
            if ((sessionFlags & BlackFlag) != 0) return FlagStatus.Black;
            if ((sessionFlags & WhiteFlag) != 0) return FlagStatus.White;
            if ((sessionFlags & YellowWaving) != 0) return FlagStatus.YellowWaving;
            if ((sessionFlags & YellowFlag) != 0) return FlagStatus.Yellow;
            if ((sessionFlags & OneLapToGreen) != 0) return FlagStatus.OneLapToGreen;
            if ((sessionFlags & BlueFlag) != 0) return FlagStatus.Blue;
            if ((sessionFlags & DebrisFlag) != 0) return FlagStatus.Debris;
            if ((sessionFlags & CrossedFlag) != 0) return FlagStatus.Crossed;
            if ((sessionFlags & GreenFlag) != 0) return FlagStatus.Green;

            return FlagStatus.None;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _updateTimer?.Dispose();
            _isDisposed = true;
        }
    }
}
