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

        public SettingsTab(ConfigurationService configService, AppSettings settings, GoXLRService goXLRService, MainWindow mainWindow)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _goXLRService = goXLRService;
            _mainWindow = mainWindow;

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
