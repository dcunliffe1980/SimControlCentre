using System.Diagnostics;
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
    public AppSettings Settings { get; private set; } = new();

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

            // Initialize GoXLR service
            _goXLRService = new GoXLRService(Settings);

            // Always warm up the GoXLR API connection on startup
            _ = Task.Run(async () =>
            {
                // Wait indefinitely for GoXLR Daemon to be running
                Console.WriteLine("[App] Waiting for GoXLR Daemon to start...");
                await WaitForGoXLRUtilityIndefinitely();
                
                Console.WriteLine("[App] GoXLR Daemon detected! Warming up API connection...");
                // Removed notification popup
                
                // Give the daemon time to fully initialize its API
                await Task.Delay(5000);
                
                // Test connection
                Console.WriteLine("[App] Testing GoXLR connection...");
                bool isConnected = await _goXLRService.IsConnectedAsync();
                
                if (!isConnected)
                {
                    Console.WriteLine("[App] Connection test failed - API might still be initializing");
                    // Wait a bit more and retry
                    await Task.Delay(5000);
                    isConnected = await _goXLRService.IsConnectedAsync();
                }
                
                if (isConnected)
                {
                    Console.WriteLine("[App] Successfully connected to GoXLR!");
                    
                    // Pre-warm the volume cache BEFORE showing "connected" notification
                    Console.WriteLine("[App] Pre-warming volume cache for all enabled channels...");
                    await Task.Delay(1000);
                    
                    foreach (var channel in Settings.EnabledChannels)
                    {
                        Console.WriteLine($"[App] Warming cache for {channel}...");
                        await _goXLRService.WarmVolumeCacheAsync(channel);
                        Console.WriteLine($"[App] Cache warmed for {channel}");
                    }
                    Console.WriteLine("[App] Volume cache pre-warming complete!");
                    
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
            
            // Initialize hotkey service with main window handle
            var windowInterop = new System.Windows.Interop.WindowInteropHelper(_mainWindow);
            windowInterop.EnsureHandle(); // Force window handle creation
            
            _hotkeyService = new HotkeyService();
            _hotkeyService.Initialize(windowInterop.Handle);
            
            // Initialize hotkey manager
            _hotkeyManager = new HotkeyManager(_hotkeyService, _goXLRService, Settings);
            var registeredCount = _hotkeyManager.RegisterAllHotkeys();
            
            // Initialize controller input
            _directInputService = new DirectInputService();
            _controllerManager = new ControllerManager(_directInputService, _goXLRService, Settings);
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
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting application: {ex.Message}\n\n{ex.StackTrace}", 
                "SimControlCentre Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // Window settings are saved by MainWindow's OnClosed handler
        
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


