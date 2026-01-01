using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ClockbusterApps.Views
{
    public partial class TimeclockViewerWindow : Window
    {
        private readonly string _logFilePath;

        public TimeclockViewerWindow()
        {
            InitializeComponent();
            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ClockbusterApps",
                "timeclock.log"
            );
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    StatusTextViewer.Text = "No session logs found.";
                    DataGridLogs.ItemsSource = null;
                    return;
                }

                var sessions = new List<SessionViewModel>();
                var lines = File.ReadAllLines(_logFilePath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Requirement 5: Format parsing: SessionID|AppName|Start|End|Duration
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

                // Requirement 4: Sort by most recent first
                sessions.Reverse();

                DataGridLogs.ItemsSource = sessions;
                StatusTextViewer.Text = string.Format("Loaded {0} completed sessions.", sessions.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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