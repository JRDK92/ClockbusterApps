using ClockbusterApps.Services;
using ClockbusterApps.Views;
using System;
using System.Windows;
using System.Windows.Media;

namespace ClockbusterApps
{
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

                BtnToggleLogging.Content = "Stop Monitoring";
                BtnToggleLogging.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                StatusText.Text = "Monitoring";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                _isLogging = true;
            }
            else
            {
                _timingService.Stop();

                BtnToggleLogging.Content = "Start Monitoring";
                BtnToggleLogging.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                StatusText.Text = "Idle";
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D"));
                _isLogging = false;
            }
        }

        private void ViewData_Click(object sender, RoutedEventArgs e)
        {
            var viewer = new TimeclockViewerWindow(_timingService);
            viewer.Owner = this;
            viewer.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (_isLogging)
            {
                _timingService.Stop();
            }
            Application.Current.Shutdown();
        }
    }
}