using System;
using System.Threading.Tasks;
using System.Windows;
using SimControlCentre.Models;

namespace SimControlCentre.Views;

/// <summary>
/// Test window for GoXLR API functionality - DEPRECATED
/// GoXLR functionality moved to plugins
/// </summary>
public partial class GoXLRTestWindow : Window
{
    private readonly AppSettings _settings;

    public GoXLRTestWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "GoXLR functionality has been moved to plugins.\nThis test window is deprecated.\n";
        await Task.CompletedTask;
    }

    private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
    {
        await Task.CompletedTask;
    }

    private async void TestRawAPI_Click(object sender, RoutedEventArgs e)
    {
        await Task.CompletedTask;
    }

    private async void VolumeDown_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for volume control.\n";
        await Task.CompletedTask;
    }

    private async void VolumeUp_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for volume control.\n";
        await Task.CompletedTask;
    }

    private async void LoadProfile_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for profile loading.\n";
        await Task.CompletedTask;
    }

    private async Task RefreshStatus()
    {
        await Task.CompletedTask;
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab instead.\n";
        await Task.CompletedTask;
    }

    private async void VolumeUpButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for volume control.\n";
        await Task.CompletedTask;
    }

    private async void VolumeDownButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for volume control.\n";
        await Task.CompletedTask;
    }

    private async void LoadProfileButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Use Device Control tab for profile loading.\n";
        await Task.CompletedTask;
    }
}

