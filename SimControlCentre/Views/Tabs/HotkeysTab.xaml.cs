using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    /// <summary>
    /// Generic Device Control tab - displays plugin-provided UI
    /// Main app provides ZERO device-specific functionality
    /// </summary>
    public partial class HotkeysTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;

        public HotkeysTab(ConfigurationService configService, AppSettings settings)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            
            CheckPluginAvailability();
            LoadPluginUI();
        }

        public void CheckPluginAvailability()
        {
            var deviceControlService = App.GetDeviceControlService();
            bool hasEnabledPlugins = deviceControlService?.Plugins.Any(p => p.IsEnabled) ?? false;
            
            Logger.Info("Device Control Tab", $"Enabled plugins: {hasEnabledPlugins}");
            
            if (!hasEnabledPlugins)
            {
                NoPluginsWarning.Visibility = Visibility.Visible;
                PluginConfigContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoPluginsWarning.Visibility = Visibility.Collapsed;
                PluginConfigContent.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Load and display plugin-provided UI
        /// Plugin provides EVERYTHING - channels, profiles, hotkeys, etc.
        /// </summary>
        private void LoadPluginUI()
        {
            PluginConfigContent.Content = null;
            
            var deviceControlService = App.GetDeviceControlService();
            if (deviceControlService == null)
                return;
            
            // Get first enabled plugin
            var plugin = deviceControlService.Plugins.FirstOrDefault(p => p.IsEnabled);
            if (plugin == null)
            {
                Logger.Info("Device Control Tab", "No enabled plugins found");
                return;
            }
            
            try
            {
                // Get plugin's complete UI
                var pluginUI = plugin.GetConfigurationControl();
                
                if (pluginUI != null)
                {
                    PluginConfigContent.Content = pluginUI;
                    Logger.Info("Device Control Tab", $"Loaded complete UI from plugin: {plugin.PluginId}");
                }
                else
                {
                    PluginConfigContent.Content = new TextBlock
                    {
                        Text = $"Plugin '{plugin.PluginId}' does not provide configuration UI.",
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Gray
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Device Control Tab", $"Failed to load plugin UI: {ex.Message}", ex);
                PluginConfigContent.Content = new TextBlock
                {
                    Text = $"Error loading plugin UI: {ex.Message}",
                    Foreground = System.Windows.Media.Brushes.Red
                };
            }
        }

        public void RefreshHotkeys()
        {
            CheckPluginAvailability();
            LoadPluginUI();
        }
    }
}

