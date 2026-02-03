using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SimControlCentre.Models;
using SimControlCentre.Services;
using SimControlCentre.Contracts;

namespace SimControlCentre.Views.Tabs
{
    /// <summary>
    /// Generic Device Control tab that displays plugin-provided configuration UI
    /// </summary>
    public partial class HotkeysTab : UserControl
    {
        private readonly ConfigurationService _configService;
        private readonly AppSettings _settings;
        
        // Hotkey capture state for generic hotkeys (if needed in future)
        private bool _isCapturingHotkey = false;
        private string? _captureType;
        private string? _captureAction;
        private TextBox? _captureTextBox;

        public HotkeysTab(ConfigurationService configService, AppSettings settings)

        {
            InitializeComponent();
            
            _configService = configService;
            _settings = settings;
            
            // Add key event handler for hotkey capture
            PreviewKeyDown += HotkeysTab_PreviewKeyDown;
            
            CheckPluginAvailability();
            LoadPluginUI();
            PopulateGenericHotkeys();
        }

        public void CheckPluginAvailability()
        {
            // Check if device control component is enabled
            bool deviceControlComponentEnabled = _settings.Lighting?.EnabledPlugins?.GetValueOrDefault("goxlr-device-control", true) ?? true;
            
            // Check if any device control plugins are available
            var deviceControlService = App.GetDeviceControlService();
            bool hasEnabledPlugins = deviceControlService?.Plugins.Any(p => p.IsEnabled) ?? false;
            
            Logger.Info("Device Control Tab", $"Component enabled: {deviceControlComponentEnabled}");
            Logger.Info("Device Control Tab", $"Plugin count: {deviceControlService?.Plugins.Count ?? 0}");
            
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

        /// <summary>
        /// Load plugin-provided UI control and display it
        /// </summary>
        private void LoadPluginUI()
        {
            PluginConfigContent.Content = null;
            
            var deviceControlService = App.GetDeviceControlService();
            if (deviceControlService == null)
                return;
            
            // Get the first enabled device control plugin
            var plugin = deviceControlService.Plugins.FirstOrDefault(p => p.IsEnabled);
            if (plugin == null)
            {
                Logger.Info("Device Control Tab", "No enabled plugins found");
                return;
            }
            
            try
            {
                // Get plugin-provided configuration UI
                var pluginUI = plugin.GetConfigurationControl();
                
                if (pluginUI != null)
                {
                    PluginConfigContent.Content = pluginUI;
                    Logger.Info("Device Control Tab", $"Loaded UI from plugin: {plugin.PluginId}");
                }
                else
                {
                    // Plugin doesn't provide custom UI - show message
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

        /// <summary>
        /// Populate generic hotkeys (non-plugin-specific)
        /// </summary>
        private void PopulateGenericHotkeys()
        {
            GenericHotkeysPanel.Children.Clear();
            
            // TODO: Add any generic/common hotkeys here that aren't device-specific
            // For now, this is empty as all hotkeys are device-specific
            
            var infoText = new TextBlock
            {
                Text = "Device-specific hotkeys are configured in the plugin section below.",
                FontStyle = FontStyles.Italic,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 5, 0, 5)
            };
            GenericHotkeysPanel.Children.Add(infoText);
        }

        public void RefreshHotkeys()
        {
            CheckPluginAvailability();
            LoadPluginUI();
            PopulateGenericHotkeys();
        }

        // ==================== Hotkey Capture (Generic) ====================
        // These methods handle generic hotkey/button capture for any plugin
        
        private void HotkeysTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturingHotkey)
                return;

            e.Handled = true;

            // Ignore modifier keys by themselves
            if (IsModifierKey(e.Key))
                return;

            // Build hotkey string
            var hotkey = BuildHotkeyString(Keyboard.Modifiers, e.Key);

            // Update the textbox
            if (_captureTextBox != null)
            {
                _captureTextBox.Text = hotkey;
                
                // Save hotkey based on type
                SaveCapturedHotkey(_captureType, _captureAction, hotkey);
            }

            // Stop capturing
            StopCapture();
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

        private void SaveCapturedHotkey(string? captureType, string? action, string hotkey)
        {
            // TODO: Implement generic hotkey saving
            // This would need to be plugin-agnostic or delegated to the plugin
            Logger.Info("Device Control Tab", $"Captured hotkey: {captureType}/{action} = {hotkey}");
        }

        private void StopCapture()
        {
            _isCapturingHotkey = false;
            _captureType = null;
            _captureAction = null;
            _captureTextBox = null;
        }

    }
}
