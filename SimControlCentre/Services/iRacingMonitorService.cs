using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SimControlCentre.Models;

namespace SimControlCentre.Services
{
    /// <summary>
    /// Monitors iRacing process and manages external applications lifecycle
    /// </summary>
    public class iRacingMonitorService : IDisposable
    {
        private readonly AppSettings _settings;
        private Timer? _monitorTimer;
        private bool _iRacingWasRunning = false;
        private readonly Dictionary<string, int> _runningApps = new(); // ExternalApp.Name -> ProcessId
        private bool _isDisposed = false;

        // Windows API for minimizing windows
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_MINIMIZE = 6;

        public event EventHandler<iRacingStateChangedEventArgs>? iRacingStateChanged;

        public iRacingMonitorService(AppSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Start monitoring for iRacing process
        /// </summary>
        public void StartMonitoring(int intervalMs = 3000)
        {
            _monitorTimer = new Timer(CheckiRacingState, null, 0, intervalMs);
            Logger.Info("iRacingMonitor", $"Started monitoring (checking every {intervalMs}ms)");
        }

        /// <summary>
        /// Stop monitoring
        /// </summary>
        public void StopMonitoring()
        {
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            Logger.Info("iRacingMonitor", "Stopped monitoring");
        }

        private void CheckiRacingState(object? state)
        {
            try
            {
                bool isRunning = IsiRacingRunning();

                // State changed: iRacing started
                if (isRunning && !_iRacingWasRunning)
                {
                    Logger.Info("iRacingMonitor", "========================================");
                    Logger.Info("iRacingMonitor", "iRacing STARTED");
                    Logger.Info("iRacingMonitor", "========================================");
                    _iRacingWasRunning = true;
                    
                    iRacingStateChanged?.Invoke(this, new iRacingStateChangedEventArgs { IsRunning = true });
                    
                    // Stop running apps that should stop when iRacing starts
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await StopRunningAppsForRacing();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("iRacingMonitor", "ERROR stopping apps for racing", ex);
                        }
                    });
                    
                    // Start external apps (async, don't block timer)
                    Logger.Debug("iRacingMonitor", "Launching Task.Run to start apps...");
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await StartExternalApps(disconnected: false);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("iRacingMonitor", "ERROR in Task.Run", ex);
                        }
                    });
                }
                // State changed: iRacing stopped
                else if (!isRunning && _iRacingWasRunning)
                {
                    Logger.Info("iRacingMonitor", "========================================");
                    Logger.Info("iRacingMonitor", "iRacing STOPPED");
                    Logger.Info("iRacingMonitor", "========================================");
                    _iRacingWasRunning = false;
                    
                    iRacingStateChanged?.Invoke(this, new iRacingStateChangedEventArgs { IsRunning = false });
                    
                    // Stop external apps (async, don't block timer)
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await StopExternalApps();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("iRacingMonitor", "ERROR in Task.Run (stop)", ex);
                        }
                    });
                    
                    // Restart apps that were stopped when iRacing started
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await RestartStoppedApps();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("iRacingMonitor", "ERROR restarting stopped apps", ex);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("iRacingMonitor", "Error checking state", ex);
            }
        }

        /// <summary>
        /// Check if iRacing is currently running
        /// </summary>
        public bool IsiRacingRunning()
        {
            var processes = Process.GetProcessesByName("iRacingSim64DX11");
            return processes.Length > 0;
        }

        /// <summary>
        /// Start all configured external applications
        /// </summary>
        private async Task StartExternalApps(bool disconnected)
        {
            Logger.Info("iRacingMonitor", $"StartExternalApps called (disconnected: {disconnected})");
            
            var appsToStart = _settings.ExternalApps
                .Where(a => a.AppType == ExternalAppType.StartWithRacing)
                .ToList();
            
            Logger.Info("iRacingMonitor", $"Found {appsToStart.Count} app(s) configured to start");
            
            if (appsToStart.Count == 0)
            {
                Logger.Warning("iRacingMonitor", "No external apps configured to start");
                return;
            }

            foreach (var app in appsToStart)
            {
                try
                {
                    Logger.Debug("iRacingMonitor", $"Processing app: {app.Name}");
                    Logger.Debug("iRacingMonitor", $"  - StartWithiRacing: {app.StartWithiRacing}");
                    Logger.Debug("iRacingMonitor", $"  - DelayStartSeconds: {app.DelayStartSeconds}");
                    
                    // Check if this specific app is enabled
                    if (!app.StartWithiRacing)
                    {
                        Logger.Debug("iRacingMonitor", $"Skipping {app.Name} - StartWithiRacing is disabled");
                        continue;
                    }

                    // Check if already running
                    if (_runningApps.ContainsKey(app.Name))
                    {
                        Logger.Info("iRacingMonitor", $"{app.Name} already running (PID: {_runningApps[app.Name]})");
                        continue;
                    }

                    // Apply delay
                    if (app.DelayStartSeconds > 0)
                    {
                        Logger.Info("iRacingMonitor", $"Delaying start of {app.Name} by {app.DelayStartSeconds}s");
                        await Task.Delay(app.DelayStartSeconds * 1000);
                    }

                    Logger.Info("iRacingMonitor", $"Starting {app.Name}...");
                    Logger.Debug("iRacingMonitor", $"  - Path: {app.ExecutablePath}");
                    Logger.Debug("iRacingMonitor", $"  - Args: {app.Arguments}");
                    Logger.Debug("iRacingMonitor", $"  - Hidden: {app.StartHidden}");

                    // Start the app
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.ExecutablePath,
                        Arguments = app.Arguments,
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(app.ExecutablePath) ?? ""
                    };

                    if (app.StartHidden)
                    {
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    }

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        var initialPid = process.Id;
                        Logger.Info("iRacingMonitor", $"Initial process started for {app.Name} (PID: {initialPid})");
                        
                        // Wait a bit to see if it spawns child processes (like SimHub does)
                        await Task.Delay(2000);
                        
                        // Check if the process still exists or if children were spawned
                        var actualProcess = await FindActualProcess(app.ExecutablePath, initialPid);
                        
                        _runningApps[app.Name] = actualProcess.Id;
                        app.ProcessId = actualProcess.Id;
                        app.IsRunning = true;
                        
                        if (actualProcess.Id != initialPid)
                        {
                            Logger.Info("iRacingMonitor", $"Detected child process for {app.Name} (PID: {actualProcess.Id})");
                        }
                        
                        // Force minimize if start hidden is enabled
                        if (app.StartHidden)
                        {
                            await MinimizeProcessWindow(actualProcess, app.Name);
                        }
                        
                        Logger.Info("iRacingMonitor", $"? Started {app.Name} (PID: {actualProcess.Id})");
                    }
                    else
                    {
                        Logger.Error("iRacingMonitor", $"? Failed to start {app.Name} - Process.Start returned null");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("iRacingMonitor", $"? Failed to start {app.Name}", ex);
                }
            }
            
            Logger.Info("iRacingMonitor", "StartExternalApps completed");
        }

        /// <summary>
        /// Stop all running external applications (in parallel)
        /// </summary>
        private async Task StopExternalApps()
        {
            Logger.Info("iRacingMonitor", "StopExternalApps called");
            
            var appsToStop = _settings.ExternalApps
                .Where(a => a.AppType == ExternalAppType.StartWithRacing && a.StopWithiRacing)
                .ToList();
            
            // Create tasks for each app that needs to stop
            var stopTasks = new List<Task>();
            
            foreach (var app in appsToStop)
            {
                // Create a task for this app's shutdown (runs in parallel)
                var stopTask = Task.Run(async () =>
                {
                    try
                    {
                        // Apply delay
                        if (app.DelayStopSeconds > 0)
                        {
                            Logger.Info("iRacingMonitor", $"Delaying stop of {app.Name} by {app.DelayStopSeconds}s");
                            await Task.Delay(app.DelayStopSeconds * 1000);
                        }

                        // Stop the app
                        var processId = _runningApps[app.Name];
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            
                            // Try graceful shutdown first
                            Logger.Info("iRacingMonitor", $"Attempting graceful shutdown of {app.Name} (PID: {processId})");
                            bool closedGracefully = process.CloseMainWindow();
                            
                            if (closedGracefully)
                            {
                                // Wait up to 5 seconds for graceful exit
                                bool exited = process.WaitForExit(5000);
                                
                                if (exited)
                                {
                                    Logger.Info("iRacingMonitor", $"? {app.Name} closed gracefully");
                                }
                                else
                                {
                                    Logger.Warning("iRacingMonitor", $"{app.Name} did not exit gracefully, forcing kill");
                                    process.Kill();
                                    Logger.Info("iRacingMonitor", $"? Forcefully killed {app.Name}");
                                }
                            }
                            else
                            {
                                // No main window or couldn't close, force kill
                                Logger.Warning("iRacingMonitor", $"{app.Name} has no main window or could not close, forcing kill");
                                process.Kill();
                                Logger.Info("iRacingMonitor", $"? Forcefully killed {app.Name}");
                            }
                        }
                        catch (ArgumentException)
                        {
                            Logger.Info("iRacingMonitor", $"{app.Name} already exited");
                        }

                        _runningApps.Remove(app.Name);
                        app.ProcessId = 0;
                        app.IsRunning = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("iRacingMonitor", $"Failed to stop {app.Name}", ex);
                    }
                });
                
                stopTasks.Add(stopTask);
            }
            
            // Wait for all stop tasks to complete (parallel execution)
            if (stopTasks.Count > 0)
            {
                Logger.Info("iRacingMonitor", $"Stopping {stopTasks.Count} app(s) in parallel...");
                await Task.WhenAll(stopTasks);
            }
            
            Logger.Info("iRacingMonitor", "StopExternalApps completed");
        }

        /// <summary>
        /// Manually start a specific external app (for testing)
        /// </summary>
        public async Task<bool> StartApp(ExternalApp app)
        {
            try
            {
                Logger.Info("iRacingMonitor", $"Manual start requested for {app.Name}");
                
                if (_runningApps.ContainsKey(app.Name))
                {
                    Logger.Warning("iRacingMonitor", $"{app.Name} already running");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = app.ExecutablePath,
                    Arguments = app.Arguments,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(app.ExecutablePath) ?? ""
                };

                if (app.StartHidden)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    _runningApps[app.Name] = process.Id;
                    app.ProcessId = process.Id;
                    app.IsRunning = true;
                    
                    // Force minimize if start hidden is enabled
                    if (app.StartHidden)
                    {
                        await MinimizeProcessWindow(process, app.Name);
                    }
                    
                    Logger.Info("iRacingMonitor", $"? Manually started {app.Name} (PID: {process.Id})");
                    return true;
                }

                Logger.Error("iRacingMonitor", $"? Failed to manually start {app.Name} - Process.Start returned null");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("iRacingMonitor", $"Failed to manually start {app.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Manually stop a specific external app
        /// </summary>
        public bool StopApp(ExternalApp app)
        {
            try
            {
                Logger.Info("iRacingMonitor", $"Manual stop requested for {app.Name}");
                
                if (!_runningApps.ContainsKey(app.Name))
                {
                    Logger.Warning("iRacingMonitor", $"{app.Name} not running");
                    return false;
                }

                var processId = _runningApps[app.Name];
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                    Logger.Info("iRacingMonitor", $"? Manually stopped {app.Name} (PID: {processId})");
                }
                catch (ArgumentException)
                {
                    Logger.Info("iRacingMonitor", $"{app.Name} already exited");
                }

                _runningApps.Remove(app.Name);
                app.ProcessId = 0;
                app.IsRunning = false;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("iRacingMonitor", $"Failed to manually stop {app.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Stop currently running applications when iRacing starts (to free resources)
        /// </summary>
        private async Task StopRunningAppsForRacing()
        {
            Logger.Info("iRacingMonitor", "StopRunningAppsForRacing called");
            
            var appsToStop = _settings.ExternalApps
                .Where(a => a.AppType == ExternalAppType.StopForRacing)
                .ToList();
            
            if (appsToStop.Count == 0)
            {
                Logger.Debug("iRacingMonitor", "No apps configured to stop when iRacing starts");
                return;
            }
            
            Logger.Info("iRacingMonitor", $"Checking {appsToStop.Count} app(s) to stop for racing...");
            
            // Create parallel stop tasks
            var stopTasks = new List<Task>();
            
            foreach (var app in appsToStop)
            {
                var stopTask = Task.Run(async () =>
                {
                    try
                    {
                        var exeName = System.IO.Path.GetFileNameWithoutExtension(app.ExecutablePath);
                        var runningProcesses = Process.GetProcessesByName(exeName);
                        
                        if (runningProcesses.Length == 0)
                        {
                            Logger.Debug("iRacingMonitor", $"{app.Name} is not running, skipping");
                            return;
                        }
                        
                        Logger.Info("iRacingMonitor", $"Found {runningProcesses.Length} instance(s) of {app.Name} running");
                        
                        // Stop all instances
                        foreach (var process in runningProcesses)
                        {
                            try
                            {
                                Logger.Info("iRacingMonitor", $"Stopping {app.Name} (PID: {process.Id}) for racing...");
                                
                                // Try graceful shutdown first
                                bool closedGracefully = process.CloseMainWindow();
                                
                                if (closedGracefully)
                                {
                                    bool exited = process.WaitForExit(3000);
                                    if (exited)
                                    {
                                        Logger.Info("iRacingMonitor", $"? {app.Name} closed gracefully");
                                    }
                                    else
                                    {
                                        process.Kill();
                                        Logger.Info("iRacingMonitor", $"? Forcefully killed {app.Name}");
                                    }
                                }
                                else
                                {
                                    process.Kill();
                                    Logger.Info("iRacingMonitor", $"? Forcefully killed {app.Name}");
                                }
                                
                                // Mark that we stopped it so we can restart it later
                                app.WasStoppedByUs = true;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("iRacingMonitor", $"Failed to stop {app.Name} instance", ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("iRacingMonitor", $"Error stopping {app.Name} for racing", ex);
                    }
                });
                
                stopTasks.Add(stopTask);
            }
            
            if (stopTasks.Count > 0)
            {
                await Task.WhenAll(stopTasks);
            }
            
            Logger.Info("iRacingMonitor", "StopRunningAppsForRacing completed");
        }

        /// <summary>
        /// Restart applications that were stopped when iRacing started
        /// </summary>
        private async Task RestartStoppedApps()
        {
            Logger.Info("iRacingMonitor", "RestartStoppedApps called");
            
            var appsToRestart = _settings.ExternalApps
                .Where(a => a.WasStoppedByUs && a.RestartWhenIRacingStops)
                .ToList();
            
            if (appsToRestart.Count == 0)
            {
                Logger.Debug("iRacingMonitor", "No apps to restart");
                return;
            }
            
            Logger.Info("iRacingMonitor", $"Restarting {appsToRestart.Count} app(s)...");
            
            foreach (var app in appsToRestart)
            {
                try
                {
                    // Apply delay before restarting
                    if (app.DelayRestartSeconds > 0)
                    {
                        Logger.Info("iRacingMonitor", $"Delaying restart of {app.Name} by {app.DelayRestartSeconds}s");
                        await Task.Delay(app.DelayRestartSeconds * 1000);
                    }
                    
                    Logger.Info("iRacingMonitor", $"Restarting {app.Name}...");
                    
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.ExecutablePath,
                        Arguments = app.Arguments,
                        UseShellExecute = true,
                        WorkingDirectory = System.IO.Path.GetDirectoryName(app.ExecutablePath) ?? ""
                    };

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        Logger.Info("iRacingMonitor", $"? Restarted {app.Name} (PID: {process.Id})");
                        
                        // Close windows continuously for 30 seconds if configured (for stubborn apps)
                        if (app.RestartHidden)
                        {
                            _ = Task.Run(async () => await ContinuouslyCloseWindows(process, app.Name, 30));
                        }
                        
                        app.WasStoppedByUs = false; // Clear the flag
                    }
                    else
                    {
                        Logger.Error("iRacingMonitor", $"? Failed to restart {app.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("iRacingMonitor", $"Failed to restart {app.Name}", ex);
                }
            }
            
            Logger.Info("iRacingMonitor", "RestartStoppedApps completed");
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _monitorTimer?.Dispose();
            _monitorTimer = null;
            _isDisposed = true;
        }

        /// <summary>
        /// Helper method to minimize a process window with retry logic
        /// </summary>
        private async Task MinimizeProcessWindow(Process process, string appName)
        {
            // Try up to 5 times with increasing delays
            int[] delays = { 500, 1000, 1500, 2000, 3000 };
            
            foreach (var delay in delays)
            {
                await Task.Delay(delay);
                
                try
                {
                    process.Refresh();
                    if (process.HasExited)
                    {
                        Logger.Warning("iRacingMonitor", $"{appName} exited before window could be minimized");
                        return;
                    }

                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        ShowWindow(handle, SW_MINIMIZE);
                        Logger.Info("iRacingMonitor", $"? Minimized window for {appName} (attempt after {delay}ms)");
                        return; // Success!
                    }
                    
                    Logger.Debug("iRacingMonitor", $"MainWindowHandle not available for {appName}, retrying...");
                }
                catch (Exception ex)
                {
                    Logger.Warning("iRacingMonitor", $"Error minimizing {appName}: {ex.Message}");
                }
            }
            
            Logger.Warning("iRacingMonitor", $"Could not minimize {appName} after 5 attempts - window may not support minimization");
        }

        /// <summary>
        /// Helper method to close a process window (for system tray apps)
        /// </summary>
        private async Task CloseProcessWindow(Process process, string appName)
        {
            // Try up to 5 times with increasing delays
            int[] delays = { 500, 1000, 1500, 2000, 3000 };
            
            foreach (var delay in delays)
            {
                await Task.Delay(delay);
                
                try
                {
                    process.Refresh();
                    if (process.HasExited)
                    {
                        Logger.Warning("iRacingMonitor", $"{appName} exited before window could be closed");
                        return;
                    }

                    // Close the main window if it exists
                    bool closedAny = false;
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        bool closed = process.CloseMainWindow();
                        if (closed)
                        {
                            Logger.Info("iRacingMonitor", $"? Closed main window for {appName}");
                            closedAny = true;
                        }
                    }
                    
                    // Also try to close all windows belonging to this process
                    // (Some apps like MSI Centre and G Hub have multiple windows)
                    var processName = process.ProcessName;
                    var allProcesses = Process.GetProcessesByName(processName);
                    
                    foreach (var proc in allProcesses)
                    {
                        try
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero && proc.Id != process.Id)
                            {
                                bool closed = proc.CloseMainWindow();
                                if (closed)
                                {
                                    Logger.Info("iRacingMonitor", $"? Closed additional window for {appName} (PID: {proc.Id})");
                                    closedAny = true;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors for individual processes
                        }
                    }
                    
                    if (closedAny)
                    {
                        Logger.Info("iRacingMonitor", $"? Closed window(s) for {appName} (attempt after {delay}ms)");
                        return; // Success!
                    }
                    
                    Logger.Debug("iRacingMonitor", $"No windows found for {appName}, retrying...");
                }
                catch (Exception ex)
                {
                    Logger.Warning("iRacingMonitor", $"Error closing window for {appName}: {ex.Message}");
                }
            }
            
            Logger.Warning("iRacingMonitor", $"Could not close window for {appName} after 5 attempts - app may not have a closeable window");
        }

        /// <summary>
        /// Continuously monitor and close windows for an app (for stubborn multi-window apps)
        /// </summary>
        private async Task ContinuouslyCloseWindows(Process process, string appName, int durationSeconds)
        {
            Logger.Info("iRacingMonitor", $"Starting continuous window monitoring for {appName} ({durationSeconds}s)");
            
            var processName = process.ProcessName;
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(durationSeconds);
            var closedWindows = new HashSet<IntPtr>();
            
            while (DateTime.Now < endTime)
            {
                try
                {
                    // Find all processes with this name
                    var allProcesses = Process.GetProcessesByName(processName);
                    
                    foreach (var proc in allProcesses)
                    {
                        try
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero && !closedWindows.Contains(proc.MainWindowHandle))
                            {
                                bool closed = proc.CloseMainWindow();
                                if (closed)
                                {
                                    closedWindows.Add(proc.MainWindowHandle);
                                    Logger.Info("iRacingMonitor", $"? Closed window for {appName} (PID: {proc.Id}, Handle: {proc.MainWindowHandle})");
                                }
                            }
                        }
                        catch
                        {
                            // Process might have exited, ignore
                        }
                    }
                    
                    // Check every 500ms
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Logger.Warning("iRacingMonitor", $"Error in continuous window monitoring for {appName}: {ex.Message}");
                }
            }
            
            Logger.Info("iRacingMonitor", $"Finished continuous window monitoring for {appName} (closed {closedWindows.Count} window(s))");
        }

        /// <summary>
        /// Find the actual running process (handles launchers that spawn child processes)
        /// </summary>
        private async Task<Process> FindActualProcess(string executablePath, int initialPid)
        {
            try
            {
                var exeName = System.IO.Path.GetFileNameWithoutExtension(executablePath);
                
                // First check if the original process still exists
                try
                {
                    var originalProcess = Process.GetProcessById(initialPid);
                    if (!originalProcess.HasExited)
                    {
                        // Process still exists, use it
                        return originalProcess;
                    }
                }
                catch (ArgumentException)
                {
                    // Original process exited, look for child processes
                }
                
                // Look for processes matching the executable name
                var matchingProcesses = Process.GetProcessesByName(exeName);
                
                if (matchingProcesses.Length > 0)
                {
                    // Return the most recently started one
                    var newestProcess = matchingProcesses.OrderByDescending(p => p.StartTime).First();
                    Logger.Info("iRacingMonitor", $"Found matching process by name: {exeName} (PID: {newestProcess.Id})");
                    return newestProcess;
                }
                
                // Fallback: return the original process even if it might have exited
                return Process.GetProcessById(initialPid);
            }
            catch (Exception ex)
            {
                Logger.Warning("iRacingMonitor", $"Error finding actual process: {ex.Message}");
                return Process.GetProcessById(initialPid);
            }
        }
    }

    /// <summary>
    /// Event args for iRacing state changes
    /// </summary>
    public class iRacingStateChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
    }
}
