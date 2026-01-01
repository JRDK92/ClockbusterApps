using ClockbusterApps.Services;
using ClockbusterApps.Views;
using ClockbusterApps.Services;
using ClockbusterApps.Views;
using System.Windows;

namespace ClockbusterApps;

public partial class MainWindow : Window
{
    private readonly TimingService _timingService;
    private bool _isLogging;

    public MainWindow()
    {
        InitializeComponent();
        _timingService = new TimingService();
    }

    private void BtnToggleLogging_Click(object sender, RoutedEventArgs e)
    {
        if (!_isLogging)
        {
            _timingService.Start();
            BtnToggleLogging.Content = "Stop Logging";
            StatusText.Text = "Status: Logging Active";
            _isLogging = true;
        }
        else
        {
            _timingService.Stop();
            BtnToggleLogging.Content = "Start Logging";
            StatusText.Text = "Status: Idle";
            _isLogging = false;
        }
    }

    private void ViewData_Click(object sender, RoutedEventArgs e)
    {
        var viewer = new TimeclockViewerWindow();
        viewer.Owner = this;
        viewer.Show();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}