using System.Windows;

namespace ClockbusterApps.Views
{
    public partial class OptionsWindow : Window
    {
        public bool TrackExistingApplications { get; private set; }

        public OptionsWindow(bool currentSetting)
        {
            InitializeComponent();
            ChkTrackExisting.IsChecked = currentSetting;
            TrackExistingApplications = currentSetting;
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