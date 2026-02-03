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

        public GoXLRDeviceControlPanel(IPluginContext context, GoXLRDeviceControlPlugin plugin)
        {
            InitializeComponent();
            
            _context = context;
            _plugin = plugin;

            
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
            var settings = _context.Settings;
            var enabledChannels = settings.GetValue<List<string>>("EnabledChannels") ?? new List<string>();
            
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
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var label = new TextBlock
            {
                Text = channel,
                FontWeight = FontWeights.Bold,
                Width = 100,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(label);

            var infoText = new TextBlock
            {
                Text = "(Hotkeys configured in main Hotkeys tab)",
                FontStyle = FontStyles.Italic,
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            panel.Children.Add(infoText);

            var removeButton = new Button
            {
                Content = "? Remove",
                Padding = new Thickness(8, 4, 8, 4),
                Tag = channel,
                Background = System.Windows.Media.Brushes.LightCoral
            };
            removeButton.Click += RemoveChannel_Click;
            panel.Children.Add(removeButton);

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

            var infoText = new TextBlock
            {
                Text = "(Hotkey configured in main Hotkeys tab)",
                FontStyle = FontStyles.Italic,
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };
            panel.Children.Add(infoText);

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
    }
}
