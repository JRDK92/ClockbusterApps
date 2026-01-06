using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ClockbusterApps.Views
{
    public partial class OptionsWindow : Window
    {
        public bool TrackExistingApplications { get; private set; }

        // Use ObservableCollection for UI binding
        public ObservableCollection<string> IgnoredProcesses { get; private set; }

        public OptionsWindow(bool currentSetting, List<string> currentIgnoredList)
        {
            InitializeComponent();

            // Setup Checkbox
            ChkTrackExisting.IsChecked = currentSetting;
            TrackExistingApplications = currentSetting;

            // Setup Ignore List (Clone the list so we don't modify reference directly until OK)
            IgnoredProcesses = new ObservableCollection<string>(currentIgnoredList ?? new List<string>());
            LstIgnoredProcesses.ItemsSource = IgnoredProcesses;
        }

        private void BtnAddProcess_Click(object sender, RoutedEventArgs e)
        {
            var processName = TxtProcessName.Text.Trim();
            if (!string.IsNullOrWhiteSpace(processName) && !IgnoredProcesses.Contains(processName))
            {
                IgnoredProcesses.Add(processName);
                TxtProcessName.Clear();
            }
        }

        private void BtnRemoveProcess_Click(object sender, RoutedEventArgs e)
        {
            if (LstIgnoredProcesses.SelectedItem is string selectedProcess)
            {
                IgnoredProcesses.Remove(selectedProcess);
            }
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            TrackExistingApplications = ChkTrackExisting.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}