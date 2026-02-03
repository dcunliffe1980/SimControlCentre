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
    /// GoXLR-specific device control UI panel
    /// Manages channels and profiles for GoXLR device
    /// </summary>
    public partial class GoXLRDeviceControlPanel : UserControl
    {
        private readonly IPluginContext _context;
        private readonly GoXLRDeviceControlPlugin _plugin;
        private List<string> _allAvailableProfiles = new List<string>();
        
        // Hotkey capture state
        private bool _isCapturingHotkey = false;
        private string? _captureChannel;
        private string? _captureAction;
        private int _captureTextBoxHash;

        public GoXLRDeviceControlPanel(IPluginContext context, GoXLRDeviceControlPlugin plugin)
        {
            InitializeComponent();
            
            _context = context;
            _plugin = plugin;
            
            // Add key handler for hotkey capture
            PreviewKeyDown += GoXLRDeviceControlPanel_PreviewKeyDown;
            
            Loaded += async (s, e) =>
            {
                await LoadAvailableProfilesAsync();
                PopulateUI();
            };
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
            
            var allChannels = new[] { "Game", "Music", "Chat", "System" };
            
            // Access settings directly - EnabledChannels is a property on AppSettings
            var enabledChannels = _context.Settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
            var availableChannels = allChannels.Where(c => !enabledChannels.Contains(c));
            
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
            
            var settings = _context.Settings;
            var profileHotkeys = settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
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
            
            var settings = _context.Settings;
            var enabledChannels = settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
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
                CreateChannelRow(channel);
            }
        }

        private void CreateChannelRow(string channel)
        {
            // Get hotkeys for this channel
            var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                ?? new Dictionary<string, object>();
            
            // Try to get the channel hotkeys
            object? channelHotkeysObj = null;
            string upHotkey = "";
            string downHotkey = "";
            
            if (volumeHotkeys.TryGetValue(channel, out channelHotkeysObj))
            {
                // Channel hotkeys exist - extract up/down
                var channelHotkeysDict = channelHotkeysObj as Dictionary<string, object>;
                if (channelHotkeysDict != null)
                {
                    if (channelHotkeysDict.TryGetValue("VolumeUp", out var upObj))
                        upHotkey = upObj?.ToString() ?? "";
                    if (channelHotkeysDict.TryGetValue("VolumeDown", out var downObj))
                        downHotkey = downObj?.ToString() ?? "";
                }
            }

            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Channel header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var label = new TextBlock
            {
                Text = channel,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            headerPanel.Children.Add(label);

            var removeButton = new Button
            {
                Content = "? Remove Channel",
                Padding = new Thickness(8, 4, 8, 4),
                Tag = channel,
                Background = System.Windows.Media.Brushes.LightCoral
            };
            removeButton.Click += RemoveChannel_Click;
            headerPanel.Children.Add(removeButton);

            panel.Children.Add(headerPanel);

            // Volume Up row
            var upPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 0, 5)
            };
            
            upPanel.Children.Add(new TextBlock 
            { 
                Text = "Volume Up:", 
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center 
            });
            
            var upTextBox = new TextBox
            {
                Text = upHotkey,
                Width = 200,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"{channel}|VolumeUp"
            };
            upPanel.Children.Add(upTextBox);
            
            var upCaptureBtn = new Button
            {
                Content = "Capture",
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"{channel}|VolumeUp|{upTextBox.GetHashCode()}"
            };
            upCaptureBtn.Click += CaptureHotkey_Click;
            upPanel.Children.Add(upCaptureBtn);
            
            var upClearBtn = new Button
            {
                Content = "Clear",
                Padding = new Thickness(10, 4, 10, 4),
                Tag = $"{channel}|VolumeUp"
            };
            upClearBtn.Click += ClearHotkey_Click;
            upPanel.Children.Add(upClearBtn);

            panel.Children.Add(upPanel);

            // Volume Down row
            var downPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 0, 5)
            };
            
            downPanel.Children.Add(new TextBlock 
            { 
                Text = "Volume Down:", 
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center 
            });
            
            var downTextBox = new TextBox
            {
                Text = downHotkey,
                Width = 200,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"{channel}|VolumeDown"
            };
            downPanel.Children.Add(downTextBox);
            
            var downCaptureBtn = new Button
            {
                Content = "Capture",
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"{channel}|VolumeDown|{downTextBox.GetHashCode()}"
            };
            downCaptureBtn.Click += CaptureHotkey_Click;
            downPanel.Children.Add(downCaptureBtn);
            
            var downClearBtn = new Button
            {
                Content = "Clear",
                Padding = new Thickness(10, 4, 10, 4),
                Tag = $"{channel}|VolumeDown"
            };
            downClearBtn.Click += ClearHotkey_Click;
            downPanel.Children.Add(downClearBtn);

            panel.Children.Add(downPanel);

            VolumeHotkeysPanel.Children.Add(panel);
        }


        private void RenderProfileHotkeys()
        {
            ProfileHotkeysPanel.Children.Clear();
            
            var settings = _context.Settings;
            var profileHotkeys = settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
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
            
            string hotkey = profileHotkeys.ContainsKey(profile) ? profileHotkeys[profile] : "";

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var label = new TextBlock
            {
                Text = profile,
                FontWeight = FontWeights.Bold,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(label);

            var hotkeyTextBox = new TextBox
            {
                Text = hotkey,
                Width = 200,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"Profile|{profile}"
            };
            panel.Children.Add(hotkeyTextBox);

            var captureBtn = new Button
            {
                Content = "Capture",
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"Profile|{profile}|{hotkeyTextBox.GetHashCode()}"
            };
            captureBtn.Click += CaptureHotkey_Click;
            panel.Children.Add(captureBtn);

            var clearBtn = new Button
            {
                Content = "Clear",
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = $"Profile|{profile}"
            };
            clearBtn.Click += ClearHotkey_Click;
            panel.Children.Add(clearBtn);

            var removeButton = new Button
            {
                Content = "? Remove",
                Padding = new Thickness(8, 4, 8, 4),
                Tag = profile,
                Background = System.Windows.Media.Brushes.LightCoral
            };
            removeButton.Click += RemoveProfile_Click;
            panel.Children.Add(removeButton);

            ProfileHotkeysPanel.Children.Add(panel);
        }


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
            
            var settings = _context.Settings;
            var enabledChannels = settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
            if (enabledChannels.Contains(channelName))
            {
                MessageBox.Show($"Channel '{channelName}' is already added.", "Already Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            enabledChannels.Add(channelName);
            settings.SetValue("EnabledChannels", enabledChannels);
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

            var settings = _context.Settings;
            var profileHotkeys = settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                ?? new Dictionary<string, string>();
            var profileButtons = settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                ?? new Dictionary<string, string>();
            
            if (profileHotkeys.ContainsKey(selectedProfile))
            {
                MessageBox.Show($"Profile '{selectedProfile}' is already added.", "Already Added", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            profileHotkeys[selectedProfile] = "";
            profileButtons[selectedProfile] = "";
            settings.SetValue("ProfileHotkeys", profileHotkeys);
            settings.SetValue("ProfileButtons", profileButtons);
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
                var settings = _context.Settings;
                var enabledChannels = settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
                enabledChannels.Remove(channelName);
                settings.SetValue("EnabledChannels", enabledChannels);
                
                // Also remove hotkeys
                var volumeHotkeys = settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                volumeHotkeys.Remove(channelName);
                settings.SetValue("VolumeHotkeys", volumeHotkeys);
                
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
                var settings = _context.Settings;
                var profileHotkeys = settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                var profileButtons = settings.GetValue<Dictionary<string, string>>("ProfileButtons") 
                    ?? new Dictionary<string, string>();
                
                profileHotkeys.Remove(profileName);
                profileButtons.Remove(profileName);
                
                settings.SetValue("ProfileHotkeys", profileHotkeys);
                settings.SetValue("ProfileButtons", profileButtons);
                _context.SaveSettings();
                
                _context.LogInfo("GoXLR Device Control", $"Removed profile: {profileName}");
                
                PopulateUI();
            }
        }
        
        // ==================== Hotkey Capture ====================
        
        private void CaptureHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button?.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            var parts = tag.Split('|');
            if (parts.Length < 3) return;

            _captureChannel = parts[0];
            _captureAction = parts[1];
            _captureTextBoxHash = int.Parse(parts[2]);
            _isCapturingHotkey = true;

            button.Content = "Press keys...";
            button.IsEnabled = false;
            
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

            // Clear the hotkey
            if (channel == "Profile")
            {
                var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                if (profileHotkeys.ContainsKey(action))
                {
                    profileHotkeys[action] = "";
                    _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
                    _context.SaveSettings();
                }
            }
            else
            {
                // Channel hotkey
                var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                
                if (volumeHotkeys.TryGetValue(channel, out var channelHotkeysObj))
                {
                    var channelHotkeys = channelHotkeysObj as Dictionary<string, object>;
                    if (channelHotkeys != null)
                    {
                        channelHotkeys[action] = "";
                        volumeHotkeys[channel] = channelHotkeys;
                        _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
                        _context.SaveSettings();
                    }
                }
            }

            PopulateUI();
        }

        private void GoXLRDeviceControlPanel_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isCapturingHotkey)
                return;

            e.Handled = true;

            // Ignore modifier keys by themselves
            if (IsModifierKey(e.Key))
                return;

            // Build hotkey string
            var hotkey = BuildHotkeyString(System.Windows.Input.Keyboard.Modifiers, e.Key);

            // Find the textbox and update it
            var textBox = FindTextBoxByHash(_captureTextBoxHash);
            if (textBox != null)
            {
                textBox.Text = hotkey;
            }

            // Save the hotkey
            SaveHotkey(_captureChannel, _captureAction, hotkey);

            // Stop capturing
            StopCapture();
            PopulateUI();
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

        private TextBox? FindTextBoxByHash(int hash)
        {
            return FindTextBoxInPanel(VolumeHotkeysPanel, hash) 
                ?? FindTextBoxInPanel(ProfileHotkeysPanel, hash);
        }

        private TextBox? FindTextBoxInPanel(StackPanel panel, int hash)
        {
            foreach (var child in panel.Children)
            {
                if (child is StackPanel childPanel)
                {
                    foreach (var subChild in childPanel.Children)
                    {
                        if (subChild is TextBox textBox && textBox.GetHashCode() == hash)
                            return textBox;
                        if (subChild is StackPanel subPanel)
                        {
                            var found = FindTextBoxInPanel(subPanel, hash);
                            if (found != null) return found;
                        }
                    }
                }
            }
            return null;
        }

        private void SaveHotkey(string? channel, string? action, string hotkey)
        {
            if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(action))
                return;

            if (channel == "Profile")
            {
                var profileHotkeys = _context.Settings.GetValue<Dictionary<string, string>>("ProfileHotkeys") 
                    ?? new Dictionary<string, string>();
                profileHotkeys[action] = hotkey;
                _context.Settings.SetValue("ProfileHotkeys", profileHotkeys);
                _context.SaveSettings();
                _context.LogInfo("GoXLR Device Control", $"Saved profile hotkey: {action} = {hotkey}");
            }
            else
            {
                var volumeHotkeys = _context.Settings.GetValue<Dictionary<string, object>>("VolumeHotkeys") 
                    ?? new Dictionary<string, object>();
                
                if (!volumeHotkeys.TryGetValue(channel, out var channelHotkeysObj))
                {
                    channelHotkeysObj = new Dictionary<string, object>();
                    volumeHotkeys[channel] = channelHotkeysObj;
                }

                var channelHotkeys = channelHotkeysObj as Dictionary<string, object>;
                if (channelHotkeys != null)
                {
                    channelHotkeys[action] = hotkey;
                    volumeHotkeys[channel] = channelHotkeys;
                    _context.Settings.SetValue("VolumeHotkeys", volumeHotkeys);
                    _context.SaveSettings();
                    _context.LogInfo("GoXLR Device Control", $"Saved channel hotkey: {channel}/{action} = {hotkey}");
                }
            }
        }

        private void StopCapture()
        {
            _isCapturingHotkey = false;
            _captureChannel = null;
            _captureAction = null;
            _captureTextBoxHash = 0;
        }
    }
}

