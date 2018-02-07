using System;
using System.Windows;

namespace LogOut {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();
            
            // Import initial settings
            TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
            TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();

            TextBox_HealthLimit.IsEnabled = Settings.trackHealth;
            TextBox_PollRate.IsEnabled = Settings.trackHealth;
            CheckBox_AutoAction.IsChecked = Settings.trackHealth;
            CheckBox_Logout.IsEnabled = Settings.trackHealth;
        }

        private void Button_Apply_Click(object sender, RoutedEventArgs e) {
            int.TryParse(TextBox_PollRate.Text, out int rate);
            if (rate != Settings.healthPollRateMS) {
                if (rate > Settings.healthPollRate_Min && rate <= Settings.healthPollRate_Max) {
                    Console.WriteLine("[Settings][rate] Changed value '{0}' to '{1}' for pollRate", Settings.healthPollRateMS, rate);
                    Settings.healthPollRateMS = rate;
                } else {
                    TextBox_PollRate.Text = Settings.healthPollRateMS.ToString();
                    Console.WriteLine("[Settings][rate] Error applying value '{0}'");
                }
            }

            int.TryParse(TextBox_HealthLimit.Text, out int limit);
            if (limit != Settings.healthLimitPercent) {
                if (limit > Settings.healthLimit_Min && limit <= Settings.healthLimit_Max) {
                    Console.WriteLine("[Settings][limit] Changed value '{0}' to '{1}'", Settings.healthLimitPercent, limit);
                    Settings.healthLimitPercent = limit;
                } else {
                    TextBox_HealthLimit.Text = Settings.healthLimitPercent.ToString();
                    Console.WriteLine("[Settings][limit] Error applying value '{0}'");
                }
            } 

            Hide();
        }

        private void CheckBox_Minimized_Checked(object sender, RoutedEventArgs e) {
            Settings.workMinimized = (bool)CheckBox_Minimized.IsChecked;
        }

        private void CheckBox_AutoAction_Click(object sender, RoutedEventArgs e) {
            Settings.trackHealth = (bool)CheckBox_AutoAction.IsChecked;

            // Enable/disable settings
            TextBox_HealthLimit.IsEnabled = Settings.trackHealth;
            TextBox_PollRate.IsEnabled = Settings.trackHealth;
            CheckBox_Logout.IsEnabled = Settings.trackHealth;
        }

        /// <summary>
        /// Instead of closing the window, hide it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Hide();
        }

        private void CheckBox_Logout_Click(object sender, RoutedEventArgs e) {
            Settings.doLogout = (bool)CheckBox_Logout.IsChecked;
        }
    }
}
