using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ClockbusterApps.Services;

namespace ClockbusterApps.Views
{
    public partial class TimeclockViewerWindow : Window
    {
        private readonly string _logFilePath;
        private readonly TimingService _timingService;
        private readonly AppSettings _settings;
        private DispatcherTimer _refreshTimer;

        public TimeclockViewerWindow(TimingService timingService, AppSettings settings)
        {
            InitializeComponent();
            _timingService = timingService;
            _settings = settings;

            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "timeclock.log"
            );

            _refreshTimer = new DispatcherTimer();
            /* 2 second refresh */
            _refreshTimer.Interval = TimeSpan.FromSeconds(60);

            /* only refresh if the application is currently monitoring */
            _refreshTimer.Tick += (s, e) =>
            {
                if (_timingService.GetActiveSessions().Any())
                {
                    LoadData();
                }
            };
            _refreshTimer.Start();

            LoadData();
        }

        private void CtxAddToIgnore_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridLogs.SelectedItem is SessionViewModel selectedSession)
            {
                string appName = selectedSession.ApplicationName;

                if (!_settings.IgnoredProcesses.Contains(appName))
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to ignore '{appName}'?\n\nThis will stop tracking it immediately.",
                        "Confirm Ignore",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _settings.IgnoredProcesses.Add(appName);
                        _settings.Save();
                        _timingService.UpdateIgnoredProcesses(_settings.IgnoredProcesses);

                        StatusTextViewer.Text = $"'{appName}' added to ignore list.";
                        LoadData();
                    }
                }
            }
        }

        private void LoadData()
        {
            try
            {
                var sessions = new List<SessionViewModel>();

                // Get active sessions
                var activeSessions = _timingService.GetActiveSessions();
                foreach (var s in activeSessions)
                {
                    sessions.Add(new SessionViewModel
                    {
                        Id = s.Id,
                        ApplicationName = s.ApplicationName,
                        StartTimeStr = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndTimeStr = "ACTIVE",
                        DurationMinutes = ((int)s.DurationMinutes).ToString()
                    });
                }

                // Load from file
                if (File.Exists(_logFilePath))
                {
                    var lines = File.ReadAllLines(_logFilePath);
                    foreach (var line in lines.Reverse())
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            sessions.Add(new SessionViewModel
                            {
                                Id = parts[0],
                                ApplicationName = parts[1],
                                StartTimeStr = parts[2],
                                EndTimeStr = parts[3],
                                DurationMinutes = parts[4]
                            });
                        }
                    }
                }

                DataGridLogs.ItemsSource = sessions;
                StatusTextViewer.Text = $"Loaded {sessions.Count} sessions.";
            }
            catch (Exception ex)
            {
                StatusTextViewer.Text = "Error loading data: " + ex.Message;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer.Stop();
            base.OnClosed(e);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Delete all history?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (File.Exists(_logFilePath)) File.Delete(_logFilePath);
                LoadData();
            }
        }

        public class SessionViewModel
        {
            public string Id { get; set; }
            public string ApplicationName { get; set; }
            public string StartTimeStr { get; set; }
            public string EndTimeStr { get; set; }
            public string DurationMinutes { get; set; }
        }
    }
}