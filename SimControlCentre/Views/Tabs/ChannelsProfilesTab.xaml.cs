using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class ChannelsProfilesTab : UserControl
    {
        private readonly GoXLRService _goXLRService;
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        private List<string> _availableProfiles = new();
        
        public event EventHandler? HotkeysChanged;

        public ChannelsProfilesTab(GoXLRService goXLRService, ConfigurationService configService, AppSettings settings)
        {
            InitializeComponent();
            
            _goXLRService = goXLRService;
            _configService = configService;
            _settings = settings;
            
            PopulateChannelsAndProfiles();
        }

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
            
            // Fetch profiles from GoXLR
            _ = FetchGoXLRProfilesAsync();
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
                Console.WriteLine($"[ChannelsProfilesTab] Failed to fetch profiles: {ex.Message}");
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
            
            // Notify HotkeysTab to refresh
            HotkeysChanged?.Invoke(this, EventArgs.Empty);
            
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
                
                // Auto-save
                _configService.Save(_settings);
                
                RefreshProfilesList();
                
                // Notify HotkeysTab to refresh
                HotkeysChanged?.Invoke(this, EventArgs.Empty);
                
                // Refresh available profiles
                var unconfiguredProfiles = _availableProfiles
                    .Where(p => !_settings.ProfileHotkeys.ContainsKey(p))
                    .ToList();
                AvailableProfilesComboBox.ItemsSource = unconfiguredProfiles;
                if (unconfiguredProfiles.Count > 0)
                    AvailableProfilesComboBox.SelectedIndex = 0;
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
            
            // Notify HotkeysTab to refresh
            HotkeysChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
