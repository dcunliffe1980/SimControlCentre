using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;
using SimControlCentre.Views;

namespace SimControlCentre;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ConfigurationService _configService;
    private readonly GoXLRService _goXLRService;
    
    // Hotkey capture state
    private bool _isCapturingHotkey = false;
    private string? _captureType;
    private string? _captureAction;
    private TextBox? _captureTextBox;
    
    // Controller capture state
    private bool _isCapturingButton = false;
    private TextBox? _captureButtonTextBox;
    
    // Available GoXLR profiles
    private List<string> _availableProfiles = new();

    public MainWindow(AppSettings settings, ConfigurationService configService, GoXLRService goXLRService)
    {
        _settings = settings;
        _configService = configService;
        _goXLRService = goXLRService;

        InitializeComponent();

        // Set initial values
        UpdateSerialNumberDisplay(_settings.General.SerialNumber);
        VolumeStepBox.Text = _settings.General.VolumeStep.ToString();
        CacheTimeBox.Text = _settings.General.VolumeCacheTimeMs.ToString();
        ConfigPathText.Text = _configService.GetConfigFilePath();

        // Restore window position and size
        RestoreWindowSettings();

        // Handle window state changes
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
        
        // Add key event handlers for hotkey capture (use PreviewKeyDown to catch before global hotkeys)
        PreviewKeyDown += MainWindow_KeyDown;
        
        // Check connection on startup (but only if serial is configured)
        if (!string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
        {
            // Delay to allow API warm-up from App.xaml.cs
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000); // Wait for warm-up
                await Dispatcher.InvokeAsync(async () => await CheckConnectionAsync());
            });
        }
        else
        {
            // Show waiting message if auto-detect will run
            ConnectionStatusText.Text = "Connection Status: Waiting for auto-detect...";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
        
        // Show hotkey status
        // UpdateHotkeyStatus(); // No longer needed - using PopulateHotkeyEditor instead
        
        // Populate hotkey editor
        PopulateHotkeyEditor();
        
        // Populate channels and profiles management
        PopulateChannelsAndProfiles();
        
        // Initialize Start with Windows checkbox
        StartWithWindowsCheckBox.IsChecked = IsStartWithWindowsEnabled();
        
        // Refresh controller list after a short delay to ensure DirectInput is initialized
        Dispatcher.InvokeAsync(async () =>
        {
            await Task.Delay(500);
            RefreshControllerList();
            
            // Also populate Controllers tab list
            RefreshControllerList2();
            
            // Subscribe to button events for the indicator
            var directInputService = App.GetDirectInputService();
            if (directInputService != null)
            {
                directInputService.ButtonPressed += OnAnyButtonPressed;
                directInputService.ButtonReleased += OnAnyButtonReleased;
            }
            
            // Auto-fetch GoXLR profiles
            await Task.Delay(2500); // Additional delay for GoXLR API warm-up
            await FetchGoXLRProfilesAsync();
        });
    }

    private void OnAnyButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            ButtonIndicator.Fill = System.Windows.Media.Brushes.LimeGreen;
            ButtonIndicatorText.Text = $"Button {e.ButtonNumber} pressed!";
            ButtonIndicatorText.Foreground = System.Windows.Media.Brushes.Green;
        });
    }

    private void OnAnyButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            // Delay reset to make it visible
            Task.Delay(100).ContinueWith(_ => Dispatcher.Invoke(() =>
            {
                ButtonIndicator.Fill = System.Windows.Media.Brushes.Gray;
                ButtonIndicatorText.Text = "Waiting for input...";
                ButtonIndicatorText.Foreground = System.Windows.Media.Brushes.Gray;
            }));
        });
    }

    private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Only capture if we're in capture mode
        if (!_isCapturingHotkey || _captureTextBox == null)
            return;

        // Ignore modifier keys alone
        if (IsModifierKey(e.Key))
            return;

        // Build hotkey string
        var modifiers = System.Windows.Input.Keyboard.Modifiers;
        var hotkeyString = BuildHotkeyString(modifiers, e.Key);

        // Check for conflicts - silently reject if already in use
        var conflict = CheckHotkeyConflict(hotkeyString, _captureType!, _captureAction!);
        
        if (!string.IsNullOrEmpty(conflict))
        {
            // Show "Already in use" briefly in the textbox
            var originalValue = _captureType == "Profile" 
                ? _settings.ProfileHotkeys[_captureAction!] ?? ""
                : _captureAction == "VolumeUp"
                    ? _settings.VolumeHotkeys[_captureType!].VolumeUp ?? ""
                    : _settings.VolumeHotkeys[_captureType!].VolumeDown ?? "";
            
            _captureTextBox.Text = "Already in use";
            _captureTextBox.Foreground = System.Windows.Media.Brushes.Red;
            _captureTextBox.Background = System.Windows.Media.Brushes.LightPink;
            
            // Capture the textbox reference before StopCapture() clears it
            var textBoxToReset = _captureTextBox;
            
            StopCombinedCapture();
            
            // Re-register hotkeys since we stopped capture
            var mgr = App.GetHotkeyManager();
            mgr?.RegisterAllHotkeys();
            
            // Reset after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                textBoxToReset.Text = originalValue;
                textBoxToReset.Foreground = System.Windows.Media.Brushes.Black;
                textBoxToReset.Background = System.Windows.Media.Brushes.White;
            };
            timer.Start();
            
            e.Handled = true;
            return;
        }

        // Save the hotkey
        if (_captureType == "Profile")
        {
            _settings.ProfileHotkeys[_captureAction!] = hotkeyString;
        }
        else
        {
            if (_captureAction == "VolumeUp")
                _settings.VolumeHotkeys[_captureType!].VolumeUp = hotkeyString;
            else if (_captureAction == "VolumeDown")
                _settings.VolumeHotkeys[_captureType!].VolumeDown = hotkeyString;
        }

        // Update UI
        _captureTextBox.Text = hotkeyString;
        _captureTextBox.Foreground = System.Windows.Media.Brushes.Black;
        _captureTextBox.Background = System.Windows.Media.Brushes.White;
        
        // Auto-save to config file
        _configService.Save(_settings);
        
        // Auto-register the hotkey
        var hotkeyManager = App.GetHotkeyManager();
        if (hotkeyManager != null)
        {
            hotkeyManager.RegisterAllHotkeys();
        }
        
        // Stop capture mode
        StopCombinedCapture();

        e.Handled = true;
    }

    private bool IsModifierKey(System.Windows.Input.Key key)
    {
        return key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
               key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
               key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
               key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin;
    }

    private string BuildHotkeyString(System.Windows.Input.ModifierKeys modifiers, System.Windows.Input.Key key)
    {
        var parts = new List<string>();

        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            parts.Add("Alt");
        if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
            parts.Add("Win");

        parts.Add(key.ToString());

        return string.Join("+", parts);
    }

    private void StopCapture()
    {
        _isCapturingHotkey = false;
        _captureType = null;
        _captureAction = null;
        _captureTextBox = null;
        
        // Re-register all global hotkeys (unless we're in the auto-save path which does this already)
        // Note: RegisterAllHotkeys is called after saving in MainWindow_KeyDown, so we don't need to call it here
    }

    public void UpdateSerialNumberDisplay(string serialNumber)
    {
        SerialNumberBox.Text = serialNumber;
        _settings.General.SerialNumber = serialNumber;
    }

    public async Task TestConnectionAfterAutoDetect()
    {
        // Update status to show we're testing
        ConnectionStatusText.Text = "Connection Status: Testing connection...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Brief delay to let UI update
        await Task.Delay(500);
        
        // Test connection
        await CheckConnectionAsync();
        
        // Show result notification
        if (ConnectionStatusText.Foreground == System.Windows.Media.Brushes.Green)
        {
            var app = (App)Application.Current;
            app.ShowVolumeNotification($"✓ Connected to GoXLR - Ready!");
        }
    }

    private async Task SaveAndTestSerial(string serialNumber)
    {
        // Update status to show we're saving
        ConnectionStatusText.Text = "Connection Status: Saving and testing...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Reload current settings from disk
        var currentSettings = _configService.Load();
        
        // Update serial number
        currentSettings.General.SerialNumber = serialNumber;
        
        // Save back to file
        _configService.Save(currentSettings);
        
        // Update in-memory settings
        _settings.General.SerialNumber = currentSettings.General.SerialNumber;
        
        // No need to Reinitialize - Settings object is shared, serial will be picked up automatically
        // Brief delay to let UI update
        await Task.Delay(500);
        
        // Test connection
        await CheckConnectionAsync();
        
        // Show result
        if (ConnectionStatusText.Foreground == System.Windows.Media.Brushes.Green)
        {
            MessageBox.Show($"✓ Connected to GoXLR successfully!\n\nSerial: {serialNumber}", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Serial saved but connection test failed.\n\nSerial: {serialNumber}\n\nMake sure GoXLR Utility is running.", 
                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReloadHotkeys_Click(object sender, RoutedEventArgs e)
    {
        // Reload settings from disk
        var newSettings = _configService.Load();
        
        // Copy new settings to app settings
        App.GetSettings().VolumeHotkeys.Clear();
        foreach (var kvp in newSettings.VolumeHotkeys)
            App.GetSettings().VolumeHotkeys[kvp.Key] = kvp.Value;
            
        App.GetSettings().ProfileHotkeys.Clear();
        foreach (var kvp in newSettings.ProfileHotkeys)
            App.GetSettings().ProfileHotkeys[kvp.Key] = kvp.Value;
        
        // Re-register hotkeys
        var hotkeyManager = App.GetHotkeyManager();
        if (hotkeyManager != null)
        {
            var count = hotkeyManager.RegisterAllHotkeys();
            MessageBox.Show($"Reloaded {count} hotkey(s)", "Hotkeys Reloaded", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            PopulateHotkeyEditor();
        }
    }

    private async void SaveSerial_Click(object sender, RoutedEventArgs e)
    {
        // Update status
        ConnectionStatusText.Text = "Connection Status: Saving and testing...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Reload current settings from disk
        var currentSettings = _configService.Load();
        
        // Update serial number
        currentSettings.General.SerialNumber = SerialNumberBox.Text.Trim();
        
        // Save back to file
        _configService.Save(currentSettings);
        
        // Update in-memory settings
        _settings.General.SerialNumber = currentSettings.General.SerialNumber;
        
        // No need to Reinitialize - Settings object is shared, serial will be picked up automatically
        // Brief delay to let UI update
        await Task.Delay(500);
        
        MessageBox.Show("Serial number saved successfully!", "Success", 
            MessageBoxButton.OK, MessageBoxImage.Information);
        
        // Re-check connection
        await CheckConnectionAsync();
    }

    private async void DetectSerial_Click(object sender, RoutedEventArgs e)
    {
        DetectSerialBtn.IsEnabled = false;
        DetectSerialBtn.Content = "Detecting...";
        
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await httpClient.GetAsync("http://localhost:14564/api/get-devices");
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var fullResponse = System.Text.Json.JsonSerializer.Deserialize<Models.GoXLRFullResponse>(json);
                
                if (fullResponse?.Mixers != null && fullResponse.Mixers.Count > 0)
                {
                    var serialNumbers = fullResponse.Mixers.Keys.ToList();
                    
                    if (serialNumbers.Count == 1)
                    {
                        // Auto-populate single device
                        SerialNumberBox.Text = serialNumbers[0];
                        
                        // Automatically save and test
                        await SaveAndTestSerial(serialNumbers[0]);
                    }
                    else
                    {
                        // Multiple devices - let user choose
                        var deviceList = string.Join("\n", serialNumbers);
                        var result = MessageBox.Show(
                            $"Found {serialNumbers.Count} GoXLR devices:\n\n{deviceList}\n\nUsing first device. Click OK to accept or Cancel to enter manually.",
                            "Multiple Devices Found",
                            MessageBoxButton.OKCancel,
                            MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.OK)
                        {
                            SerialNumberBox.Text = serialNumbers[0];
                            
                            // Automatically save and test
                            await SaveAndTestSerial(serialNumbers[0]);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No GoXLR devices found. Make sure your GoXLR is connected and GoXLR Utility is running.",
                        "No Devices Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Unable to connect to GoXLR Utility.\n\nMake sure GoXLR Utility is running on http://localhost:14564",
                    "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error detecting serial number:\n\n{ex.Message}\n\nMake sure GoXLR Utility is running.",
                "Detection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            DetectSerialBtn.IsEnabled = true;
            DetectSerialBtn.Content = "Detect";
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        await CheckConnectionAsync();
    }

    private async Task CheckConnectionAsync()
    {
        ConnectionStatusText.Text = "Connection Status: Checking...";
        ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        // Check if serial is configured
        if (string.IsNullOrWhiteSpace(_settings.General.SerialNumber))
        {
            ConnectionStatusText.Text = "Connection Status: ⚠ Serial number not configured. Click 'Detect' to auto-detect.";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }
        
        var isConnected = await _goXLRService.IsConnectedAsync();
        
        if (isConnected)
        {
            var profile = await _goXLRService.GetCurrentProfileAsync();
            if (profile != null)
            {
                ConnectionStatusText.Text = $"Connection Status: ✓ Connected (Profile: {profile})";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ConnectionStatusText.Text = "Connection Status: ✓ Connected (Check serial number)";
                ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }
        else
        {
            ConnectionStatusText.Text = "Connection Status: ✗ GoXLR Utility not running";
            ConnectionStatusText.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private void TestGoXLR_Click(object sender, RoutedEventArgs e)
    {
        var testWindow = new GoXLRTestWindow(_goXLRService, _settings);
        testWindow.Owner = this;
        testWindow.ShowDialog();
    }

    private void SaveVolumeSettings_Click(object sender, RoutedEventArgs e)
    {
        // Validate inputs
        if (!int.TryParse(VolumeStepBox.Text, out var volumeStep) || volumeStep < 1 || volumeStep > 255)
        {
            MessageBox.Show("Volume Step must be between 1 and 255", "Invalid Input", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(CacheTimeBox.Text, out var cacheTime) || cacheTime < 0)
        {
            MessageBox.Show("Cache Time must be a positive number", "Invalid Input", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Update settings
        _settings.General.VolumeStep = volumeStep;
        _settings.General.VolumeCacheTimeMs = cacheTime;

        // Save to file
        _configService.Save(_settings);

        MessageBox.Show("Volume settings saved successfully!\n\nRestart the app for cache time changes to take effect.", 
            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
    {
        var configPath = _configService.GetConfigFilePath();
        var folderPath = System.IO.Path.GetDirectoryName(configPath);
        
        if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        }
    }

    private void RefreshControllerList()
    {
        var directInputService = App.GetDirectInputService();
        if (directInputService == null)
        {
            ControllerStatusText.Text = "Controller service not available";
            ControllerStatusText.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        var controllers = directInputService.GetConnectedDevices();
        
        if (controllers.Count == 0)
        {
            ControllerStatusText.Text = "No controllers detected";
            ControllerStatusText.Foreground = System.Windows.Media.Brushes.Gray;
            ControllersListBox.ItemsSource = null;
        }
        else
        {
            ControllerStatusText.Text = $"Found {controllers.Count} controller(s):";
            ControllerStatusText.Foreground = System.Windows.Media.Brushes.Green;
            ControllersListBox.ItemsSource = controllers;
        }
    }

    private void RefreshControllers_Click(object sender, RoutedEventArgs e)
    {
        RefreshControllerList();
    }

    private void PopulateHotkeyEditor()
    {
        VolumeHotkeysPanel.Children.Clear();
        ProfileHotkeysPanel.Children.Clear();

        // Populate Volume Hotkeys - ONLY for enabled channels
        var enabledChannels = _settings.EnabledChannels
            .Where(c => _settings.VolumeHotkeys.ContainsKey(c))
            .OrderBy(c => c)
            .ToList();
        
        if (enabledChannels.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "No channels enabled. Go to 'Channels & Profiles' tab to enable channels.",
                FontStyle = FontStyles.Italic,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 10, 0, 10)
            };
            VolumeHotkeysPanel.Children.Add(emptyText);
        }
        else
        {
            foreach (var channel in enabledChannels)
            {
                var hotkeys = _settings.VolumeHotkeys[channel];
            
            // Create a grid for this channel  
            var channelGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            channelGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Channel label
            var label = new TextBlock
            {
                Text = channel + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, 0);
            channelGrid.Children.Add(label);
            
            // "Up" label
            var upLabel = new TextBlock
            {
                Text = "⬆ Up:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetColumn(upLabel, 1);
            Grid.SetRow(upLabel, 0);
            channelGrid.Children.Add(upLabel);
            
            // Volume Up TextBox - shows both keyboard AND button
            var upBox = new TextBox
            {
                Text = GetCombinedHotkeyDisplay(hotkeys.VolumeUp, hotkeys.VolumeUpButton),
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeUp",
                MinWidth = 150
            };
            Grid.SetColumn(upBox, 2);
            Grid.SetRow(upBox, 0);
            channelGrid.Children.Add(upBox);
            
            // Volume Up Capture Button - captures BOTH keyboard and controller
            var upButton = new Button
            {
                Content = "⌨/🎮 Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeUp|{upBox.GetHashCode()}"
            };
            upButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(upButton, 3);
            Grid.SetRow(upButton, 0);
            channelGrid.Children.Add(upButton);
            
            // Volume Up Clear Button
            var upClearButton = new Button
            {
                Content = "✖",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 15, 0),
                Tag = $"{channel}|VolumeUp",
                ToolTip = "Clear hotkey"
            };
            upClearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(upClearButton, 4);
            Grid.SetRow(upClearButton, 0);
            channelGrid.Children.Add(upClearButton);
            
            // "Down" label
            var downLabel = new TextBlock
            {
                Text = "⬇ Down:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetColumn(downLabel, 5);
            Grid.SetRow(downLabel, 0);
            channelGrid.Children.Add(downLabel);
            
            // Volume Down TextBox - shows both keyboard AND button
            var downBox = new TextBox
            {
                Text = GetCombinedHotkeyDisplay(hotkeys.VolumeDown, hotkeys.VolumeDownButton),
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeDown",
                MinWidth = 150
            };
            Grid.SetColumn(downBox, 6);
            Grid.SetRow(downBox, 0);
            channelGrid.Children.Add(downBox);
            
            // Volume Down Capture Button - captures BOTH
            var downButton = new Button
            {
                Content = "⌨/🎮 Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeDown|{downBox.GetHashCode()}"
            };
            downButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(downButton, 7);
            Grid.SetRow(downButton, 0);
            channelGrid.Children.Add(downButton);
            
            // Volume Down Clear Button
            var downClearButton = new Button
            {
                Content = "✖",
                Padding = new Thickness(8, 5, 8, 5),
                Tag = $"{channel}|VolumeDown",
                ToolTip = "Clear hotkey"
            };
            downClearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(downClearButton, 8);
            Grid.SetRow(downClearButton, 0);
            channelGrid.Children.Add(downClearButton);
            
            VolumeHotkeysPanel.Children.Add(channelGrid);
            }
        }

        // Populate Profile Hotkeys
        foreach (var profile in _settings.ProfileHotkeys.Keys.OrderBy(k => k))
        {
            var hotkey = _settings.ProfileHotkeys[profile];
            var button = _settings.ProfileButtons.ContainsKey(profile) ? _settings.ProfileButtons[profile] : "";
            
            var profileGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
            // Profile label
            var label = new TextBlock
            {
                Text = profile + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(label, 0);
            profileGrid.Children.Add(label);
            
            // Hotkey textbox - shows both keyboard AND button
            var hotkeyBox = new TextBox
            {
                Text = GetCombinedHotkeyDisplay(hotkey, button),
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"Profile|{profile}"
            };
            Grid.SetColumn(hotkeyBox, 1);
            profileGrid.Children.Add(hotkeyBox);
            
            // Capture button - captures BOTH
            var captureButton = new Button
            {
                Content = "⌨/🎮 Capture",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"Profile|{profile}|{hotkeyBox.GetHashCode()}"
            };
            captureButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(captureButton, 2);
            profileGrid.Children.Add(captureButton);
            
            // Clear button
            var clearButton = new Button
            {
                Content = "✖",
                Padding = new Thickness(10, 5, 10, 5),
                Tag = $"Profile|{profile}",
                ToolTip = "Clear hotkey"
            };
            clearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(clearButton, 3);
            profileGrid.Children.Add(clearButton);
            
            ProfileHotkeysPanel.Children.Add(profileGrid);
        }
    }

    private void StartCapture_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var parts = tag.Split('|');
        if (parts.Length < 3)
            return;

        var type = parts[0]; // Channel name or "Profile"
        var action = parts[1]; // "VolumeUp", "VolumeDown", or profile name
        var textBoxHashCode = int.Parse(parts[2]);

        // Find the TextBox to highlight
        TextBox? targetTextBox = null;
        if (type == "Profile")
        {
            foreach (var child in ProfileHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == textBoxHashCode)
                        {
                            targetTextBox = tb;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var child in VolumeHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == textBoxHashCode)
                        {
                            targetTextBox = tb;
                            break;
                        }
                    }
                }
            }
        }

        if (targetTextBox == null)
            return;

        // Start capture mode
        _isCapturingHotkey = true;
        _captureType = type;
        _captureAction = action;
        _captureTextBox = targetTextBox;

        // Temporarily unregister all global hotkeys so they don't interfere
        var hotkeyManager = App.GetHotkeyManager();
        hotkeyManager?.TemporaryUnregisterAll();

        // Highlight the textbox
        targetTextBox.Background = System.Windows.Media.Brushes.LightYellow;
        targetTextBox.Foreground = System.Windows.Media.Brushes.Black;
        targetTextBox.Text = "Press keys now...";
        
        // Focus the window to ensure it receives key events
        Focus();
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var parts = tag.Split('|');
        if (parts.Length < 2)
            return;

        var type = parts[0]; // Channel name or "Profile"
        var action = parts[1]; // "VolumeUp", "VolumeDown", or profile name

        // Clear BOTH keyboard hotkey AND controller button
        if (type == "Profile")
        {
            _settings.ProfileHotkeys[action] = "";
            if (_settings.ProfileButtons.ContainsKey(action))
                _settings.ProfileButtons[action] = "";
        }
        else
        {
            if (action == "VolumeUp")
            {
                _settings.VolumeHotkeys[type].VolumeUp = "";
                _settings.VolumeHotkeys[type].VolumeUpButton = "";
            }
            else if (action == "VolumeDown")
            {
                _settings.VolumeHotkeys[type].VolumeDown = "";
                _settings.VolumeHotkeys[type].VolumeDownButton = "";
            }
        }

        // Auto-save to config file
        _configService.Save(_settings);
        
        // Auto-register hotkeys (removes the cleared one)
        var hotkeyManager = App.GetHotkeyManager();
        if (hotkeyManager != null)
        {
            hotkeyManager.RegisterAllHotkeys();
        }

        // Refresh UI
        PopulateHotkeyEditor();
    }

    private string? CheckHotkeyConflict(string hotkey, string excludeType, string excludeAction)
    {
        // Check volume hotkeys
        foreach (var kvp in _settings.VolumeHotkeys)
        {
            if (kvp.Value.VolumeUp == hotkey && !(excludeType == kvp.Key && excludeAction == "VolumeUp"))
                return $"{kvp.Key} Volume Up";
            
            if (kvp.Value.VolumeDown == hotkey && !(excludeType == kvp.Key && excludeAction == "VolumeDown"))
                return $"{kvp.Key} Volume Down";
        }

        // Check profile hotkeys
        foreach (var kvp in _settings.ProfileHotkeys)
        {
            if (kvp.Value == hotkey && !(excludeType == "Profile" && excludeAction == kvp.Key))
                return $"Profile: {kvp.Key}";
        }

        return null;
    }

    private string? CheckButtonConflict(string buttonString, string excludeType, string excludeAction)
    {
        // Check volume button assignments
        foreach (var kvp in _settings.VolumeHotkeys)
        {
            if (kvp.Value.VolumeUpButton == buttonString && !(excludeType == kvp.Key && excludeAction == "VolumeUp"))
                return $"{kvp.Key} Volume Up";
            
            if (kvp.Value.VolumeDownButton == buttonString && !(excludeType == kvp.Key && excludeAction == "VolumeDown"))
                return $"{kvp.Key} Volume Down";
        }

        // Check profile button assignments
        foreach (var kvp in _settings.ProfileButtons)
        {
            if (kvp.Value == buttonString && !(excludeType == "Profile" && excludeAction == kvp.Key))
                return $"Profile: {kvp.Key}";
        }

        return null;
    }

    private void SaveHotkeys_Click(object sender, RoutedEventArgs e)
    {
        // Save to config file
        _configService.Save(_settings);

        // Re-register hotkeys
        var hotkeyManager = App.GetHotkeyManager();
        if (hotkeyManager != null)
        {
            var count = hotkeyManager.RegisterAllHotkeys();
            MessageBox.Show($"Saved and registered {count} hotkey(s) successfully!", 
                "Hotkeys Saved", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Hotkeys saved to configuration file.", 
                "Saved", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        PopulateHotkeyEditor();
    }

    private void RestoreWindowSettings()
    {
        if (_settings.Window.Width > 0 && _settings.Window.Height > 0)
        {
            Width = _settings.Window.Width;
            Height = _settings.Window.Height;
        }

        // Ensure window is on screen
        if (_settings.Window.Left >= 0 && _settings.Window.Top >= 0)
        {
            Left = _settings.Window.Left;
            Top = _settings.Window.Top;
        }
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        // Hide to tray when minimized
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // Don't actually close, just hide to tray
        e.Cancel = true;
        Hide();
    }

    public void SaveWindowSettings()
    {
        // Reload settings from disk to preserve any manual edits
        var currentSettings = _configService.Load();
        
        // Only update window settings
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

    private void RefreshControllerList2()
    {
        var directInputService = App.GetDirectInputService();
        if (directInputService == null)
        {
            ControllerStatusText2.Text = "Controller service not available";
            ControllerStatusText2.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        var controllers = directInputService.GetConnectedDevices();
        
        if (controllers.Count == 0)
        {
            ControllerStatusText2.Text = "No controllers detected";
            ControllerStatusText2.Foreground = System.Windows.Media.Brushes.Gray;
            ControllersListBox2.ItemsSource = null;
        }
        else
        {
            ControllerStatusText2.Text = $"Found {controllers.Count} controller(s):";
            ControllerStatusText2.Foreground = System.Windows.Media.Brushes.Green;
            ControllersListBox2.ItemsSource = controllers;
        }
    }

    private void RefreshControllers2_Click(object sender, RoutedEventArgs e)
    {
        RefreshControllerList2();
    }

    // ===== Start with Windows =====
    
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SimControlCentre";
    
    private void StartWithWindows_Changed(object sender, RoutedEventArgs e)
    {
        if (StartWithWindowsCheckBox.IsChecked == true)
        {
            EnableStartWithWindows();
        }
        else
        {
            DisableStartWithWindows();
        }
        
        // Save to settings
        _settings.Window.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;
        _configService.Save(_settings);
    }
    
    private bool IsStartWithWindowsEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            return false;
        }
    }
    
    private void EnableStartWithWindows()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return;
            
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            key?.SetValue(AppName, $"\"{exePath}\"");
            
            Console.WriteLine($"[MainWindow] Enabled start with Windows: {exePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindow] Failed to enable start with Windows: {ex.Message}");
            MessageBox.Show($"Failed to enable start with Windows.\n\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void DisableStartWithWindows()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            key?.DeleteValue(AppName, false);
            
            Console.WriteLine("[MainWindow] Disabled start with Windows");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainWindow] Failed to disable start with Windows: {ex.Message}");
        }
    }

    // ===== Channels & Profiles Management =====
    
    private void PopulateChannelsAndProfiles()
    {
        // All available GoXLR channels
        var allChannels = new List<string> 
        { 
            "Mic", "LineIn", "Console", "System", "Game", "Chat", "Sample", "Music", 
            "Headphones", "MicMonitor", "LineOut" 
        };
        
        // Create checkboxes with proper state tracking
        var checkBoxes = new List<CheckBox>();
        foreach (var channel in allChannels)
        {
            var checkBox = new CheckBox
            {
                Content = channel,
                IsChecked = _settings.EnabledChannels.Contains(channel),
                Margin = new Thickness(0, 5, 0, 5),
                Tag = channel
            };
            checkBox.Checked += Channel_CheckedChanged;
            checkBox.Unchecked += Channel_CheckedChanged;
            checkBoxes.Add(checkBox);
        }
        
        ChannelsListBox.ItemsSource = checkBoxes;
        
        // Populate profiles
        RefreshProfilesList();
    }
    
    private void RefreshProfilesList()
    {
        var profiles = _settings.ProfileHotkeys.Keys.ToList();
        ProfilesListBox.ItemsSource = null;
        ProfilesListBox.ItemsSource = profiles;
        
        if (profiles.Count > 0)
        {
            ProfileStatusText.Text = $"{profiles.Count} profile(s) configured";
            ProfileStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            ProfileStatusText.Text = "No profiles configured";
            ProfileStatusText.Foreground = System.Windows.Media.Brushes.Gray;
        }
    }
    
    private async void FetchProfiles_Click(object sender, RoutedEventArgs e)
    {
        await FetchGoXLRProfilesAsync();
    }
    
    private async Task FetchGoXLRProfilesAsync()
    {
        ProfileStatusText.Text = "Fetching profiles from GoXLR...";
        ProfileStatusText.Foreground = System.Windows.Media.Brushes.Orange;
        
        try
        {
            var profiles = await _goXLRService.GetProfilesAsync();
            
            if (profiles.Count == 0)
            {
                ProfileStatusText.Text = "No profiles found on GoXLR";
                ProfileStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                _availableProfiles.Clear();
                AvailableProfilesComboBox.ItemsSource = null;
                return;
            }
            
            _availableProfiles = profiles;
            
            // Show only profiles that aren't already configured
            var unconfiguredProfiles = profiles
                .Where(p => !_settings.ProfileHotkeys.ContainsKey(p))
                .ToList();
            
            AvailableProfilesComboBox.ItemsSource = unconfiguredProfiles;
            
            if (unconfiguredProfiles.Count > 0)
            {
                AvailableProfilesComboBox.SelectedIndex = 0;
                ProfileStatusText.Text = $"Found {profiles.Count} profile(s) - {unconfiguredProfiles.Count} available to add";
                ProfileStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ProfileStatusText.Text = $"Found {profiles.Count} profile(s) - all already configured";
                ProfileStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
        }
        catch (Exception ex)
        {
            ProfileStatusText.Text = "Failed to fetch profiles (GoXLR Utility may not be running)";
            ProfileStatusText.Foreground = System.Windows.Media.Brushes.Red;
            _availableProfiles.Clear();
            AvailableProfilesComboBox.ItemsSource = null;
            Console.WriteLine($"[MainWindow] Failed to fetch profiles: {ex.Message}");
        }
    }
    
    private void AddSelectedProfile_Click(object sender, RoutedEventArgs e)
    {
        if (AvailableProfilesComboBox.SelectedItem is not string profileName)
        {
            return; // Silently ignore if nothing selected
        }
        
        if (_settings.ProfileHotkeys.ContainsKey(profileName))
        {
            return; // Silently ignore if already exists
        }
        
        // Add profile
        _settings.ProfileHotkeys[profileName] = "";
        _settings.ProfileButtons[profileName] = "";
        
        // Auto-save
        _configService.Save(_settings);
        
        // Refresh UI
        RefreshProfilesList();
        PopulateHotkeyEditor();
        
        // Refresh available profiles dropdown
        var unconfiguredProfiles = _availableProfiles
            .Where(p => !_settings.ProfileHotkeys.ContainsKey(p))
            .ToList();
        AvailableProfilesComboBox.ItemsSource = unconfiguredProfiles;
        if (unconfiguredProfiles.Count > 0)
            AvailableProfilesComboBox.SelectedIndex = 0;
    }
    
    private async void RefreshProfilesList_Click(object sender, RoutedEventArgs e)
    {
        await FetchGoXLRProfilesAsync();
    }
    
    private void RemoveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string profileName)
            return;
        
        var result = MessageBox.Show($"Remove profile '{profileName}'?\n\nThis will also remove its hotkey assignments.", 
            "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _settings.ProfileHotkeys.Remove(profileName);
            _settings.ProfileButtons.Remove(profileName);
            RefreshProfilesList();
        }
    }
    
    private void Channel_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.Tag is not string channel)
            return;
        
        if (checkBox.IsChecked == true)
        {
            // Enable channel
            if (!_settings.EnabledChannels.Contains(channel))
            {
                _settings.EnabledChannels.Add(channel);
            }
            
            // Ensure it has a VolumeHotkeys entry
            if (!_settings.VolumeHotkeys.ContainsKey(channel))
            {
                _settings.VolumeHotkeys[channel] = new ChannelHotkeys();
            }
        }
        else
        {
            // Disable channel
            _settings.EnabledChannels.Remove(channel);
        }
        
        // Auto-save
        _configService.Save(_settings);
        
        // Refresh hotkeys tab
        PopulateHotkeyEditor();
    }
    
    private void SaveChannelsProfiles_Click(object sender, RoutedEventArgs e)
    {
        _configService.Save(_settings);
        
        // Refresh hotkeys tab to show new profiles
        PopulateHotkeyEditor();
    }

    // ===== Controller Button Capture =====
    
    private string GetCombinedHotkeyDisplay(string? keyboard, string? button)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(keyboard))
            parts.Add(keyboard);
        
        if (!string.IsNullOrWhiteSpace(button))
        {
            // Parse button string: Device:{GUID}:Button:{number}
            var buttonParts = button.Split(':');
            if (buttonParts.Length >= 4)
                parts.Add($"Btn {buttonParts[3]}");
        }
        
        return parts.Count > 0 ? string.Join(" OR ", parts) : "";
    }
    
    private void StartCombinedCapture_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var parts = tag.Split('|');
        if (parts.Length < 3)
            return;

        var type = parts[0]; // Channel name or "Profile"
        var action = parts[1]; // "VolumeUp", "VolumeDown", or profile name
        var textBoxHashCode = int.Parse(parts[2]);

        // Find the TextBox to highlight
        TextBox? targetTextBox = null;
        
        if (type == "Profile")
        {
            foreach (var child in ProfileHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == textBoxHashCode)
                        {
                            targetTextBox = tb;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var child in VolumeHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == textBoxHashCode)
                        {
                            targetTextBox = tb;
                            break;
                        }
                    }
                }
            }
        }

        if (targetTextBox == null)
            return;

        // Start BOTH keyboard and button capture
        _isCapturingHotkey = true;
        _isCapturingButton = true;
        _captureType = type;
        _captureAction = action;
        _captureTextBox = targetTextBox;
        _captureButtonTextBox = targetTextBox; // Same textbox for both

        // Temporarily unregister all global hotkeys
        var hotkeyManager = App.GetHotkeyManager();
        hotkeyManager?.TemporaryUnregisterAll();

        // Subscribe to button events
        var directInputService = App.GetDirectInputService();
        if (directInputService != null)
        {
            directInputService.ButtonPressed += OnCombinedButtonCaptured;
        }

        // Highlight the textbox
        targetTextBox.Background = System.Windows.Media.Brushes.LightYellow;
        targetTextBox.Foreground = System.Windows.Media.Brushes.Black;
        targetTextBox.Text = "Press key or button...";
        
        // Focus the window to ensure it receives key events
        Focus();
    }

    private void OnCombinedButtonCaptured(object? sender, ButtonPressedEventArgs e)
    {
        if (!_isCapturingButton || _captureButtonTextBox == null)
            return;

        Dispatcher.Invoke(() =>
        {
            // Format: Device:{GUID}:Button:{number}
            var buttonString = $"Device:{e.DeviceGuid}:Button:{e.ButtonNumber}";
            
            // Check for button conflicts
            var conflict = CheckButtonConflict(buttonString, _captureType!, _captureAction!);
            if (!string.IsNullOrEmpty(conflict))
            {
                // Show "Already in use" message
                string originalButton = "";
                string originalKeyboard = "";
                
                if (_captureType == "Profile")
                {
                    originalButton = _settings.ProfileButtons.ContainsKey(_captureAction!) 
                        ? _settings.ProfileButtons[_captureAction!] : "";
                    originalKeyboard = _settings.ProfileHotkeys.ContainsKey(_captureAction!) 
                        ? _settings.ProfileHotkeys[_captureAction!] : "";
                }
                else
                {
                    originalButton = _captureAction == "VolumeUp"
                        ? _settings.VolumeHotkeys[_captureType!].VolumeUpButton ?? ""
                        : _settings.VolumeHotkeys[_captureType!].VolumeDownButton ?? "";
                    
                    originalKeyboard = _captureAction == "VolumeUp"
                        ? _settings.VolumeHotkeys[_captureType!].VolumeUp ?? ""
                        : _settings.VolumeHotkeys[_captureType!].VolumeDown ?? "";
                }

                _captureButtonTextBox.Text = "Already in use";
                _captureButtonTextBox.Foreground = System.Windows.Media.Brushes.Red;
                _captureButtonTextBox.Background = System.Windows.Media.Brushes.LightPink;
                
                var textBoxToReset = _captureButtonTextBox;
                
                StopCombinedCapture();
                
                // Re-register hotkeys
                var mgr = App.GetHotkeyManager();
                mgr?.RegisterAllHotkeys();
                
                // Reset after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    textBoxToReset.Text = GetCombinedHotkeyDisplay(originalKeyboard, originalButton);
                    textBoxToReset.Foreground = System.Windows.Media.Brushes.Black;
                    textBoxToReset.Background = System.Windows.Media.Brushes.White;
                };
                timer.Start();
                
                return;
            }
            
            // Save based on action type
            if (_captureType == "Profile")
            {
                _settings.ProfileButtons[_captureAction!] = buttonString;
                _captureButtonTextBox.Text = GetCombinedHotkeyDisplay(
                    _settings.ProfileHotkeys.ContainsKey(_captureAction!) ? _settings.ProfileHotkeys[_captureAction!] : "",
                    buttonString);
            }
            else if (_captureAction == "VolumeUp")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeUpButton = buttonString;
                _captureButtonTextBox.Text = GetCombinedHotkeyDisplay(
                    _settings.VolumeHotkeys[_captureType!].VolumeUp,
                    buttonString);
            }
            else if (_captureAction == "VolumeDown")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeDownButton = buttonString;
                _captureButtonTextBox.Text = GetCombinedHotkeyDisplay(
                    _settings.VolumeHotkeys[_captureType!].VolumeDown,
                    buttonString);
            }

            _captureButtonTextBox.Background = System.Windows.Media.Brushes.White;
            
            // Auto-save
            _configService.Save(_settings);
            
            // Re-register hotkeys
            var hotkeyManager = App.GetHotkeyManager();
            hotkeyManager?.RegisterAllHotkeys();
            
            // Stop capture
            StopCombinedCapture();
        });
    }

    private void StopCombinedCapture()
    {
        _isCapturingHotkey = false;
        _isCapturingButton = false;
        _captureType = null;
        _captureAction = null;
        _captureTextBox = null;
        _captureButtonTextBox = null;

        var directInputService = App.GetDirectInputService();
        if (directInputService != null)
        {
            directInputService.ButtonPressed -= OnCombinedButtonCaptured;
        }
    }
    
    private void StartButtonCapture_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var parts = tag.Split('|');
        if (parts.Length < 3)
            return;

        var channel = parts[0]; // Channel name or "Profile"
        var action = parts[1]; // "VolumeUpButton", "VolumeDownButton", etc.
        var textBoxHashCode = int.Parse(parts[2]);

        // Find the TextBox to highlight
        TextBox? targetTextBox = null;
        foreach (var child in VolumeHotkeysPanel.Children)
        {
            if (child is Grid grid)
            {
                foreach (var gridChild in grid.Children)
                {
                    if (gridChild is TextBox tb && tb.GetHashCode() == textBoxHashCode)
                    {
                        targetTextBox = tb;
                        break;
                    }
                }
            }
        }

        if (targetTextBox == null)
            return;

        // Start capture mode
        _isCapturingButton = true;
        _captureButtonTextBox = targetTextBox;
        _captureType = channel;
        _captureAction = action;

        // Highlight the textbox
        targetTextBox.Background = System.Windows.Media.Brushes.LightYellow;
        targetTextBox.Text = "Press button...";
        
        // Subscribe to button events
        var directInputService = App.GetDirectInputService();
        if (directInputService != null)
        {
            directInputService.ButtonPressed += OnControllerButtonCaptured;
        }
        
        // Focus the window
        Focus();
    }

    private void OnControllerButtonCaptured(object? sender, ButtonPressedEventArgs e)
    {
        if (!_isCapturingButton || _captureButtonTextBox == null)
            return;

        Dispatcher.Invoke(() =>
        {
            // Format: Device:{GUID}:Button:{number}
            var buttonString = $"Device:{e.DeviceGuid}:Button:{e.ButtonNumber}";
            
            // Save based on action type
            if (_captureAction == "VolumeUpButton")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeUpButton = buttonString;
            }
            else if (_captureAction == "VolumeDownButton")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeDownButton = buttonString;
            }
            else if (_captureAction?.StartsWith("Profile") == true)
            {
                // Handle profile buttons if needed
                var profileName = _captureAction.Replace("ProfileButton|", "");
                _settings.ProfileButtons[profileName] = buttonString;
            }

            // Update UI
            _captureButtonTextBox.Text = $"Btn {e.ButtonNumber}";
            _captureButtonTextBox.Background = System.Windows.Media.Brushes.White;
            
            // Auto-save
            _configService.Save(_settings);
            
            // Stop capture
            StopControllerButtonCapture();
        });
    }

    private void StopControllerButtonCapture()
    {
        _isCapturingButton = false;
        _captureButtonTextBox = null;
        _captureType = null;
        _captureAction = null;

        var directInputService = App.GetDirectInputService();
        if (directInputService != null)
        {
            directInputService.ButtonPressed -= OnControllerButtonCaptured;
        }
    }
}


