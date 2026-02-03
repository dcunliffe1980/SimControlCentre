using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class HotkeysTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        
        // Hotkey capture state
        private bool _isCapturingHotkey = false;
        private bool _isCapturingButton = false;
        private string? _captureType;
        private string? _captureAction;
        private TextBox? _captureTextBox;
        private TextBox? _captureButtonTextBox;

        public HotkeysTab(ConfigurationService configService, AppSettings settings)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            
            // Add key event handler for hotkey capture
            PreviewKeyDown += HotkeysTab_PreviewKeyDown;
            
            CheckPluginAvailability();
            PopulateHotkeyEditor();
        }

        public void CheckPluginAvailability()
        {
            // Check if device control component is specifically enabled
            bool deviceControlComponentEnabled = _settings.Lighting?.EnabledPlugins?.GetValueOrDefault("goxlr-device-control", true) ?? true;
            
            // Also check if any device control plugins are actually available
            var deviceControlService = App.GetDeviceControlService();
            bool hasEnabledPlugins = deviceControlService?.Plugins.Any(p => p.IsEnabled) ?? false;
            
            bool pluginsAvailable = hasEnabledPlugins && deviceControlComponentEnabled;
            
            if (!pluginsAvailable)
            {
                NoPluginsWarning.Visibility = Visibility.Visible;
                HotkeysContentPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoPluginsWarning.Visibility = Visibility.Collapsed;
                HotkeysContentPanel.Visibility = Visibility.Visible;
            }
        }

        public void RefreshHotkeys()
        {
            CheckPluginAvailability();
            PopulateHotkeyEditor();
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
                    CreateVolumeHotkeyRow(channel, hotkeys);
                }
            }

            // Populate Profile Hotkeys
            foreach (var profile in _settings.ProfileHotkeys.Keys.OrderBy(k => k))
            {
                var hotkey = _settings.ProfileHotkeys[profile];
                var button = _settings.ProfileButtons.ContainsKey(profile) ? _settings.ProfileButtons[profile] : "";
                CreateProfileHotkeyRow(profile, hotkey, button);
            }
        }

        private void CreateVolumeHotkeyRow(string channel, ChannelHotkeys hotkeys)
        {
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
            
            // Channel label
            var label = new TextBlock
            {
                Text = channel + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(label, 0);
            channelGrid.Children.Add(label);
            
            // Volume Up
            var upLabel = new TextBlock
            {
                Text = "Up:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetColumn(upLabel, 1);
            channelGrid.Children.Add(upLabel);
            
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
            channelGrid.Children.Add(upBox);
            
            var upButton = new Button
            {
                Content = "Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeUp|{upBox.GetHashCode()}"
            };
            upButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(upButton, 3);
            channelGrid.Children.Add(upButton);
            
            var upClearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 15, 0),
                Tag = $"{channel}|VolumeUp",
                ToolTip = "Clear hotkey"
            };
            upClearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(upClearButton, 4);
            channelGrid.Children.Add(upClearButton);
            
            // Volume Down
            var downLabel = new TextBlock
            {
                Text = "Down:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Foreground = System.Windows.Media.Brushes.Gray
            };
            Grid.SetColumn(downLabel, 5);
            channelGrid.Children.Add(downLabel);
            
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
            channelGrid.Children.Add(downBox);
            
            var downButton = new Button
            {
                Content = "Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeDown|{downBox.GetHashCode()}"
            };
            downButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(downButton, 7);
            channelGrid.Children.Add(downButton);
            
            var downClearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(8, 5, 8, 5),
                Tag = $"{channel}|VolumeDown",
                ToolTip = "Clear hotkey"
            };
            downClearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(downClearButton, 8);
            channelGrid.Children.Add(downClearButton);
            
            VolumeHotkeysPanel.Children.Add(channelGrid);
        }

        private void CreateProfileHotkeyRow(string profile, string hotkey, string button)
        {
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
                Text = GetCombinedHotkeyDisplay(hotkey, button),
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
                Content = "Capture",
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
                Content = "Clear",
                Padding = new Thickness(10, 5, 10, 5),
                Tag = $"Profile|{profile}",
                ToolTip = "Clear hotkey"
            };
            clearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(clearButton, 3);
            profileGrid.Children.Add(clearButton);
            
            ProfileHotkeysPanel.Children.Add(profileGrid);
        }

        private string GetCombinedHotkeyDisplay(string? keyboard, string? button)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(keyboard))
                parts.Add(keyboard);
            
            if (!string.IsNullOrWhiteSpace(button))
            {
                // Parse button string: DeviceName:{ProductGuid}:Button:{number}
                var buttonParts = button.Split(':');
                if (buttonParts.Length >= 4)
                {
                    var deviceName = buttonParts[0].Trim();
                    var buttonNumber = buttonParts[3];
                    parts.Add($"{deviceName}: Btn {buttonNumber}");
                }
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

            var type = parts[0];
            var action = parts[1];
            var textBoxHashCode = int.Parse(parts[2]);

            // Find the TextBox
            TextBox? targetTextBox = FindTextBoxByHashCode(textBoxHashCode);
            if (targetTextBox == null)
                return;

            // Start capture
            _isCapturingHotkey = true;
            _isCapturingButton = true;
            _captureType = type;
            _captureAction = action;
            _captureTextBox = targetTextBox;
            _captureButtonTextBox = targetTextBox;

            // Temporarily unregister hotkeys
            var hotkeyManager = App.GetHotkeyManager();
            hotkeyManager?.TemporaryUnregisterAll();

            // Subscribe to button events
            var directInputService = App.GetDirectInputService();
            if (directInputService != null)
            {
                directInputService.ButtonPressed += OnCombinedButtonCaptured;
            }

            // Highlight textbox
            targetTextBox.Background = System.Windows.Media.Brushes.LightYellow;
            targetTextBox.Foreground = System.Windows.Media.Brushes.Black;
            targetTextBox.Text = "Press key or button...";
            
            Focus();
        }

        private TextBox? FindTextBoxByHashCode(int hashCode)
        {
            // Search in volume hotkeys
            foreach (var child in VolumeHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == hashCode)
                            return tb;
                    }
                }
            }
            
            // Search in profile hotkeys
            foreach (var child in ProfileHotkeysPanel.Children)
            {
                if (child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is TextBox tb && tb.GetHashCode() == hashCode)
                            return tb;
                    }
                }
            }
            
            return null;
        }

        private void HotkeysTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey || _captureTextBox == null)
                return;

            // Ignore modifier-only keys
            if (IsModifierKey(e.Key))
                return;

            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // Build hotkey string
            var hotkeyString = BuildHotkeyString(modifiers, key);

            // Check for conflicts
            var conflict = CheckHotkeyConflict(hotkeyString, _captureType!, _captureAction!);
            if (!string.IsNullOrEmpty(conflict))
            {
                ShowConflictMessage(conflict);
                return;
            }

            // Save hotkey
            SaveHotkey(hotkeyString);

            e.Handled = true;
        }

        private void OnCombinedButtonCaptured(object? sender, ButtonPressedEventArgs e)
        {
            if (!_isCapturingButton || _captureButtonTextBox == null)
                return;

            Dispatcher.Invoke(() =>
            {
                var buttonString = $"{e.DeviceName}:{e.ProductGuid}:Button:{e.ButtonNumber}";
                
                // Check for conflicts
                var conflict = CheckButtonConflict(buttonString, _captureType!, _captureAction!);
                if (!string.IsNullOrEmpty(conflict))
                {
                    ShowConflictMessage(conflict, isButton: true);
                    return;
                }
                
                // Save button
                SaveButton(buttonString);
            });
        }

        private void SaveHotkey(string hotkeyString)
        {
            if (_captureType == "Profile")
            {
                _settings.ProfileHotkeys[_captureAction!] = hotkeyString;
                _captureTextBox!.Text = GetCombinedHotkeyDisplay(
                    hotkeyString,
                    _settings.ProfileButtons.ContainsKey(_captureAction!) ? _settings.ProfileButtons[_captureAction!] : "");
            }
            else if (_captureAction == "VolumeUp")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeUp = hotkeyString;
                _captureTextBox!.Text = GetCombinedHotkeyDisplay(
                    hotkeyString,
                    _settings.VolumeHotkeys[_captureType!].VolumeUpButton);
            }
            else if (_captureAction == "VolumeDown")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeDown = hotkeyString;
                _captureTextBox!.Text = GetCombinedHotkeyDisplay(
                    hotkeyString,
                    _settings.VolumeHotkeys[_captureType!].VolumeDownButton);
            }

            _captureTextBox!.Background = System.Windows.Media.Brushes.White;
            
            // Auto-save
            _configService.Save(_settings);
            
            // Re-register hotkeys
            var hotkeyManager = App.GetHotkeyManager();
            hotkeyManager?.RegisterAllHotkeys();
            
            StopCombinedCapture();
        }

        private void SaveButton(string buttonString)
        {
            if (_captureType == "Profile")
            {
                _settings.ProfileButtons[_captureAction!] = buttonString;
                _captureButtonTextBox!.Text = GetCombinedHotkeyDisplay(
                    _settings.ProfileHotkeys.ContainsKey(_captureAction!) ? _settings.ProfileHotkeys[_captureAction!] : "",
                    buttonString);
            }
            else if (_captureAction == "VolumeUp")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeUpButton = buttonString;
                _captureButtonTextBox!.Text = GetCombinedHotkeyDisplay(
                    _settings.VolumeHotkeys[_captureType!].VolumeUp,
                    buttonString);
            }
            else if (_captureAction == "VolumeDown")
            {
                _settings.VolumeHotkeys[_captureType!].VolumeDownButton = buttonString;
                _captureButtonTextBox!.Text = GetCombinedHotkeyDisplay(
                    _settings.VolumeHotkeys[_captureType!].VolumeDown,
                    buttonString);
            }

            _captureButtonTextBox!.Background = System.Windows.Media.Brushes.White;
            
            // Auto-save
            _configService.Save(_settings);
            
            // Re-register hotkeys
            var hotkeyManager = App.GetHotkeyManager();
            hotkeyManager?.RegisterAllHotkeys();
            
            StopCombinedCapture();
        }

        private void ShowConflictMessage(string conflict, bool isButton = false)
        {
            var textBox = isButton ? _captureButtonTextBox : _captureTextBox;
            if (textBox == null) return;

            // Get original values
            string originalButton = "";
            string originalKeyboard = "";
            
            if (_captureType == "Profile")
            {
                originalButton = _settings.ProfileButtons.ContainsKey(_captureAction!) ? _settings.ProfileButtons[_captureAction!] : "";
                originalKeyboard = _settings.ProfileHotkeys.ContainsKey(_captureAction!) ? _settings.ProfileHotkeys[_captureAction!] : "";
            }
            else
            {
                if (_captureAction == "VolumeUp")
                {
                    originalButton = _settings.VolumeHotkeys[_captureType!].VolumeUpButton ?? "";
                    originalKeyboard = _settings.VolumeHotkeys[_captureType!].VolumeUp ?? "";
                }
                else
                {
                    originalButton = _settings.VolumeHotkeys[_captureType!].VolumeDownButton ?? "";
                    originalKeyboard = _settings.VolumeHotkeys[_captureType!].VolumeDown ?? "";
                }
            }

            textBox.Text = "Already in use";
            textBox.Foreground = System.Windows.Media.Brushes.Red;
            textBox.Background = System.Windows.Media.Brushes.LightPink;
            
            var textBoxToReset = textBox;
            
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
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string tag)
                return;

            var parts = tag.Split('|');
            if (parts.Length < 2)
                return;

            var type = parts[0];
            var action = parts[1];

            // Clear BOTH keyboard and button
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

            // Auto-save
            _configService.Save(_settings);
            
            // Re-register hotkeys
            var hotkeyManager = App.GetHotkeyManager();
            hotkeyManager?.RegisterAllHotkeys();

            // Refresh UI
            PopulateHotkeyEditor();
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

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin;
        }

        private string BuildHotkeyString(ModifierKeys modifiers, Key key)
        {
            var parts = new List<string>();

            if (modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(key.ToString());

            return string.Join("+", parts);
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
            // Check volume buttons
            foreach (var kvp in _settings.VolumeHotkeys)
            {
                if (kvp.Value.VolumeUpButton == buttonString && !(excludeType == kvp.Key && excludeAction == "VolumeUp"))
                    return $"{kvp.Key} Volume Up";
                
                if (kvp.Value.VolumeDownButton == buttonString && !(excludeType == kvp.Key && excludeAction == "VolumeDown"))
                    return $"{kvp.Key} Volume Down";
            }

            // Check profile buttons
            foreach (var kvp in _settings.ProfileButtons)
            {
                if (kvp.Value == buttonString && !(excludeType == "Profile" && excludeAction == kvp.Key))
                    return $"Profile: {kvp.Key}";
            }

            return null;
        }
    }
}
