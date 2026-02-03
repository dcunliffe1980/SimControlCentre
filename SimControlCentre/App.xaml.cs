using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using SimControlCentre.Services;
using SimControlCentre.Models;

namespace SimControlCentre;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private ConfigurationService _configService = new();
    private GoXLRService? _goXLRService;
    private HotkeyService? _hotkeyService;
    private HotkeyManager? _hotkeyManager;
    private DirectInputService? _directInputService;
    private ControllerManager? _controllerManager;
    private iRacingMonitorService? _iRacingMonitor;
    private UpdateCheckService? _updateCheckService;
    private TelemetryService? _telemetryService;
    private LightingService? _lightingService;
    private DeviceControlService? _deviceControlService;
    public AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// Saves current settings to disk
    /// </summary>
    public void SaveSettings()
    {
        try
        {
            _configService.Save(Settings);
            Logger.Info("App", "Settings saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error("App", "Failed to save settings", ex);
        }
    }

    public App()
    {
        // Add global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Application_Startup will be called automatically by the base.OnStartup
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        MessageBox.Show($"Fatal Error:\n\n{ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}", 
            "SimControlCentre - Fatal Error", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Error:\n\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", 
            "SimControlCentre - Error", 
            MessageBoxButton.OK, 
            MessageBoxImage.Error);
        e.Handled = true; // Prevent app from crashing
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            // Load configuration
            Settings = _configService.Load();

            // Initialize logging
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SimControlCentre",
                "logs"
            );
            Directory.CreateDirectory(logDirectory);
            
            // Initialize GoXLR diagnostics
            GoXLRDiagnostics.Initialize(logDirectory, Settings.General.EnableGoXLRDiagnostics);
            
            // Initialize update diagnostics
            UpdateDiagnostics.Initialize(logDirectory);

            // Initialize GoXLR service
            _goXLRService = new GoXLRService(Settings);

            // Initialize update check service
            _updateCheckService = new UpdateCheckService();
            
            // Subscribe to update available event
            _updateCheckService.StatusChanged += OnUpdateStatusChanged;

            // Initialize telemetry service
            _telemetryService = new TelemetryService();
            
            // Register iRacing telemetry provider
            var iRacingProvider = new iRacingTelemetryProvider();
            _telemetryService.RegisterProvider(iRacingProvider);
            
            // Start telemetry monitoring
            _telemetryService.StartAll();
            Logger.Info("App", "Telemetry service started");


            // Initialize lighting service
            _lightingService = new LightingService();
            
            // Initialize device control service
            _deviceControlService = new DeviceControlService();
            
            // Create plugin context
            var pluginContext = new PluginContext(Settings, _configService);
            
            // Create plugin loader and load all plugins
            var pluginLoader = new PluginLoader(pluginContext);
            var loadedPlugins = pluginLoader.LoadPlugins();
            
            Logger.Info("App", $"Loaded {loadedPlugins.Count} plugin(s) from plugins directory");
            
            // Register lighting plugins
            var lightingPlugins = PluginLoader.GetLightingPlugins(loadedPlugins);
            foreach (var plugin in lightingPlugins)
            {
                // Check if plugin is enabled in settings
                bool isEnabled = Settings.Lighting?.EnabledPlugins?.GetValueOrDefault(plugin.PluginId, true) ?? true;
                plugin.IsEnabled = isEnabled;
                
                if (isEnabled)
                {
                    Logger.Info("App", $"Registering lighting plugin: {plugin.DisplayName}");
                    
                    // Apply saved configuration if available
                    if (plugin.PluginId == "goxlr" && Settings.Lighting?.GoXlrSelectedButtons?.Any() == true)
                    {
                        plugin.ApplyConfiguration(new Dictionary<string, object>
                        {
                            { "selected_buttons", Settings.Lighting.GoXlrSelectedButtons }
                        });
                        Logger.Info("App", $"Applied button configuration: {string.Join(", ", Settings.Lighting.GoXlrSelectedButtons)}");
                    }
                    
                    _lightingService.RegisterPlugin(plugin);
                }
                else
                {
                    Logger.Info("App", $"Plugin {plugin.DisplayName} is disabled in settings");
                }
            }
            
            // Register device control plugins (always register, just set IsEnabled)
            var deviceControlPlugins = PluginLoader.GetDeviceControlPlugins(loadedPlugins);
            foreach (var plugin in deviceControlPlugins)
            {
                // Check if component is enabled
                bool isEnabled = Settings.Lighting?.EnabledPlugins?.GetValueOrDefault("goxlr-device-control", true) ?? true;
                plugin.IsEnabled = isEnabled;
                
                // Always register the plugin, even if disabled
                // The service will respect the IsEnabled property
                Logger.Info("App", $"Registering device control plugin: {plugin.DisplayName} (Enabled: {isEnabled})");
                _deviceControlService.RegisterPlugin(plugin);
            }
            

            
            // Initialize all lighting plugins and wait for completion
            Logger.Info("App", "Initializing lighting devices...");
            _ = Task.Run(async () => 
            {
                await _lightingService.InitializeAsync();
                Logger.Info("App", "Lighting devices initialized and ready");
            });
            
            // Subscribe to flag changes and update lighting
            _telemetryService.FlagChanged += async (s, e) =>
            {
                // Ensure devices are available before updating
                if (_lightingService.Devices.Any())
                {
                    await _lightingService.UpdateForFlagAsync(e.NewFlag);
                }
            };
            
            Logger.Info("App", "Plugin system initialized");


            // Always warm up the GoXLR API connection on startup
            _ = Task.Run(async () =>
            {
                // Wait indefinitely for GoXLR Daemon to be running
                Console.WriteLine("[App] Waiting for GoXLR Daemon to start...");
                Logger.Info("App", "Waiting for GoXLR Daemon...");
                await WaitForGoXLRUtilityIndefinitely();
                
                Console.WriteLine("[App] GoXLR Daemon detected! Warming up API connection...");
                Logger.Info("App", "GoXLR Daemon detected, starting warmup...");
                
                // Give the daemon time to fully initialize its API
                await Task.Delay(5000);
                
                // Test connection
                Console.WriteLine("[App] Testing GoXLR connection...");
                Logger.Info("App", "Testing connection...");
                bool isConnected = await _goXLRService.IsConnectedAsync();
                
                if (!isConnected)
                {
                    Console.WriteLine("[App] Connection test failed - API might still be initializing");
                    Logger.Warning("App", "Connection test failed, retrying...");
                    // Wait a bit more and retry
                    await Task.Delay(5000);
                    isConnected = await _goXLRService.IsConnectedAsync();
                }
                
                if (isConnected)
                {
                    Console.WriteLine("[App] Successfully connected to GoXLR!");
                    Logger.Info("App", "✓ GoXLR connected successfully");
                    
                    // Pre-warm the volume cache BEFORE other warmup
                    Console.WriteLine("[App] Pre-warming volume cache for all enabled channels...");
                    Logger.Info("App", "Warming volume cache...");
                    await Task.Delay(1000);
                    
                    foreach (var channel in Settings.EnabledChannels)
                    {
                        Console.WriteLine($"[App] Warming cache for {channel}...");
                        await _goXLRService.WarmVolumeCacheAsync(channel);
                        Console.WriteLine($"[App] Cache warmed for {channel}");
                    }
                    Console.WriteLine("[App] Volume cache pre-warming complete!");
                    Logger.Info("App", "✓ Volume cache warmed");
                    
                    // NEW: Warm up button color API for lighting
                    Console.WriteLine("[App] Warming up button color API...");
                    Logger.Info("App", "Warming button color API...");
                    await _goXLRService.WarmButtonColorApiAsync();
                    Console.WriteLine("[App] Button color API warmed!");
                    Logger.Info("App", "✓ Button color API warmed");
                    
                    
                    // Removed notification popup - cache is ready silently
                }
                else
                {
                    Console.WriteLine("[App] Could not connect to GoXLR API");
                }
                
                // Auto-detect serial if not configured
                if (string.IsNullOrWhiteSpace(Settings.General.SerialNumber))
                {
                    try
                    {
                        Console.WriteLine("[App] Starting auto-detect...");
                        
                        // Get devices to extract serial
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                        var response = await httpClient.GetFromJsonAsync<GoXLRFullResponse>(
                            Settings.General.ApiEndpoint + "/api/get-devices");
                        
                        if (response?.Mixers != null && response.Mixers.Count == 1)
                        {
                            var serial = response.Mixers.Keys.First();
                            Settings.General.SerialNumber = serial;
                            Console.WriteLine($"[App] Auto-detected serial: {serial}");
                            
                            // Save to config
                            Dispatcher.Invoke(() =>
                            {
                                _configService.Save(Settings);
                                
                                // Removed notification popup for auto-detect
                            });
                        }
                        else
                        {
                            Console.WriteLine($"[App] Auto-detect found {response?.Mixers?.Count ?? 0} devices");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[App] Auto-detect failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[App] Serial already configured: {Settings.General.SerialNumber}");
                }
            });

            // Create system tray icon
            _notifyIcon = new TaskbarIcon
            {
                IconSource = CreateSimpleIcon(),
                ToolTipText = "SimControlCentre - GoXLR Control",
                ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayContextMenu"),
                Visibility = System.Windows.Visibility.Visible
            };

            // Double-click tray icon to open settings
            _notifyIcon.TrayLeftMouseDown += (s, args) => OpenSettingsWindow();

            // Initialize iRacing monitor service
            _iRacingMonitor = new iRacingMonitorService(Settings);
            _iRacingMonitor.StartMonitoring();

            // Create main window but don't show it yet
            _mainWindow = new MainWindow(Settings, _configService, _goXLRService, _iRacingMonitor);
            
            // Refresh Device Control tab now that plugins are loaded
            _mainWindow.UpdateDeviceControlTabVisibility();
            
            // Initialize hotkey service with main window handle
            var windowInterop = new System.Windows.Interop.WindowInteropHelper(_mainWindow);
            windowInterop.EnsureHandle(); // Force window handle creation
            
            _hotkeyService = new HotkeyService();
            _hotkeyService.Initialize(windowInterop.Handle);

            
            // Initialize hotkey manager with device control service
            _hotkeyManager = new HotkeyManager(_hotkeyService, _deviceControlService!, Settings);
            var registeredCount = _hotkeyManager.RegisterAllHotkeys();
            
            // Initialize controller input with device control service
            _directInputService = new DirectInputService();
            _controllerManager = new ControllerManager(_directInputService, _deviceControlService!, Settings);
            _controllerManager.InitializeControllers(windowInterop.Handle);
            
            // Initialize Controllers Tab now that DirectInputService exists
            if (_mainWindow != null)
            {
                _mainWindow.InitializeControllersTab(_directInputService);
            }
            
            // Removed notification popup for hotkeys
            Console.WriteLine($"Registered {registeredCount} keyboard hotkeys");
            
            // Show window if not set to start minimized
            if (!Settings.Window.StartMinimized)
            {
                OpenSettingsWindow();
            }
            
            // Start update check in background if enabled
            if (Settings.General.CheckForUpdatesOnStartup && _updateCheckService != null)
            {
                _updateCheckService.StartCheckInBackground();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", 
                "SimControlCentre Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void OnUpdateStatusChanged(object? sender, UpdateCheckStatusChangedEventArgs e)
    {
        // Only show popup if update is available
        if (e.Status == UpdateCheckStatus.UpdateAvailable && e.UpdateInfo != null)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessageBox.Show(
                    $"A new version is available!\n\nCurrent: v{e.UpdateInfo.CurrentVersion}\nLatest: v{e.UpdateInfo.LatestVersion}\n\nOpen Settings and go to the About section to download the update.",
                    "Update Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }));
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // Window settings are saved by MainWindow's OnClosed handler
        
        _telemetryService?.Dispose();
        _iRacingMonitor?.Dispose();
        _hotkeyManager?.Dispose();
        _hotkeyService?.Dispose();
        _controllerManager?.Dispose();
        _directInputService?.Dispose();
        _goXLRService?.Dispose();
        _notifyIcon?.Dispose();
    }

    /// <summary>
    /// Waits for GoXLR Utility process to be running
    /// </summary>
    private async Task<bool> WaitForGoXLRUtility(TimeSpan timeout)
    {
        var startTime = DateTime.Now;
        var checkInterval = TimeSpan.FromSeconds(5);
        
        while (DateTime.Now - startTime < timeout)
        {
            // Check if goxlr-daemon process is running
            var processes = System.Diagnostics.Process.GetProcessesByName("goxlr-daemon");
            if (processes.Length > 0)
            {
                Console.WriteLine($"[App] GoXLR Daemon found (PID: {processes[0].Id})");
                return true;
            }
            
            // Not found yet, wait and retry
            var elapsed = DateTime.Now - startTime;
            Console.WriteLine($"[App] GoXLR Daemon not running yet... ({elapsed.TotalSeconds:F0}s elapsed, will retry)");
            await Task.Delay(checkInterval);
        }
        
        Console.WriteLine("[App] Timeout waiting for GoXLR Daemon");
        return false;
    }

    private async Task WaitForGoXLRUtilityIndefinitely()
    {
        int checkCount = 0;
        while (true)
        {
            var processes = Process.GetProcessesByName("goxlr-utility");
            if (processes.Length > 0)
            {
                Console.WriteLine($"[App] GoXLR Daemon found after {checkCount * 2} seconds (PID: {processes[0].Id})");
                return;
            }
            
            checkCount++;
            if (checkCount % 30 == 0) // Every minute
            {
                Console.WriteLine($"[App] Still waiting for GoXLR Daemon... ({checkCount * 2}s elapsed)");
            }
            
            await Task.Delay(2000); // Check every 2 seconds
        }
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        OpenSettingsWindow();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    private void OpenSettingsWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(Settings, _configService, _goXLRService!, _iRacingMonitor);
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public static ConfigurationService GetConfigService()
    {
        return ((App)Current)._configService;
    }

    public static AppSettings GetSettings()
    {
        return ((App)Current).Settings;
    }

    public static GoXLRService? GetGoXLRService()
    {
        return ((App)Current)._goXLRService;
    }

    public static HotkeyManager? GetHotkeyManager()
    {
        return ((App)Current)._hotkeyManager;
    }

    public static DirectInputService? GetDirectInputService()
    {
        return ((App)Current)._directInputService;
    }

    public static UpdateCheckService? GetUpdateCheckService()
    {
        return ((App)Current)._updateCheckService;
    }

    public static TelemetryService? GetTelemetryService()
    {
        return ((App)Current)._telemetryService;
    }

    public static LightingService? GetLightingService()
    {
        return ((App)Current)._lightingService;
    }

    public static DeviceControlService? GetDeviceControlService()
    {
        return ((App)Current)._deviceControlService;
    }

    private void ToggleHotkeys_Click(object sender, RoutedEventArgs e)
    {
        if (_hotkeyManager == null)
            return;

        if (sender is MenuItem menuItem)
        {
            if (menuItem.IsChecked)
            {
                // Enable hotkeys
                var count = _hotkeyManager.RegisterAllHotkeys();
                // Removed notification popup
                Console.WriteLine("[App] Hotkeys enabled");
            }
            else
            {
                // Disable hotkeys
                _hotkeyManager.TemporaryUnregisterAll();
                // Removed notification popup
                Console.WriteLine("[App] Hotkeys disabled");
            }
        }
    }

    public void ShowVolumeNotification(string message)
    {
        // Removed notification popup for volume changes
        Console.WriteLine($"[App] Volume: {message}");
    }

    private System.Windows.Media.ImageSource CreateSimpleIcon()
    {
        // Create a simple 16x16 blue square with "SC" text
        var width = 16;
        var height = 16;
        
        var drawingVisual = new System.Windows.Media.DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            // Blue background
            drawingContext.DrawRectangle(
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                null,
                new Rect(0, 0, width, height));
            
            // White "SC" text
            var formattedText = new System.Windows.Media.FormattedText(
                "SC",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new System.Windows.Media.Typeface("Arial"),
                10,
                System.Windows.Media.Brushes.White,
                96);
            
            drawingContext.DrawText(formattedText, new Point(1, 1));
        }
        
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
            width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(drawingVisual);
        
        return bitmap;
    }
}


