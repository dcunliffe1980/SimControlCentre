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
        // GoXLR functionality now in plugins
        private readonly MainWindow _mainWindow;
        private ChannelsProfilesTab? _channelsProfilesTab;
        private ControllersTab? _controllersTab;
        private PluginsTab? _pluginsTab;
        private readonly UpdateCheckService? _updateCheckService;
        private TextBlock? _updateStatusText;
        private Button? _downloadButton;

        public SettingsTab(ConfigurationService configService, AppSettings settings, MainWindow mainWindow)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _mainWindow = mainWindow;
            _updateCheckService = App.GetUpdateCheckService();

            // Subscribe to update check status changes
            if (_updateCheckService != null)
            {
                _updateCheckService.StatusChanged += OnUpdateCheckStatusChanged;
            }

            // Initialize PluginsTab at startup
            var lightingService = App.GetLightingService();
            if (lightingService != null)
            {
                _pluginsTab = new PluginsTab(_configService, _settings, lightingService, _mainWindow);
            }

            // Select first category by default
            CategoryListBox.SelectedIndex = 0;
        }

        private void OnUpdateCheckStatusChanged(object? sender, UpdateCheckStatusChangedEventArgs e)
        {
            // Update the status text if it exists
            if (_updateStatusText != null)
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateStatusTextFromService();
                });
            }
        }

        public void InitializeChannelsProfilesTab()
        {
            _channelsProfilesTab = new ChannelsProfilesTab( _configService, _settings);
            
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
                case "Plugins":
                    LoadPluginsSettings();
                    break;
                case "Controllers":
                    LoadControllersSettings();
                    break;
                case "Telemetry":
                    LoadTelemetryDebug();
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
                _mainWindow.UpdateDeviceControlTabVisibility();
                RefreshGoXLRSettings();
            };
            enableGoXLRCheck.Unchecked += (s, e) =>
            {
                _settings.General.GoXLREnabled = false;
                _configService.Save(_settings);
                _mainWindow.UpdateDeviceControlTabVisibility();
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
                bool isConnected = await Task.FromResult(false) /* Plugin handles connection */;
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

                bool isConnected = await Task.FromResult(false) /* Plugin handles connection */;

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

            // Add the entire UserControl to the settings content
            if (_controllersTab != null)
            {
                SettingsContent.Children.Add(_controllersTab);
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

        private void LoadPluginsSettings()
        {
            // Add the entire UserControl to the settings content
            if (_pluginsTab != null)
            {
                SettingsContent.Children.Add(_pluginsTab);
            }
            else
            {
                var notInitializedMessage = new TextBlock
                {
                    Text = "Plugins not yet initialized. Please wait...",
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
                // Logger.SetEnabled(true);
            };
            goxlrDiagCheck.Unchecked += (s, e) =>
            {
                _settings.General.EnableGoXLRDiagnostics = false;
                _configService.Save(_settings);
                // Logger.SetEnabled(false);
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

        private void LoadTelemetryDebug()
        {
            var telemetryService = App.GetTelemetryService();
            
            if (telemetryService == null)
            {
                var errorText = new TextBlock
                {
                    Text = "Telemetry service not available",
                    Foreground = System.Windows.Media.Brushes.Red,
                    FontSize = 14
                };
                SettingsContent.Children.Add(errorText);
                return;
            }

            // Embed the TelemetryDebugTab
            var telemetryTab = new TelemetryDebugTab(telemetryService);
            SettingsContent.Children.Add(telemetryTab);
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

            // Version Info (dynamic - reads from assembly)
            var currentVersion = GetCurrentVersion();
            var versionText = new TextBlock
            {
                Text = $"Version: {currentVersion}",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 20)
            };
            SettingsContent.Children.Add(versionText);

            // Update Status (this is the key field that will be updated)
            _updateStatusText = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            SettingsContent.Children.Add(_updateStatusText);
            
            // Set initial status from service
            UpdateStatusTextFromService();

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

                if (_updateCheckService != null)
                {
                    var updateInfo = await _updateCheckService.CheckNowAsync();

                    if (updateInfo.IsAvailable && !string.IsNullOrEmpty(updateInfo.LatestVersion))
                    {
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
                }

                checkUpdateButton.IsEnabled = true;
                checkUpdateButton.Content = "Check for Updates";
            };
            SettingsContent.Children.Add(checkUpdateButton);

            // Download Update Button (shown only when update is available)
            var downloadButton = new Button
            {
                Content = "Download Update",
                Padding = new Thickness(15, 8, 15, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20),
                Visibility = Visibility.Collapsed // Hidden by default
            };
            downloadButton.Click += async (s, e) =>
            {
                if (_updateCheckService?.LatestUpdateInfo != null)
                {
                    var updateInfo = _updateCheckService.LatestUpdateInfo;
                    
                    // Determine which version to download
                    var downloadUrl = await DetermineDownloadUrlAsync(updateInfo);
                    
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        downloadButton.IsEnabled = false;
                        downloadButton.Content = "Downloading...";
                        
                        try
                        {
                            // Start download in browser
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = downloadUrl,
                                UseShellExecute = true
                            });
                            
                            MessageBox.Show(
                                "The download has started in your browser.\n\nOnce downloaded, close SimControlCentre and run the installer to update.",
                                "Download Started",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to start download:\n\n{ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            downloadButton.IsEnabled = true;
                            downloadButton.Content = "Download Update";
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not determine the correct download URL.\n\nPlease visit the releases page to download manually.",
                            "Download Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        // Fallback to releases page
                        if (!string.IsNullOrEmpty(updateInfo.ReleaseUrl))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = updateInfo.ReleaseUrl,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            };
            SettingsContent.Children.Add(downloadButton);
            
            // Store reference for visibility updates
            _downloadButton = downloadButton;

            // Separator
            var separator = new System.Windows.Controls.Separator
            {
                Margin = new Thickness(0, 20, 0, 20)
            };
            SettingsContent.Children.Add(separator);

            // Configuration section
            var configTitle = new TextBlock
            {
                Text = "Configuration",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SettingsContent.Children.Add(configTitle);

            // Config file path
            var configPathLabel = new TextBlock
            {
                Text = "Configuration File:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            SettingsContent.Children.Add(configPathLabel);

            var configPathText = new TextBlock
            {
                Text = _configService.GetConfigFilePath(),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SettingsContent.Children.Add(configPathText);

            var openConfigButton = new Button
            {
                Content = "Open Config Folder",
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20)
            };
            openConfigButton.Click += (s, e) =>
            {
                var configPath = _configService.GetConfigFilePath();
                var folderPath = Path.GetDirectoryName(configPath);

                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folderPath,
                        UseShellExecute = true
                    });
                }
            };
            SettingsContent.Children.Add(openConfigButton);

            // Log file path
            var logPathLabel = new TextBlock
            {
                Text = "Log Folder:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            SettingsContent.Children.Add(logPathLabel);

            var logPathText = new TextBlock
            {
                Text = Logger.GetLogFilePath(),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SettingsContent.Children.Add(logPathText);

            var openLogsButton2 = new Button
            {
                Content = "Open Logs Folder",
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 20)
            };
            openLogsButton2.Click += (s, e) =>
            {
                Logger.OpenLogsFolder();
            };
            SettingsContent.Children.Add(openLogsButton2);

            // Links section
            var linksTitle = new TextBlock
            {
                Text = "Links",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            SettingsContent.Children.Add(linksTitle);

            var githubLink = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 5)
            };
            var githubHyperlink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("GitHub Repository"))
            {
                NavigateUri = new Uri("https://github.com/dcunliffe1980/SimControlCentre")
            };
            githubHyperlink.RequestNavigate += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            githubLink.Inlines.Add(githubHyperlink);
            SettingsContent.Children.Add(githubLink);

            var quickstartLink = new TextBlock
            {
                Margin = new Thickness(0, 5, 0, 0)
            };
            var quickstartHyperlink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("Quick Start Guide"))
            {
                NavigateUri = new Uri("https://github.com/dcunliffe1980/SimControlCentre/blob/master/QUICKSTART.md")
            };
            quickstartHyperlink.RequestNavigate += (s, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            quickstartLink.Inlines.Add(quickstartHyperlink);
            SettingsContent.Children.Add(quickstartLink);
        }

        private void UpdateStatusTextFromService()
        {
            if (_updateStatusText == null || _updateCheckService == null)
                return;

            switch (_updateCheckService.Status)
            {
                case UpdateCheckStatus.NotStarted:
                    _updateStatusText.Text = "Update check has not started yet";
                    _updateStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                    if (_downloadButton != null) _downloadButton.Visibility = Visibility.Collapsed;
                    break;

                case UpdateCheckStatus.Checking:
                    _updateStatusText.Text = "Checking for updates...";
                    _updateStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    if (_downloadButton != null) _downloadButton.Visibility = Visibility.Collapsed;
                    break;

                case UpdateCheckStatus.UpToDate:
                    _updateStatusText.Text = "You are running the latest version!";
                    _updateStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    if (_downloadButton != null) _downloadButton.Visibility = Visibility.Collapsed;
                    break;

                case UpdateCheckStatus.UpdateAvailable:
                    var updateInfo = _updateCheckService.LatestUpdateInfo;
                    if (updateInfo != null)
                    {
                        _updateStatusText.Text = $"Update available: v{updateInfo.LatestVersion}";
                        _updateStatusText.Foreground = System.Windows.Media.Brushes.Green;
                        if (_downloadButton != null) _downloadButton.Visibility = Visibility.Visible;
                    }
                    break;

                case UpdateCheckStatus.Error:
                    var errorInfo = _updateCheckService.LatestUpdateInfo;
                    if (errorInfo != null && !string.IsNullOrEmpty(errorInfo.Error))
                    {
                        _updateStatusText.Text = errorInfo.Error;
                    }
                    else
                    {
                        _updateStatusText.Text = "Failed to check for updates";
                    }
                    _updateStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                    if (_downloadButton != null) _downloadButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private async Task<string?> DetermineDownloadUrlAsync(UpdateInfo updateInfo)
        {
            if (updateInfo.Assets == null || updateInfo.Assets.Count == 0)
            {
                UpdateDiagnostics.Log("[DetermineDownload] No assets found in release");
                return null;
            }

            // Detect if we're running the standalone version
            bool isStandalone = IsStandaloneVersion();
            
            UpdateDiagnostics.Log($"[DetermineDownload] Is standalone: {isStandalone}");
            UpdateDiagnostics.Log($"[DetermineDownload] Available assets:");
            foreach (var asset in updateInfo.Assets)
            {
                UpdateDiagnostics.Log($"  - {asset.Name}");
            }

            // Look for the appropriate installer
            // For standalone: must contain "standalone"
            // For framework-dependent: must NOT contain "standalone" but must contain "setup"
            
            ReleaseAsset? matchingAsset = null;
            
            if (isStandalone)
            {
                // Look for standalone installer
                matchingAsset = updateInfo.Assets.FirstOrDefault(a => 
                    a.Name.Contains("standalone", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                
                UpdateDiagnostics.Log($"[DetermineDownload] Looking for standalone installer");
            }
            else
            {
                // Look for framework-dependent installer (setup but NOT standalone)
                matchingAsset = updateInfo.Assets.FirstOrDefault(a => 
                    a.Name.Contains("setup", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.Contains("standalone", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                
                UpdateDiagnostics.Log($"[DetermineDownload] Looking for framework-dependent installer (setup without standalone)");
            }

            if (matchingAsset != null)
            {
                UpdateDiagnostics.Log($"[DetermineDownload] Found matching asset: {matchingAsset.Name}");
                return matchingAsset.BrowserDownloadUrl;
            }

            // Fallback: if standalone pattern not found, try to find any .exe installer
            var fallbackAsset = updateInfo.Assets.FirstOrDefault(a => 
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (fallbackAsset != null)
            {
                UpdateDiagnostics.Log($"[DetermineDownload] Using fallback asset: {fallbackAsset.Name}");
                return fallbackAsset.BrowserDownloadUrl;
            }

            UpdateDiagnostics.Log("[DetermineDownload] No suitable installer found");
            return null;
        }

        private string GetCurrentVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                // Fallback if version can't be read
            }

            return "Unknown";
        }

        private bool IsStandaloneVersion()
        {
            try
            {
                // Check if we're running from a single-file publish
                // In standalone, the main exe contains everything
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
#pragma warning disable IL3000 // Assembly.Location is intentionally used to detect single-file bundle
                var location = assembly.Location;
#pragma warning restore IL3000
                
                UpdateDiagnostics.Log($"[IsStandalone] Assembly location: {location}");
                
                // If location is empty, we're likely in a single-file bundle
                if (string.IsNullOrEmpty(location))
                {
                    UpdateDiagnostics.Log("[IsStandalone] Empty location - single file bundle detected");
                    return true;
                }
                
                // Check if SimControlCentre.dll exists alongside the exe
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    UpdateDiagnostics.Log("[IsStandalone] Could not get exe path");
                    return false;
                }
                
                var exeDir = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(exeDir))
                {
                    UpdateDiagnostics.Log("[IsStandalone] Could not get exe directory");
                    return false;
                }
                
                var dllPath = Path.Combine(exeDir, "SimControlCentre.dll");
                bool dllExists = File.Exists(dllPath);
                
                UpdateDiagnostics.Log($"[IsStandalone] DLL path: {dllPath}");
                UpdateDiagnostics.Log($"[IsStandalone] DLL exists: {dllExists}");
                
                // If .dll exists separately, it's NOT standalone
                // If .dll doesn't exist, it's bundled in the exe (standalone)
                return !dllExists;
            }
            catch (Exception ex)
            {
                UpdateDiagnostics.Log($"[IsStandalone] Error: {ex.Message}");
                // Default to non-standalone on error
                return false;
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
#pragma warning disable IL3000 // Assembly.Location usage is acceptable here for registry path
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
#pragma warning restore IL3000
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



