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
        private readonly GoXLRService _goXLRService;
        private readonly iRacingMonitorService? _iRacingMonitor;
        
        // Tab controls
        private GeneralTab? _generalTab;
        private HotkeysTab? _hotkeysTab;
        private ChannelsProfilesTab? _channelsProfilesTab;
        private ControllersTab? _controllersTab;
        private ExternalAppsTab? _externalAppsTab;
        private AboutTab? _aboutTab;

        public MainWindow(AppSettings settings, ConfigurationService configService, GoXLRService goXLRService, iRacingMonitorService? iRacingMonitor = null)
        {
            _settings = settings;
            _configService = configService;
            _goXLRService = goXLRService;
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
            // Create General Tab
            _generalTab = new GeneralTab(_goXLRService, _configService, _settings);
            GeneralTabItem.Content = _generalTab;
            
            // Create Hotkeys Tab
            _hotkeysTab = new HotkeysTab(_configService, _settings);
            HotkeysTabItem.Content = _hotkeysTab;
            
            // Create Channels & Profiles Tab
            _channelsProfilesTab = new ChannelsProfilesTab(_goXLRService, _configService, _settings);
            _channelsProfilesTab.HotkeysChanged += (s, e) => _hotkeysTab?.RefreshHotkeys();
            ChannelsProfilesTabItem.Content = _channelsProfilesTab;
            
            // Controllers Tab will be initialized later via InitializeControllersTab()
            // (DirectInputService is created after MainWindow in App.xaml.cs)
            
            // Create iRacing Tab
            if (_iRacingMonitor != null)
            {
                _externalAppsTab = new ExternalAppsTab(_configService, _settings, _iRacingMonitor);
                iRacingTabItem.Content = _externalAppsTab;
            }
            
            // Create About Tab
            _aboutTab = new AboutTab(_configService);
            AboutTabItem.Content = _aboutTab;
            
            // Check connection on startup (delayed to allow API warm-up)
            if (!string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(2000);
                    await _generalTab.CheckConnectionAsync();
                });
            }
        }

        public void InitializeControllersTab(DirectInputService directInputService)
        {
            _controllersTab = new ControllersTab(directInputService);
            ControllersTabItem.Content = _controllersTab;
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
