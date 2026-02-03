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
        private readonly MainWindow? _mainWindow;

        public PluginsTab(ConfigurationService configService, AppSettings settings, LightingService lightingService, MainWindow? mainWindow = null)
        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            _lightingService = lightingService;
            _mainWindow = mainWindow;
            
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
            
            // Load component states
            EnableLightingComponentCheckBox.IsChecked = _settings.Lighting?.EnabledPlugins?.GetValueOrDefault("goxlr-lighting", true) ?? true;
            EnableDeviceControlComponentCheckBox.IsChecked = _settings.Lighting?.EnabledPlugins?.GetValueOrDefault("goxlr-device-control", true) ?? true;
            
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
                    
                    // If plugin is disabled, also disable flag lighting
                    if (!isEnabled)
                    {
                        _settings.Lighting.EnableFlagLighting = false;
                        _configService.Save(_settings);
                    }
                }
                
                // Update UI visibility
                if (pluginId == "goxlr")
                {
                    UpdateGoXlrComponentsVisibility();
                }
                
                // Notify MainWindow to refresh LightingTab after a short delay to ensure plugin state is updated
                if (_mainWindow != null)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                        new Action(() => _mainWindow.RefreshLightingTab()),
                        System.Windows.Threading.DispatcherPriority.Background);
                }
                
                Logger.Info("Plugins", $"Plugin '{pluginId}' {(isEnabled ? "enabled" : "disabled")}");
            }
        }

        private void ComponentEnabled_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.Tag is string componentId)
            {
                bool isEnabled = checkbox.IsChecked == true;
                
                // Check if at least one component is enabled
                bool lightingEnabled = EnableLightingComponentCheckBox.IsChecked == true;
                bool deviceControlEnabled = EnableDeviceControlComponentCheckBox.IsChecked == true;
                
                if (!lightingEnabled && !deviceControlEnabled)
                {
                    // Prevent disabling both - re-enable this one
                    checkbox.IsChecked = true;
                    NoComponentsWarning.Visibility = Visibility.Visible;
                    return;
                }
                else
                {
                    NoComponentsWarning.Visibility = Visibility.Collapsed;
                }
                
                // Update settings
                if (_settings.Lighting == null)
                {
                    _settings.Lighting = new LightingSettings();
                }
                
                if (_settings.Lighting.EnabledPlugins == null)
                {
                    _settings.Lighting.EnabledPlugins = new Dictionary<string, bool>();
                }
                
                string pluginKey = componentId == "lighting" ? "goxlr-lighting" : "goxlr-device-control";
                _settings.Lighting.EnabledPlugins[pluginKey] = isEnabled;
                
                // Save settings
                _configService.Save(_settings);
                
                // Apply changes based on component
                if (componentId == "lighting")
                {
                    // Toggle lighting plugin
                    var lightingPlugin = _lightingService.Plugins.FirstOrDefault(p => p.PluginId == "goxlr");
                    if (lightingPlugin != null)
                    {
                        lightingPlugin.IsEnabled = isEnabled;
                        _ = _lightingService.InitializeAsync();
                    }
                    
                    // Notify lighting tab
                    _mainWindow?.RefreshLightingTab();
                }
                else if (componentId == "device_control")
                {
                    // Toggle device control plugin
                    var deviceControlService = App.GetDeviceControlService();
                    var deviceControlPlugin = deviceControlService?.GetPlugin("goxlr-control");
                    if (deviceControlPlugin != null)
                    {
                        deviceControlPlugin.IsEnabled = isEnabled;
                    }
                    
                    // Refresh device control tab to update plugin availability
                    _mainWindow?.UpdateDeviceControlTabVisibility();
                    _mainWindow?.RefreshHotkeysTab();
                }
                
                Logger.Info("Plugins", $"Component '{componentId}' {(isEnabled ? "enabled" : "disabled")}");
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

