using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading; // ADD THIS
using ClockbusterApps.Services; // ADD THIS

namespace ClockbusterApps.Views
{
    public partial class TimeclockViewerWindow : Window
    {
        private readonly string _logFilePath;
        private readonly TimingService _timingService; // ADD THIS
        private DispatcherTimer _refreshTimer; // ADD THIS

        // CHANGE THE CONSTRUCTOR to accept TimingService
        public TimeclockViewerWindow(TimingService timingService)
        {
            InitializeComponent();
            _timingService = timingService; // ADD THIS
            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "timeclock.log"
            );

            // ADD THIS: Auto-refresh every 2 seconds to show live updates
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += (s, e) => LoadData();
            _refreshTimer.Start();

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var sessions = new List<SessionViewModel>();

                // ADD THIS: Get active sessions first
                var activeSessions = _timingService.GetActiveSessions();
                foreach (var session in activeSessions)
                {
                    sessions.Add(new SessionViewModel
                    {
                        Id = session.Id,
                        ApplicationName = session.ApplicationName,
                        StartTimeStr = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndTimeStr = "In Progress", // ADD THIS to show it's active
                        DurationMinutes = session.DurationMinutes.ToString("F0")
                    });
                }

                // EXISTING CODE: Get completed sessions from log
                if (File.Exists(_logFilePath))
                {
                    var lines = File.ReadAllLines(_logFilePath);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

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

                // Sort by start time - most recent first (active sessions will be on top)
                sessions = sessions.OrderByDescending(s => s.StartTimeStr).ToList();

                DataGridLogs.ItemsSource = sessions;

                // UPDATE STATUS MESSAGE
                var activeCount = activeSessions.Count();
                var completedCount = sessions.Count - activeCount;
                StatusTextViewer.Text = string.Format("{0} active, {1} completed sessions.",
                    activeCount, completedCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ADD THIS: Stop the timer when window closes
        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnClosed(e);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete all session history?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists(_logFilePath))
                    {
                        File.Delete(_logFilePath);
                        StatusTextViewer.Text = "All data cleared successfully.";
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting log: " + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // View Model for the DataGrid
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