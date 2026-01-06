using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using ClockbusterApps.Services;

namespace ClockbusterApps.Views
{
    public partial class TimeclockViewerWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly TimingService _timingService;
        private readonly AppSettings _settings;
        private DispatcherTimer _refreshTimer;

        public TimeclockViewerWindow(TimingService timingService, AppSettings settings, DatabaseService databaseService)
        {
            InitializeComponent();
            _timingService = timingService;
            _settings = settings;
            _databaseService = databaseService;

            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(60);

            _refreshTimer.Tick += (s, e) =>
            {
                if (_timingService.GetActiveSessions().Any())
                {
                    LoadData();
                }
            };
            _refreshTimer.Start();

            LoadData();
            UpdateDeleteButtonText();
        }

        private void DataGridLogs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateDeleteButtonText();
        }

        private void UpdateDeleteButtonText()
        {
            int selectedCount = DataGridLogs.SelectedItems.Count;

            if (selectedCount == 0)
            {
                BtnDelete.Content = "Delete All";
            }
            else if (selectedCount == 1)
            {
                BtnDelete.Content = "Delete Selected (1)";
            }
            else
            {
                BtnDelete.Content = $"Delete Selected ({selectedCount})";
            }
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
                else
                {
                    MessageBox.Show(
                        $"'{appName}' is already in the ignore list.",
                        "Already Ignored",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }

        private void LoadData()
        {
            try
            {
                var sessions = new List<SessionViewModel>();

                // Get active sessions from TimingService
                var activeSessions = _timingService.GetActiveSessions();
                foreach (var s in activeSessions)
                {
                    sessions.Add(new SessionViewModel
                    {
                        Id = s.Id,
                        ApplicationName = s.ApplicationName,
                        StartTimeStr = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndTimeStr = "ACTIVE",
                        DurationMinutes = ((int)s.DurationMinutes).ToString(),
                        IsActive = true
                    });
                }

                // Load completed sessions from database
                var dbSessions = _databaseService.GetAllSessions();
                foreach (var s in dbSessions.Where(s => !s.IsActive))
                {
                    sessions.Add(new SessionViewModel
                    {
                        Id = s.Id,
                        ApplicationName = s.ApplicationName,
                        StartTimeStr = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndTimeStr = s.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A",
                        DurationMinutes = ((int)s.DurationMinutes).ToString(),
                        IsActive = false
                    });
                }

                DataGridLogs.ItemsSource = sessions;
                StatusTextViewer.Text = $"Loaded {sessions.Count} session(s).";
            }
            catch (Exception ex)
            {
                StatusTextViewer.Text = "Error loading data: " + ex.Message;
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _refreshTimer?.Stop();
            base.OnClosed(e);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            UpdateDeleteButtonText();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var totalCount = DataGridLogs.Items.Count;

            if (totalCount == 0)
            {
                MessageBox.Show("No sessions to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete all {totalCount} session(s)?\n\nThis action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _databaseService.DeleteAllSessions();
                    LoadData();
                    StatusTextViewer.Text = $"Deleted all {totalCount} session(s).";
                }
                catch (Exception ex)
                {
                    StatusTextViewer.Text = "Error deleting sessions: " + ex.Message;
                    MessageBox.Show($"Error deleting sessions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            int selectedCount = DataGridLogs.SelectedItems.Count;

            // If nothing selected, delete all
            if (selectedCount == 0)
            {
                ClearAll_Click(sender, e);
                return;
            }

            // Delete selected items
            var sessionsToDelete = DataGridLogs.SelectedItems.Cast<SessionViewModel>().ToList();

            var result = MessageBox.Show(
                $"Are you sure you want to delete {selectedCount} selected session(s)?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Check if any active sessions are selected
                    var activeSessions = sessionsToDelete.Where(s => s.IsActive).ToList();
                    if (activeSessions.Any())
                    {
                        MessageBox.Show(
                            "Cannot delete active sessions. Please wait until the session ends or stop monitoring first.",
                            "Cannot Delete Active Sessions",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    // Delete from database
                    var idsToDelete = sessionsToDelete.Select(s => s.Id).ToList();
                    _databaseService.DeleteSessions(idsToDelete);

                    LoadData();
                    StatusTextViewer.Text = $"Deleted {selectedCount} session(s).";
                }
                catch (Exception ex)
                {
                    StatusTextViewer.Text = "Error deleting sessions: " + ex.Message;
                    MessageBox.Show($"Error deleting sessions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridLogs.SelectedItems.Count > 0)
            {
                Delete_Click(sender, e);
            }
            else
            {
                MessageBox.Show("No sessions selected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public class SessionViewModel
        {
            public string Id { get; set; }
            public string ApplicationName { get; set; }
            public string StartTimeStr { get; set; }
            public string EndTimeStr { get; set; }
            public string DurationMinutes { get; set; }
            public bool IsActive { get; set; }
        }
    }
}