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

        public SettingsTab(ConfigurationService configService, AppSettings settings, GoXLRService goXLRService)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _goXLRService = goXLRService;

            // Select first category by default
            CategoryListBox.SelectedIndex = 0;
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
                RefreshGoXLRSettings();
            };
            enableGoXLRCheck.Unchecked += (s, e) =>
            {
                _settings.General.GoXLREnabled = false;
                _configService.Save(_settings);
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

                bool isConnected = await _goXLRService.IsConnectedAsync();

                if (isConnected)
                {
                    MessageBox.Show("Successfully connected to GoXLR!", 
                        "Connection Test", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Could not connect to GoXLR.\n\nMake sure:\n- GoXLR Utility is running\n- API endpoint is correct\n- Serial number is correct", 
                        "Connection Failed", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }

                testButton.IsEnabled = true;
                testButton.Content = "Test Connection";
            };
            SettingsContent.Children.Add(testButton);
        }

        private void RefreshGoXLRSettings()
        {
            LoadSettingsCategory("GoXLR");
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
