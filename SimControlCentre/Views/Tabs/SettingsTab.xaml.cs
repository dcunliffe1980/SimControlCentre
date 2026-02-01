using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class SettingsTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        private readonly GoXLRService _goXLRService;
        private readonly MainWindow _mainWindow;
        private ChannelsProfilesTab? _channelsProfilesTab;
        private ControllersTab? _controllersTab;
        private AboutTab? _aboutTab;
        private readonly UpdateService _updateService;

        public SettingsTab(ConfigurationService configService, AppSettings settings, GoXLRService goXLRService, MainWindow mainWindow)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _goXLRService = goXLRService;
            _mainWindow = mainWindow;
            _updateService = new UpdateService();

            // Select first category by default
            CategoryListBox.SelectedIndex = 0;
        }

        public void InitializeChannelsProfilesTab()
        {
            _channelsProfilesTab = new ChannelsProfilesTab(_goXLRService, _configService, _settings);
            
            // Subscribe to hotkeys changed event to notify the HotkeysTab
            _channelsProfilesTab.HotkeysChanged += (s, e) =>
            {
                // Notify MainWindow to refresh HotkeysTab if it exists
                _mainWindow.RefreshHotkeysTab();
            };
            
            // If GoXLR is currently selected, refresh to show the new content
            if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag as string == "GoXLR")
            {
                RefreshGoXLRSettings();
            }
        }

        public void InitializeControllersTab(DirectInputService directInputService)
        {
            _controllersTab = new ControllersTab(directInputService);
            
            // If Controllers is currently selected, refresh to show the new content
            if (CategoryListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag as string == "Controllers")
            {
                LoadControllersSettings();
            }
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryListBox.SelectedItem is not ListBoxItem selectedItem)
                return;

            var category = selectedItem.Tag as string;
            LoadSettingsCategory(category);
        }

        private void LoadSettingsCategory(string? category)
        {
            SettingsContent.Children.Clear();

            switch (category)
            {
                case "General":
                    LoadGeneralSettings();
                    break;
                case "GoXLR":
                    LoadGoXLRSettings();
                    break;
                case "Controllers":
                    LoadControllersSettings();
                    break;
                case "Logs":
                    LoadLogsSettings();
                    break;
                case "About":
                    LoadAboutSettings();
                    break;
            }
        }

        private void LoadGeneralSettings()
        {
            // Title
            var title = new TextBlock
            {
                Text = "General Settings",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(title);

            // Start Minimized
            var startMinimizedCheck = new CheckBox
            {
                Content = "Start minimized to system tray",
                IsChecked = _settings.Window.StartMinimized,
                Margin = new Thickness(0, 0, 0, 10)
            };
            startMinimizedCheck.Checked += (s, e) =>
            {
                _settings.Window.StartMinimized = true;
                _configService.Save(_settings);
            };
            startMinimizedCheck.Unchecked += (s, e) =>
            {
                _settings.Window.StartMinimized = false;
                _configService.Save(_settings);
            };
            SettingsContent.Children.Add(startMinimizedCheck);

            // Run at Windows Startup
            var startupCheck = new CheckBox
            {
                Content = "Run at Windows startup",
                IsChecked = _settings.General.RunAtStartup,
                Margin = new Thickness(0, 0, 0, 10)
            };
            startupCheck.Checked += (s, e) =>
            {
                _settings.General.RunAtStartup = true;
                _configService.Save(_settings);
                SetStartupRegistry(true);
            };
            startupCheck.Unchecked += (s, e) =>
            {
                _settings.General.RunAtStartup = false;
                _configService.Save(_settings);
                SetStartupRegistry(false);
            };
            SettingsContent.Children.Add(startupCheck);

            // Check for Updates on Startup
            var updateCheck = new CheckBox
            {
                Content = "Check for updates on startup",
                IsChecked = _settings.General.CheckForUpdatesOnStartup,
                Margin = new Thickness(0, 0, 0, 10)
            };
            updateCheck.Checked += (s, e) =>
            {
                _settings.General.CheckForUpdatesOnStartup = true;
                _configService.Save(_settings);
            };
            updateCheck.Unchecked += (s, e) =>
            {
                _settings.General.CheckForUpdatesOnStartup = false;
                _configService.Save(_settings);
            };
            SettingsContent.Children.Add(updateCheck);
        }

        private void LoadGoXLRSettings()
        {
            // Title
            var title = new TextBlock
            {
                Text = "GoXLR Configuration",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(title);

            // Enable GoXLR checkbox
            var enableGoXLRCheck = new CheckBox
            {
                Content = "Enable GoXLR Integration",
                IsChecked = _settings.General.GoXLREnabled,
                Margin = new Thickness(0, 0, 0, 20),
                FontWeight = FontWeights.SemiBold
            };
            enableGoXLRCheck.Checked += (s, e) =>
            {
                _settings.General.GoXLREnabled = true;
                _configService.Save(_settings);
                _mainWindow.UpdateHotkeysTabVisibility();
                RefreshGoXLRSettings();
            };
            enableGoXLRCheck.Unchecked += (s, e) =>
            {
                _settings.General.GoXLREnabled = false;
                _configService.Save(_settings);
                _mainWindow.UpdateHotkeysTabVisibility();
                RefreshGoXLRSettings();
            };
            SettingsContent.Children.Add(enableGoXLRCheck);

            // Only show GoXLR settings if enabled
            if (!_settings.General.GoXLREnabled)
            {
                var disabledMessage = new TextBlock
                {
                    Text = "GoXLR integration is disabled. Enable it above to configure GoXLR settings.",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                SettingsContent.Children.Add(disabledMessage);
                return;
            }

            // Connection Status Text
            var statusLabel = new TextBlock
            {
                Text = "Connection Status: Checking...",
                Foreground = System.Windows.Media.Brushes.Orange,
                Margin = new Thickness(0, 0, 0, 20),
                FontWeight = FontWeights.SemiBold
            };
            SettingsContent.Children.Add(statusLabel);

            // Check connection status
            _ = Task.Run(async () =>
            {
                bool isConnected = await _goXLRService.IsConnectedAsync();
                Dispatcher.Invoke(() =>
                {
                    if (isConnected)
                    {
                        statusLabel.Text = "Connection Status: Connected";
                        statusLabel.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        statusLabel.Text = "Connection Status: Not Connected";
                        statusLabel.Foreground = System.Windows.Media.Brushes.Red;
                    }
                });
            });

            // API Endpoint
            var apiEndpointLabel = new TextBlock
            {
                Text = "API Endpoint:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            SettingsContent.Children.Add(apiEndpointLabel);

            var apiEndpointBox = new TextBox
            {
                Text = _settings.General.ApiEndpoint,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };
            apiEndpointBox.LostFocus += (s, e) =>
            {
                _settings.General.ApiEndpoint = apiEndpointBox.Text;
                _configService.Save(_settings);
            };
            SettingsContent.Children.Add(apiEndpointBox);

            // Serial Number
            var serialLabel = new TextBlock
            {
                Text = "Serial Number:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            SettingsContent.Children.Add(serialLabel);

            var serialBox = new TextBox
            {
                Text = _settings.General.SerialNumber,
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(5)
            };
            serialBox.LostFocus += (s, e) =>
            {
                _settings.General.SerialNumber = serialBox.Text;
                _configService.Save(_settings);
            };
            SettingsContent.Children.Add(serialBox);

            // Test Connection Button
            var testButton = new Button
            {
                Content = "Test Connection",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0)
            };
            testButton.Click += async (s, e) =>
            {
                testButton.IsEnabled = false;
                testButton.Content = "Testing...";
                statusLabel.Text = "Connection Status: Testing...";
                statusLabel.Foreground = System.Windows.Media.Brushes.Orange;

                bool isConnected = await _goXLRService.IsConnectedAsync();

                if (isConnected)
                {
                    statusLabel.Text = "Connection Status: Connected";
                    statusLabel.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    statusLabel.Text = "Connection Status: Not Connected";
                    statusLabel.Foreground = System.Windows.Media.Brushes.Red;
                }

                testButton.IsEnabled = true;
                testButton.Content = "Test Connection";
            };
            SettingsContent.Children.Add(testButton);

            // Add Channels & Profiles content if available
            if (_channelsProfilesTab != null)
            {
                // Separator
                var separator = new System.Windows.Controls.Separator
                {
                    Margin = new Thickness(0, 30, 0, 20)
                };
                SettingsContent.Children.Add(separator);

                // Channels & Profiles Title
                var channelsTitle = new TextBlock
                {
                    Text = "Channels & Profiles",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                SettingsContent.Children.Add(channelsTitle);

                // Embed the actual ChannelsProfilesTab content
                var channelsContent = _channelsProfilesTab.Content as UIElement;
                if (channelsContent != null)
                {
                    _channelsProfilesTab.Content = null; // Detach from original parent
                    SettingsContent.Children.Add(channelsContent);
                }
            }
        }

        private void RefreshGoXLRSettings()
        {
            LoadSettingsCategory("GoXLR");
        }

        private void LoadControllersSettings()
        {
            // Title
            var title = new TextBlock
            {
                Text = "Controller Configuration",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(title);

            // Embed the ControllersTab content if available
            if (_controllersTab != null)
            {
                var controllersContent = _controllersTab.Content as UIElement;
                if (controllersContent != null)
                {
                    _controllersTab.Content = null; // Detach from original parent
                    SettingsContent.Children.Add(controllersContent);
                }
            }
            else
            {
                var notInitializedMessage = new TextBlock
                {
                    Text = "Controllers not yet initialized. Please wait...",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                SettingsContent.Children.Add(notInitializedMessage);
            }
        }

        private void LoadLogsSettings()
        {
            // Title
            var title = new TextBlock
            {
                Text = "Logging Configuration",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(title);

            // Description
            var description = new TextBlock
            {
                Text = "Configure application and diagnostic logging. Logs are stored in %LocalAppData%\\SimControlCentre\\logs",
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(description);

            // Application Logging
            var appLoggingCheck = new CheckBox
            {
                Content = "Enable Application Logging",
                IsChecked = _settings.General.EnableApplicationLogging,
                Margin = new Thickness(0, 0, 0, 5)
            };
            appLoggingCheck.Checked += (s, e) =>
            {
                _settings.General.EnableApplicationLogging = true;
                _configService.Save(_settings);
            };
            appLoggingCheck.Unchecked += (s, e) =>
            {
                _settings.General.EnableApplicationLogging = false;
                _configService.Save(_settings);
            };
            SettingsContent.Children.Add(appLoggingCheck);

            var appLoggingDesc = new TextBlock
            {
                Text = "Logs general application events, errors, and iRacing integration activities",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(20, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            SettingsContent.Children.Add(appLoggingDesc);

            // GoXLR Diagnostics
            var goxlrDiagCheck = new CheckBox
            {
                Content = "Enable GoXLR Diagnostics",
                IsChecked = _settings.General.EnableGoXLRDiagnostics,
                Margin = new Thickness(0, 0, 0, 5)
            };
            goxlrDiagCheck.Checked += (s, e) =>
            {
                _settings.General.EnableGoXLRDiagnostics = true;
                _configService.Save(_settings);
                GoXLRDiagnostics.SetEnabled(true);
            };
            goxlrDiagCheck.Unchecked += (s, e) =>
            {
                _settings.General.EnableGoXLRDiagnostics = false;
                _configService.Save(_settings);
                GoXLRDiagnostics.SetEnabled(false);
            };
            SettingsContent.Children.Add(goxlrDiagCheck);

            var goxlrDiagDesc = new TextBlock
            {
                Text = "Detailed connection timing, warmup attempts, and API responses for troubleshooting GoXLR issues",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(20, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };
            SettingsContent.Children.Add(goxlrDiagDesc);

            // Open Logs Folder Button
            var openLogsButton = new Button
            {
                Content = "Open Logs Folder",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 0)
            };
            openLogsButton.Click += (s, e) =>
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SimControlCentre",
                    "logs"
                );

                if (Directory.Exists(logDirectory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logDirectory,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Logs folder does not exist yet. It will be created when logging is enabled.",
                        "Logs Folder",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            };
            SettingsContent.Children.Add(openLogsButton);
        }

        private void LoadAboutSettings()
        {
            // Title
            var title = new TextBlock
            {
                Text = "About SimControlCentre",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(title);

            // Version Info
            var currentVersion = _updateService.GetCurrentVersion();
            var versionText = new TextBlock
            {
                Text = $"Version: {currentVersion}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(versionText);

            // Update Status
            var updateStatusText = new TextBlock
            {
                Text = "Checking for updates...",
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SettingsContent.Children.Add(updateStatusText);

            // Check for Updates Button
            var checkUpdateButton = new Button
            {
                Content = "Check for Updates",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 10, 0, 20)
            };
            checkUpdateButton.Click += async (s, e) =>
            {
                checkUpdateButton.IsEnabled = false;
                checkUpdateButton.Content = "Checking...";
                updateStatusText.Text = "Checking for updates...";
                updateStatusText.Foreground = System.Windows.Media.Brushes.Gray;

                var updateInfo = await _updateService.CheckForUpdateAsync();

                if (!string.IsNullOrEmpty(updateInfo.Error))
                {
                    updateStatusText.Text = updateInfo.Error;
                    updateStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else if (updateInfo.IsAvailable)
                {
                    updateStatusText.Text = $"Update available: v{updateInfo.LatestVersion}";
                    updateStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    
                    var result = MessageBox.Show(
                        $"A new version is available!\n\nCurrent: v{updateInfo.CurrentVersion}\nLatest: v{updateInfo.LatestVersion}\n\nWould you like to download it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(updateInfo.ReleaseUrl))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = updateInfo.ReleaseUrl,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    updateStatusText.Text = "You are running the latest version!";
                    updateStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }

                checkUpdateButton.IsEnabled = true;
                checkUpdateButton.Content = "Check for Updates";
            };
            SettingsContent.Children.Add(checkUpdateButton);

            // Test GitHub API Button (for debugging)
            var testApiButton = new Button
            {
                Content = "Test GitHub API (Debug)",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10, 10, 0, 20)
            };
            testApiButton.Click += async (s, e) =>
            {
                testApiButton.IsEnabled = false;
                
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "SimControlCentre");
                    
                    // Test 1: Latest release
                    var url1 = "https://api.github.com/repos/dcunliffe1980/SimControlCentre/releases/latest";
                    UpdateDiagnostics.Log($"[DEBUG] Testing: {url1}");
                    
                    var response1 = await client.GetAsync(url1);
                    UpdateDiagnostics.Log($"[DEBUG] /latest Status: {response1.StatusCode}");
                    
                    var content1 = await response1.Content.ReadAsStringAsync();
                    UpdateDiagnostics.Log($"[DEBUG] /latest Response: {content1}");
                    
                    // Test 2: All releases
                    var url2 = "https://api.github.com/repos/dcunliffe1980/SimControlCentre/releases";
                    UpdateDiagnostics.Log($"[DEBUG] Testing: {url2}");
                    
                    var response2 = await client.GetAsync(url2);
                    UpdateDiagnostics.Log($"[DEBUG] /releases Status: {response2.StatusCode}");
                    
                    var content2 = await response2.Content.ReadAsStringAsync();
                    UpdateDiagnostics.Log($"[DEBUG] /releases Response: {content2}");
                    
                    // Test 3: Specific release by tag
                    var url3 = "https://api.github.com/repos/dcunliffe1980/SimControlCentre/releases/tags/v1.1.1";
                    UpdateDiagnostics.Log($"[DEBUG] Testing: {url3}");
                    
                    var response3 = await client.GetAsync(url3);
                    UpdateDiagnostics.Log($"[DEBUG] /tags/v1.1.1 Status: {response3.StatusCode}");
                    
                    var content3 = await response3.Content.ReadAsStringAsync();
                    UpdateDiagnostics.Log($"[DEBUG] /tags/v1.1.1 Response: {content3}");
                    
                    MessageBox.Show($"/latest: {response1.StatusCode}\n/releases: {response2.StatusCode}\n/tags/v1.1.1: {response3.StatusCode}\n\nCheck logs folder for full responses",
                        "API Test Results",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    UpdateDiagnostics.Log($"[DEBUG] Error: {ex.Message}");
                    MessageBox.Show($"Error: {ex.Message}\n\nCheck logs folder for details",
                        "API Test Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                
                testApiButton.IsEnabled = true;
            };
            SettingsContent.Children.Add(testApiButton);

            // Separator
            var separator = new System.Windows.Controls.Separator
            {
                Margin = new Thickness(0, 20, 0, 20)
            };
            SettingsContent.Children.Add(separator);

            // Embed AboutTab content if initialized
            if (_aboutTab == null)
            {
                _aboutTab = new AboutTab(_configService);
            }

            var aboutContent = _aboutTab.Content as UIElement;
            if (aboutContent != null)
            {
                _aboutTab.Content = null; // Detach from original parent
                SettingsContent.Children.Add(aboutContent);
            }
        }

        private void SetStartupRegistry(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                    {
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                        key.SetValue("SimControlCentre", exePath);
                    }
                    else
                    {
                        key.DeleteValue("SimControlCentre", false);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to update startup setting: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }
}
