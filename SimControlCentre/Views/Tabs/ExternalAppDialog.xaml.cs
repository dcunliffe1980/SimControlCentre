using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SimControlCentre.Models;

namespace SimControlCentre.Views.Tabs
{
    public partial class ExternalAppDialog : Window
    {
        private readonly ExternalApp? _existingApp;
        private readonly AppSettings _settings;
        private readonly ExternalAppType _appType;
        
        public ExternalApp? ResultApp { get; private set; }

        public ExternalAppDialog(ExternalApp? existingApp, AppSettings settings, ExternalAppType appType)
        {
            InitializeComponent();
            
            _existingApp = existingApp;
            _settings = settings;
            _appType = appType;

            // Set title based on type
            DialogTitle.Text = appType == ExternalAppType.StartWithRacing 
                ? (existingApp == null ? "Add App to Start" : "Edit App")
                : "App Configuration";
            
            // Show/hide sections based on type
            if (appType == ExternalAppType.StartWithRacing)
            {
                // Show start-specific options, hide stop options
                StartWithRacingSection.Visibility = Visibility.Visible;
                StopForRacingSection.Visibility = Visibility.Collapsed;
                DelaysSection.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide start-specific options, show stop options
                StartWithRacingSection.Visibility = Visibility.Collapsed;
                StopForRacingSection.Visibility = Visibility.Visible;
                DelaysSection.Visibility = Visibility.Collapsed;
            }

            // Load existing values if editing
            if (existingApp != null)
            {
                ExecutablePathBox.Text = existingApp.ExecutablePath;
                NameBox.Text = existingApp.Name;
                ArgumentsBox.Text = existingApp.Arguments;
                
                if (appType == ExternalAppType.StartWithRacing)
                {
                    StartWithiRacingCheckBox.IsChecked = existingApp.StartWithiRacing;
                    StopWithiRacingCheckBox.IsChecked = existingApp.StopWithiRacing;
                    StartHiddenCheckBox.IsChecked = existingApp.StartHidden;
                    DelayStartBox.Text = existingApp.DelayStartSeconds.ToString();
                    DelayStopBox.Text = existingApp.DelayStopSeconds.ToString();
                }
                else
                {
                    RestartWhenIRacingStopsCheckBox.IsChecked = existingApp.RestartWhenIRacingStops;
                    RestartHiddenCheckBox.IsChecked = existingApp.RestartHidden;
                    DelayRestartBox.Text = existingApp.DelayRestartSeconds.ToString();
                }
            }
        }

        private void BrowseExecutable_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Executable"
            };

            if (dialog.ShowDialog() == true)
            {
                ExecutablePathBox.Text = dialog.FileName;
                
                // Auto-populate name from filename if empty
                if (string.IsNullOrWhiteSpace(NameBox.Text))
                {
                    NameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(ExecutablePathBox.Text))
            {
                MessageBox.Show("Please select an executable", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(ExecutablePathBox.Text))
            {
                MessageBox.Show("Executable file not found", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Please enter a name", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse delays
            if (!int.TryParse(DelayStartBox.Text, out var delayStart) || delayStart < 0)
            {
                MessageBox.Show("Delay Start must be a positive number", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelayStopBox.Text, out var delayStop) || delayStop < 0)
            {
                MessageBox.Show("Delay Stop must be a positive number", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Parse restart delay for StopForRacing apps
            int delayRestart = 2;
            if (_appType == ExternalAppType.StopForRacing)
            {
                if (!int.TryParse(DelayRestartBox.Text, out delayRestart) || delayRestart < 0)
                {
                    MessageBox.Show("Delay Restart must be a positive number", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Create result
            ResultApp = new ExternalApp
            {
                Name = NameBox.Text.Trim(),
                ExecutablePath = ExecutablePathBox.Text.Trim(),
                Arguments = ArgumentsBox.Text.Trim(),
                AppType = _appType
            };
            
            if (_appType == ExternalAppType.StartWithRacing)
            {
                ResultApp.StartWithiRacing = StartWithiRacingCheckBox.IsChecked == true;
                ResultApp.StopWithiRacing = StopWithiRacingCheckBox.IsChecked == true;
                ResultApp.StartHidden = StartHiddenCheckBox.IsChecked == true;
                ResultApp.DelayStartSeconds = delayStart;
                ResultApp.DelayStopSeconds = delayStop;
            }
            else
            {
                ResultApp.RestartWhenIRacingStops = RestartWhenIRacingStopsCheckBox.IsChecked == true;
                ResultApp.RestartHidden = RestartHiddenCheckBox.IsChecked == true;
                ResultApp.DelayRestartSeconds = delayRestart;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
