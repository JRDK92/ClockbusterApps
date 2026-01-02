using ClockbusterApps.Services;
using ClockbusterApps.Views;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClockbusterApps
{
    public partial class MainWindow : Window
    {
        private readonly TimingService _timingService;
        private bool _isLogging;
        private DispatcherTimer _updateTimer;
        private TimeclockViewerWindow _viewerWindow;
        private AppSettings _settings;

        public MainWindow()
        {
            InitializeComponent();
            _timingService = new TimingService();
            _settings = AppSettings.Load();

            // Timer to update current activity display
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(2);
            _updateTimer.Tick += UpdateCurrentActivity;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            _timingService.Start(_settings.TrackExistingApplications);

            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;

            StatusText.Text = "Monitoring";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10"));
            _isLogging = true;

            _updateTimer.Start();
            UpdateCurrentActivity(null, null);
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _timingService.Stop();

            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;

            StatusText.Text = "Idle";
            StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#606060"));
            _isLogging = false;

            _updateTimer.Stop();
            CurrentActivityText.Text = "No applications being monitored.";
        }

        private void UpdateCurrentActivity(object sender, EventArgs e)
        {
            var currentSession = _timingService.GetCurrentSession();
            if (currentSession != null)
            {
                CurrentActivityText.Text = string.Format(
                    "Currently tracking: {0}\nDuration: {1} minutes",
                    currentSession.ApplicationName,
                    ((int)currentSession.DurationMinutes).ToString());
            }
            else
            {
                CurrentActivityText.Text = "Monitoring active - waiting for application focus...";
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var optionsWindow = new OptionsWindow(_settings.TrackExistingApplications);
            optionsWindow.Owner = this;

            if (optionsWindow.ShowDialog() == true)
            {
                _settings.TrackExistingApplications = optionsWindow.TrackExistingApplications;
                _settings.Save();
            }
        }

        private void ViewData_Click(object sender, RoutedEventArgs e)
        {
            if (_viewerWindow != null && _viewerWindow.IsLoaded)
            {
                _viewerWindow.Activate();
                _viewerWindow.Focus();
                return;
            }

            _viewerWindow = new TimeclockViewerWindow(_timingService);
            _viewerWindow.Owner = this;
            _viewerWindow.Closed += (s, args) => _viewerWindow = null;
            _viewerWindow.Show();
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