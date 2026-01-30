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
            
            StopCapture();
            
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
        StopCapture();

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

    private void PopulateHotkeyEditor()
    {
        VolumeHotkeysPanel.Children.Clear();
        ProfileHotkeysPanel.Children.Clear();

        // Populate Volume Hotkeys
        foreach (var channel in _settings.VolumeHotkeys.Keys.OrderBy(k => k))
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
            
            // Volume Up TextBox
            var upBox = new TextBox
            {
                Text = hotkeys.VolumeUp ?? "",
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeUp",
                MinWidth = 120
            };
            Grid.SetColumn(upBox, 2);
            Grid.SetRow(upBox, 0);
            channelGrid.Children.Add(upBox);
            
            // Volume Up Capture Button
            var upButton = new Button
            {
                Content = "⌨ Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeUp|{upBox.GetHashCode()}"
            };
            upButton.Click += StartCapture_Click;
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
            
            // Volume Down TextBox
            var downBox = new TextBox
            {
                Text = hotkeys.VolumeDown ?? "",
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeDown",
                MinWidth = 120
            };
            Grid.SetColumn(downBox, 6);
            Grid.SetRow(downBox, 0);
            channelGrid.Children.Add(downBox);
            
            // Volume Down Capture Button
            var downButton = new Button
            {
                Content = "⌨ Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeDown|{downBox.GetHashCode()}"
            };
            downButton.Click += StartCapture_Click;
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

        // Populate Profile Hotkeys
        foreach (var profile in _settings.ProfileHotkeys.Keys.OrderBy(k => k))
        {
            var hotkey = _settings.ProfileHotkeys[profile];
            
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
            
            // Hotkey textbox
            var hotkeyBox = new TextBox
            {
                Text = hotkey ?? "",
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"Profile|{profile}"
            };
            Grid.SetColumn(hotkeyBox, 1);
            profileGrid.Children.Add(hotkeyBox);
            
            // Capture button
            var captureButton = new Button
            {
                Content = "⌨ Capture",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"Profile|{profile}|{hotkeyBox.GetHashCode()}"
            };
            captureButton.Click += StartCapture_Click;
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

        // Clear the hotkey
        if (type == "Profile")
        {
            _settings.ProfileHotkeys[action] = "";
        }
        else
        {
            if (action == "VolumeUp")
                _settings.VolumeHotkeys[type].VolumeUp = "";
            else if (action == "VolumeDown")
                _settings.VolumeHotkeys[type].VolumeDown = "";
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
}
