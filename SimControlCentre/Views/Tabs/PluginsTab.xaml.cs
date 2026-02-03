using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class PluginsTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        private readonly LightingService _lightingService;

        public PluginsTab(ConfigurationService configService, AppSettings settings, LightingService lightingService)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _lightingService = lightingService;
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load GoXLR plugin enabled state
            if (_settings.Lighting?.EnabledPlugins != null && 
                _settings.Lighting.EnabledPlugins.ContainsKey("goxlr"))
            {
                EnableGoXlrPluginCheckBox.IsChecked = _settings.Lighting.EnabledPlugins["goxlr"];
            }
            else
            {
                EnableGoXlrPluginCheckBox.IsChecked = true; // Default to enabled
            }
            
            UpdateGoXlrComponentsVisibility();
        }

        private void PluginEnabled_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.Tag is string pluginId)
            {
                bool isEnabled = checkbox.IsChecked == true;
                
                // Update settings
                if (_settings.Lighting == null)
                {
                    _settings.Lighting = new LightingSettings();
                }
                
                if (_settings.Lighting.EnabledPlugins == null)
                {
                    _settings.Lighting.EnabledPlugins = new Dictionary<string, bool>();
                }
                
                _settings.Lighting.EnabledPlugins[pluginId] = isEnabled;
                
                // Save settings using correct method name
                _configService.Save(_settings);
                
                // Apply to lighting service
                var plugin = _lightingService.Plugins.FirstOrDefault(p => p.PluginId == pluginId);
                if (plugin != null)
                {
                    plugin.IsEnabled = isEnabled;
                    
                    // Reinitialize devices to apply changes
                    _ = _lightingService.InitializeAsync();
                }
                
                // Update UI visibility
                if (pluginId == "goxlr")
                {
                    UpdateGoXlrComponentsVisibility();
                }
                
                Logger.Info("Plugins", $"Plugin '{pluginId}' {(isEnabled ? "enabled" : "disabled")}");
            }
        }

        private void UpdateGoXlrComponentsVisibility()
        {
            bool isEnabled = EnableGoXlrPluginCheckBox.IsChecked == true;
            
            if (isEnabled)
            {
                GoXlrComponentsPanel.Visibility = Visibility.Visible;
                GoXlrDisabledMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                GoXlrComponentsPanel.Visibility = Visibility.Collapsed;
                GoXlrDisabledMessage.Visibility = Visibility.Visible;
            }
        }
    }
}

