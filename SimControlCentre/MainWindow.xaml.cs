using System;
using System.Windows;
using SimControlCentre.Models;
using SimControlCentre.Services;
using SimControlCentre.Views.Tabs;

namespace SimControlCentre
{
    public partial class MainWindow : Window
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        
        private readonly iRacingMonitorService? _iRacingMonitor;
        
        // Tab controls
        private SettingsTab? _settingsTab;
        private HotkeysTab? _hotkeysTab;
        private ExternalAppsTab? _externalAppsTab;
        private LightingTab? _lightingTab;

        public MainWindow(AppSettings settings, ConfigurationService configService, iRacingMonitorService? iRacingMonitor = null)
        {
            _settings = settings;
            _configService = configService;
            _iRacingMonitor = iRacingMonitor;

            InitializeComponent();

            // Initialize all tab controls
            InitializeTabs();

            // Restore window position and size
            RestoreWindowSettings();

            // Handle window state changes
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
        }

        private void InitializeTabs()
        {
            // Create Settings Tab
            _settingsTab = new SettingsTab(_configService, _settings, this);
            SettingsTabItem.Content = _settingsTab;
            
            // Create Device Control Tab (always visible like Lighting tab)
            InitializeDeviceControlTab();
            
            // Initialize Channels & Profiles in Settings Tab
            _settingsTab.InitializeChannelsProfilesTab();
            
            // Controllers Tab will be initialized later via InitializeControllersTab()
            // (DirectInputService is created after MainWindow in App.xaml.cs)
            
            // Create Application Manager Tab
            if (_iRacingMonitor != null)
            {
                _externalAppsTab = new ExternalAppsTab(_configService, _settings, _iRacingMonitor);
                iRacingTabItem.Content = _externalAppsTab;
            }
            
            // Create Lighting Tab
            var lightingService = App.GetLightingService();
            var telemetryService = App.GetTelemetryService();
            if (lightingService != null && telemetryService != null)
            {
                _lightingTab = new LightingTab(lightingService, telemetryService);
                LightingTabItem.Content = _lightingTab;
            }
            
            // Create Logs Tab
            var logsTab = new LogsTab();
            LogsTabItem.Content = logsTab;
        }


        public void InitializeControllersTab(DirectInputService directInputService)
        {
            _settingsTab?.InitializeControllersTab(directInputService);
        }

        public void InitializeDeviceControlTab()
        {
            // Always create and show Device Control tab (like Lighting tab)
            // The tab itself will show a warning if no plugins are available
            if (_hotkeysTab == null)
            {
                _hotkeysTab = new HotkeysTab(_configService, _settings);
                DeviceControlTabItem.Content = _hotkeysTab;
            }
            DeviceControlTabItem.Visibility = Visibility.Visible;
        }

        public void UpdateDeviceControlTabVisibility()
        {
            // This method is kept for backward compatibility but now just refreshes
            // The tab is always visible now, warnings are handled inside the tab
            _hotkeysTab?.CheckPluginAvailability();
        }


        public void RefreshHotkeysTab()
        {
            _hotkeysTab?.RefreshHotkeys();
        }

        public void RefreshLightingTab()
        {
            _lightingTab?.CheckPluginAvailability();
        }

        private void RestoreWindowSettings()
        {
            if (_settings.Window.Width > 0 && _settings.Window.Height > 0)
            {
                Width = _settings.Window.Width;
                Height = _settings.Window.Height;
            }

            if (_settings.Window.Left > 0 && _settings.Window.Top > 0)
            {
                Left = _settings.Window.Left;
                Top = _settings.Window.Top;
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void SaveWindowSettings()
        {
            var currentSettings = _configService.Load();

            if (WindowState == WindowState.Normal)
            {
                currentSettings.Window.Width = Width;
                currentSettings.Window.Height = Height;
                currentSettings.Window.Left = Left;
                currentSettings.Window.Top = Top;
            }

            _configService.Save(currentSettings);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveWindowSettings();
            base.OnClosed(e);
        }
    }
}

