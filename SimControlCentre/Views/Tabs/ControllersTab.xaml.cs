using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class ControllersTab : UserControl
    {
        private readonly DirectInputService? _directInputService;

        public ControllersTab(DirectInputService directInputService)
        {
            InitializeComponent();
            
            _directInputService = directInputService;
            
            // Subscribe to button events for indicator
            if (_directInputService != null)
            {
                _directInputService.ButtonPressed += OnButtonPressed;
            }
            
            // Initial refresh
            RefreshControllerList();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Flash indicator green
                ButtonIndicator.Fill = Brushes.LimeGreen;
                
                // Fade back to gray after 200ms
                var animation = new ColorAnimation
                {
                    From = Colors.LimeGreen,
                    To = Colors.Gray,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                
                var brush = new SolidColorBrush();
                brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
                ButtonIndicator.Fill = brush;
            });
        }

        private void RefreshControllerList()
        {
            if (_directInputService == null)
            {
                ControllerStatusText.Text = "Controller service not available";
                ControllerStatusText.Foreground = Brushes.Red;
                return;
            }

            var controllers = _directInputService.GetConnectedDevices();
            
            if (controllers.Count == 0)
            {
                ControllerStatusText.Text = "No controllers detected";
                ControllerStatusText.Foreground = Brushes.Gray;
                ControllersListBox.ItemsSource = null;
            }
            else
            {
                ControllerStatusText.Text = $"Found {controllers.Count} controller(s):";
                ControllerStatusText.Foreground = Brushes.Green;
                ControllersListBox.ItemsSource = controllers;
            }
        }

        private void RefreshControllers_Click(object sender, RoutedEventArgs e)
        {
            RefreshControllerList();
        }
    }
}
