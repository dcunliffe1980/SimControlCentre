using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SimControlCentre.Models;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class LightingTab : UserControl
    {
        private readonly LightingService _lightingService;
        private readonly TelemetryService _telemetryService;
        private GoXLRLightingPlugin? _goxlrPlugin;

        public LightingTab(LightingService lightingService, TelemetryService telemetryService)
        {
            InitializeComponent();
            
            _lightingService = lightingService;
            _telemetryService = telemetryService;
            
            // Subscribe to flag changes to update current flag display
            _telemetryService.FlagChanged += OnFlagChanged;
            
            LoadSettings();
            UpdateDevicesList();
            
            // Load button selection after a short delay to allow device detection
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // Wait for device type detection
                Dispatcher.Invoke(() => LoadButtonSelection());
            });
        }

        private void LoadButtonSelection()
        {
            ButtonSelectionPanel.Children.Clear();

            // Find GoXLR plugin
            _goxlrPlugin = _lightingService.Plugins.OfType<GoXLRLightingPlugin>().FirstOrDefault();
            
            if (_goxlrPlugin != null)
            {
                var configOptions = _goxlrPlugin.GetConfigOptions();
                var buttonOption = configOptions.FirstOrDefault(o => o.Key == "selected_buttons");
                
                if (buttonOption != null && buttonOption.AvailableOptions != null)
                {
                    var currentSelection = (List<string>?)buttonOption.DefaultValue ?? new List<string>();
                    
                    foreach (var button in buttonOption.AvailableOptions)
                    {
                        var checkbox = new CheckBox
                        {
                            Content = button,
                            IsChecked = currentSelection.Contains(button),
                            Margin = new Thickness(0, 0, 15, 5)
                        };
                        
                        checkbox.Checked += ButtonSelection_Changed;
                        checkbox.Unchecked += ButtonSelection_Changed;
                        
                        ButtonSelectionPanel.Children.Add(checkbox);
                    }
                }
            }
        }

        private void ButtonSelection_Changed(object sender, RoutedEventArgs e)
        {
            if (_goxlrPlugin == null) return;

            // Collect selected buttons
            var selectedButtons = ButtonSelectionPanel.Children
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content.ToString())
                .Where(s => s != null)
                .Cast<string>()
                .ToList();

            // Apply configuration
            var config = new Dictionary<string, object>
            {
                { "selected_buttons", selectedButtons }
            };
            
            _goxlrPlugin.ApplyConfiguration(config);
            
            // Reinitialize devices
            _ = Task.Run(async () => await _lightingService.InitializeAsync());
            
            Logger.Info("Lighting Tab", $"Button selection updated: {string.Join(", ", selectedButtons)}");
        }

        private void LoadSettings()
        {
            // For now, always enabled (will add settings later)
            EnableLightingCheckBox.IsChecked = true;
        }

        private void UpdateDevicesList()
        {
            DevicesPanel.Children.Clear();

            // Get registered devices via reflection (since LightingService doesn't expose them)
            // For now, just show that GoXLR is registered
            var deviceInfo = new StackPanel { Orientation = Orientation.Horizontal };
            
            var statusIcon = new TextBlock
            {
                Text = "?",
                Foreground = Brushes.Green,
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var deviceName = new TextBlock
            {
                Text = "GoXLR (Fader Button LEDs)",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            deviceInfo.Children.Add(statusIcon);
            deviceInfo.Children.Add(deviceName);
            
            DevicesPanel.Children.Add(deviceInfo);
        }

        private void OnFlagChanged(object? sender, FlagChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentFlagText.Text = e.NewFlag.ToString();
                CurrentFlagText.Foreground = GetFlagColor(e.NewFlag);
            });
        }

        private void EnableLighting_Changed(object sender, RoutedEventArgs e)
        {
            // Future: Save to settings and enable/disable lighting
            var isEnabled = EnableLightingCheckBox.IsChecked ?? false;
            
            if (isEnabled)
            {
                StatusText.Text = "Active";
                StatusText.Foreground = Brushes.Green;
            }
            else
            {
                StatusText.Text = "Disabled";
                StatusText.Foreground = Brushes.Gray;
                
                // Clear any active flags
                _ = Task.Run(async () => await _lightingService.UpdateForFlagAsync(FlagStatus.None));
            }
        }

        private async void TestFlag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            if (button.Tag is not string flagString) return;

            try
            {
                // Parse the flag status
                if (!Enum.TryParse<FlagStatus>(flagString, out var flag))
                {
                    StatusText.Text = $"Invalid flag: {flagString}";
                    StatusText.Foreground = Brushes.Red;
                    return;
                }

                // Update lighting
                await _lightingService.UpdateForFlagAsync(flag);
                
                // Update UI
                CurrentFlagText.Text = flag.ToString();
                CurrentFlagText.Foreground = GetFlagColor(flag);
                
                StatusText.Text = $"Testing: {flag}";
                StatusText.Foreground = Brushes.Orange;
                
                // Log it
                Logger.Info("Lighting Tab", $"Manual flag test: {flag}");
                
                // After 5 seconds, show success (but don't clear the flag)
                await Task.Delay(5000);
                if (StatusText.Text.StartsWith("Testing:"))
                {
                    StatusText.Text = "Active";
                    StatusText.Foreground = Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error testing flag";
                StatusText.Foreground = Brushes.Red;
                Logger.Error("Lighting Tab", "Error testing flag", ex);
                
                MessageBox.Show($"Error testing flag: {ex.Message}", 
                    "Test Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Brush GetFlagColor(FlagStatus flag)
        {
            return flag switch
            {
                FlagStatus.Green => Brushes.LimeGreen,
                FlagStatus.Yellow or FlagStatus.YellowWaving => Brushes.Yellow,
                FlagStatus.Blue => Brushes.DeepSkyBlue,
                FlagStatus.White => Brushes.White,
                FlagStatus.Checkered => Brushes.WhiteSmoke,
                FlagStatus.Red => Brushes.Red,
                FlagStatus.Black => Brushes.Black,
                FlagStatus.Debris => Brushes.Orange,
                FlagStatus.OneLapToGreen => Brushes.YellowGreen,
                _ => Brushes.Gray
            };
        }
    }
}
