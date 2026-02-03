using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Contracts;
using SimControlCentre.Plugins.GoXLR.Services;

namespace SimControlCentre.Plugins.GoXLR.Views
{
    /// <summary>
    /// GoXLR-specific device control UI panel with COMPLETE functionality
    /// </summary>
    public partial class GoXLRDeviceControlPanel : UserControl
    {
        private readonly IPluginContext _context;
        private readonly GoXLRDeviceControlPlugin _plugin;
        private List<string> _allAvailableProfiles = new List<string>();
        private List<string> _allAvailableChannels = new List<string>();
        
        // Combined hotkey+button capture state
        private bool _isCapturingCombined = false;
        private string? _captureChannel;
        private string? _captureAction;
        private int _captureTextBoxHash;

        public GoXLRDeviceControlPanel(IPluginContext context, GoXLRDeviceControlPlugin plugin)
        {
            InitializeComponent();
            
            _context = context;
            _plugin = plugin;
            
            // Hook into keyboard events for hotkey capture
            PreviewKeyDown += GoXLRDeviceControlPanel_PreviewKeyDown;
            
            Loaded += async (s, e) =>
            {
                await LoadAvailableChannelsAsync();
                await LoadAvailableProfilesAsync();
                PopulateUI();
            };
        }

        private async Task LoadAvailableChannelsAsync()
        {
            try
            {
                _allAvailableChannels = await _plugin.GetAvailableChannelsAsync();
                _context.LogInfo("GoXLR Device Control", $"Loaded {_allAvailableChannels.Count} channels from device");
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Device Control", $"Failed to load channels: {ex.Message}", ex);
                _allAvailableChannels = new List<string>();
            }
        }

        private void PopulateUI()
        {
            UpdateChannelDropdown();
            UpdateProfileDropdown();
            RenderVolumeHotkeys();
            RenderProfileHotkeys();
        }

        private void UpdateChannelDropdown()
        {
            ChannelComboBox.Items.Clear();
            
            var enabledChannels = _context.Settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            var availableChannels = _allAvailableChannels.Where(c => !enabledChannels.Contains(c));
            
            foreach (var channel in availableChannels)
            {
                var item = new ComboBoxItem
                {
                    Content = channel,
                    Tag = channel
                };
                ChannelComboBox.Items.Add(item);
            }
            
            if (ChannelComboBox.Items.Count > 0)
            {
                ChannelComboBox.SelectedIndex = 0;
            }
        }

        private void UpdateProfileDropdown()
        {
            ProfileComboBox.Items.Clear();
            
            var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                ?? new Dictionary<string, string>();
            
            var availableProfiles = _allAvailableProfiles
                .Where(p => !profileHotkeys.ContainsKey(p))
                .ToList();
            
            foreach (var profile in availableProfiles)
            {
                ProfileComboBox.Items.Add(profile);
            }
            
            if (ProfileComboBox.Items.Count > 0)
            {
                ProfileComboBox.SelectedIndex = 0;
            }
            else
            {
                if (_allAvailableProfiles.Count == 0)
                {
                    ProfileComboBox.Items.Add("(No profiles found - click Refresh)");
                }
                else
                {
                    ProfileComboBox.Items.Add("(All profiles added)");
                }
                ProfileComboBox.SelectedIndex = 0;
            }
        }

        private void RenderVolumeHotkeys()
        {
            VolumeHotkeysPanel.Children.Clear();
            
            var enabledChannels = _context.Settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
            if (enabledChannels.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No channels added. Use the dropdown above to add channels.",
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                VolumeHotkeysPanel.Children.Add(emptyText);
                return;
            }

            foreach (var channel in enabledChannels.OrderBy(c => c))
            {
                CreateVolumeHotkeyRow(channel);
            }
        }

        private void CreateVolumeHotkeyRow(string channel)
        {
            // Get existing hotkeys for this channel
            var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                ?? new Dictionary<string, object>();
            
            string upKeyboard = "";
            string upButton = "";
            string downKeyboard = "";
            string downButton = "";
            
            if (volumeHotkeys.TryGetValue(channel, out var channelHotkeysObj))
            {
                var channelHotkeys = channelHotkeysObj as Dictionary<string, object>;
                if (channelHotkeys != null)
                {
                    if (channelHotkeys.TryGetValue("VolumeUp", out var upObj))
                        upKeyboard = upObj?.ToString() ?? "";
                    if (channelHotkeys.TryGetValue("VolumeUpButton", out var upBtnObj))
                        upButton = upBtnObj?.ToString() ?? "";
                    if (channelHotkeys.TryGetValue("VolumeDown", out var downObj))
                        downKeyboard = downObj?.ToString() ?? "";
                    if (channelHotkeys.TryGetValue("VolumeDownButton", out var downBtnObj))
                        downButton = downBtnObj?.ToString() ?? "";
                }
            }

            // Create Grid with horizontal layout (OLD STYLE)
            var channelGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // 0: Label
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 1: "Up:"
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 2: Up textbox
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 3: Up Capture
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 4: Up Clear
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 5: "Down:"
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 6: Down textbox
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 7: Down Capture
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 8: Down Clear
            channelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });       // 9: Remove button
            
            // Channel Label
            var label = new TextBlock
            {
                Text = channel + ":",
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(label, 0);
            channelGrid.Children.Add(label);
            
            // Volume Up section
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
                Text = GetCombinedHotkeyDisplay(upKeyboard, upButton),
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeUp",
                MinWidth = 150
            };
            Grid.SetColumn(upBox, 2);
            channelGrid.Children.Add(upBox);
            
            var upCaptureButton = new Button
            {
                Content = "Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeUp|{upBox.GetHashCode()}"
            };
            upCaptureButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(upCaptureButton, 3);
            channelGrid.Children.Add(upCaptureButton);
            
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
            
            // Volume Down section
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
                Text = GetCombinedHotkeyDisplay(downKeyboard, downButton),
                IsReadOnly = true,
                Padding = new Thickness(5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = $"{channel}|VolumeDown",
                MinWidth = 150
            };
            Grid.SetColumn(downBox, 6);
            channelGrid.Children.Add(downBox);
            
            var downCaptureButton = new Button
            {
                Content = "Capture",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 5, 0),
                Tag = $"{channel}|VolumeDown|{downBox.GetHashCode()}"
            };
            downCaptureButton.Click += StartCombinedCapture_Click;
            Grid.SetColumn(downCaptureButton, 7);
            channelGrid.Children.Add(downCaptureButton);
            
            var downClearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 15, 0),
                Tag = $"{channel}|VolumeDown",
                ToolTip = "Clear hotkey"
            };
            downClearButton.Click += ClearHotkey_Click;
            Grid.SetColumn(downClearButton, 8);
            channelGrid.Children.Add(downClearButton);
            
            // Remove Channel Button
            var removeButton = new Button
            {
                Content = "? Remove",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(10, 0, 0, 0),
                Tag = channel,
                ToolTip = $"Remove {channel} channel",
                Background = System.Windows.Media.Brushes.LightCoral
            };
            removeButton.Click += RemoveChannel_Click;
            Grid.SetColumn(removeButton, 9);
            channelGrid.Children.Add(removeButton);
            
            VolumeHotkeysPanel.Children.Add(channelGrid);
        }

        private string GetCombinedHotkeyDisplay(string? keyboard, string? button)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(keyboard))
                parts.Add(keyboard);
            
            if (!string.IsNullOrWhiteSpace(button))
            {
                // Parse button string: DeviceName:{ProductGuid}:Button:{number}
                // Format as: DeviceName: Btn X
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

        private void RenderProfileHotkeys()
        {
            ProfileHotkeysPanel.Children.Clear();
            
            var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                ?? new Dictionary<string, string>();
            var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                ?? new Dictionary<string, string>();
            
            if (profileHotkeys.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No profiles added. Use the dropdown above to add profiles.",
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                ProfileHotkeysPanel.Children.Add(emptyText);
                return;
            }

            foreach (var profile in profileHotkeys.Keys.OrderBy(k => k))
            {
                CreateProfileRow(profile);
            }
        }

        private void CreateProfileRow(string profile)
        {
            var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                ?? new Dictionary<string, string>();
            var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                ?? new Dictionary<string, string>();
            
            string hotkey = profileHotkeys.ContainsKey(profile) ? profileHotkeys[profile] : "";
            string button = profileButtons.ContainsKey(profile) ? profileButtons[profile] : "";

            var profileGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // 0: Label
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // 1: Textbox
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 2: Capture
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 3: Clear
            profileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 4: Remove
            
            // Profile Label
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
            
            // Hotkey Textbox
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
            
            // Capture Button
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
            
            // Clear Button
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
            
            // Remove Profile Button
            var removeButton = new Button
            {
                Content = "? Remove",
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(5, 0, 0, 0),
                Tag = profile,
                ToolTip = $"Remove {profile} profile",
                Background = System.Windows.Media.Brushes.LightCoral
            };
            removeButton.Click += RemoveProfile_Click;
            Grid.SetColumn(removeButton, 4);
            profileGrid.Children.Add(removeButton);
            
            ProfileHotkeysPanel.Children.Add(profileGrid);
        }

        // ==================== Channel/Profile Management ====================
        
        private void AddChannel_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ChannelComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Please select a channel to add.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string channelName = selectedItem.Tag?.ToString() ?? "";
            
            var enabledChannels = _context.Settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
            if (enabledChannels.Contains(channelName))
            {
                MessageBox.Show($"Channel '{channelName}' is already added.", "Already Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            enabledChannels.Add(channelName);
            _context.Settings.SetValue("EnabledChannels", enabledChannels);
            
            // Initialize hotkeys dict if needed
            var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                ?? new Dictionary<string, object>();
            if (!volumeHotkeys.ContainsKey(channelName))
            {
                volumeHotkeys[channelName] = new Dictionary<string, object>
                {
                    { "VolumeUp", "" },
                    { "VolumeUpButton", "" },
                    { "VolumeDown", "" },
                    { "VolumeDownButton", "" }
                };
                _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
            }
            
            _context.SaveSettings();
            _context.LogInfo("GoXLR Device Control", $"Added channel: {channelName}");
            
            PopulateUI();
        }

        private async void AddProfile_Click(object sender, RoutedEventArgs e)
        {
            var selectedProfile = ProfileComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedProfile) || selectedProfile.StartsWith("("))
            {
                MessageBox.Show("Please select a profile to add.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                ?? new Dictionary<string, string>();
            var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                ?? new Dictionary<string, string>();
            
            if (profileHotkeys.ContainsKey(selectedProfile))
            {
                MessageBox.Show($"Profile '{selectedProfile}' is already added.", "Already Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            profileHotkeys[selectedProfile] = "";
            profileButtons[selectedProfile] = "";
            _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
            _context.Settings.SetValue("ProfileButtons", profileButtons);
            _context.SaveSettings();
            
            _context.LogInfo("GoXLR Device Control", $"Added profile: {selectedProfile}");
            
            PopulateUI();
        }

        private async void RefreshProfiles_Click(object sender, RoutedEventArgs e)
        {
            await LoadAvailableProfilesAsync();
            PopulateUI();
        }

        private async Task LoadAvailableProfilesAsync()
        {
            try
            {
                _allAvailableProfiles = await _plugin.GetAvailableProfilesAsync();
                _context.LogInfo("GoXLR Device Control", $"Loaded {_allAvailableProfiles.Count} profiles from device");
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Device Control", $"Failed to load profiles: {ex.Message}", ex);
                _allAvailableProfiles = new List<string>();
            }
        }

        private void RemoveChannel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var channelName = button?.Tag?.ToString();
            
            if (string.IsNullOrEmpty(channelName))
                return;

            var result = MessageBox.Show(
                $"Remove channel '{channelName}' and its hotkeys?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var enabledChannels = _context.Settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
                enabledChannels.Remove(channelName);
                _context.Settings.SetValue("EnabledChannels", enabledChannels);
                
                var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                volumeHotkeys.Remove(channelName);
                _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
                
                _context.SaveSettings();
                _context.LogInfo("GoXLR Device Control", $"Removed channel: {channelName}");
                
                PopulateUI();
            }
        }

        private void RemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profileName = button?.Tag?.ToString();
            
            if (string.IsNullOrEmpty(profileName))
                return;

            var result = MessageBox.Show(
                $"Remove profile '{profileName}' hotkey?",
                "Confirm Remove",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                    ?? new Dictionary<string, string>();
                
                profileHotkeys.Remove(profileName);
                profileButtons.Remove(profileName);
                
                _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
                _context.Settings.SetValue("ProfileButtons", profileButtons);
                _context.SaveSettings();
                
                _context.LogInfo("GoXLR Device Control", $"Removed profile: {profileName}");
                
                PopulateUI();
            }
        }

        // ==================== Combined Keyboard + Button Capture ====================
        
        private void StartCombinedCapture_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            var parts = tag.Split('|');
            if (parts.Length < 3) return;

            _captureChannel = parts[0];
            _captureAction = parts[1];
            _captureTextBoxHash = int.Parse(parts[2]);
            _isCapturingCombined = true;

            button.Content = "Press key or button...";
            button.IsEnabled = false;
            
            // Subscribe to DirectInput button events using reflection
            var directInputService = GetDirectInputService();
            if (directInputService != null)
            {
                var eventInfo = directInputService.GetType().GetEvent("ButtonPressed");
                if (eventInfo != null)
                {
                    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, this, nameof(OnCombinedButtonCaptured));
                    eventInfo.AddEventHandler(directInputService, handler);
                }
            }


            
            Focus(); // Focus this control to receive key events
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            var parts = tag.Split('|');
            if (parts.Length < 2) return;

            string channel = parts[0];
            string action = parts[1];

            if (channel == "Profile")
            {
                // Clear profile hotkey
                var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                    ?? new Dictionary<string, string>();
                
                if (profileHotkeys.ContainsKey(action))
                    profileHotkeys[action] = "";
                if (profileButtons.ContainsKey(action))
                    profileButtons[action] = "";
                
                _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
                _context.Settings.SetValue("ProfileButtons", profileButtons);
            }
            else
            {
                // Clear channel hotkey
                var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                
                if (volumeHotkeys.TryGetValue(channel, out var channelHotkeysObj))
                {
                    var channelHotkeys = channelHotkeysObj as Dictionary<string, object>;
                    if (channelHotkeys != null)
                    {
                        if (action == "VolumeUp")
                        {
                            channelHotkeys["VolumeUp"] = "";
                            channelHotkeys["VolumeUpButton"] = "";
                        }
                        else if (action == "VolumeDown")
                        {
                            channelHotkeys["VolumeDown"] = "";
                            channelHotkeys["VolumeDownButton"] = "";
                        }
                        volumeHotkeys[channel] = channelHotkeys;
                        _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
                    }
                }
            }

            _context.SaveSettings();
            PopulateUI();
        }

        private void GoXLRDeviceControlPanel_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturingCombined)
                return;

            e.Handled = true;

            // Ignore modifier keys by themselves
            if (IsModifierKey(e.Key))
                return;

            // Build hotkey string
            var hotkey = BuildHotkeyString(System.Windows.Input.Keyboard.Modifiers, e.Key);

            // Save as KEYBOARD hotkey
            SaveCombinedHotkey(_captureChannel, _captureAction, keyboard: hotkey, button: null);

            StopCombinedCapture();
            PopulateUI();
        }

        private void OnCombinedButtonCaptured(object? sender, string buttonString)
        {
            if (!_isCapturingCombined)
                return;

            // Dispatcher because this comes from DirectInput thread
            Dispatcher.Invoke(() =>
            {
                // Save as BUTTON hotkey
                SaveCombinedHotkey(_captureChannel, _captureAction, keyboard: null, button: buttonString);
                
                StopCombinedCapture();
                PopulateUI();
            });
        }

        private void SaveCombinedHotkey(string? channel, string? action, string? keyboard, string? button)
        {
            if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(action))
                return;

            if (channel == "Profile")
            {
                // Save profile hotkey/button
                var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                var profileButtons = _context.Settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                    ?? new Dictionary<string, string>();
                
                if (keyboard != null)
                    profileHotkeys[action] = keyboard;
                if (button != null)
                    profileButtons[action] = button;
                
                _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
                _context.Settings.SetValue("ProfileButtons", profileButtons);
                
                _context.LogInfo("GoXLR Device Control", $"Saved profile hotkey: {action} = {keyboard ?? button}");
            }
            else
            {
                // Save channel volume hotkey/button
                var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                
                if (!volumeHotkeys.TryGetValue(channel, out var channelHotkeysObj))
                {
                    channelHotkeysObj = new Dictionary<string, object>
                    {
                        { "VolumeUp", "" },
                        { "VolumeUpButton", "" },
                        { "VolumeDown", "" },
                        { "VolumeDownButton", "" }
                    };
                    volumeHotkeys[channel] = channelHotkeysObj;
                }

                var channelHotkeys = channelHotkeysObj as Dictionary<string, object>;
                if (channelHotkeys != null)
                {
                    if (keyboard != null)
                        channelHotkeys[action] = keyboard;
                    if (button != null)
                        channelHotkeys[action + "Button"] = button;
                    
                    volumeHotkeys[channel] = channelHotkeys;
                    _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
                    
                    _context.LogInfo("GoXLR Device Control", $"Saved channel hotkey: {channel}/{action} = {keyboard ?? button}");
                }
            }

            _context.SaveSettings();
        }

        private void StopCombinedCapture()
        {
            _isCapturingCombined = false;
            _captureChannel = null;
            _captureAction = null;
            _captureTextBoxHash = 0;
            
            // Unsubscribe from DirectInput using reflection
            var directInputService = GetDirectInputService();
            if (directInputService != null)
            {
                var eventInfo = directInputService.GetType().GetEvent("ButtonPressed");
                if (eventInfo != null)
                {
                    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, this, nameof(OnCombinedButtonCaptured));
                    eventInfo.RemoveEventHandler(directInputService, handler);
                }
            }


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

        private object? GetDirectInputService()
        {
            // Use reflection to get DirectInputService from main app
            try
            {
                var appType = Type.GetType("SimControlCentre.App, SimControlCentre");
                if (appType != null)
                {
                    var method = appType.GetMethod("GetDirectInputService", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        return method.Invoke(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _context.LogError("GoXLR Device Control", $"Failed to get DirectInputService: {ex.Message}", ex);
            }
            return null;
        }
    }
}
