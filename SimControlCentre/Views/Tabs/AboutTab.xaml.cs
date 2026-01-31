using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using SimControlCentre.Services;

namespace SimControlCentre.Views.Tabs
{
    public partial class AboutTab : UserControl
    {
        private readonly ConfigurationService _configService;

        public AboutTab(ConfigurationService configService)
        {
            InitializeComponent();
            
            _configService = configService;
            
            // Display config path
            ConfigPathText.Text = _configService.GetConfigFilePath();
        }

        private void OpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            var configPath = _configService.GetConfigFilePath();
            var folderPath = System.IO.Path.GetDirectoryName(configPath);
            
            if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
            {
                Process.Start("explorer.exe", folderPath);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
