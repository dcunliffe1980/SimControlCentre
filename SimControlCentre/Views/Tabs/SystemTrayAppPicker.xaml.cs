using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SimControlCentre.Models;

namespace SimControlCentre.Views.Tabs
{
    public partial class SystemTrayAppPicker : Window
    {
        public ExternalApp? ResultApp { get; private set; }
        private List<RunningAppInfo> _allApps = new List<RunningAppInfo>();

        public SystemTrayAppPicker()
        {
            InitializeComponent();
            
            LoadRunningApps();
        }

        private void LoadRunningApps()
        {
            var runningApps = new List<RunningAppInfo>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Get ALL processes
            var processes = Process.GetProcesses();
            
            // Exclude system processes and our own app
            var excludedNames = new[] 
            { 
                "ApplicationFrameHost", "ShellExperienceHost", "SystemSettings",
                "TextInputHost", "SearchHost", "StartMenuExperienceHost",
                "explorer", "taskmgr", "SimControlCentre", "svchost", "System",
                "Registry", "Memory Compression", "dwm", "csrss", "wininit",
                "services", "lsass", "winlogon", "fontdrvhost", "smss",
                "conhost", "RuntimeBroker", "dllhost", "sihost", "ctfmon",
                "SecurityHealthService", "MsMpEng", "NisSrv", "SgrmBroker",
                "spoolsv", "WUDFHost", "dasHost", "audiodg", "WmiPrvSE",
                // Steam child processes
                "steamwebhelper", "steamservice", "GameOverlayUI",
                // Stream Deck helper processes (keep main StreamDeck.exe)
                "StreamDeckService", "StreamDeckHub", "StreamDeckCore",
                // Discord helper processes
                "DiscordPTB", "DiscordCanary",
                // Nvidia helper processes
                "NVDisplay.Container", "NVIDIA Web Helper", "NVIDIA Share",
                // AMD helper processes  
                "RadeonSoftware", "AMDRSServ",
                // Other common helpers
                "CefSharp.BrowserSubprocess", "node"
            };
            
            foreach (var process in processes)
            {
                if (excludedNames.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                    continue;
                
                try
                {
                    var executablePath = "";
                    
                    try
                    {
                        executablePath = process.MainModule?.FileName ?? "";
                    }
                    catch
                    {
                        continue; // Permission issues
                    }
                    
                    // Skip if no path or in system directories
                    if (string.IsNullOrWhiteSpace(executablePath) ||
                        executablePath.StartsWith(@"C:\Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    
                    // Skip common helper/subprocess patterns by filename
                    var fileName = Path.GetFileName(executablePath).ToLowerInvariant();
                    if (fileName.Contains("helper") ||
                        fileName.Contains("subprocess") ||
                        fileName.Contains("crashpad") ||
                        fileName.Contains("updater") ||
                        fileName.EndsWith("_host.exe") ||
                        fileName.EndsWith("_service.exe") && !fileName.Equals("streamdeck.exe"))
                    {
                        continue;
                    }
                    
                    // Skip child processes (only keep main executables)
                    // Check if we've already seen this exact path
                    if (seenPaths.Contains(executablePath))
                    {
                        continue;
                    }
                    
                    seenPaths.Add(executablePath);
                    
                    // Get friendly name from file metadata
                    var friendlyName = GetFriendlyName(executablePath, process.ProcessName);
                    
                    // Get description
                    var description = GetDescription(executablePath, process);
                    
                    runningApps.Add(new RunningAppInfo
                    {
                        ProcessName = friendlyName,
                        MainWindowTitle = description,
                        ExecutablePath = executablePath,
                        ProcessId = process.Id,
                        HasMainWindow = !string.IsNullOrWhiteSpace(process.MainWindowTitle)
                    });
                }
                catch
                {
                    // Skip processes we can't access
                }
            }
            
            // Sort by friendly name
            var sortedApps = runningApps
                .OrderByDescending(a => a.HasMainWindow) // Main window apps first
                .ThenBy(a => a.ProcessName)
                .ToList();
            
            _allApps = sortedApps;
            AppsListBox.ItemsSource = _allApps;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show all apps
                AppsListBox.ItemsSource = _allApps;
            }
            else
            {
                // Filter apps based on search text
                var filteredApps = _allApps.Where(app =>
                    app.ProcessName.ToLower().Contains(searchText) ||
                    app.MainWindowTitle.ToLower().Contains(searchText) ||
                    app.ExecutablePath.ToLower().Contains(searchText)
                ).ToList();
                
                AppsListBox.ItemsSource = filteredApps;
            }
        }

        private string GetFriendlyName(string executablePath, string fallbackName)
        {
            try
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(executablePath);
                
                // Try Product Name first (most user-friendly)
                if (!string.IsNullOrWhiteSpace(fileInfo.ProductName))
                {
                    return fileInfo.ProductName.Trim();
                }
                
                // Try File Description
                if (!string.IsNullOrWhiteSpace(fileInfo.FileDescription))
                {
                    return fileInfo.FileDescription.Trim();
                }
                
                // Fallback to filename without extension
                return Path.GetFileNameWithoutExtension(executablePath);
            }
            catch
            {
                return fallbackName;
            }
        }

        private string GetDescription(string executablePath, Process process)
        {
            try
            {
                // If it has a main window, show the title
                if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                {
                    return process.MainWindowTitle;
                }
                
                // Otherwise, show "(System Tray)" and the path
                var fileName = Path.GetFileName(executablePath);
                return $"(System Tray) • {fileName}";
            }
            catch
            {
                return "(Background)";
            }
        }

        private void AppItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is RunningAppInfo appInfo)
            {
                // Create ExternalApp from selected process
                ResultApp = new ExternalApp
                {
                    Name = appInfo.ProcessName,
                    ExecutablePath = appInfo.ExecutablePath,
                    Arguments = "",
                    AppType = ExternalAppType.StopForRacing,
                    RestartWhenIRacingStops = true, // Default to true
                    RestartHidden = true, // Default to restart minimized
                    DelayRestartSeconds = 2 // Default 2 second delay
                };
                
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class RunningAppInfo
    {
        public string ProcessName { get; set; } = "";
        public string MainWindowTitle { get; set; } = "";
        public string ExecutablePath { get; set; } = "";
        public int ProcessId { get; set; }
        public bool HasMainWindow { get; set; }
    }
}

